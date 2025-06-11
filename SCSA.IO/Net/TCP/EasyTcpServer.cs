using System.Net;
using System.Net.Sockets;

namespace SCSA.IO.Net.TCP;

public class EasyTcpServer<T> : ITcpServer<T> where T : class, new()
{
    private Thread _acceptThread;


    private List<ITcpClient<T>> _clientList;
    private bool _running;

    private Socket _server;


    public event EventHandler<ITcpClient<T>> SessionClosed;
    public event EventHandler<ITcpClient<T>> SessionConnected;

    public void OnSessionClosed(object sender, ITcpClient<T> client)
    {
        if (SessionClosed != null)
            SessionClosed.Invoke(sender, client);
    }

    public void OnSessionConnected(object sender, ITcpClient<T> client)
    {
        if (SessionConnected != null)
            SessionConnected.Invoke(sender, client);
    }


    public void Send(T netDataStream, IPEndPoint ipEndPoint)
    {
        //foreach (var tcpClient in _clientList)
        //{
        //    tcpClient?.SendMessage(netDataStream);
        //}
        //var client = _clientList.FirstOrDefault(c => c.IpEndPoint.Address.Equals(ipEndPoint.Address));

        var client = _clientList.FirstOrDefault(c => c.IpEndPoint.Address.Equals(ipEndPoint.Address));
        client?.SendMessage(netDataStream);
    }

    public void Start(IPEndPoint server)
    {
        if (_server != null)
            Stop();
        _clientList = new List<ITcpClient<T>>();
        _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _server.Bind(server);
        _server.Listen(1000);
        _running = true;
        _acceptThread = new Thread(() =>
        {
            while (_running)
                try
                {
                    var socket = _server.Accept();
                    socket.SendBufferSize = 1024 * 1024; // 1MB
                    socket.ReceiveBufferSize = 1024 * 1024 * 5; // 2MB
                    // 启用 Nagle 算法（小数据包合并）
                    socket.NoDelay = false;

                    var client = new EasyTcpClient<T>(this, socket);
                    _clientList.Add(client);
                    OnSessionConnected(this, client);
                }
                catch
                {
                }
        });
        _acceptThread.IsBackground = true;
        _acceptThread.Start();
    }

    public void Stop()
    {
        _running = false;
        //_server.Shutdown(SocketShutdown.Both);
        if (_server != null)
            _server.Close();
        if (_clientList != null)
        {
            foreach (var client in _clientList) client?.Stop();
            _clientList.Clear();
        }

        if (_acceptThread != null)
            _acceptThread?.Join();
    }
}