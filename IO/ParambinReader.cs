using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.IO
{
    public class ParambinReader : IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly long _baseOffset = 0x20;
        private readonly long _fileLength;

        public ParambinReader(string filePath)
        {
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _reader = new BinaryReader(stream, Encoding.UTF8);
            _fileLength = _reader.BaseStream.Length;
        }

        public ArchiveHeader ReadHeader()
        {
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);

            var header = new ArchiveHeader();

            byte[] idBytes = _reader.ReadBytes(12);
            header.Identifier = Encoding.UTF8.GetString(idBytes).TrimEnd('\0');

            header.Size = _reader.ReadInt32();

            header.ResourceId.Type = _reader.ReadUInt32();
            header.ResourceId.Primary = _reader.ReadUInt32();
            header.ResourceId.Secondary = _reader.ReadUInt32();
            header.ResourceId.Flag = _reader.ReadUInt32();

            _reader.BaseStream.Seek(_baseOffset, SeekOrigin.Begin);

            header.Version = _reader.ReadUInt32();
            header.TableCount = _reader.ReadUInt32();

            header.TableOffsets = new uint[header.TableCount];

            for (int i = 0; i < header.TableCount; i++)
            {
                header.TableOffsets[i] = _reader.ReadUInt32();
            }

            return header;
        }

        public List<ParamTable> ReadAllTables(ArchiveHeader header)
        {
            var tables = new List<ParamTable>();

            for (int i = 0; i < header.TableOffsets.Length; i++)
            {
                long tableOffset = _baseOffset + header.TableOffsets[i];

                long nextTableOffset;

                if (i < header.TableOffsets.Length - 1)
                {
                    nextTableOffset = _baseOffset + header.TableOffsets[i + 1];
                }
                else
                {
                    nextTableOffset = _fileLength;
                }

                _reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);

                tables.Add(ReadTable(nextTableOffset));
            }

            return tables;
        }

        private ParamTable ReadTable(long nextTableOffset)
        {
            var table = new ParamTable();

            table.TableStartOffset = _reader.BaseStream.Position;
            table.OffsetBase = table.TableStartOffset + 32;

            table.TableId = _reader.ReadUInt32();

            table.TagCount = _reader.ReadUInt32();
            table.ElementCount = _reader.ReadUInt32();
            table.ElementSize = _reader.ReadUInt32();

            table.BooleanOffset = _reader.ReadUInt32();
            table.ArrayOffset = _reader.ReadUInt32();
            table.StringOffset = _reader.ReadUInt32();

            _reader.ReadInt32();

            for (int i = 0; i < table.TagCount; i++)
            {
                table.Tags.Add(new ParamTag
                {
                    Id = _reader.ReadUInt32(),
                    EngineFlag = _reader.ReadUInt16(),
                    Offset = _reader.ReadUInt16()
                });
            }

            uint valueCount = table.ElementSize >= 4
                ? (table.ElementSize / 4) - 1
                : 0;

            for (int e = 0; e < table.ElementCount; e++)
            {
                var element = new ParamElement
                {
                    Id = _reader.ReadUInt32()
                };

                for (int v = 0; v < valueCount; v++)
                {
                    element.Values.Add(_reader.ReadUInt32());
                }

                table.Elements.Add(element);
            }

            table.ArrayHeapBase = table.OffsetBase + table.ArrayOffset;
            table.ArrayHeapSize = Math.Max(0, table.StringOffset - table.ArrayOffset);

            table.StringHeapBase = table.OffsetBase + table.StringOffset;
            table.StringHeapSize = Math.Max(0, nextTableOffset - table.StringHeapBase);

            if (table.ArrayHeapSize > 0)
            {
                _reader.BaseStream.Seek(table.ArrayHeapBase, SeekOrigin.Begin);
                table.ArrayHeapRaw = _reader.ReadBytes((int)table.ArrayHeapSize);
            }

            if (table.StringHeapSize > 0)
            {
                _reader.BaseStream.Seek(table.StringHeapBase, SeekOrigin.Begin);
                table.StringHeapRaw = _reader.ReadBytes((int)table.StringHeapSize);
            }

            return table;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
} 