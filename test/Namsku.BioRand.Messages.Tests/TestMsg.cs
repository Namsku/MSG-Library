namespace Namsku.BioRand.Messages.Tests
{
    public class TestMsg
    {
        [Fact]
        public void Read_14()
        {
            var msg = new Msg(Resources.mes_sys_mainmenu_msg_14);
            Assert.Equal(14, msg.Version);
            Assert.Equal(42, msg.Count);
            Assert.Equal("Bonuses", msg.GetString(new Guid("39942e2f-84fa-45b9-b2dc-e5911a00795c"), LanguageId.English));
            Assert.Equal("Magasin en ligne", msg.GetString("Mes_Sys_MainMenu_OnlineStore", LanguageId.French));
        }

        [Fact]
        public void Read_22()
        {
            var msg = new Msg(Resources.ch_mes_main_wpcustom_msg_22);
            Assert.Equal(22, msg.Version);
            Assert.Equal(503, msg.Count);
            Assert.Equal("2x Ammo Capacity", msg.GetString(new Guid("dcf42fa5-a6ca-4b50-a154-eeac185e0f15"), LanguageId.English));
            Assert.Equal("弾が無限に使える", msg.GetString("CH_Mes_Main_WpCustom_wp4201_00", LanguageId.Japanese));
        }

        [Fact]
        public void EditEntry()
        {
            var msg = new Msg(Resources.mes_sys_mainmenu_msg_14);
            var builder = msg.ToBuilder();
            builder.SetString(new Guid("39942e2f-84fa-45b9-b2dc-e5911a00795c"), LanguageId.English, "Nice Bonuses");
            builder.SetStringAll("Mes_Sys_MainMenu_OnlineStore", "ONLINE STORE");
            var msg2 = builder.ToMsg();

            Assert.Equal(14, msg2.Version);
            Assert.Equal(42, msg2.Count);
            Assert.Equal("Nice Bonuses", msg2.GetString(new Guid("39942e2f-84fa-45b9-b2dc-e5911a00795c"), LanguageId.English));
            Assert.Equal("ONLINE STORE", msg2.GetString("Mes_Sys_MainMenu_OnlineStore", LanguageId.French));
        }

        [Fact]
        public void ReadWrite()
        {
            var input = Resources.mes_sys_mainmenu_msg_14;
            var msg = new Msg(input);
            var output = msg.Data;
            MemoryAssert.AssertAndCompareMemory(input, output);
        }

        [Fact]
        public void TestJson()
        {
            var input = Resources.mes_sys_mainmenu_msg_14;

            var msg = new Msg(input);
            var json = msg.ToJson();
            var newMsg = Msg.FromJson(json);
        }
    }
}
