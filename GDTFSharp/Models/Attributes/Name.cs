namespace GDTFSharp
{
    public class Name
    {
        public string Value { get; }
        
        public Name(string value)
        {
            // To Do: Validate characters in the string
            Value = value;
        }
    }
}