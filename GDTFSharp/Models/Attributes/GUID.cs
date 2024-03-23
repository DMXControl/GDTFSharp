using System.Text.RegularExpressions;

namespace GDTFSharp
{
    public class GUID
    {
        public string Value { get; }
        
        public GUID(string value)
        {
            if (!Regex.IsMatch(value, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"))
            {
                throw new System.ArgumentException("Invalid GUID format");
            }
            Value = value;
        }
    }
}