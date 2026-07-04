using System;
using System.IO;
using System.Linq;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.Core
{
    public static class GlobalTextRegistry
    {
        public static bool UseId2s { get; set; } = true;        
        public static bool UseId2Dif { get; set; } = true; // Only works if UseId2s is also true

        private static readonly Id2sMap _id2sMap;

        static GlobalTextRegistry()
        {
            _id2sMap = new Id2sMap();

            string localId2sPath = Path.Combine(AppContext.BaseDirectory, "param_table_cache_translation.id2s");

            if (File.Exists(localId2sPath))
            {
                _id2sMap = Id2sReader.Read(localId2sPath);
            }
            else
            {
                Console.WriteLine("[Warning] param_table_cache_translation.id2s not found in output directory.");
            }
        }

        //This is a bit tricky, there are a lot of maps, and some resolve into more useful names than others
        //such as using localized Item names instead of Engine Name placeholders
        //i.e 738227552 ("LIST_EQP_NAIL_35_NAME") would be changed to "Stay Frosty"
        //can set UseId2s to false to see examples, or change to allow FixidRegistry to overwrite Id2s entries possibly. 
        //currently kept as Id2s as its the most accurate to how they would have seen it
        public static string Resolve(uint fixid)
        {
            if (UseId2s)
            {
                // 1. Primary check: dynamic ID2S map
                if (_id2sMap.Entries.TryGetValue(fixid, out var id2sText))
                {
                    return $"{fixid} (\"{id2sText}\")";
                }

                // 2. Secondary check: diff subset (only if toggled on)
                if (UseId2Dif && FixidRegistry_id2dif.Table.TryGetValue(fixid, out var difText))
                {
                    return $"{fixid} (\"{difText}\")";
                }
            }
            else
            {
                // Legacy path: Ignore id2s/dif entirely and only check the main static registry
                if (FixidRegistry.Table.TryGetValue(fixid, out var text))
                {
                    return $"{fixid} (\"{text}\")";
                }
            }

            // if not matches found, just give it back as a raw fixid
            return fixid.ToString();
        }

        public static string ResolveForFilename(uint fixid)
        {
            string textToUse = null;

            if (UseId2s)
            {
                if (_id2sMap.Entries.TryGetValue(fixid, out var id2sText))
                {
                    textToUse = id2sText;
                }
                else if (UseId2Dif && FixidRegistry_id2dif.Table.TryGetValue(fixid, out var difText))
                {
                    textToUse = difText;
                }
            }
            else
            {
                if (FixidRegistry.Table.TryGetValue(fixid, out var text))
                {
                    textToUse = text;
                }
            }

            if (textToUse != null)
            {
                var invalidChars = Path.GetInvalidFileNameChars();

                var safeText = new string(textToUse
                    .Where(c => !invalidChars.Contains(c) && c != '"' && c != ' ')
                    .ToArray());

                return $"{fixid}_{safeText}";
            }

            return fixid.ToString();
        }
    }
}