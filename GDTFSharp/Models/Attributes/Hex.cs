using System;
using System.Globalization;

namespace GDTFSharp
{
    
    public class Hex
    {
        private int _value;

        public string HexString
        {
          get => $"0x{this._value:X}";
          set => this._value = int.Parse(value, NumberStyles.HexNumber);
        }

        public int IntValue
        {
            get => this._value;
            set => this._value = value;
        }

        public Hex(int value)
        {
            this._value = value;
        }

        public Hex(string value)
        {
            this._value = int.Parse(value, NumberStyles.HexNumber);
        }
    }
    
}