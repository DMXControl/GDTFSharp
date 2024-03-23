using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public interface INode<T>
    {
        public IEnumerable<T> Children { get; }
        
        public void AddChild(T node);
        public StringBuilder ToXmlBuilder(StringBuilder builder);
        public string ToXml();
    }
}