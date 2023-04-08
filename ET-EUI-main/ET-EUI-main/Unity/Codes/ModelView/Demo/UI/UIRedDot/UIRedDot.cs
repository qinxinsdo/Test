namespace ET
{
	[ComponentOf(typeof(UIBaseWindow))]
	public  class UIRedDot :Entity,IAwake,IUILogic
	{

		public UIRedDotViewComponent View { get => this.Parent.GetComponent<UIRedDotViewComponent>();}

		public int RedDotBagCount1 = 0;
		public int RedDotBagCount2 = 0;
		public int RedDotMailCount1 = 0;
		public int RedDotMailCount2 = 0;
	}
}
