namespace Namsku.REE.Messages
{
    public readonly struct AttributeHeader : IEquatable<AttributeHeader>
    {
        public string? Name { get; }
        public int ValueType { get; }

        public AttributeHeader(int valueType)
        {
            ValueType = valueType;
        }

        public AttributeHeader(string name, int valueType)
        {
            Name = name;
            ValueType = valueType;
        }

        public AttributeHeader WithName(string name) => new(name, ValueType);
        public override string ToString() => $"{Name} ({ValueType})";

        public override bool Equals(object? obj)
        {
            return obj is AttributeHeader header && Equals(header);
        }

        public bool Equals(AttributeHeader other) => Name == other.Name && ValueType == other.ValueType;
        public override int GetHashCode() => HashCode.Combine(Name, ValueType);

        public static bool operator ==(AttributeHeader left, AttributeHeader right) => left.Equals(right);
        public static bool operator !=(AttributeHeader left, AttributeHeader right) => !(left == right);
    }
}
