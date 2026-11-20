using System;
using System.Collections.ObjectModel;

namespace GDTFSharp.Models
{
    /// <summary>
    /// A list of child nodes that keeps each child's parent pointer up to date. Parent pointers let the writer
    /// build <c>Node</c> paths (e.g. <c>FeatureGroup.Feature</c>) from object references.
    /// A node can only be in one parent at a time; remove it from the old parent first.
    /// </summary>
    public sealed class ChildCollection<TParent, TChild> : Collection<TChild>
        where TParent : class
        where TChild : class
    {
        private readonly TParent _owner;
        private readonly Func<TChild, TParent?> _getParent;
        private readonly Action<TChild, TParent?> _setParent;

        internal ChildCollection(TParent owner, Func<TChild, TParent?> getParent, Action<TChild, TParent?> setParent)
        {
            _owner = owner;
            _getParent = getParent;
            _setParent = setParent;
        }

        protected override void InsertItem(int index, TChild item)
        {
            Attach(item);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, TChild item)
        {
            var old = this[index];
            if (ReferenceEquals(old, item)) return;
            Attach(item);
            _setParent(old, null);
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            _setParent(this[index], null);
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            foreach (var item in this) _setParent(item, null);
            base.ClearItems();
        }

        private void Attach(TChild item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var parent = _getParent(item);
            if (parent != null)
            {
                throw new InvalidOperationException(ReferenceEquals(parent, _owner)
                    ? "The node is already in this collection."
                    : "The node already belongs to another parent; remove it there first.");
            }

            _setParent(item, _owner);
        }
    }
}
