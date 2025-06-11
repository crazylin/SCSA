namespace SCSA.IO.Net.TCP;

public interface INetDataPackage
{
    public byte[] RawData { set; get; }

    public void Read(BinaryReader reader)
    {
    }

    public byte[] Get()
    {
        return null;
    }
}