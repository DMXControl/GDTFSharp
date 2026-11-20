namespace GDTFSharp.Serialization
{
    /// <summary>
    /// Identifies a namespace that <c>Node</c> paths are resolved in, e.g. "Features, starting at FeatureGroups".
    /// Every GDTF attribute of type Node defines its own starting point, so each starting point gets one scope.
    /// </summary>
    /// <typeparam name="T">Type of the nodes registered in this scope.</typeparam>
    internal sealed class ReferenceScope<T> where T : class
    {
        public string Name { get; }

        public ReferenceScope(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}
