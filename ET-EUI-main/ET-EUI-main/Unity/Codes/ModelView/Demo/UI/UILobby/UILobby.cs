using System.Collections.Generic;

namespace ET
{
	[ComponentOf(typeof(UIBaseWindow))]
	public  class UILobby :Entity,IAwake,IDestroy,IUILogic
	{

		public UILobbyViewComponent View { get => this.Parent.GetComponent<UILobbyViewComponent>();}

	

	}
}
