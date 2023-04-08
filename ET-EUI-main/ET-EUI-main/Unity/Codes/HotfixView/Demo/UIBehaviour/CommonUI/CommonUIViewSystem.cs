
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ObjectSystem]
	public class CommonUIAwakeSystem : AwakeSystem<CommonUI,Transform> 
	{
		public override void Awake(CommonUI self,Transform transform)
		{
			self.uiTransform = transform;
		}
	}


	[ObjectSystem]
	public class CommonUIDestroySystem : DestroySystem<CommonUI> 
	{
		public override void Destroy(CommonUI self)
		{
			self.DestroyWidget();
		}
	}
}
