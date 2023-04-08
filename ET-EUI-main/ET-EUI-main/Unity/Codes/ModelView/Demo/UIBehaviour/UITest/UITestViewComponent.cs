
using UnityEngine;
using UnityEngine.UI;
namespace ET
{
	[ComponentOf(typeof(UIBaseWindow))]
	[EnableMethod]
	public  class UITestViewComponent : Entity,IAwake,IDestroy 
	{
		public UnityEngine.UI.Button m_LoginButton
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_m_LoginButton == null )
     			{
		    		this.m_m_LoginButton = UIFindHelper.FindDeepChild<UnityEngine.UI.Button>(this.uiTransform.gameObject,"Sprite_BackGround/m_Login");
     			}
     			return this.m_m_LoginButton;
     		}
     	}

		public UnityEngine.UI.Image m_LoginImage
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_m_LoginImage == null )
     			{
		    		this.m_m_LoginImage = UIFindHelper.FindDeepChild<UnityEngine.UI.Image>(this.uiTransform.gameObject,"Sprite_BackGround/m_Login");
     			}
     			return this.m_m_LoginImage;
     		}
     	}

		public UnityEngine.UI.InputField m_AccountInputField
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_m_AccountInputField == null )
     			{
		    		this.m_m_AccountInputField = UIFindHelper.FindDeepChild<UnityEngine.UI.InputField>(this.uiTransform.gameObject,"Sprite_BackGround/m_Account");
     			}
     			return this.m_m_AccountInputField;
     		}
     	}

		public UnityEngine.UI.Image m_AccountImage
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_m_AccountImage == null )
     			{
		    		this.m_m_AccountImage = UIFindHelper.FindDeepChild<UnityEngine.UI.Image>(this.uiTransform.gameObject,"Sprite_BackGround/m_Account");
     			}
     			return this.m_m_AccountImage;
     		}
     	}

		public UnityEngine.UI.InputField m_PasswordInputField
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_m_PasswordInputField == null )
     			{
		    		this.m_m_PasswordInputField = UIFindHelper.FindDeepChild<UnityEngine.UI.InputField>(this.uiTransform.gameObject,"Sprite_BackGround/m_Password");
     			}
     			return this.m_m_PasswordInputField;
     		}
     	}

		public UnityEngine.UI.Image m_PasswordImage
     	{
     		get
     		{
     			if (this.uiTransform == null)
     			{
     				Log.Error("uiTransform is null.");
     				return null;
     			}
     			if( this.m_m_PasswordImage == null )
     			{
		    		this.m_m_PasswordImage = UIFindHelper.FindDeepChild<UnityEngine.UI.Image>(this.uiTransform.gameObject,"Sprite_BackGround/m_Password");
     			}
     			return this.m_m_PasswordImage;
     		}
     	}

		public void DestroyWidget()
		{
			this.m_m_LoginButton = null;
			this.m_m_LoginImage = null;
			this.m_m_AccountInputField = null;
			this.m_m_AccountImage = null;
			this.m_m_PasswordInputField = null;
			this.m_m_PasswordImage = null;
			this.uiTransform = null;
		}

		private UnityEngine.UI.Button m_m_LoginButton = null;
		private UnityEngine.UI.Image m_m_LoginImage = null;
		private UnityEngine.UI.InputField m_m_AccountInputField = null;
		private UnityEngine.UI.Image m_m_AccountImage = null;
		private UnityEngine.UI.InputField m_m_PasswordInputField = null;
		private UnityEngine.UI.Image m_m_PasswordImage = null;
		public Transform uiTransform = null;
	}
}
