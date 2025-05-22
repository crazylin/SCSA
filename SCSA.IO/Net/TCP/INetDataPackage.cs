using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.IO.Net.TCP
{
    public interface INetDataPackage
    {
        public byte[] RawData { set; get; }

        public void Read(BinaryReader reader){}

        public byte[] Get()
        {
            return null;
        }

    }
}
