using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace ForspokenBinTool.IO
{
    public static class BinaryReaderExtensions
    {
        private static readonly Dictionary<string, string> OpcodeMap;
        private static readonly Dictionary<string, string> ReverseOpcodeMap;

        static BinaryReaderExtensions()
        {
            OpcodeMap = new Dictionary<string, string>();
            ReverseOpcodeMap = new Dictionary<string, string>();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ForspokenOpcodes.json");

            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (map == null)
                    return;

                OpcodeMap = map;

                var duplicateNames = new HashSet<string>();

                foreach (var kvp in OpcodeMap)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        throw new Exception($"Opcode '{kvp.Key}' has an empty name.");
                    }

                    if (!kvp.Value.StartsWith("<") || !kvp.Value.EndsWith(">"))
                    {
                        throw new Exception($"Opcode '{kvp.Key}' has invalid tag format '{kvp.Value}'.");
                    }

                    if (!ReverseOpcodeMap.TryAdd(kvp.Value, kvp.Key))
                    {
                        duplicateNames.Add(kvp.Value);
                    }
                }

                if (duplicateNames.Count > 0)
                {
                    throw new Exception(
                        "Duplicate opcode names found:\n" +
                        string.Join("\n", duplicateNames.OrderBy(x => x))
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("ForspokenOpcodes.json contains errors:");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Environment.Exit(1);
            }
        }

        public static string ReadGameStringAt(this BinaryReader br, uint offset)
        {
            long returnPos = br.BaseStream.Position;

            br.BaseStream.Seek(offset, SeekOrigin.Begin);

            using var ms = new MemoryStream();

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                byte b = br.ReadByte();

                if (b == 0x00)
                    break;

                ms.WriteByte(b);
            }

            br.BaseStream.Seek(returnPos, SeekOrigin.Begin);

            byte[] raw = ms.ToArray();

            return DecodeGameString(raw);
        }

        public static string DecodeGameString(byte[] raw)
        {
            var sb = new StringBuilder();
            var textBytes = new List<byte>();

            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == 0x01 && i + 1 < raw.Length)
                {
                    byte payloadLength = raw[i + 1];
                    if (i + 1 + payloadLength < raw.Length)
                    {
                        if (textBytes.Count > 0)
                        {
                            sb.Append(Encoding.UTF8.GetString(textBytes.ToArray()));
                            textBytes.Clear();
                        }
                        var hexBuilder = new StringBuilder();
                        hexBuilder.Append("01");
                        hexBuilder.Append(payloadLength.ToString("X2"));
                        for (int p = 0; p < payloadLength; p++)
                        {
                            hexBuilder.Append(raw[i + 2 + p].ToString("X2"));
                        }
                        string fullHex = hexBuilder.ToString();

                        if (OpcodeMap.TryGetValue(fullHex, out string friendlyName))
                        {
                            sb.Append(friendlyName);
                        }
                        else
                        {
                            sb.Append($"<OP:{fullHex}>");
                        }

                        i += 1 + payloadLength;
                        continue;
                    }
                }
                textBytes.Add(raw[i]);
            }
            if (textBytes.Count > 0)
            {
                sb.Append(Encoding.UTF8.GetString(textBytes.ToArray()));
            }

            return sb.ToString();
        }

        public static byte[] EncodeGameString(string text)
        {
            using var ms = new MemoryStream();
            int lastIndex = 0;
            var matches = Regex.Matches(text, @"<([^>]+)>");

            foreach (Match match in matches)
            {
                string normalText = text.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(normalText))
                {
                    byte[] tBytes = Encoding.UTF8.GetBytes(normalText);
                    ms.Write(tBytes, 0, tBytes.Length);
                }

                string tag = match.Groups[1].Value;
                string hexCode = null;

                if (tag.StartsWith("OP:"))
                {
                    hexCode = tag.Substring(3);
                }
                else if (ReverseOpcodeMap.TryGetValue("<" + tag + ">", out string mappedHex))
                {
                    hexCode = mappedHex;
                }

                if (hexCode != null)
                {
                    byte[] opcodeBytes = ConvertHexString(hexCode);
                    ms.Write(opcodeBytes, 0, opcodeBytes.Length);
                }
                else
                {
                    byte[] rawBytes = Encoding.UTF8.GetBytes(match.Value);
                    ms.Write(rawBytes, 0, rawBytes.Length);
                }

                lastIndex = match.Index + match.Length;
            }

            string remainingText = text.Substring(lastIndex);
            if (!string.IsNullOrEmpty(remainingText))
            {
                byte[] rBytes = Encoding.UTF8.GetBytes(remainingText);
                ms.Write(rBytes, 0, rBytes.Length);
            }

            return ms.ToArray();
        }

        private static byte[] ConvertHexString(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new Exception($"Invalid opcode hex length: {hex}");

            byte[] result = new byte[hex.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return result;
        }
    }
}