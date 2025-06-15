using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SCSA.IO.Net.TCP;

public class PipelineTcpServer<T> where T : class, IPipelineDataPackage<T>, IPacketWritable, new()
{
    private ConcurrentDictionary<EndPoint, PipelineTcpClient<T>> _clients;
    private Socket _listener;
    private IPEndPoint _localEndPoint;
    private bool _running;

    /// <summary>
    ///     新客户端连接时触发
    /// </summary>
    public event EventHandler<PipelineTcpClient<T>> ClientConnected;

    /// <summary>
    ///     客户端断开时触发
    /// </summary>
    public event EventHandler<PipelineTcpClient<T>> ClientDisconnected;

    /// <summary>
    ///     当某个客户端收到一个完整包时触发
    /// </summary>
    public event EventHandler<(PipelineTcpClient<T> Client, T Packet)> DataReceived;

    /// <summary>
    ///     启动监听
    /// </summary>
    public void Start(IPEndPoint localEndPoint)
    {
        if (_running)
            return;

        _localEndPoint = localEndPoint;

        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _clients = new ConcurrentDictionary<EndPoint, PipelineTcpClient<T>>();

        _listener.Bind(_localEndPoint);
        _listener.Listen(1024);
        _running = true;
        AcceptLoop();
    }

    /// <summary>
    ///     停止并关闭所有客户端
    /// </summary>
    public void Stop()
    {
        _running = false;
        try
        {
            _listener?.Close();
        }
        catch
        {
        }

        foreach (var kv in _clients) kv.Value.Close();
        _clients.Clear();
    }

    private void AcceptLoop()
    {
        Task.Run(async () =>
        {
            while (_running)
            {
                Socket clientSocket;
                try
                {
                    clientSocket = await Task.Factory.FromAsync(
                        _listener.BeginAccept,
                        _listener.EndAccept,
                        null);
                }
                catch
                {
                    break;
                }

                // 1) 为这个 clientSocket 创建一个 PipelineTcpClient
                var pipelineClient = new PipelineTcpClient<T>(clientSocket);

                // 2) 把它加入到客户端字典里
                _clients.TryAdd(clientSocket.RemoteEndPoint, pipelineClient);

                // 3) 订阅该 client 的事件，用于转发给外层订阅者
                pipelineClient.DataReceived += (s, packet) => { DataReceived?.Invoke(this, (pipelineClient, packet)); };
                pipelineClient.Disconnected += (s, e) =>
                {
                    _clients.TryRemove(pipelineClient.RemoteEndPoint, out _);
                    ClientDisconnected?.Invoke(this, pipelineClient);
                };

                // 4) 通知外层“新的客户端连上来了”
                ClientConnected?.Invoke(this, pipelineClient);

                // 5) 启动它的拆包/接收循环
                pipelineClient.Start();
            }
        });
    }

    /// <summary>
    ///     向指定客户端发送一个包
    /// </summary>
    public async Task<bool> SendAsync(IPEndPoint remoteEP, T packet)
    {
        if (_clients.TryGetValue(remoteEP, out var client)) return await client.SendAsync(packet);
        return false;
    }
}