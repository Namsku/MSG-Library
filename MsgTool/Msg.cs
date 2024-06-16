using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsgTool
{
    public class MSG
    {
        public List<MsgEntry> Entries { get; set; }
        public List<Dictionary<string, object>> AttributeHeaders { get; set; }
        public int Version { get; set; }
        public List<int> Languages { get; set; }

        public MSG() { }

        public void ReadMSG(Stream filestream)
        {
            using (var reader = new BinaryReader(filestream, Encoding.UTF8, true))
            {
                // Header
                Version = reader.ReadInt32();
                string magic = Encoding.UTF8.GetString(reader.ReadBytes(4));
                ulong headerOffset = reader.ReadUInt64();
                int entryCount = reader.ReadInt32();
                int attributeCount = reader.ReadInt32();
                int langCount = reader.ReadInt32();
                PadAlignUp(reader, 8);

                ulong dataOffset = 0;
                if (IsVersionEncrypt(Version))
                {
                    dataOffset = reader.ReadUInt64();
                }

                ulong unknDataOffset = reader.ReadUInt64();
                ulong langOffset = reader.ReadUInt64();
                ulong attributeOffset = reader.ReadUInt64();
                ulong attributeNameOffset = reader.ReadUInt64();

                // entries headers' offset
                List<ulong> entryOffsets = new List<ulong>();
                for (int i = 0; i < entryCount; i++)
                {
                    entryOffsets.Add(reader.ReadUInt64());
                }

                // always 64bit null
                if (unknDataOffset != (ulong)reader.BaseStream.Position)
                    throw new InvalidDataException($"Expected unknData at {unknDataOffset} but at {reader.BaseStream.Position}");
                ulong unknData = reader.ReadUInt64();
                if (unknData != 0)
                    throw new InvalidDataException($"unknData should be 0 but found {unknData}");

                // indexes of all lang (follow via.Language)
                if (langOffset != (ulong)reader.BaseStream.Position)
                    throw new InvalidDataException($"Expected languages at {langOffset} but at {reader.BaseStream.Position}");
                Languages = new List<int>();
                for (int i = 0; i < langCount; i++)
                {
                    Languages.Add(reader.ReadInt32());
                }
                if (!Languages.All(lang => Constants.LANG_LIST.ContainsKey(lang) && Languages.IndexOf(lang) == lang))
                {
                    Console.WriteLine($"unkn lang found. {string.Join(", ", Languages)}. Please update LANG_LIST from via.Language");
                }

                // Pad to 8
                PadAlignUp(reader, 8);

                // get attribute headers, get type of each attr
                if (attributeOffset != (ulong)reader.BaseStream.Position)
                    throw new InvalidDataException($"Expected attributeValueTypes at {attributeOffset} but at {reader.BaseStream.Position}");
                AttributeHeaders = new List<Dictionary<string, object>>();
                for (int i = 0; i < attributeCount; i++)
                {
                    AttributeHeaders.Add(new Dictionary<string, object> { { "valueType", reader.ReadInt32() } });
                }

                // Pad to 8
                PadAlignUp(reader, 8);

                // Attribute names offsets
                if (attributeNameOffset != (ulong)reader.BaseStream.Position)
                    throw new InvalidDataException($"Expected attributeNamesOffset at {attributeNameOffset} but at {reader.BaseStream.Position}");
                List<ulong> attributeNamesOffsets = new List<ulong>();
                for (int i = 0; i < attributeCount; i++)
                {
                    attributeNamesOffsets.Add(reader.ReadUInt64());
                }

                // Read entry headers
                Entries = new List<MsgEntry>();
                for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
                {
                    if (entryOffsets[entryIndex] != (ulong)reader.BaseStream.Position)
                        throw new InvalidDataException($"Expected entryOffsets[{entryIndex}] at {entryOffsets[entryIndex]} but at {reader.BaseStream.Position}");
                    var entry = new MsgEntry(Version);
                    entry.ReadHead(reader, langCount);
                    Entries.Add(entry);
                }

                // Read attributes of each entry
                foreach (var entry in Entries)
                {
                    if (entry.AttributeOffset != (long)reader.BaseStream.Position)
                        throw new InvalidDataException($"Expected entry.attributeOffset at {entry.AttributeOffset} but at {reader.BaseStream.Position}");
                    entry.ReadAttributes(reader, AttributeHeaders);
                }

                // Read/decrypt string pool
                if (IsVersionEncrypt(Version))
                {
                    if (dataOffset != (ulong)reader.BaseStream.Position)
                        throw new InvalidDataException($"Expected dataOffset at {dataOffset} but at {reader.BaseStream.Position}");
                }
                else
                {
                    dataOffset = (ulong)reader.BaseStream.Position;
                }
                reader.BaseStream.Seek(0, SeekOrigin.End);
                long dataSize = reader.BaseStream.Position - (long)dataOffset;
                if (dataSize % 2 != 0)
                    throw new InvalidDataException($"wstring pool size should be even: {dataSize}");
                reader.BaseStream.Seek((long)dataOffset, SeekOrigin.Begin);
                byte[] data = reader.ReadBytes((int)dataSize);
                byte[] wcharPool = IsVersionEncrypt(Version) ? Helper.Decrypt(data) : data;
                var stringDict = Helper.WcharPoolToStrDict(wcharPool);

                // Read attribute names
                for (int i = 0; i < attributeCount; i++)
                {
                    AttributeHeaders[i]["name"] = Helper.SeekString((int)(attributeNamesOffsets[i] - dataOffset), stringDict);
                }

                // Read content of each entry
                for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
                {
                    var entry = Entries[entryIndex];
                    entry.SetName(Helper.SeekString((int)(entry.EntryNameOffset - (long)dataOffset), stringDict));
                    if (IsVersionEntryByHash(Version))
                    {
                        uint nameHash = MMH3.Hash32(entry.Name, -1);
                        if (nameHash != entry.Hash)
                            throw new InvalidDataException($"Expected {entry.Hash} for {entry.Name} but got {nameHash}");
                    }
                    else
                    {
                        if (entryIndex != entry.Index)
                            throw new InvalidDataException($"Expected {entryIndex} for {entry.Name} but got {entry.Index}");
                    }

                    // Set content by each language
                    var langContents = new List<string>();
                    foreach (var strOffset in entry.ContentOffsetsByLangs)
                    {
                        langContents.Add(Helper.SeekString((int)(strOffset - (long)dataOffset), stringDict));
                    }
                    entry.SetContent(langContents);

                    // Seek string value of each attribute
                    for (int i = 0; i < attributeCount; i++)
                    {
                        var attrHead = AttributeHeaders[i];
                        if ((int)attrHead["valueType"] == 2)
                        {
                            entry.Attributes[i] = Helper.SeekString((int)((long)entry.Attributes[i] - (long)dataOffset), stringDict);
                        }
                        else if ((int)attrHead["valueType"] == -1)
                        {
                            var temp = Helper.SeekString((int)((long)entry.Attributes[i] - (long)dataOffset), stringDict);
                            if (!string.IsNullOrEmpty(temp) && temp != "\x00")
                                throw new InvalidDataException($"attr value type -1 contain non-null value {temp}");
                            entry.Attributes[i] = temp;
                        }
                    }
                }
            }
        }

        public byte[] WriteMSG()
        {
            // Initialize memory stream and binary writer
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8))
            {
                // Header
                writer.Write((uint)this.Version);
                writer.Write(Encoding.ASCII.GetBytes("GMSG"));
                writer.Write((ulong)16);

                int entryCount = this.Entries.Count;
                writer.Write(entryCount);

                int attributeCount = this.AttributeHeaders.Count;
                writer.Write(attributeCount);

                int langCount = this.Languages.Count;
                writer.Write(langCount);

                writer.Write(new byte[8 - (memoryStream.Length % 8)]); // pad to 8 bytes

                long dataOffsetPH = 0;

                if (IsVersionEncrypt(this.Version))
                {
                    dataOffsetPH = memoryStream.Length;
                    writer.Write((long)-1);
                }

                long unknDataOffsetPH = memoryStream.Length;
                writer.Write((long)-1);

                long langOffsetPH = memoryStream.Length;
                writer.Write((long)-1);

                long attributeOffsetPH = memoryStream.Length;
                writer.Write((long)-1);

                long attributeNameOffsetPH = memoryStream.Length;
                writer.Write((long)-1);

                // Write entries headers' offset
                List<long> entryOffsetsPH = new List<long>();
                foreach (var entry in this.Entries)
                {
                    entryOffsetsPH.Add(memoryStream.Length);
                    writer.Write((long)-1);
                }

                // Update unknData offset
                writer.Seek((int)unknDataOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                writer.Write((ulong)0); // unknData

                File.WriteAllBytes("testings.bin", memoryStream.ToArray());

                // Write languages
                writer.Seek((int)langOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                foreach (var lang in this.Languages)
                {
                    writer.Write(lang);
                }


                File.WriteAllBytes("testings.bin", memoryStream.ToArray());

                // writer.Write(new byte[8 - (memoryStream.Length % 8)]); // pad to 8 bytes

                // Write attribute headers
                writer.Seek((int)attributeOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                foreach (var attributeHeader in this.AttributeHeaders)
                {
                    writer.Write((int)attributeHeader["valueType"]);
                }


                File.WriteAllBytes("testings.bin", memoryStream.ToArray());

                // writer.Write(new byte[8 - (memoryStream.Length % 8)]); // pad to 8 bytes

                // Write attribute names offset
                writer.Seek((int)attributeNameOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                List<long> attributeNamesOffsetsPH = new List<long>();
                foreach (var attributeHeader in this.AttributeHeaders)
                {
                    attributeNamesOffsetsPH.Add(memoryStream.Length);
                    writer.Write((long)-1);
                }

                File.WriteAllBytes("testings.bin", memoryStream.ToArray());

                // Write info(entry head) of each entry
                foreach (var entry in this.Entries)
                {
                    writer.BaseStream.Seek(entryOffsetsPH[this.Entries.IndexOf(entry)], SeekOrigin.Begin);
                    writer.Write((ulong)memoryStream.Length);

                    writer.Seek(0, SeekOrigin.End);
                    entry.WriteHead(writer);
                }

                // Write attributes of each entry
                foreach (var entry in this.Entries)
                {
                    writer.BaseStream.Seek(entry.AttributeOffsetPH, SeekOrigin.Begin);
                    writer.Write((ulong)memoryStream.Length);

                    entry.WriteAttributes(writer, this.AttributeHeaders);
                }

                // Read / decrypt string pool
                long dataOffset = memoryStream.Length;
                if (IsVersionEncrypt(this.Version))
                {
                    writer.Seek((int)dataOffsetPH, SeekOrigin.Begin);
                    writer.Write((ulong)memoryStream.Length);
                }

                writer.Seek(0, SeekOrigin.End);

                File.WriteAllBytes("testings.bin", memoryStream.ToArray());

                // Construct string pool
                var stringPoolSet = new HashSet<string>();
                var isStrAttrIdx = new List<int>();
                var isNullAttrIdx = new List<int>();

                var i = 0;
                foreach (var attributeHeader in AttributeHeaders)
                {
                    if ((int)attributeHeader["valueType"] == -1)
                    {
                        stringPoolSet.Add("");
                        isNullAttrIdx.Add(AttributeHeaders.IndexOf(attributeHeader));
                    }
                    else if ((int)attributeHeader["valueType"] == 2)
                    {
                        isStrAttrIdx.Add(AttributeHeaders.IndexOf(attributeHeader));
                    }
                }

                foreach (var attributeHeader in AttributeHeaders)
                {
                    string name = attributeHeader["name"].ToString(); // Assuming "name" is stored as a string in the dictionary
                    stringPoolSet.Add(name);
                }

                foreach (var entry in this.Entries)
                {
                    stringPoolSet.Add(entry.Name);

                    foreach (var lang in this.Languages)
                    {
                        stringPoolSet.Add(entry.Langs[lang]);
                    }

                    foreach (var attributeIdx in isStrAttrIdx)
                    {
                        stringPoolSet.Add(entry.Attributes[attributeIdx].ToString());
                    }
                }

                var strOffsetDict = Helper.CalcStrPoolOffsets(stringPoolSet);
                var wcharPool = new List<byte>();
                foreach (var str in strOffsetDict.Keys)
                {
                    wcharPool.AddRange(Helper.ToWcharBytes(str));
                }

                if (IsVersionEncrypt(this.Version))
                {
                    writer.Write(Helper.Encrypt(wcharPool.ToArray()));
                }
                else
                {
                    writer.Write(wcharPool.ToArray());
                }

                // Update string offsets
                foreach (var attributeHeader in this.AttributeHeaders)
                {
                    writer.BaseStream.Seek(attributeNamesOffsetsPH[this.AttributeHeaders.IndexOf(attributeHeader)], SeekOrigin.Begin);
                    writer.Write((ulong)(strOffsetDict[attributeHeader["name"].ToString()] + dataOffset));
                }

                foreach (var entry in this.Entries)
                {
                    long offset = entry.EntryNameOffsetPH;
                    writer.Seek((int)offset, SeekOrigin.Begin);
                    writer.Write((ulong)(strOffsetDict[entry.Name] + dataOffset));

                    foreach (var lang in this.Languages)
                    {
                        offset = entry.ContentOffsetsByLangsPH[lang];
                        writer.Seek((int)offset, SeekOrigin.Begin);
                        writer.Write((ulong)(strOffsetDict[entry.Langs[lang]] + dataOffset));
                    }

                    foreach (var idx in isStrAttrIdx)
                    {
                        offset = entry.AttributesPH[idx];
                        writer.Seek((int)offset, SeekOrigin.Begin);
                        writer.Write((ulong)(strOffsetDict[entry.Attributes[idx].ToString()] + dataOffset));
                    }

                    foreach (var idx in isNullAttrIdx)
                    {
                        offset = entry.AttributesPH[idx];
                        writer.Seek((int)offset, SeekOrigin.Begin);
                        writer.Write((ulong)(strOffsetDict[""] + dataOffset));
                    }
                }

                return memoryStream.ToArray();
            }
        }


        private void PadAlignUp(BinaryReader reader, int alignment)
        {
            long pos = reader.BaseStream.Position;
            if (pos % alignment != 0)
            {
                reader.BaseStream.Seek(alignment - (pos % alignment), SeekOrigin.Current);
            }
        }

        public static bool IsVersionEncrypt(int version)
        {
            return version > 12 && version != 0x2022033D;
        }

        public static bool IsVersionEntryByHash(int version)
        {
            return version > 15 && version != 0x2022033D;
        }
    }
}
