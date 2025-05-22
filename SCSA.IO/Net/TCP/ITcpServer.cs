using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.IO.Net.TCP
{
    public interface ITcpServer<T> where T : class,new()
    {  
        void Start(IPEndPoint server);
        void Stop();
        void Send(T netDataStream, IPEndPoint client);
        ///// <summary>
        ///// 数据接收事件
        ///// </summary>
        //event EventHandler<BaseNetDataPackage> OnDataReceived;
        /// <summary>
        /// 端口断开事件
        /// </summary>
        public event EventHandler<ITcpClient<T>> SessionClosed;

        public event EventHandler<ITcpClient<T>> SessionConnected;

        public void OnSessionClosed(object sender, ITcpClient<T> client);
        public void OnSessionConnected(object sender, ITcpClient<T> client);
    }
}
