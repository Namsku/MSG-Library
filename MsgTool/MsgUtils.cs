using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Newtonsoft.Json;
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
                    { "ty", attr["valueType"] },
                    { "name", attr["name"] }
                }).ToList() },
            { "entries", msg.Entries.Select(entry => new Dictionary<string, object>
                {
                    { "name", entry.Name },
                    { "guid", entry.GUID.ToString() },
                    { "crc?", entry.CRC },
                    { "hash", MSG.IsVersionEntryByHash(msg.Version) ? entry.Hash : 0xFFFFFFFF },
                    { "attributes", msg.AttributeHeaders.Select((attrh, i) => new Dictionary<string, object>
                        {
                            { ValueTypeEnum((int) attrh["valueType"]), entry.Attributes[i] }
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
                AttributeHeaders = new List<Dictionary<string, object>>(),
                Entries = new List<MsgEntry>()
            };

            if (mhriceJson["entries"] is JArray entriesArray && entriesArray.Count > 0)
            {
                msg.Languages = new List<int>(entriesArray[0]["content"].Count());
            }
            else
            {
                // Set default languages if entries are empty
                msg.Languages = new List<int> { 0 }; // Modify based on default logic
            }

            // Replace Attribute Headers
            foreach (var head in mhriceJson["attribute_headers"])
            {
                msg.AttributeHeaders.Add(new Dictionary<string, object>
            {
                { "valueType", (int)head["ty"] },
                { "name", (string)head["name"] }
            });
            }

            // Process entries
            foreach (var jEntry in mhriceJson["entries"])
            {
                var entry = new MsgEntry
                {
                    GUID = (Guid)jEntry["guid"],
                    CRC = (uint)jEntry["crc?"],
                    Name = (string)jEntry["name"],
                    Attributes = new List<object>(),
                    Langs = new List<string>(),
                    Hash = (MSG.IsVersionEntryByHash(msg.Version) ? (int)MMH3.Hash32((string)jEntry["name"]) : (int?) null),
                    Index = (MSG.IsVersionEntryByHash(msg.Version) ? (int?)null : jEntry["index"]?.ToObject<int>())
                };

                foreach (var attr in jEntry["attributes"])
                {
                    entry.Attributes.Add(ReadAttributeFromStr(attr.First.ToString(), (int)msg.AttributeHeaders[entry.Attributes.Count]["valueType"]));
                }

                foreach (var content in jEntry["content"])
                {
                    entry.Langs.Add(Helper.ForceWindowsLineBreak((string)content));
                }

                msg.Entries.Add(entry);
            }

            return msg;
        }
    }
}
