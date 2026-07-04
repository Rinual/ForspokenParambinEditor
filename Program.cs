using ForspokenBinTool.Core;
using ForspokenBinTool.Core.FixidMapping;
using ForspokenBinTool.Core.FixidMapping.Generators;
using ForspokenBinTool.Debug;
using ForspokenBinTool.IO;
using ForspokenBinTool.Models;
using System;
using System.IO;
using System.Text;

namespace ForspokenBinTool
{
    class Program
    {
            static void Main(string[] args)
            {
                Console.WriteLine("=========================================");
                Console.WriteLine(" Forspoken Parambin Tool v1.0");
                Console.WriteLine("=========================================\n");

                if (args.Length > 0)
                {
                    ProcessCommand(args);
                    return;
                }

                PrintUsage();

                while (true)
                {
                    Console.Write("\nForspokenBinTool> ");
                    string input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    string[] interactiveArgs = ParseInteractiveArguments(input);

                    if (interactiveArgs.Length > 0 &&
                       (interactiveArgs[0].Equals("ForspokenBinTool", StringComparison.OrdinalIgnoreCase) ||
                        interactiveArgs[0].Equals("ForspokenBinTool.exe", StringComparison.OrdinalIgnoreCase)))
                    {
                        interactiveArgs = interactiveArgs.Skip(1).ToArray();
                    }

                    if (interactiveArgs.Length == 0)
                    {
                        continue;
                    }

                    ProcessCommand(interactiveArgs);
                }
            }

