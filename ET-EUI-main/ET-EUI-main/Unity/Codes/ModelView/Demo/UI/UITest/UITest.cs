namespace ET
{
	 [ComponentOf(typeof(UIBaseWindow))]
	public  class UITest :Entity,IAwake,IUILogic
	{

		public UITestViewComponent View { get => this.Parent.GetComponent<UITestViewComponent>();} 

		 

	}
}
