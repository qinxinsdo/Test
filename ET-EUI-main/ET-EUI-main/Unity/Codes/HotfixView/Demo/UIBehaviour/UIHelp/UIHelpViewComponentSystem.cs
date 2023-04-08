
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ObjectSystem]
	public class UIHelpViewComponentAwakeSystem : AwakeSystem<UIHelpViewComponent> 
	{
		public override void Awake(UIHelpViewComponent self)
		{
			self.uiTransform = self.GetParent<UIBaseWindow>().uiTransform;
		}
	}


	[ObjectSystem]
	public class UIHelperViewComponentDestroySystem : DestroySystem<UIHelpViewComponent> 
	{
		public override void Destroy(UIHelpViewComponent self)
		{
			self.DestroyWidget();
		}
	}
}
