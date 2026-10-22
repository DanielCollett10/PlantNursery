using System;

namespace PlantNursery.Sim.Content
{
    /// <summary>Thrown when content JSON is malformed or fails validation.</summary>
    public sealed class ContentException : Exception
    {
        /// <summary>Content file name, e.g. "items.json".</summary>
        public string File { get; }

        /// <summary>Path of the offending field, e.g. "items[2].price". May be empty.</summary>
        public string Field { get; }

        /// <summary>Creates an exception naming the file and field.</summary>
        public ContentException(string file, string field, string message)
            : base(file + ": " + (string.IsNullOrEmpty(field) ? "" : field + ": ") + message)
        {
            File = file;
            Field = field ?? "";
        }
    }
}
