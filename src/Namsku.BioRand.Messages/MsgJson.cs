using System.Text.Json.Serialization;

namespace Namsku.BioRand.Messages
{
    public class MsgJson
    {
        public int? Version { get; set; }
        public AttributeHeaderJson[]? AttributeHeaders { get; set; }
        public MsgEntryJson[]? Entries { get; set; }
    }

    public class AttributeHeaderJson
    {
        public string? Name { get; set; }
        [JsonPropertyName("ty")]
        public int? ValueType { get; set; }
    }

    public class MsgEntryJson
    {
        public string? Name { get; set; }
        public Guid? Guid { get; set; }

        [JsonPropertyName("crc?")]
        public uint? Crc { get; set; }
        public int? Hash { get; set; }
        public object[]? Attributes { get; set; }
        public string[]? Content { get; set; }
    }
}
