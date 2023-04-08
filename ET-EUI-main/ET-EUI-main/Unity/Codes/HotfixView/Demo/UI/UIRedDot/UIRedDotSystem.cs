using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ET
{
	[FriendClass(typeof(UIRedDot))]
	public static  class UIRedDotSystem
	{

		public static void RegisterUIEvent(this UIRedDot self)
		{

			
		}

		public static void ShowWindow(this UIRedDot self, Entity contextData = null)
		{
			
		}

		public static void OnBagNode1ClickHandler(this UIRedDot self)
		{
			self.RedDotBagCount1 += 1;

		}
		 
		public static void OnBagNode2ClickHandler(this UIRedDot self)
		{
			self.RedDotBagCount2 += 1;

		}
		
		
		public static void OnMailNode1ClickHandler(this UIRedDot self)
		{
			self.RedDotMailCount1 += 1;
	
		}
		 
		public static void OnMailNode2ClickHandler(this UIRedDot self)
		{
			self.RedDotMailCount2 += 1;

		}
		
	}
}
