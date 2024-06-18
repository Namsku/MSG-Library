using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace MsgTool
{
    public static class MsgConverter
    {
        public static Dictionary<string, object> BuildmhriceJson(MSG msg)
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
                    { "guid", entry.GUID.ToString() },
                    { "crc?", entry.CRC },
                    { "hash", MSG.IsVersionEntryByHash(msg.Version) ? entry.Hash : 0xFFFFFFFF },
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
        public static void ExportJson(MSG msg, string filename)
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

        public static MSG ImportJson(MSG msgObj, string filename)
        {
            // Read JSON file content
            string jsonString = File.ReadAllText(filename);

            // Parse JSON into a JObject
            JObject mhriceJson = JObject.Parse(jsonString);

            // Initialize new MSG object
            var msg = new MSG
            {
                Version = (int)mhriceJson["version"],
                Languages = new List<int>(),
                AttributeHeaders = new List<AttributeHeader>(),
                Entries = new List<MsgEntry>()
            };

            // Check if "entries" exists and is not null
            if (mhriceJson["entries"] != null && mhriceJson["entries"].Count() > 0)
            {
                // Check if "content" exists and is not null
                if (mhriceJson["entries"][0]["content"] != null)
                {
                    msg.Languages = Enumerable.Range(0, mhriceJson["entries"][0]["content"].Count()).ToList();
                }
            }
            else
            {
                // Check if the version exists in VERSION_2_LANG_COUNT
                if (Constants.VERSION_2_LANG_COUNT.ContainsKey(msg.Version))
                {
                    msg.Languages = Enumerable.Range(0, Constants.VERSION_2_LANG_COUNT[msg.Version]).ToList();
                }
            }

            // Replace Attribute Headers
            if (mhriceJson["attribute_headers"] != null)
            {
                foreach (var head in mhriceJson["attribute_headers"])
                {
                    msg.AttributeHeaders.Add(new AttributeHeader((string)head["name"], (int)head["ty"]));
                }
            }

            // Process entries
            if (mhriceJson["entries"] != null)
            {
                foreach (var jEntry in mhriceJson["entries"])
                {
                    var entry = new MsgEntry(msg.Version);
                    var attributes = new List<object>();
                    var langs = new List<string>();

                    if (jEntry["attributes"] != null)
                    {
                        foreach (var attr in jEntry["attributes"])
                        {
                            attributes.Add(ReadAttributeFromStr(attr.First.ToString(), msg.AttributeHeaders[entry.Attributes.Count].ValueType));
                        }
                    }

                    if (jEntry["content"] != null)
                    {
                        foreach (var content in jEntry["content"])
                        {
                            langs.Add(Helper.ForceWindowsLineBreak(content.ToString()));
                        }
                    }

                    entry.BuildEntry(
                        jEntry["guid"]?.ToString(),
                        (uint?)jEntry["crc?"] ?? 0,
                        jEntry["name"]?.ToString(),
                        attributes,
                        langs,
                        (MSG.IsVersionEntryByHash(msg.Version) ? (int)MMH3.Hash32(jEntry["name"]?.ToString(), -1) : 0),
                        (MSG.IsVersionEntryByHash(msg.Version) ? 0 : (jEntry["index"]?.ToObject<int>() ?? 0))
                    );

                    msg.Entries.Add(entry);
                }
            }

            return msg;
        }
    }
}
