
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ComponentOf(typeof(UIBaseWindow))]
	[EnableMethod]
	public  class UILobbyViewComponent : Entity,IAwake,IDestroy 
	{
		public UnityEngine.RectTransform BackGroundRectTransform
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_BackGroundRectTransform == null )
     			{
		    		this.m_BackGroundRectTransform = UIFindHelper.FindDeepChild<UnityEngine.RectTransform>(this.uiTransform.gameObject,"EGBackGround");
     			}
     			return this.m_BackGroundRectTransform;
     		}
     	}

		public UnityEngine.UI.Button E_EnterMapButton
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_EnterMapButton == null )
     			{
		    		this.m_EnterMapButton = UIFindHelper.FindDeepChild<UnityEngine.UI.Button>(this.uiTransform.gameObject,"EGBackGround/E_EnterMap");
     			}
     			return this.m_EnterMapButton;
     		}
     	}

		public UnityEngine.UI.Image E_EnterMapImage
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_EnterMapImage == null )
     			{
		    		this.m_EnterMapImage = UIFindHelper.FindDeepChild<UnityEngine.UI.Image>(this.uiTransform.gameObject,"EGBackGround/E_EnterMap");
     			}
     			return this.m_EnterMapImage;
     		}
     	}

		public void DestroyWidget()
		{
			this.m_BackGroundRectTransform = null;
			this.m_EnterMapButton = null;
			this.m_EnterMapImage = null;
			this.uiTransform = null;
		}

		private UnityEngine.RectTransform m_BackGroundRectTransform = null;
		private UnityEngine.UI.Button m_EnterMapButton = null;
		private UnityEngine.UI.Image m_EnterMapImage = null;
		public Transform uiTransform = null;
	}
}
