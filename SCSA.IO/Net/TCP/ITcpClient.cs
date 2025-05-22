using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.IO.Net.TCP
{
    public interface ITcpClient<T> where T : class,new()
    {
        public ITcpServer<T> TcpServer { set; get; }
        public IPEndPoint IpEndPoint { set; get; }
        public void Start();
        public void Stop();

        public bool SendMessage(T netDataPackage);

        public bool? Connected { get; }


        event EventHandler<T> DataReceived;

    }
}
