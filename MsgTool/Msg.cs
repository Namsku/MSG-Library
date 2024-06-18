using System.Text;

namespace MsgTool
{
    public class MSG
    {
        private StringPool _stringPool;

        public List<MsgEntry> Entries { get; set; }
        public List<AttributeHeader> AttributeHeaders { get; set; }
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
                AttributeHeaders = new List<AttributeHeader>();
                for (int i = 0; i < attributeCount; i++)
                {
                    AttributeHeaders.Add(new AttributeHeader("", reader.ReadInt32()));
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
                _stringPool = new StringPool(reader.ReadBytes((int)dataSize), encrypted: IsVersionEncrypt(Version));

                // Read attribute names
                for (int i = 0; i < attributeCount; i++)
                {
                    var name = _stringPool.Find(attributeNamesOffsets[i] - dataOffset);
                    AttributeHeaders[i] = AttributeHeaders[i].WithName(name);
                }

                // Read content of each entry
                for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
                {
                    var entry = Entries[entryIndex];
                    var name = _stringPool.Find(entry.EntryNameOffset - (long)dataOffset);
                    entry.SetName(name);
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
                        langContents.Add(_stringPool.Find(strOffset - (long)dataOffset));
                    }
                    entry.SetContent(langContents);

                    // Seek string value of each attribute
                    for (int i = 0; i < attributeCount; i++)
                    {
                        var attrHead = AttributeHeaders[i];
                        if (attrHead.ValueType == AttributeKinds.Wstring)
                        {
                            entry.Attributes[i] = _stringPool.Find((long)entry.Attributes[i] - (long)dataOffset);
                        }
                        else if (attrHead.ValueType == AttributeKinds.Null)
                        {
                            var temp = _stringPool.Find((long)entry.Attributes[i] - (long)dataOffset);
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
                writer.Write((uint)Version);
                writer.Write(Encoding.ASCII.GetBytes("GMSG"));
                writer.Write((ulong)16);

                int entryCount = Entries.Count;
                writer.Write(entryCount);

                int attributeCount = AttributeHeaders.Count;
                writer.Write(attributeCount);

                int langCount = Languages.Count;
                writer.Write(langCount);

                writer.Write(new byte[8 - (memoryStream.Length % 8)]); // pad to 8 bytes

                long dataOffsetPH = 0;

                if (IsVersionEncrypt(Version))
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
                foreach (var entry in Entries)
                {
                    entryOffsetsPH.Add(memoryStream.Length);
                    writer.Write((long)-1);
                }

                // Update unknData offset
                writer.Seek((int)unknDataOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                writer.Write((ulong)0); // unknData

                // Write languages
                writer.Seek((int)langOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                foreach (var lang in Languages)
                {
                    writer.Write(lang);
                }

                // writer.Write(new byte[8 - (memoryStream.Length % 8)]); // pad to 8 bytes

                // Write attribute headers
                writer.Seek((int)attributeOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                foreach (var attributeHeader in AttributeHeaders)
                {
                    writer.Write(attributeHeader.ValueType);
                }

                // writer.Write(new byte[8 - (memoryStream.Length % 8)]); // pad to 8 bytes

                // Write attribute names offset
                writer.Seek((int)attributeNameOffsetPH, SeekOrigin.Begin);
                writer.Write((ulong)memoryStream.Length);

                writer.Seek(0, SeekOrigin.End);
                List<long> attributeNamesOffsetsPH = new List<long>();
                foreach (var attributeHeader in AttributeHeaders)
                {
                    attributeNamesOffsetsPH.Add(memoryStream.Length);
                    writer.Write((long)-1);
                }

                // Write info(entry head) of each entry
                foreach (var entry in Entries)
                {
                    writer.BaseStream.Seek(entryOffsetsPH[Entries.IndexOf(entry)], SeekOrigin.Begin);
                    writer.Write((ulong)memoryStream.Length);

                    writer.Seek(0, SeekOrigin.End);
                    entry.WriteHead(writer);
                }

                // Write attributes of each entry
                foreach (var entry in Entries)
                {
                    writer.BaseStream.Seek(entry.AttributeOffsetPH, SeekOrigin.Begin);
                    writer.Write((ulong)memoryStream.Length);

                    entry.WriteAttributes(writer, AttributeHeaders);
                }

                // Read / decrypt string pool
                long dataOffset = memoryStream.Length;
                if (IsVersionEncrypt(Version))
                {
                    writer.Seek((int)dataOffsetPH, SeekOrigin.Begin);
                    writer.Write((ulong)memoryStream.Length);
                }

                writer.Seek(0, SeekOrigin.End);

                // Construct string pool
                var stringPoolSet = new HashSet<string>();
                var isStrAttrIdx = new List<int>();
                var isNullAttrIdx = new List<int>();

                var i = 0;
                foreach (var attributeHeader in AttributeHeaders)
                {
                    if (attributeHeader.ValueType == AttributeKinds.Null)
                    {
                        stringPoolSet.Add("");
                        isNullAttrIdx.Add(AttributeHeaders.IndexOf(attributeHeader));
                    }
                    else if (attributeHeader.ValueType == AttributeKinds.Wstring)
                    {
                        isStrAttrIdx.Add(AttributeHeaders.IndexOf(attributeHeader));
                    }
                }

                foreach (var attributeHeader in AttributeHeaders)
                {
                    stringPoolSet.Add(attributeHeader.Name!);
                }

                foreach (var entry in Entries)
                {
                    stringPoolSet.Add(entry.Name);

                    foreach (var lang in Languages)
                    {
                        stringPoolSet.Add(entry.Langs[lang]);
                    }

                    foreach (var attributeIdx in isStrAttrIdx)
                    {
                        stringPoolSet.Add(entry.Attributes[attributeIdx].ToString());
                    }
                }

                var stringPool = new StringPool(stringPoolSet);
                // var stringPool = _stringPool;
                if (IsVersionEncrypt(Version))
                {
                    writer.Write(stringPool.Encryped);
                }
                else
                {
                    writer.Write(stringPool.Unencryped);
                }

                // Update string offsets
                foreach (var attributeHeader in AttributeHeaders)
                {
                    writer.BaseStream.Seek(attributeNamesOffsetsPH[AttributeHeaders.IndexOf(attributeHeader)], SeekOrigin.Begin);
                    writer.Write((ulong)(stringPool.FindOffset(attributeHeader.Name!) + dataOffset));
                }

                foreach (var entry in Entries)
                {
                    long offset = entry.EntryNameOffsetPH;
                    writer.Seek((int)offset, SeekOrigin.Begin);
                    writer.Write((ulong)(stringPool.FindOffset(entry.Name) + dataOffset));

                    foreach (var lang in Languages)
                    {
                        offset = entry.ContentOffsetsByLangsPH[lang];
                        writer.Seek((int)offset, SeekOrigin.Begin);
                        writer.Write((ulong)(stringPool.FindOffset(entry.Langs[lang]) + dataOffset));
                    }

                    foreach (var idx in isStrAttrIdx)
                    {
                        offset = entry.AttributesPH[idx];
                        writer.Seek((int)offset, SeekOrigin.Begin);
                        writer.Write((ulong)(stringPool.FindOffset(entry.Attributes[idx].ToString()) + dataOffset));
                    }

                    foreach (var idx in isNullAttrIdx)
                    {
                        offset = entry.AttributesPH[idx];
                        writer.Seek((int)offset, SeekOrigin.Begin);
                        writer.Write((ulong)(stringPool.FindOffset("") + dataOffset));
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
