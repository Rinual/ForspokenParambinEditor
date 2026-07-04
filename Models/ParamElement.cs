using System.Collections.Generic;

namespace ForspokenBinTool.Models
{
    public class ParamElement
    {
        public uint Id { get; set; }

        public List<uint> Values { get; set; } = new();
    }
}