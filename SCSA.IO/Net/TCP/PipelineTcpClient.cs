using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipelines;
using PipeOptions = System.IO.Pipelines.PipeOptions;
using System.Diagnostics;
using System.Threading.Channels;

namespace SCSA.IO.Net.TCP
{
    public class PipelineTcpClient<T> where T : class, IPipelineDataPackage<T>, new()
    {
        private readonly Socket _socket;
        private readonly Pipe _pipe;
        private readonly T _parserPrototype; // 用来调用 TryParse 解析
        private bool _running;


        /// <summary>
        /// 当收到一个完整的 T 才会触发该事件
        /// </summary>
        public event EventHandler<T> DataReceived;

        /// <summary>
        /// 当连接断开时触发
        /// </summary>
        public event EventHandler Disconnected;

        public IPEndPoint RemoteEndPoint { set; get; }

        private readonly Channel<T> _processingChannel = Channel.CreateUnbounded<T>();
        public PipelineTcpClient(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _socket.NoDelay = false;
            _socket.ReceiveBufferSize = 512 * 1024; // 512 KB
            _socket.SendBufferSize = 512 * 1024;    // 512 KB

            RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;

            // 1MB / 512KB 门限避免管道高峰写阻塞
            _pipe = new Pipe(new PipeOptions(
                readerScheduler: PipeScheduler.ThreadPool,
                writerScheduler: PipeScheduler.ThreadPool,
                pauseWriterThreshold: 1 << 20,   // 1 MB
                resumeWriterThreshold: 512 << 10 // 512 KB
            ));

            _parserPrototype = new T();
        }

        /// <summary>
        /// 启动接收和拆包循环
        /// </summary>
        public void Start()
        {
            _running = true;


            _ = Task.Run(async () =>
            {
                var reader = _processingChannel.Reader;
                while (await reader.WaitToReadAsync())
                {
                    while (reader.TryRead(out var packet))
                    {
                        DataReceived?.Invoke(this, packet);
                    }
                }
            });

            // 1) 开一个 Task，把 socket ReceiveAsync 写入 PipeWriter
            _ = Task.Run(async () =>
            {
                try
                {
                    while (_running)
                    {
                        // 向管道申请一个最大 64KB 的可写内存
                        Memory<byte> memory = _pipe.Writer.GetMemory(64 * 1024);
                        int bytesRead = 0;
                        try
                        {
                            bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None);
                        }
                        catch
                        {
                            // socket 异常或断开
                            break;
                        }

                        if (bytesRead == 0)
                            break; // 客户端已断开

                        // 通知管道本次写入了 bytesRead 字节
                        _pipe.Writer.Advance(bytesRead);

                        // 刷新到管道
                        var result = await _pipe.Writer.FlushAsync();
                        if (result.IsCompleted)
                            break;
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e);
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
                    SequencePosition consumed = buffer.Start;
                    SequencePosition examined = buffer.End;

                    try
                    {
                        // 不断拆帧，直到拆不出完整帧再跳出
                        while (_parserPrototype.TryParse(buffer, out var packet, out var frameEnd))
                        {
                            // 触发 DataReceived，并立刻把事件分发到线程池，避免阻塞拆包循环
                            await _processingChannel.Writer.WriteAsync(packet);
                            // 消耗掉这一帧所有字节
                            consumed = frameEnd;

                            buffer = buffer.Slice(frameEnd);
                        }

                        // 没有更多完整帧可以拆，需要等下次 ReadAsync，那么这里只把 "检查到的最前" 标记给 examined
                        examined = buffer.Start;
                    }
                    catch
                    {
                        break;
                    }

                    // 通知管道：从 buffer.Start 到 consumed 的字节已经消费掉；buffer.Start 到 examined 的字节已经“检查过”
                    _pipe.Reader.AdvanceTo(consumed, examined);

                    if (readResult.IsCompleted)
                        break;
                }

                // 读取循环结束，关闭
                try { await _pipe.Reader.CompleteAsync(); } catch { }
                _running = false;
                try { _socket.Close(); } catch { }
                _processingChannel.Writer.Complete();
                Disconnected?.Invoke(this, EventArgs.Empty);
            });

        }

        /// <summary>
        /// 发送一个 T 包到远端，直接拿到包的字节数组往 socket.SendAsync
        /// </summary>
        public async Task<bool> SendAsync(T packet)
        {
            if (!_running || packet == null)
                return false;

            // 假设 T 自己能提供 "ToArray()" 或 "GetBytes()"，代表一个完整帧
            // 这里新版约定：你在实现 IPipelineDataPackage<T> 的类里，必须提供一个 ToArray() 方法，
            // 它返回一个“可直接 socket.Send 的字节数组”，包含 Magic/Ver/Cmd/CmdId/Len/Body/CRC
            if (packet is IPacketWritable w)
            {
                byte[] data = w.GetBytes();
                try
                {
                    int sent = await _socket.SendAsync(data, SocketFlags.None);
                    return sent == data.Length;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                throw new InvalidOperationException("Your data package must implement IPacketWritable to enable SendAsync.");
            }
        }

        /// <summary>
        /// 停止并关闭连接
        /// </summary>
        public void Close()
        {
            _running = false;
            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            try { _socket.Close(); } catch { }
        }
    }

    /// <summary>
    /// 仅用于 SendAsync 时，把 T 转成字节数组发出
    /// </summary>
    public interface IPacketWritable
    {
        /// <summary>
        /// 返回一个完整的字节数组，包含 Magic/Ver/Cmd/CmdId/Len/Body/CRC
        /// </summary>
        byte[] GetBytes();
    }
}
