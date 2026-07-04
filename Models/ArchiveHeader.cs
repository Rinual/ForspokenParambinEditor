using System;

namespace ForspokenBinTool.Models
{
    public class ResourceId
    {
        public uint Type { get; set; }
        public uint Primary { get; set; }
        public uint Secondary { get; set; }
        public uint Flag { get; set; }
    }

    public class ArchiveHeader
    {
        // BinHeader Data
        public string Identifier { get; set; } = string.Empty; // "BdevResource"
        public int Size { get; set; }
        public ResourceId ResourceId { get; set; } = new();

        // ParamTableHeader Data
        public uint Version { get; set; }
        public uint TableCount { get; set; }
        public uint[] TableOffsets { get; set; } = Array.Empty<uint>();
    }
}