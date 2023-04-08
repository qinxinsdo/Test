
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ObjectSystem]
	public class UIRedDotViewComponentAwakeSystem : AwakeSystem<UIRedDotViewComponent> 
	{
		public override void Awake(UIRedDotViewComponent self)
		{
			self.uiTransform = self.GetParent<UIBaseWindow>().uiTransform;
		}
	}


	[ObjectSystem]
	public class UIRedDotViewComponentDestroySystem : DestroySystem<UIRedDotViewComponent> 
	{
		public override void Destroy(UIRedDotViewComponent self)
		{
			self.DestroyWidget();
		}
	}
}
