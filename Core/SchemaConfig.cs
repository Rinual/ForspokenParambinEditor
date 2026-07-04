using ForspokenBinTool.Models;
using ForspokenBinTool.Models.Enums;
using System.Collections.Generic;

namespace ForspokenBinTool.Core
{
    public static class SchemaConfig
    {
        public static bool UseSchemaOverrides { get; set; } = false;

        public static readonly Dictionary<(uint, uint), ParambinDataType> RegistryOverrides = new()
        {
            // These are all 1 element tables that have array and string offsets to 0 or 1 that make identifying them near impossible without guessing
            // Especially if there are multiple rows with 0 or 1 there, which so far, has been the case for all. 
            // these get read sequentially, so though some are marked likely, you can't just skip the ones that say guess work
            // i tend not to guess arrays or strings being the first row / tag though, as that is so often just order number in other tables

            // 
            // Arrays
            // 

            // waterinteraction Table_00_16849121
            { (16849121, 16849141), ParambinDataType.IntegerArray }, // entirely guess work, offset 0
            { (16849121, 16849142), ParambinDataType.IntegerArray }, // likely
            { (16849121, 16934032), ParambinDataType.IntegerArray }, // likely
            
            // Parkour Table_05_17290723
            { (17290723, 17290725), ParambinDataType.IntegerArray }, // entirely guess work, offset 0
            { (17290723, 17291197), ParambinDataType.IntegerArray }, // likely            
            // Parkour Table_08_17541751
            { (17541751, 17260135), ParambinDataType.FixidArray },   // entirely guess work, offset 0
            
            // QTE Table_00_17396645 
            { (17396645, 17396656), ParambinDataType.FixidArray },   // entirely guess work, offset 0
            
            // physics Table_01_17543564
            { (17543564, 16997256), ParambinDataType.IntegerArray }, //entirely guess work, offset 0
            { (17543564, 17543503), ParambinDataType.FixidArray }, //likely (this is a massive 1690 FIXID array with enemy names)
            { (17543564, 17543504), ParambinDataType.FixidArray }, //likely
            { (17543564, 17543505), ParambinDataType.FixidArray }, //likely

            // Acttable Table_13_17297122.tsv
            { (17297122, 17276661), ParambinDataType.FixidArray },   // entirely guess work, offset 0
            { (17297122, 17466384), ParambinDataType.FixidArray },   //likely
            // Acttabl Table_58_17578574_TABLE_ACT_PG331 (seems to match even the TagIds as above, so schema should be the same between)
            { (17578574, 17276661), ParambinDataType.FixidArray },   // entirely guess work, offset 0
            { (17578574, 17466384), ParambinDataType.FixidArray },   //likely

            // uioption Table_02_17283315
            { (17283315, 17260135), ParambinDataType.FixidArray }, //entirely guess work, offset 0
            { (17283315, 17283350), ParambinDataType.FixidArray }, //likely 
            { (17283315, 17283351), ParambinDataType.FixidArray }, //likely
            { (17283315, 17283352), ParambinDataType.FixidArray }, //likely


            //
            // String tables, seem to all have the same TagID between different parambins, so this is actually more likely right, or all wrong
            //

            // npc Table_04_17310226 
            { (17310226, 17260218), ParambinDataType.String }, //entirely guess work, offset 1, and an empty string, but certainly a string is set based on stringHeap size
            // npc Table_05_17307051 (seems to match even the TagIds as above, so schema should be the same between)
            { (17307051, 17260218), ParambinDataType.String }, //entirely guess work, offset 1, and an empty string, but certainly a string is set based on stringHeap size

            // Navigationparamtable Table_00_16870182
            { (16870182, 17260218), ParambinDataType.String }, //entirely guess work, offset 1, single string
            // Navigationparamtable Table_06_17279215 (multiple options, but this tag ID matches the one guessed in the first table)
            { (17279215, 17260218), ParambinDataType.String }, //entirely guess work, offset 1, single string

            // Sound Table_05_17266986
            { (17266986, 17260218), ParambinDataType.String }, //entirely guess work, offset 1, and an empty string, but certainly a string is set based on stringHeap size


            //
            // Mixed string & array
            //

            // vehicle Table_10_16827895 (Many of the TagIDs are consistent between tables, recommend testing changes to all)
             { (16827895, 17260135), ParambinDataType.FixidArray }, //entirely guess work, offset 0
            // vehicle Table_11_16866917
            { (16866917, 17260135), ParambinDataType.FixidArray }, //entirely guess work, offset 0
            { (16866917, 17260218), ParambinDataType.String }, // (probably correct based on TagID, but still) entirely guess work, offset 1, and an empty string, but certainly a string is set based on stringHeap size
            // vehicle Table_14_17258514
            { (17258514, 17260135), ParambinDataType.FloatArray }, //entirely guess work, offset 0
            // vehicle Table_16_17258504
            { (17258504, 17258507), ParambinDataType.FloatArray }, //entirely guess work, offset 0
            { (17258504, 17258508), ParambinDataType.FloatArray }, //likely
            // vehicle Table_18_17258492
            { (17258492, 17258496), ParambinDataType.FloatArray }, // probably, but entirely guess work, offset 0 (but its consecutive in TagID with the likely)
            { (17258492, 17258497), ParambinDataType.FloatArray }, //likely
            { (17258492, 17258498), ParambinDataType.FloatArray }, //likely
  
        };
        public static bool TryGetDataType(uint tableId, uint tagId, out ParambinDataType type)
        {
            if (UseSchemaOverrides && RegistryOverrides.TryGetValue((tableId, tagId), out type))
            {
                return true;
            }
            // Fallback to the compiled schema map
            return SchemaRegistry.Table.TryGetValue((tableId, tagId), out type);
        }
        public static bool IsTableFlagged(uint tableId)
        {
            foreach (var key in RegistryOverrides.Keys)
            {
                if (key.Item1 == tableId) return true;
            }
            return false;
        }
    }
}