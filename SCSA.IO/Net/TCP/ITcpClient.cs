using System.Net;

namespace SCSA.IO.Net.TCP;

public interface ITcpClient<T> where T : class, new()
{
    public ITcpServer<T> TcpServer { set; get; }
    public IPEndPoint IpEndPoint { set; get; }

    public bool? Connected { get; }
    public void Start();
    public void Stop();

    public bool SendMessage(T netDataPackage);


    event EventHandler<T> DataReceived;
}