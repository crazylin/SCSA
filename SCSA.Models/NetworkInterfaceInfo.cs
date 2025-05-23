using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Models
{
    public class NetworkInterfaceInfo
    {
        public string Name { get; set; }          // 网卡名称
        public string Description { get; set; }  // 网卡描述
        public string IPAddress { get; set; }    // IP 地址
        public NetworkInterface RawInterface { get; set; } // 原始 NetworkInterface 对象

        public override string ToString()
        {
            return $"{Name} - {Description} - {IPAddress}";
        }
    }
}
