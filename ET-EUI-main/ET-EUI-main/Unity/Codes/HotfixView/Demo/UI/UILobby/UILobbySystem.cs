using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ET
{
	public static  class UILobbySystem
	{

		public static void RegisterUIEvent(this UILobby self)
		{
		  self.View.E_EnterMapButton.AddListener(()=>
		  {
			  self.OnEnterMapClickHandler().Coroutine();
		  });
		
		}

		public static void ShowWindow(this UILobby self, Entity contextData = null)
		{

		}
		
		public static async ETTask OnEnterMapClickHandler(this UILobby self)
		{
			// await self.ZoneScene().GetComponent<UIComponent>().ShowWindowAsync(WindowID.WindowID_Helper);
			await EnterMapHelper.EnterMapAsync(self.ZoneScene());
			self.ZoneScene().GetComponent<UIComponent>().CloseWindow(WindowID.WindowID_Lobby);
			
		}
	}
}
