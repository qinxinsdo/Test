using System.Collections.Generic;

namespace ET
{
	[ComponentOf(typeof(UIBaseWindow))]
	public  class UILogin :Entity,IAwake,IUILogic
	{

		public UILoginViewComponent View { get => this.Parent.GetComponent<UILoginViewComponent>();} 

		
	}
}
