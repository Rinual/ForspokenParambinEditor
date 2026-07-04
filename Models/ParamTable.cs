using System;
using System.Collections.Generic;

namespace ForspokenBinTool.Models
{
    public class ParamTable
    {
        public uint TableId { get; set; }

        public uint TagCount { get; set; }
        public uint ElementCount { get; set; }
        public uint ElementSize { get; set; }

        public uint BooleanOffset { get; set; }
        public uint ArrayOffset { get; set; }
        public uint StringOffset { get; set; }

        public long TableStartOffset { get; set; }
        public long OffsetBase { get; set; }

        public long ArrayHeapBase { get; set; }
        public long ArrayHeapSize { get; set; }

        public long StringHeapBase { get; set; }
        public long StringHeapSize { get; set; }

        // Raw heap data extracted by ParambinReader
        public byte[] ArrayHeapRaw { get; set; } = Array.Empty<byte>();
        public byte[] StringHeapRaw { get; set; } = Array.Empty<byte>();

        public List<ParamTag> Tags { get; set; } = new();
        public List<ParamElement> Elements { get; set; } = new();
    }
}