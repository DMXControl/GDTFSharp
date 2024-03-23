using System.Collections.Generic;

namespace GDTFSharp.Nodes
{
    public abstract class ChildlessNode<T>: AbstractNode<T>
    {
        
        public new IEnumerable<T> Children => new T[]{};

        public new void AddChild(T node)
        {
            throw new System.Exception("ActivationGroup cannot have children");
        }
    }
}