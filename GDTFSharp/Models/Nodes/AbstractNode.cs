using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public abstract class AbstractNode<T> : INode<T>
    {
        public IEnumerable<T> Children => _children;

        protected readonly List<T> _children = new List<T>();

        public void AddChild(T node)
        {
            _children.Add(node);
        }

        public abstract StringBuilder ToXmlBuilder(StringBuilder builder);

        public string ToXml()
        {
            var builder = new StringBuilder();
            this.ToXmlBuilder(builder);
            return builder.ToString();
        }
    }
}