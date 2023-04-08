
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ObjectSystem]
	public class UITestViewComponentAwakeSystem : AwakeSystem<UITestViewComponent> 
	{
		public override void Awake(UITestViewComponent self)
		{
			self.uiTransform = self.GetParent<UIBaseWindow>().uiTransform;
		}
	}


	[ObjectSystem]
	public class UITestViewComponentDestroySystem : DestroySystem<UITestViewComponent> 
	{
		public override void Destroy(UITestViewComponent self)
		{
			self.DestroyWidget();
		}
	}
}
