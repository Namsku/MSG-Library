using MsgTool;

namespace Project
{
    public class Program
    {
        public static void CompareMsgFiles(Msg msg1, Msg msg2)
        {
            if (msg1.Version != msg2.Version)
            {
                Console.WriteLine($"Version mismatch: {msg1.Version} vs {msg2.Version}");
            }

            if (msg1.AttributeHeaders.Count != msg2.AttributeHeaders.Count)
            {
                Console.WriteLine("AttributeHeaders count mismatch");
            }
            else
            {
                for (int i = 0; i < msg1.AttributeHeaders.Count; i++)
                {
                    if (msg1.AttributeHeaders[i] != msg2.AttributeHeaders[i])
                    {
                        Console.WriteLine($"AttributeHeaders[{i}] mismatch");
                    }
                }
            }

            if (msg1.Entries.Count != msg2.Entries.Count)
            {
                Console.WriteLine("Entries count mismatch");
            }
            else
            {
                for (int i = 0; i < msg1.Entries.Count; i++)
                {
                    CompareEntries(msg1.Entries[i], msg2.Entries[i], i);
                }
            }
        }

        private static void CompareEntries(MsgEntry entry1, MsgEntry entry2, int index)
        {
            if (entry1.Name != entry2.Name)
            {
                Console.WriteLine($"Entries[{index}] Name mismatch: {entry1.Name} vs {entry2.Name}");
            }
            if (entry1.Guid != entry2.Guid)
            {
                Console.WriteLine($"Entries[{index}] Guid mismatch: {entry1.Guid} vs {entry2.Guid}");
            }
            if (entry1.CRC != entry2.CRC)
            {
                Console.WriteLine($"Entries[{index}] Crc mismatch: {entry1.CRC} vs {entry2.CRC}");
            }
            if (entry1.Hash != entry2.Hash)
            {
                Console.WriteLine($"Entries[{index}] Hash mismatch: {entry1.Hash} vs {entry2.Hash}");
            }
            if (entry1.Attributes.Count != entry2.Attributes.Count)
            {
                Console.WriteLine($"Entries[{index}] Attributes count mismatch");
            }
            else
            {
                for (int j = 0; j < entry1.Attributes.Count; j++)
                {
                    if (!entry1.Attributes[j].Equals(entry2.Attributes[j]))
                    {
                        Console.WriteLine($"Entries[{index}] Attributes[{j}] mismatch: {entry1.Attributes[j]} vs {entry2.Attributes[j]}");
                    }
                }
            }
            if (entry1.Langs.Count != entry2.Langs.Count)
            {
                Console.WriteLine($"Entries[{index}] Langs count mismatch");
            }
            else
            {
                for (int k = 0; k < entry1.Langs.Count; k++)
                {
                    if (entry1.Langs[k] != entry2.Langs[k])
                    {
                        Console.WriteLine($"Entries[{index}] Langs[{k}] mismatch: {entry1.Langs[k]} vs {entry2.Langs[k]}");
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
        }
    }
}
