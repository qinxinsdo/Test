
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ObjectSystem]
	public class ReuseUIAwakeSystem : AwakeSystem<ReuseUI,Transform> 
	{
		public override void Awake(ReuseUI self,Transform transform)
		{
			self.uiTransform = transform;
		}
	}


	[ObjectSystem]
	public class ReuseUIDestroySystem : DestroySystem<ReuseUI> 
	{
		public override void Destroy(ReuseUI self)
		{
			self.DestroyWidget();
		}
	}
}
