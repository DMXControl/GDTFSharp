using System.Text.RegularExpressions;

namespace GDTFSharp
{
    public class Name
    {
        // Allowed characters are defined here: https://gdtf.eu/gdtf/annex/annex-c/
        private const string ValidCharactersPattern = @"^[^\u0000-\u0019\u0021\u0024\u0026\u002C\u002E\u003F\u005B-\u005E\u007B-\u007F]*$";
        
        public string Value { get; }
        
        public Name(string value)
        {
            if (!Regex.IsMatch(value, ValidCharactersPattern))
            {
                throw new System.ArgumentException("Input contains invalid characters!");
            }
            Value = value;
        }
    }
}