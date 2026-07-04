using System.Collections.Generic;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.Core
{
    public static class TagRegistry
    {
        private static readonly Dictionary<uint, TagDefinition> _tagMap = new()
        {
            // currently unused, as TagID in the current system should not be trusted, TagID is currently just used with TableID to map unique column value data types
/*            { 17277207, new TagDefinition { Name = "AIDialogue" } },
            { 738201009, new TagDefinition { Name = "ActorName" } },
            { 738201010, new TagDefinition { Name = "UnknownString" } },
            { 738201011, new TagDefinition { Name = "GameText" } },
            { 16822199, new TagDefinition { Name = "DebugText" } },
            { 16781491, new TagDefinition { Name = "Order" } },
            { 16817411, new TagDefinition { Name = "Description" } },
            { 17313506, new TagDefinition { Name = "Description2(Source)" } },
            { 17327332, new TagDefinition { Name = "Description3(Location)" } },
            { 17196745, new TagDefinition { Name = "URI Path" } },
            { 16785072, new TagDefinition { Name = "Name" } },
            { 16779977, new TagDefinition { Name = "Label" } },
            { 16857085, new TagDefinition { Name = "Label2" } },
            { 16781490, new TagDefinition { Name = "Label3" } }*/
        };

        public static string GetTagName(uint tagId)
        {
            if (_tagMap.TryGetValue(tagId, out var def) && !string.IsNullOrEmpty(def.Name))
            {
                return def.Name;
            }
            return string.Empty;
        }
    }
}