
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ObjectSystem]
	public class DlgLoginViewComponentAwakeSystem : AwakeSystem<UILoginViewComponent> 
	{
		public override void Awake(UILoginViewComponent self)
		{
			self.uiTransform = self.GetParent<UIBaseWindow>().uiTransform;
		}
	}


	[ObjectSystem]
	public class UILoginViewComponentDestroySystem : DestroySystem<UILoginViewComponent> 
	{
		public override void Destroy(UILoginViewComponent self)
		{
			self.DestroyWidget();
		}
	}
}
