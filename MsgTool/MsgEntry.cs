using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsgTool
{
    public class MsgEntry
    {
        public Guid GUID { get; set; }
        public uint CRC { get; set; }
        public int? Hash { get; set; }
        public int? Index { get; set; }
        public long EntryNameOffset { get; set; }
        public long AttributeOffset { get; set; }
        public List<long> ContentOffsetsByLangs { get; set; }
        public List<object> Attributes { get; set; }
        public List<int> AttributesPH { get; set; }
        public string Name { get; set; }
        public List<string> Langs { get; set; }
        public int AttributeOffsetPH { get; set; }
        public int EntryNameOffsetPH { get; set; }
        public List<int> ContentOffsetsByLangsPH { get; set; }

        private int _version;

        public bool IsVersionEncrypt(int version)
        {
            return version > 12 && version != 0x2022033D;
        }

        public bool IsVersionEntryByHash(int version)
        {
            return version > 15 && version != 0x2022033D;
        }

        public MsgEntry(int version)
        {
            _version = version;
        }

        public MsgEntry() { }
        public void ReadHead(BinaryReader filestream, int langCount)
        {
            GUID = new Guid(filestream.ReadBytes(16));
            CRC = (uint) filestream.ReadInt32();

            if (IsVersionEntryByHash(_version))
            {
                Hash = filestream.ReadInt32();
            }
            else
            {
                Index = filestream.ReadInt32();
            }

            EntryNameOffset = filestream.ReadInt64();
            AttributeOffset = filestream.ReadInt64();

            ContentOffsetsByLangs = new List<long>();
            for (int i = 0; i < langCount; i++)
            {
                ContentOffsetsByLangs.Add(filestream.ReadInt64());
            }
        }

        public void WriteHead(BinaryWriter writer)
        {
            writer.Seek(0, SeekOrigin.End);
            writer.Write(GUID.ToByteArray());
            writer.Write(CRC);

            if (IsVersionEntryByHash(_version))
            {
                writer.Write(Hash ?? 0);
            }
            else
            {
                writer.Write(Hash ?? 0);
            }

            EntryNameOffsetPH = (int)writer.BaseStream.Position;
            writer.Write((long)-1);

            AttributeOffsetPH = (int)writer.BaseStream.Position;
            writer.Write((long)-1);

            ContentOffsetsByLangsPH = new List<int>();
            foreach (var _ in Langs)
            {
                ContentOffsetsByLangsPH.Add((int)writer.BaseStream.Position);
                writer.Write((long)-1);
            }
        }

        public void ReadAttributes(BinaryReader filestream, List<Dictionary<string, object>> attributeHeaders)
        {
            this.Attributes = new List<object>();
            foreach (var header in attributeHeaders)
            {
                long value = 0;
                switch (header["valueType"])
                {
                    case -1:  // null wstring
                        value = filestream.ReadInt64();
                        break;
                    case 0:  // int64
                        value = filestream.ReadInt64();
                        break;
                    case 1:  // double
                        value = BitConverter.ToInt64(filestream.ReadBytes(8), 0);
                        break;
                    case 2:  // wstring
                        value = filestream.ReadInt64();
                        break;
                    default:
                        throw new NotImplementedException($"{value} not implemented");
                }
                Attributes.Add(value);
            }
        }

        public void WriteAttributes(BinaryWriter writer, List<Dictionary<string, object>> attributeHeaders)
        {
            AttributesPH = new List<int>();

            for (int i = 0; i < attributeHeaders.Count; i++)
            {
                switch ((int)attributeHeaders[i]["valueType"])
                {
                    case -1: // null wstring
                        writer.Write((long)-1);
                        break;
                    case 0: // int64
                        writer.Write((long)Attributes[i]);
                        break;
                    case 1: // double
                        writer.Write((double)Attributes[i]);
                        break;
                    case 2: // wstring
                        writer.Write((long)-1);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported valueType.");
                }

                AttributesPH.Add((int) writer.BaseStream.Length);
            }
        }


        public void SetName(string name)
        {
            Name = name;
        }

        public void SetContent(List<string> langs)
        {
            Langs = langs;
        }

        public void BuildEntry(string guid, uint crc, string name, List<object> attributeValues, List<string> langs, int hash = 0, int index = 0)
        {
            GUID = new Guid(guid);
            CRC = crc;

            if (IsVersionEntryByHash(_version))
            {
                Hash = hash;
            }
            else
            {
                Index = index;
            }

            Name = name;
            Attributes = new List<object>(attributeValues);
            Langs = langs;
        }

    }

}
