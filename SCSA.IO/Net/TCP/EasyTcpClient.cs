using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCSA.IO.Net.TCP
{
    public class EasyTcpClient<T> : ITcpClient<T> where T : class, new()
    {
        public event EventHandler<T> DataReceived;
        private Socket _socket;
        public IPEndPoint IpEndPoint { set; get; }
        private Thread _reciveThread;
        private bool _running;
        private ITcpServer<T> _tcpServer;

        public ITcpServer<T> TcpServer
        {
            set => _tcpServer = value;
            get => _tcpServer;
        }

        public EasyTcpClient(ITcpServer<T> server, Socket socket)
        {
            _tcpServer = server;
            _socket = socket;
            IpEndPoint = (socket.RemoteEndPoint as IPEndPoint);
            var stream = new BinaryReader(new NetworkStream(socket, true));
            _reciveThread = new Thread(() =>
            {
                while (_running) //循环接收消息
                {
                    try
                    {
                        var netDataPackage = new T();
                        ((INetDataPackage)netDataPackage).Read(stream);
                        DataReceived?.Invoke(this, netDataPackage);

                    }
                    catch(Exception e)
                    {
                        _tcpServer.OnSessionClosed(_tcpServer, this);
                        _socket?.Close();
                        _socket = null;
                        break;
                    }
                }
            })
            {
                IsBackground = true
            }; //开启线程执行循环接收消息
        }


        public EasyTcpClient()
        {

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,ProtocolType.Tcp);
       
            var stream = new BinaryReader(new NetworkStream(_socket, true));
            _reciveThread = new Thread(() =>
            {
                while (_running) //循环接收消息
                {
                    try
                    {
                        var netDataPackage = new T();
                        ((INetDataPackage)netDataPackage).Read(stream);
                        DataReceived?.Invoke(this, netDataPackage);

                    }
                    catch
                    {
                        _tcpServer.OnSessionClosed(_tcpServer, this);
                        _socket?.Close();
                        _socket = null;
                        break;
                    }
                }
            })
            {
                IsBackground = true
            }; //开启线程执行循环接收消息
        }
        public void Start()
        {
            _running = true;
            _reciveThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _socket?.Close();
            _socket = null;
            _reciveThread.Join();
        }

        public bool SendMessage(T netDataPackage) //发送消息
        {
            var bytes = ((INetDataPackage)netDataPackage).Get();
            return _socket?.Send(bytes) == bytes.Length;
        }

        public bool? Connected => _socket?.Connected;

    }
}
