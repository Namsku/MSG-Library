using MsgTool;

namespace TestMsgTool
{
    public class TestMsg
    {
        [Fact]
        public void TestReadWrite()
        {
            var input = File.ReadAllBytes(@"M:\git\MSG-Library\TestMsgTool\data\mes_sys_mainmenu.msg.14");

            var msg = new MSG();
            msg.ReadMSG(new MemoryStream(input));
            var output = msg.WriteMSG();

            MemoryAssert.AssertAndCompareMemory(input, output);
        }
    }
}


