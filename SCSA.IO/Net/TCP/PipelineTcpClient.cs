using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using PipeOptions = System.IO.Pipelines.PipeOptions;
using SCSA.Utils;

namespace SCSA.IO.Net.TCP;

public class PipelineTcpClient<T> where T : class, IPipelineDataPackage<T>, new()
{
    private readonly T _parserPrototype; // 用来调用 TryParse 解析
    private readonly Pipe _pipe;
    private readonly Socket _socket;

    private Channel<T> _processingChannel;
    private bool _running;

    public PipelineTcpClient(Socket socket)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _socket.NoDelay = false;
        _socket.ReceiveBufferSize = 1024 * 1024;
        _socket.SendBufferSize = 1024 * 1024;

        RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;

        // 1MB / 512KB 门限避免管道高峰写阻塞
        _pipe = new Pipe(new PipeOptions(
            readerScheduler: PipeScheduler.ThreadPool,
            writerScheduler: PipeScheduler.ThreadPool,
            pauseWriterThreshold: 1 << 20, // 1 MB
            resumeWriterThreshold: 1 << 19 // 512 KB
        ));

        _parserPrototype = new T();
    }

    public IPEndPoint RemoteEndPoint { set; get; }


    /// <summary>
    ///     当收到一个完整的 T 才会触发该事件
    /// </summary>
    public event EventHandler<T> DataReceived;

    /// <summary>
    ///     当连接断开时触发
    /// </summary>
    public event EventHandler Disconnected;

    /// <summary>
    ///     启动接收和拆包循环
    /// </summary>
    public void Start()
    {
        _running = true;

        _processingChannel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _ = Task.Run(async () =>
        {
            var reader = _processingChannel.Reader;
            while (await reader.WaitToReadAsync())
            while (reader.TryRead(out var packet))
                DataReceived?.Invoke(this, packet);
        });

        // 1) 开一个 Task，把 socket ReceiveAsync 写入 PipeWriter
        _ = Task.Run(async () =>
        {
            try
            {
                while (_running)
                {
                    // 向管道申请一个最大 64KB 的可写内存
                    var memory = _pipe.Writer.GetMemory(64 * 1024);
                    var bytesRead = 0;
                    try
                    {
                        bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None);
                    }
                    // socket 异常或断开
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        Log.Error("PipelineTcpClient socket connection reset", e);
                        break;
                    }
                    catch (Exception e)
                    {
                        Log.Error("PipelineTcpClient socket receive failed", e);
                        break;
                    }

                    // 客户端已断开
                    if (bytesRead == 0)
                        break;

                    // 通知管道本次写入了 bytesRead 字节
                    _pipe.Writer.Advance(bytesRead);

                    // 刷新到管道
                    var result = await _pipe.Writer.FlushAsync();

                    if (result.IsCompleted || result.IsCanceled)
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error("PipelineTcpClient pipe writing failed", e);
            }
            finally
            {
                // 通知管道写入端完毕
                await _pipe.Writer.CompleteAsync();
            }
        });

        // 2) 自己在当前线程（或再开一个 Task）不停地 ReadAsync + 拆包
        _ = Task.Run(async () =>
        {
            while (_running)
            {
                var readResult = await _pipe.Reader.ReadAsync();
                var buffer = readResult.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End; // 默认都从 buffer.Start 开始

                try
                {
                    // 不断拆帧
                    while (_parserPrototype.TryParse(buffer, out var packet, out var frameEnd))
                    {
                        await _processingChannel.Writer.WriteAsync(packet);
                        // 标记已经消费到 frameEnd
                        consumed = frameEnd;
                        buffer = buffer.Slice(frameEnd);
                    }

                    // 拆不出完整帧时，把"检查到的最前"标记给 examined
                    examined = buffer.Start;
                }
                catch (Exception e)
                {
                    Log.Error("PipelineTcpClient parse packet failed", e);
                    break;
                }

                // 通知管道：从 buffer.Start 到 consumed 的字节已经消费掉；buffer.Start 到 examined 的字节已经"检查过"
                _pipe.Reader.AdvanceTo(consumed, examined);

                if (readResult.IsCompleted)
                    break;
            }

            // 读取循环结束，关闭
            try
            {
                await _pipe.Reader.CompleteAsync();
            }
            catch (Exception e)
            {
                Log.Error("PipelineTcpClient pipe reader complete failed", e);
            }

            _running = false;
            try
            {
                _socket.Close();
            }
            catch (Exception e)
            {
                Log.Error("PipelineTcpClient socket close failed", e);
            }

            _processingChannel.Writer.Complete();
            Disconnected?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>
    ///     发送一个 T 包到远端，直接拿到包的字节数组往 socket.SendAsync
    /// </summary>
    public async Task<bool> SendAsync(T packet)
    {
        if (!_running || packet == null)
            return false;

        // 假设 T 自己能提供 "ToArray()" 或 "GetBytes()"，代表一个完整帧
        // 这里新版约定：你在实现 IPipelineDataPackage<T> 的类里，必须提供一个 ToArray() 方法，
        // 它返回一个"可直接 socket.Send 的字节数组"，包含 Magic/Ver/Cmd/CmdId/Len/Body/CRC
        if (packet is IPacketWritable w)
        {
            var data = w.GetBytes();
            try
            {
                var sent = await _socket.SendAsync(data, SocketFlags.None);
                return sent == data.Length;
            }
            catch (Exception e)
            {
                Log.Error("PipelineTcpClient send message failed", e);
                return false;
            }
        }

        throw new InvalidOperationException("Your data package must implement IPacketWritable to enable SendAsync.");
    }

    /// <summary>
    ///     停止并关闭连接
    /// </summary>
    public void Close()
    {
        _running = false;
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e)
        {
            Log.Error("PipelineTcpClient socket shutdown failed", e);
        }

        try
        {
            _socket.Close();
        }
        catch (Exception e)
        {
            Log.Error("PipelineTcpClient socket close failed", e);
        }
    }
}