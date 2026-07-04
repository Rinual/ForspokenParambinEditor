using System;
using System.Collections.Generic;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.Testing
{
    public static class TableDebugReport
    {
        public static void Report(
            string fileName,
            List<ParamTable> tables,
            long absoluteOffset)
        {
            Console.WriteLine();
            Console.WriteLine("====================================================");
            Console.WriteLine(fileName);
            Console.WriteLine($"Mismatch @ 0x{absoluteOffset:X8}");
            Console.WriteLine("====================================================");

            for (int i = 0; i < tables.Count; i++)
            {
                var t = tables[i];

                long tableEnd = t.StringHeapBase + t.StringHeapSize;

                if (absoluteOffset < t.TableStartOffset ||
                    absoluteOffset >= tableEnd)
                    continue;

                Console.WriteLine($"Table Index : {i}");
                Console.WriteLine($"Table ID    : {t.TableId}");
                Console.WriteLine();

                long tagStart = t.TableStartOffset + 32;
                long tagEnd = tagStart + (t.TagCount * 8);

                long elementStart = tagEnd;
                long elementEnd = elementStart + (t.ElementCount * t.ElementSize);

                if (absoluteOffset < tagEnd)
                {
                    Console.WriteLine("Region       : Tag Table");

                    long local = absoluteOffset - tagStart;

                    Console.WriteLine($"Tag Index    : {local / 8}");
                    return;
                }

                if (absoluteOffset < elementEnd)
                {
                    Console.WriteLine("Region       : Element Data");

                    long local = absoluteOffset - elementStart;

                    long elementIndex = local / t.ElementSize;
                    long byteInElement = local % t.ElementSize;

                    Console.WriteLine($"Element Index: {elementIndex}");

                    if (elementIndex < t.Elements.Count)
                        Console.WriteLine($"Element ID   : {t.Elements[(int)elementIndex].Id}");

                    if (byteInElement >= 4)
                    {
                        long valueIndex = (byteInElement - 4) / 4;

                        Console.WriteLine($"Value Index  : {valueIndex}");

                        if (valueIndex < t.Tags.Count)
                        {
                            Console.WriteLine($"Tag ID       : {t.Tags[(int)valueIndex].Id}");
                        }
                    }

                    return;
                }

                if (absoluteOffset < t.StringHeapBase)
                {
                    Console.WriteLine("Region       : Array Heap");
                    Console.WriteLine($"Array Offset : 0x{(absoluteOffset - t.ArrayHeapBase):X}");
                    return;
                }

                Console.WriteLine("Region       : String Heap");
                Console.WriteLine($"String Offset: 0x{(absoluteOffset - t.StringHeapBase):X}");
                return;
            }

            Console.WriteLine("Offset not inside any parsed table.");
        }
    }
}