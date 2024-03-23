namespace GDTFSharp
{
    public class DataVersion
    {
        public byte Major { get; }
        public byte Minor { get; }
        
        public DataVersion(byte major, byte minor)
        {
            Major = major;
            Minor = minor;
        }
    }
}