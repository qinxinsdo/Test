
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ObjectSystem]
	public class UILobbyViewComponentAwakeSystem : AwakeSystem<UILobbyViewComponent> 
	{
		public override void Awake(UILobbyViewComponent self)
		{
			self.uiTransform = self.GetParent<UIBaseWindow>().uiTransform;
		}
	}


	[ObjectSystem]
	public class UILobbyViewComponentDestroySystem : DestroySystem<UILobbyViewComponent> 
	{
		public override void Destroy(UILobbyViewComponent self)
		{
			self.DestroyWidget();
		}
	}
}
