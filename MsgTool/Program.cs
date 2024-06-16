using MsgTool;

namespace Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string filePath = "mes_sys_map.msg.14";

            // Create an instance of MSG
            MSG msg = new MSG();

            // Open the file stream
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Read the MSG data from the file stream
                msg.ReadMSG(fs);
            }

            MsgUtils.ExportJson(msg, "output.json");
            msg = MsgUtils.ImportJson(null, "output.json");

            File.WriteAllBytes("testings.msg", msg.WriteMSG());

            // Use the parsed data
            Console.WriteLine($"Version: {msg.Version}");
            Console.WriteLine($"Entries Count: {msg.Entries.Count}");
            Console.WriteLine($"Attribute Headers Count: {msg.AttributeHeaders.Count}");
            Console.WriteLine($"Languages Count: {msg.Languages.Count}");
        }
    }
}