            static void ProcessCommand(string[] args)
            {
                bool useOverrides = args.Any(a => a.Equals("--OverwriteSchema", StringComparison.OrdinalIgnoreCase));
                args = args.Where(a => !a.Equals("--OverwriteSchema", StringComparison.OrdinalIgnoreCase)).ToArray();
                SchemaConfig.UseSchemaOverrides = useOverrides;

                if (args.Length < 1)
                {
                    PrintUsage();
                    return;
                }

                string command = args[0].ToLower();

                try
                {
                    switch (command)
                    {
                        case "unpack":
                            if (args.Length < 3)
                            {
                                Console.WriteLine("Error: Missing arguments for unpack.");
                                Console.WriteLine("Usage: ForspokenBinTool unpack <input.parambin> <output_directory>");
                                return;
                            }

                            string inputFile = args[1];
                            string outputDir = args[2];

                            var unpacker = new Unpacker();
                            unpacker.Unpack(inputFile, outputDir);
                            break;

                        case "unpackall":
                            if (args.Length < 3)
                            {
                                Console.WriteLine("Error: Missing arguments for unpackall.");
                                Console.WriteLine("Usage: ForspokenBinTool unpackall <input_directory> <output_directory>");
                                return;
                            }

                            string inputDirAll = args[1];
                            string outputDirAll = args[2];

                            var batchUnpacker = new Unpacker();
                            batchUnpacker.UnpackAll(inputDirAll, outputDirAll);
                            break;

                        case "repack":
                            if (args.Length < 4)
                            {
                                Console.WriteLine("Error: Missing arguments for repack.");
                                Console.WriteLine("Usage: ForspokenBinTool repack <originalParambin.parambin> <editedParamBin_tsv_directory> <output_directory>");
                                return;
                            }

                            string tsvInputDir = args[1];
                            string originalParambin = args[2];
                            string RepackDir = args[3];

                            var repacker = new Repacker();
                            repacker.Repack(originalParambin, tsvInputDir, RepackDir);
                            break;

                        case "testsuite":
                            if (args.Length == 2 && args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
                            {
                                PrintTestSuite();
                                return;
                            }

                            if (args.Length < 3)
                            {
                                Console.WriteLine("Error: Missing arguments for testsuite.");
                                Console.WriteLine("Usage: ForspokenBinTool testsuite <original_parambins_dir> <temp_work_dir>");                                
                                Console.WriteLine("Type 'ForspokenBinTool testsuite help' for more details.");
                                return;
                            }
                            MassRepackTester.RunFullValidation(args[1], args[2]);
                            break;

                        case "debug":
                            PrintDebug();
                            break;

                        default:
                            Console.WriteLine($"Unknown command: {command}");
                            PrintUsage();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[FATAL ERROR] {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }

            static void PrintUsage()
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  ForspokenBinTool unpack <input.parambin> <output_directory>");
                Console.WriteLine("  ForspokenBinTool unpackall <input_directory> <output_directory>");
                Console.WriteLine("  ForspokenBinTool repack <editedParamBin_tsv_directory> <originalParambin.parambin> <output_directory>");
                Console.WriteLine("  ForspokenBinTool debug");
            }

            static void PrintDebug()
            {
                Console.WriteLine("Debug:");
                Console.WriteLine("  Append --OverwriteSchema to any command to enable custom schema mappings:");
                Console.WriteLine("  By Default 18 of the games of the game's 1,418 tables are exported as read-only");
                Console.WriteLine("  All of these are tiny tables with too little data in them to properly map");
                Console.WriteLine("  To validate or test different data parsing schemas for tables, see SchemaConfig.cs");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("testsuite:");
                Console.WriteLine("  Unpack and Repack all parambin files to validate binary");
                Console.WriteLine("  Usage: ForspokenBinTool testsuite help");
                Console.WriteLine("  Usage: ForspokenBinTool testsuite <original_parambins_dir> <temp_work_dir>");
            }

            static void PrintTestSuite()
            {
                Console.WriteLine("testsuite:");
                Console.WriteLine("  Unpack and Repack all parambin files to validate binary");
                Console.WriteLine("  Usage: ForspokenBinTool testsuite <original_parambins_dir> <temp_work_dir>");
                Console.WriteLine("  [Debug] Usage: ForspokenBinTool testsuite <original_parambins_dir> <temp_work_dir> --OverwriteSchema");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("This exists to validate that unedited .parambin repacking is matching original .parmbins");
                Console.WriteLine("warning, it does delete the folder you set as your temp_work_dir when it runs, so DO NOT set this somewhere important");
                Console.WriteLine("though it should just be standard reversable delete, not like perma delete, but file limits exists so ymmv");
            }
        

        static string[] ParseInteractiveArguments(string commandLine)
        {
            var argsList = new List<string>();
            bool inQuotes = false;
            var currentArg = new StringBuilder();

            foreach (char c in commandLine)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes; 
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (currentArg.Length > 0)
                    {
                        argsList.Add(currentArg.ToString());
                        currentArg.Clear();
                    }
                }
                else
                {
                    currentArg.Append(c);
                }
            }

            if (currentArg.Length > 0)
                argsList.Add(currentArg.ToString());

            return argsList.ToArray();
        }

        // These are all debug / functions that can be used to regenerate / recreate, or improve Table Data Type Schemas 
        // You will need to change the file paths to match your system, and weren't designed with others needing to run these ever.
        // I just change this method to Main, and rename the one above to MainExe temporarily so i can trigger it from here
        static void Main2()
        {

            // Manual triggering of console stuff
            //
            //  MainExe(new string[] { "unpack", @"C:\Users\Rinual\Desktop\forspokenBins\parkour_e043e59a.parambin", @"C:\Users\Rinual\Desktop\forspokenBins\exported" });
            //16777556, 17230049
            //MainExe(new string[]{"unpackall",@"C:\Users\Rinual\Desktop\forspokenBins",@"C:\Users\Rinual\Desktop\forspokenBins\exported"});
            //  MainExe(new string[] { "repack", @"C:\Users\Rinual\Desktop\forspokenBins\exported\parkour", @"C:\Users\Rinual\Desktop\forspokenBins\parkour_e043e59a.parambin", @"C:\Users\Rinual\Desktop\forspokenBins\repacked" });




            // MainExe(new string[] { "unpack", @"C:\Users\Rinual\Desktop\forspokenBins\RepackTestSpace\TempRepackedBins\text_jp_4a4a1af9.parambin", @"C:\Users\Rinual\Desktop\forspokenBins\RepackTestSpace\TempRepackedBins\text_jp_4a4a1af9_2ndUnpack.tsv" });



            //Just for validating that unedited .parambin repacking is matching original .parmbins 
/*            MainExe(new string[] {
                                    "testsuite",
                                    @"C:\Users\Rinual\Desktop\forspokenBins",
                                    @"C:\Users\Rinual\Desktop\forspokenBins\RepackTestSpace",
                                    "--OverwriteSchema"
                                });*/


            // Use this to regenerate the SchemaRegistry, such as if you improve the detection. 
            // Otherwise, add any Table ID / TagID specific changes to the SchemaConfig.cs file
            //SchemaAutoGen();



            //FIXID Mapping workflow
            //
            // Mostly not really needed stuff, as Id2sReader.cs kinda makes all of this mapping pointless, but left in case people want to recreate how the FixidRegistry.cs was made. 
            //
            //Extract FIXIDS from scripts (likely nothing to be improved here)
            //ScriptLabelMap()
            //
            //ExtractId2s mapping (don't use this, too big)
            // GenerateId2sMap();
            //
            //Map FIXIDs to labeled Strings from .parambin files (could benefit from more thorough anlyasis
            ///StringLabelMap()
            //
            //Map Fixids to other Name labeled FIXID maps (This generates FIXIDs labels based on associated FIXID columns, like reading an Item FIXID and looking at the ID the game uses for Localization text strings
            //ReferenceLabelMap()
            //
            //Combines All three FIXID maps (ReferenceLabelMap.cs, ScriptLabelMap.cs, & StringLabelMap.cs) in a specified order, allowing certain values to overwrite others            
            //GenerateMergedFixidRegistry();
            //
            ////The map the game will use (or where you should put your generated / edited FixidMap, is in FixidRegistry.cs 
            ///

        }

        static void SchemaAutoGen()
        {
            string inputParambins = @"C:\Users\Rinual\Desktop\forspokenBins";
            SchemaAutoGenerator.GeneratePhase1(inputParambins, "SchemaRegistry.cs");
        }

        // Everything from here down can be deleted, as well as evertying in the FixidMapping folder and under. 
        // It's just left here in case I wan't to rebuild or reuse stuff

        // this ended up being pointless, dictionary was too large, still interesting to have exported
        static void GenerateId2sMap()
        {
            string id2sInputPath = @"C:\Users\Rinual\Downloads\forspoken id2s\data\tool\param_table_cache_translation.id2s";
            string outputCsPath = @"C:\Users\Rinual\Downloads\forspoken id2s\data\tool\Id2sLabelMap.cs";

            Id2sMapGenerator.GenerateCSharpMap(id2sInputPath, outputCsPath);
        }
   
        static void ScriptLabelMap()
        {
            ScriptMapGenerator.GenerateCSharpMap(@"K:\Forspoken Scripts\data", @"K:\Forspoken Scripts\ScriptLabelMap.cs");
        }

        static void StringLabelMap()
        {
            string inputParambins = @"C:\Users\Rinual\Desktop\forspokenBins";

            string outputCsPath = @"K:\Forspoken Scripts\StringLabelMap.cs";

            string collisionLogPath = @"K:\Forspoken Scripts\StringLabelCollisions.txt";

            var primaryTables = new List<ParamTable>();
            var fallbackTables = new List<ParamTable>();

            string[] allParambinFiles = Directory.GetFiles(inputParambins, "*.parambin", SearchOption.TopDirectoryOnly);

            foreach (string fullPath in allParambinFiles)
            {
                string fileName = Path.GetFileName(fullPath);
                Console.WriteLine($"Loading {fileName}...");

                using (var reader = new ParambinReader(fullPath))
                {
                    var header = reader.ReadHeader();
                    var tables = reader.ReadAllTables(header);

                    if (fileName.Equals("text_jp_4a4a1af9.parambin", StringComparison.OrdinalIgnoreCase))
                    {
                        fallbackTables.AddRange(tables);
                    }
                    else
                    {
                        primaryTables.AddRange(tables);
                    }
                }
            }

            Console.WriteLine($"Building Master String Map from {primaryTables.Count} primary tables and {fallbackTables.Count} fallback tables...");

            StringLabelMapBuilder.GenerateCSharpMap(primaryTables, fallbackTables, outputCsPath, collisionLogPath);

        }
        static void ReferenceLabelMap()
        {
            var primaryTables = new List<ParamTable>();
            var fallbackTables = new List<ParamTable>();
            string referenceMapOutPath = @"K:\Forspoken Scripts\ReferenceLabelMap.cs";

            // Specify overwrite priorioty here (StringLabelMap is standard, but you can swap this anytime)
            ReferenceMapBuilder.MapPriority = ReferenceMapPriority.StringLabelMapPrioritized;

            Console.WriteLine("\n[Main] Building ReferenceLabelMap...");
            ReferenceMapBuilder.GenerateCSharpMap(primaryTables, referenceMapOutPath);

        }
        static void GenerateMergedFixidRegistry()
        {

            string outputCsPath = @"K:\Forspoken Scripts\FixidRegistry.cs";
            FixidRegistryBuilder.GenerateCSharpMap(outputCsPath);

        }

    }

}


