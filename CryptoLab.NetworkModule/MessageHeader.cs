namespace CryptoLab.NetworkModule
{
    public enum MessageHeader : byte
    {
        Transaction = 0,
        Block = 1,
        Unknown = 255
    }
}
