using ForspokenBinTool.Core;
using ForspokenBinTool.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace ForspokenBinTool.Testing
{
    public static class MassRepackTester
    {
        public static void RunFullValidation(string originalParambinsDir, string baseWorkDir)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine(" Starting Mass 1:1 Repack Validation");
            Console.WriteLine("=========================================\n");

            if (!Directory.Exists(originalParambinsDir))
            {
                Console.WriteLine($"[ERROR] Input directory not found: {originalParambinsDir}");
                return;
            }

            string tempTsvDir = Path.Combine(baseWorkDir, "TempUnpackedTSVs");
            string tempRepackDir = Path.Combine(baseWorkDir, "TempRepackedBins");

            // Clean working directories if they exist, in theory this shouldn't matter... but be careful?
            if (Directory.Exists(tempTsvDir)) Directory.Delete(tempTsvDir, true);
            if (Directory.Exists(tempRepackDir)) Directory.Delete(tempRepackDir, true);

            Directory.CreateDirectory(tempTsvDir);
            Directory.CreateDirectory(tempRepackDir);

            var unpacker = new Unpacker();
            var repacker = new Repacker();

            string[] allParambinFiles = Directory.GetFiles(originalParambinsDir, "*.parambin", SearchOption.TopDirectoryOnly);

            int passCount = 0;
            int failCount = 0;
            var failureLogs = new List<string>();

            Console.WriteLine($"Found {allParambinFiles.Length} .parambin files to test.\n");

            foreach (string originalFile in allParambinFiles)
            {
                string fileName = Path.GetFileName(originalFile);
                string specificTsvDir = Path.Combine(tempTsvDir, Path.GetFileNameWithoutExtension(fileName));
                string repackedFile = Path.Combine(tempRepackDir, fileName);

                Console.Write($"Testing {fileName}... ");

                try
                {
                    unpacker.Unpack(originalFile, specificTsvDir);

                    string actualTsvDir = specificTsvDir;
                    string[] generatedTsvs = Directory.GetFiles(specificTsvDir, "*.tsv", SearchOption.AllDirectories);

                    if (generatedTsvs.Length > 0)
                    {
                        actualTsvDir = Path.GetDirectoryName(generatedTsvs[0]);
                    }

                    repacker.Repack(originalFile, actualTsvDir, tempRepackDir);

                    string diffResult = CompareBinaryFiles(originalFile, repackedFile);

                    if (diffResult == "MATCH")
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("1:1 PERFECT MATCH");
                        Console.ResetColor();
                        passCount++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("MISMATCH!");
                        Console.ResetColor();
                        failureLogs.Add($"[{fileName}] {diffResult}");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("EXCEPTION!");
                    Console.ResetColor();
                    failureLogs.Add($"[{fileName}] Exception during processing: {ex.Message}");
                    failCount++;
                }
            }

            Console.WriteLine("\n=========================================");
            Console.WriteLine(" Validation Report");
            Console.WriteLine("=========================================");
            Console.WriteLine($" Total Files Tested: {allParambinFiles.Length}");
            Console.WriteLine($" Passed (1:1 Match): {passCount}");
            Console.WriteLine($" Failed (Mismatch):  {failCount}");

            if (failCount > 0)
            {
                Console.WriteLine("\n--- Failure Details ---");
                foreach (var log in failureLogs)
                {
                    Console.WriteLine(log);
                }
            }
            Console.WriteLine("=========================================");
        }

        private static string CompareBinaryFiles(
            string originalFile,
            string repackedFile)
        {
            if (!File.Exists(originalFile) || !File.Exists(repackedFile))
                return "One or both files missing.";

            using var fs1 = new FileStream(originalFile, FileMode.Open, FileAccess.Read);
            using var fs2 = new FileStream(repackedFile, FileMode.Open, FileAccess.Read);

            if (fs1.Length != fs2.Length)
            {
                return $"File size mismatch. Original: {fs1.Length} bytes | Repacked: {fs2.Length} bytes.";
            }

            long offset = 0;

            while (true)
            {
                int b1 = fs1.ReadByte();
                int b2 = fs2.ReadByte();

                if (b1 == -1)
                    break;

                if (b1 != b2)
                {
                    using var reader = new ParambinReader(originalFile);

                    var header = reader.ReadHeader();
                    var tables = reader.ReadAllTables(header);

                    TableDebugReport.Report(
                        Path.GetFileName(originalFile),
                        tables,
                        offset);

                    return $"Mismatch @ 0x{offset:X8} (Original {b1:X2} Repacked {b2:X2})";
                }

                offset++;
            }

            return "MATCH";
        }
    }
}