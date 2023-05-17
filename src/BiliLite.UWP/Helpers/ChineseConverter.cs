using OpenCCNET;

namespace BiliLite.Helpers
{
    /// <summary>
    /// 简繁中文转换
    /// 使用OpenCC
    /// </summary>
    public static class ChineseConverter
    {
        private static bool opencc_inited = false;

        public static void Opencc_Init()
        {
            if (opencc_inited == true) return;
            opencc_inited = true;
            ZhConverter.Initialize();
        }
        public static string SimplifiedToTraditional(string input)
        {
            Opencc_Init();
            return OpenCCNET.ZhConverter.HansToHant(input);
        }
        public static string TraditionalToSimplified(string input)
        {
            Opencc_Init();
            return OpenCCNET.ZhConverter.HantToHans(input);
        }
    }
}
