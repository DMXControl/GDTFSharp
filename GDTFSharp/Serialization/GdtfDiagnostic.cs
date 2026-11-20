using System;

namespace GDTFSharp.Serialization
{
    /// <summary>How serious a <see cref="GdtfDiagnostic"/> is.</summary>
    public enum GdtfDiagnosticSeverity
    {
        /// <summary>Something the library does not understand yet and kept or skipped, e.g. an unknown XML attribute.</summary>
        Info,

        /// <summary>The file deviates from the spec, but a sensible value could be used (default, unchecked name).</summary>
        Warning,

        /// <summary>The file is broken in a way that loses data, e.g. an unparsable value or a dangling reference.</summary>
        Error
    }

    /// <summary>A problem found while reading a GDTF file.</summary>
    public sealed class GdtfDiagnostic
    {
        /// <summary>How serious the problem is.</summary>
        public GdtfDiagnosticSeverity Severity { get; }
        /// <summary>What is wrong.</summary>
        public string Message { get; }

        /// <summary>Location in the XML, e.g. <c>FixtureType/AttributeDefinitions/Attributes/Attribute[Pan]@Feature</c>.</summary>
        public string? Location { get; }

        /// <summary>1-based line in description.xml, when known.</summary>
        public int? Line { get; }

        public GdtfDiagnostic(GdtfDiagnosticSeverity severity, string message, string? location = null, int? line = null)
        {
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Location = location;
            Line = line;
        }

        public override string ToString() =>
            $"{Severity}: {Message}{(Location != null ? $" at {Location}" : "")}{(Line != null ? $" (line {Line})" : "")}";
    }

    /// <summary>Thrown in strict mode on the first <see cref="GdtfDiagnosticSeverity.Error"/>, and for files that cannot be read at all.</summary>
    public sealed class GdtfReadException : Exception
    {
        /// <summary>The diagnostic that caused the exception, if any.</summary>
        public GdtfDiagnostic? Diagnostic { get; }

        public GdtfReadException(GdtfDiagnostic diagnostic) : base(diagnostic.ToString())
        {
            Diagnostic = diagnostic;
        }

        public GdtfReadException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
