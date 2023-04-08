namespace ET
{
    public static class ReuseUISystem
    {
        public static void TestFunction(this ReuseUI self,string content)
        {
            self.ELabel_testText.text = content;
        }
    }
}