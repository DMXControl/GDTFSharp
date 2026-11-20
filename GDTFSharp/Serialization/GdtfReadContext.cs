using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    /// <summary>
    /// State of one read: collected diagnostics and the two-pass reference resolution.
    /// Pass 1 parses all elements, registering every referencable node under its path in a <see cref="ReferenceScope{T}"/>
    /// and queueing every reference. Pass 2 (<see cref="ResolveReferences"/>) looks the queued paths up.
    /// </summary>
    internal sealed class GdtfReadContext
    {
        private readonly List<GdtfDiagnostic> _diagnostics = new List<GdtfDiagnostic>();
        private readonly Dictionary<(object Scope, NodePath Path), object> _index = new Dictionary<(object, NodePath), object>();
        private readonly List<Action> _pendingReferences = new List<Action>();
        private readonly List<Action> _afterResolve = new List<Action>();

        public GdtfReadOptions Options { get; }

        /// <summary>DataVersion of the file being read, once the root element has been read.</summary>
        public DataVersion DataVersion { get; internal set; } = DataVersion.Current;

        public IReadOnlyList<GdtfDiagnostic> Diagnostics => _diagnostics;

        public GdtfReadContext(GdtfReadOptions? options = null)
        {
            Options = options ?? GdtfReadOptions.Default;
        }

        public void Report(GdtfDiagnosticSeverity severity, string message, XObject? source = null)
        {
            var diagnostic = new GdtfDiagnostic(severity, message, source == null ? null : DescribeLocation(source), GetLine(source));
            _diagnostics.Add(diagnostic);
            if (severity == GdtfDiagnosticSeverity.Error && Options.Strict) throw new GdtfReadException(diagnostic);
        }

        public void Info(string message, XObject? source = null) => Report(GdtfDiagnosticSeverity.Info, message, source);
        public void Warning(string message, XObject? source = null) => Report(GdtfDiagnosticSeverity.Warning, message, source);
        public void Error(string message, XObject? source = null) => Report(GdtfDiagnosticSeverity.Error, message, source);

        /// <summary>Registers <paramref name="node"/> so references to <paramref name="path"/> in <paramref name="scope"/> resolve to it.</summary>
        public void Register<T>(ReferenceScope<T> scope, NodePath path, T node, XObject? source = null) where T : class
        {
            var key = ((object)scope, path);
            if (_index.ContainsKey(key))
            {
                Error($"Duplicate name '{path}' in {scope}; references will resolve to the first one.", source);
                return;
            }

            _index[key] = node;
        }

        /// <summary>Looks a path up immediately. Only valid for nodes already registered; prefer <see cref="Resolve{T}"/>.</summary>
        public bool TryLookup<T>(ReferenceScope<T> scope, NodePath path, out T node) where T : class
        {
            if (_index.TryGetValue((scope, path), out var found))
            {
                node = (T)found;
                return true;
            }

            node = null!;
            return false;
        }

        /// <summary>
        /// Queues the lookup of <paramref name="path"/> for pass 2. On success <paramref name="assign"/> receives the node;
        /// otherwise an error is reported. An empty path is not looked up.
        /// </summary>
        public void Resolve<T>(ReferenceScope<T> scope, NodePath path, Action<T> assign, XObject? source = null) where T : class
        {
            if (path.IsEmpty) return;
            _pendingReferences.Add(() =>
            {
                if (TryLookup(scope, path, out var node)) assign(node);
                else Error($"Reference '{path}' could not be resolved in {scope}.", source);
            });
        }

        /// <summary>Queues work that needs resolved references, e.g. a lookup inside a referenced node. Runs after pass 2.</summary>
        public void AfterResolve(Action action) => _afterResolve.Add(action);

        /// <summary>Pass 2. Resolves every reference queued with <see cref="Resolve{T}"/>, then runs <see cref="AfterResolve"/> work.</summary>
        public void ResolveReferences()
        {
            var pending = _pendingReferences.ToList();
            _pendingReferences.Clear();
            foreach (var resolve in pending) resolve();

            var after = _afterResolve.ToList();
            _afterResolve.Clear();
            foreach (var action in after) action();
        }

        private static int? GetLine(XObject? source) =>
            source is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : (int?)null;

        internal static string DescribeLocation(XObject source)
        {
            var element = source as XElement ?? source.Parent;
            var segments = new List<string>();
            for (var current = element; current != null && current.Parent != null; current = current.Parent)
            {
                var name = current.Attribute("Name")?.Value;
                segments.Add(name != null ? $"{current.Name.LocalName}[{name}]" : current.Name.LocalName);
            }

            segments.Reverse();
            var location = string.Join("/", segments);
            return source is XAttribute attribute ? $"{location}@{attribute.Name.LocalName}" : location;
        }
    }
}
