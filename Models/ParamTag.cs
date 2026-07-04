namespace ForspokenBinTool.Models
{
    public class ParamTag
    {
        public uint Id { get; set; }

        //possibly responsible for mapping TagId data types to Element Values, but currently mapping TagIds to Element Values is flawed, see below
        public ushort EngineFlag { get; set; }

        public ushort Offset { get; set; } 
        //This seems like an order mapping, not an offset, but the order mapping has flaws too,
        //such as two tags sharing the same orderNumber / offset, but also if followed strictly, occasionaly resulting in too few tagIds to element values,
        //we are kind of ignoring this for now, ideally this should be figured out to we can skip the schema map generation reliance
    }
}