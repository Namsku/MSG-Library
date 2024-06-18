using MsgTool;

namespace TestMsgTool
{
    public class TestMsg
    {
        [Fact]
        public void TestReadWrite()
        {
            var input = File.ReadAllBytes(@"M:\git\MSG-Library\TestMsgTool\data\mes_sys_mainmenu.msg.14");

            var msg = MSG.FromBytes(input);
            var output = msg.WriteMSG();

            MemoryAssert.AssertAndCompareMemory(input, output);
        }

        [Fact]
        public void TestJson()
        {
            var input = File.ReadAllBytes(@"M:\git\MSG-Library\TestMsgTool\data\mes_sys_mainmenu.msg.14");

            var msg = new MSG();
            msg.ReadMSG(new MemoryStream(input));
            // MsgUtils.ExportJson(msg, "M:\\temp\\mes_sys_mainmenu.msg.14.json");
            var json = msg.ToJson();

            var outPath = @"M:\temp\mes_sys_mainmenu.msg.14.2.json";
            File.WriteAllText(outPath, json);
            // MsgUtils.ExportJson(msg, "M:\\temp\\mes_sys_mainmenu.msg.14.json");

            var newMsg = MSG.FromJson(json);
            var output = newMsg.WriteMSG();
            var input2 = MSG.FromBytes(output);
        }
    }
}


