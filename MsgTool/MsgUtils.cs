using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MsgTool
{
    public static class MsgConverter
    {
        public static Dictionary<string, object> BuildmhriceJson(Msg msg)
        {
            var infos = new Dictionary<string, object>
        {
            { "version", msg.Version },
            { "attribute_headers", msg.AttributeHeaders.Select(attr => new Dictionary<string, object>
                {
                    { "ty", attr.ValueType },
                    { "name", attr.Name! }
                }).ToList() },
            { "entries", msg.Entries.Select(entry => new Dictionary<string, object>
                {
                    { "name", entry.Name },
                    { "guid", entry.Guid.ToString() },
                    { "crc?", entry.CRC },
                    { "hash", Msg.IsVersionEntryByHash(msg.Version) ? entry.Hash : 0xFFFFFFFF },
                    { "attributes", msg.AttributeHeaders.Select((attrh, i) => new Dictionary<string, object>
                        {
                            { ValueTypeEnum(attrh.ValueType), entry.Attributes[i] }
                        }).ToList() },
                    { "content", msg.Languages.Select(lang => entry.Langs[lang]).ToList() }
                }).ToList()
            }
        };

            return infos;
        }
        public static string ValueTypeEnum(int ty)
        {
            switch (ty)
            {
                case -1:
                    return "Unknown";
                case 0:
                    return "Int";
                case 1:
                    return "Float";
                case 2:
                    return "String";
                default:
                    return "Unknown";
            }
        }
    }

    public static class MsgUtils
    {
        public static void ExportJson(Msg msg, string filename)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var infos = MsgConverter.BuildmhriceJson(msg);
            string jsonString = System.Text.Json.JsonSerializer.Serialize(infos, jsonOptions);

            File.WriteAllText(filename, jsonString);
        }


        public static object ReadAttributeFromStr(string inValue, int vtype)
        {
            return vtype switch
            {
                -1 => "",
                0 => int.Parse(inValue),
                1 => float.Parse(inValue),
                2 => inValue,
                _ => throw new ArgumentException("Invalid value type")
            };
        }

        public static Encoding GetEncoding(string filename, int bufferSize = 256 * 1024)
        {
            byte[] rawdata = new byte[bufferSize];
            int readBytes;

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                readBytes = fs.Read(rawdata, 0, bufferSize);
            }

            if (readBytes < bufferSize)
            {
                Array.Resize(ref rawdata, readBytes);
            }

            Encoding encoding = DetectEncoding(rawdata);
            return encoding;
        }

        private static Encoding DetectEncoding(byte[] data)
        {
            // Order of checking encoding matches common scenarios
            Encoding encoding = GetEncodingFromBytes(data, Encoding.UTF8);
            if (encoding == null) encoding = GetEncodingFromBytes(data, Encoding.Unicode);
            if (encoding == null) encoding = GetEncodingFromBytes(data, Encoding.UTF7);
            if (encoding == null) encoding = GetEncodingFromBytes(data, Encoding.ASCII);
            if (encoding == null) encoding = GetEncodingFromBytes(data, Encoding.GetEncoding("windows-1252")); // Latin 1

            if (encoding == null)
                encoding = Encoding.UTF8; // Default to UTF-8

            return encoding;
        }

        private static Encoding GetEncodingFromBytes(byte[] data, Encoding encoding)
        {
            try
            {
                string text = encoding.GetString(data);
                return encoding;
            }
            catch (DecoderFallbackException)
            {
                return null; // Encoding is not suitable for this data
            }
        }
    }
}
