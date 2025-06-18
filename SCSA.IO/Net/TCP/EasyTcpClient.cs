using System.Net;
using System.Net.Sockets;
using SCSA.Utils;

namespace SCSA.IO.Net.TCP;

public class EasyTcpClient<T> : ITcpClient<T> where T : class, new()
{
    private readonly Thread _reciveThread;
    private bool _running;
    private Socket _socket;

    public EasyTcpClient(ITcpServer<T> server, Socket socket)
    {
        TcpServer = server;
        _socket = socket;
        IpEndPoint = socket.RemoteEndPoint as IPEndPoint;
        var stream = new BinaryReader(new NetworkStream(socket, true));
        _reciveThread = new Thread(() =>
        {
            while (_running) //循环接收消息
                try
                {
                    var netDataPackage = new T();
                    ((INetDataPackage)netDataPackage).Read(stream);
                    DataReceived?.Invoke(this, netDataPackage);
                }
                catch (Exception e)
                {
                    Log.Error("EasyTcpClient receive data failed", e);
                    TcpServer.OnSessionClosed(TcpServer, this);
                    _socket?.Close();
                    _socket = null;
                    break;
                }
        })
        {
            IsBackground = true
        }; //开启线程执行循环接收消息
    }


    public EasyTcpClient()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        var stream = new BinaryReader(new NetworkStream(_socket, true));
        _reciveThread = new Thread(() =>
        {
            while (_running) //循环接收消息
                try
                {
                    var netDataPackage = new T();
                    ((INetDataPackage)netDataPackage).Read(stream);
                    DataReceived?.Invoke(this, netDataPackage);
                }
                catch (Exception e)
                {
                    Log.Error("EasyTcpClient receive data failed", e);
                    TcpServer.OnSessionClosed(TcpServer, this);
                    _socket?.Close();
                    _socket = null;
                    break;
                }
        })
        {
            IsBackground = true
        }; //开启线程执行循环接收消息
    }

    public event EventHandler<T> DataReceived;
    public IPEndPoint IpEndPoint { set; get; }

    public ITcpServer<T> TcpServer { set; get; }

    public void Start()
    {
        _running = true;
        _reciveThread.Start();
    }

    public void Stop()
    {
        _running = false;
        try
        {
            _socket?.Close();
        }
        catch (Exception e)
        {
            Log.Error("EasyTcpClient socket close failed", e);
        }
        _socket = null;
        _reciveThread.Join();
    }

    public bool SendMessage(T netDataPackage) //发送消息
    {
        try
        {
            var bytes = ((INetDataPackage)netDataPackage).Get();
            return _socket?.Send(bytes) == bytes.Length;
        }
        catch (Exception e)
        {
            Log.Error("EasyTcpClient send message failed", e);
            return false;
        }
    }

    public bool? Connected => _socket?.Connected;
}