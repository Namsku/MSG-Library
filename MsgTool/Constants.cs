using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsgTool
{
    public static class Constants
    {
        public static readonly Dictionary<int, string> LANG_LIST = new Dictionary<int, string>
        {
            { 0, "Japanese" },
            { 1, "English" },
            { 2, "French" },
            { 3, "Italian" },
            { 4, "German" },
            { 5, "Spanish" },
            { 6, "Russian" },
            { 7, "Polish" },
            { 8, "Dutch" },
            { 9, "Portuguese" },
            { 10, "PortugueseBr" },
            { 11, "Korean" },
            { 12, "TraditionalChinese" },
            { 13, "SimplifiedChinese" },
            { 14, "Finnish" },
            { 15, "Swedish" },
            { 16, "Danish" },
            { 17, "Norwegian" },
            { 18, "Czech" },
            { 19, "Hungarian" },
            { 20, "Slovak" },
            { 21, "Arabic" },
            { 22, "Turkish" },
            { 23, "Bulgarian" },
            { 24, "Greek" },
            { 25, "Romanian" },
            { 26, "Thai" },
            { 27, "Ukrainian" },
            { 28, "Vietnamese" },
            { 29, "Indonesian" },
            { 30, "Fiction" },
            { 31, "Hindi" },
            { 32, "LatinAmericanSpanish" },
            { 33, "Max" }
        };

        public static readonly Dictionary<int, string> LANG_CODE_LIST = new Dictionary<int, string>
        {
            { 0, "Japanese" },
            { 1, "English" },
            { 2, "French" },
            { 3, "Italian" },
            { 4, "German" },
            { 5, "Spanish" },
            { 6, "Russian" },
            { 7, "Polish" },
            { 8, "Dutch" },
            { 9, "Portuguese" },
            { 10, "PortugueseBr" },
            { 11, "Korean" },
            { 12, "TransitionalChinese" },
            { 13, "SimplelifiedChinese" },
            { 14, "Finnish" },
            { 15, "Swedish" },
            { 16, "Danish" },
            { 17, "Norwegian" },
            { 18, "Czech" },
            { 19, "Hungarian" },
            { 20, "Slovak" },
            { 21, "Arabic" },
            { 22, "Turkish" },
            { 23, "Bulgarian" },
            { 24, "Greek" },
            { 25, "Romanian" },
            { 26, "Thai" },
            { 27, "Ukrainian" },
            { 28, "Vietnamese" },
            { 29, "Indonesian" },
            { 30, "Fiction" },
            { 31, "Hindi" },
            { 32, "LatinAmericanSpanish" },
            { 33, "Max" }
        };

        public static readonly List<int> MHR_SUPPORTED_LANG = new List<int>
        {
            0, 1, 2, 3, 4, 5, 6, 7, 10, 11, 12, 13, 21, 32
        };

        public static readonly Dictionary<int, int> VERSION_2_LANG_COUNT = new Dictionary<int, int>
        {
            { 12, 23 },
            { 0x2022033D, 27 },
            { 14, 28 },
            { 15, 30 },
            { 17, 32 },
            { 20, 33 },
            { 0x20220626, 33 }, //  before 13.0.0, 0x20220626 has 32 lang count
            { 22, 33 }
        };
    }
}
