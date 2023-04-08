namespace ET
{
	 [ComponentOf(typeof(UIBaseWindow))]
	public  class UIHelp :Entity,IAwake,IUILogic
	{

		public UIHelpViewComponent View { get => this.Parent.GetComponent<UIHelpViewComponent>();} 

		 

	}
}
