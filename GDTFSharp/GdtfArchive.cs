using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;

namespace GDTFSharp
{
    /// <summary>
    /// A <c>.gdtf</c> file: a zip archive with <c>description.xml</c> plus resource files (thumbnails in the root,
    /// wheel media in <c>wheels/</c>, models in <c>models/3ds</c>, <c>models/gltf</c>, <c>models/svg</c> and their variants).
    /// Resources are loaded into memory.
    /// </summary>
    public sealed class GdtfArchive
    {
        public const string DescriptionFileName = "description.xml";
        public const string WheelsFolder = "wheels";
        public const string ModelsFolder = "models";

        /// <summary>Model folders defined by the spec, below <see cref="ModelsFolder"/>.</summary>
        public static readonly IReadOnlyList<string> ModelSubfolders = new[]
        {
            "3ds", "3ds_low", "3ds_high", "gltf", "gltf_low", "gltf_high", "svg", "svg_side", "svg_front"
        };

        public GdtfDocument Document { get; set; }

        /// <summary>
        /// Every file except description.xml, keyed by its path inside the archive with "/" separators
        /// (e.g. <c>wheels/gobo1.png</c>). Lookups ignore case.
        /// </summary>
        public IDictionary<string, byte[]> Resources { get; } = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public GdtfArchive(GdtfDocument document)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        // ---- Reading ----------------------------------------------------------------------------------------------

        public static GdtfArchiveReadResult Load(string path, GdtfReadOptions? options = null)
        {
            using var stream = File.OpenRead(path);
            return Load(stream, options);
        }

        public static GdtfArchiveReadResult Load(Stream stream, GdtfReadOptions? options = null)
        {
            ZipArchive zip;
            try
            {
                zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            }
            catch (InvalidDataException e)
            {
                throw new GdtfReadException($"The file is not a valid zip archive: {e.Message}", e);
            }

            using (zip)
            {
                var entries = zip.Entries.Where(e => !IsDirectory(e)).ToList();
                var descriptionEntry = entries.FirstOrDefault(e =>
                    string.Equals(NormalizePath(e.FullName), DescriptionFileName, StringComparison.OrdinalIgnoreCase));
                if (descriptionEntry == null) throw new GdtfReadException($"The archive has no {DescriptionFileName}.");

                GdtfReadResult description;
                using (var descriptionStream = descriptionEntry.Open())
                {
                    description = GdtfReader.Read(descriptionStream, options);
                }

                var diagnostics = description.Diagnostics.ToList();
                var archive = new GdtfArchive(description.Document);
                foreach (var entry in entries.Where(e => e != descriptionEntry))
                {
                    var path = NormalizePath(entry.FullName);
                    if (archive.Resources.ContainsKey(path))
                    {
                        diagnostics.Add(new GdtfDiagnostic(GdtfDiagnosticSeverity.Warning,
                            $"Duplicate archive entry '{entry.FullName}' was ignored."));
                        continue;
                    }

                    using var entryStream = entry.Open();
                    using var buffer = new MemoryStream();
                    entryStream.CopyTo(buffer);
                    archive.Resources[path] = buffer.ToArray();
                }

                return new GdtfArchiveReadResult(archive, diagnostics);
            }
        }

        // ---- Writing ----------------------------------------------------------------------------------------------

        public void Save(string path)
        {
            using var stream = File.Create(path);
            Save(stream);
        }

        /// <summary>Writes description.xml first, then the resources sorted by path.</summary>
        public void Save(Stream stream)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
            using (var descriptionStream = zip.CreateEntry(DescriptionFileName, CompressionLevel.Optimal).Open())
            {
                GdtfWriter.Write(Document, descriptionStream);
            }

            foreach (var resource in Resources.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                using var entryStream = zip.CreateEntry(NormalizePath(resource.Key), CompressionLevel.Optimal).Open();
                entryStream.Write(resource.Value, 0, resource.Value.Length);
            }
        }

        // ---- Resource lookup --------------------------------------------------------------------------------------

        /// <summary>
        /// Finds a GDTF <c>Resource</c> (a file name without extension and folder) in <paramref name="folder"/>
        /// ("" for the archive root). When <paramref name="extension"/> is given, only that extension matches.
        /// </summary>
        public bool TryGetResource(string folder, string resourceName, string? extension, out string path, out byte[] content)
        {
            var prefix = string.IsNullOrEmpty(folder) ? "" : NormalizePath(folder).TrimEnd('/') + "/";
            if (extension != null)
            {
                path = prefix + resourceName + "." + extension.TrimStart('.');
                if (Resources.TryGetValue(path, out content!)) return true;
            }
            else
            {
                foreach (var resource in Resources.OrderBy(r => r.Key, StringComparer.Ordinal))
                {
                    if (!resource.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    var fileName = resource.Key.Substring(prefix.Length);
                    if (fileName.Contains('/')) continue;
                    if (!string.Equals(Path.GetFileNameWithoutExtension(fileName), resourceName, StringComparison.OrdinalIgnoreCase)) continue;
                    path = resource.Key;
                    content = resource.Value;
                    return true;
                }
            }

            path = string.Empty;
            content = Array.Empty<byte>();
            return false;
        }

        /// <summary>The thumbnail in the given format ("png" or "svg"), if the fixture type names one and the archive has it.</summary>
        public byte[]? GetThumbnail(string extension) =>
            Document.FixtureType.Thumbnail is { } name && TryGetResource("", name, extension, out _, out var content) ? content : null;

        /// <summary>A wheel slot's media file. Annex E defines PNG for wheel slot images.</summary>
        public byte[]? GetWheelMedia(string mediaFileName, string extension = "png") =>
            TryGetResource(WheelsFolder, mediaFileName, extension, out _, out var content) ? content : null;

        /// <summary>A model file from one of the <see cref="ModelSubfolders"/>, e.g. <c>GetModelFile("gltf", "base", "glb")</c>.</summary>
        public byte[]? GetModelFile(string subfolder, string file, string? extension = null) =>
            TryGetResource(ModelsFolder + "/" + subfolder, file, extension, out _, out var content) ? content : null;

        internal static string NormalizePath(string path)
        {
            var normalized = path.Replace('\\', '/');
            while (normalized.StartsWith("./", StringComparison.Ordinal)) normalized = normalized.Substring(2);
            return normalized.TrimStart('/');
        }

        private static bool IsDirectory(ZipArchiveEntry entry) =>
            entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal);
    }

    public sealed class GdtfArchiveReadResult
    {
        public GdtfArchive Archive { get; }
        public IReadOnlyList<GdtfDiagnostic> Diagnostics { get; }

        public bool HasErrors => Diagnostics.Any(d => d.Severity == GdtfDiagnosticSeverity.Error);

        public GdtfArchiveReadResult(GdtfArchive archive, IReadOnlyList<GdtfDiagnostic> diagnostics)
        {
            Archive = archive;
            Diagnostics = diagnostics;
        }
    }
}
