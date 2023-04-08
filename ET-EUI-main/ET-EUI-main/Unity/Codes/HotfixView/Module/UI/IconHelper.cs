﻿using System;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace ET
{
    [FriendClass(typeof(ResourcesComponent))]
    public static class IconHelper
    {
        /// <summary>
        /// 同步加载图集图片资源
        /// </summary>
        /// <OtherParam name="spriteName"></OtherParam>
        /// <returns></returns>
        public static Sprite LoadIconSprite(string atlasName,  string spriteName)
        {
            try
            {
               
                SpriteAtlas spriteAtlas = ResourcesComponent.Instance.AddressablesManager.LoadAssetAsyncObject<SpriteAtlas>(atlasName);
                Sprite sprite = spriteAtlas.GetSprite(spriteName);
                if ( null == sprite )
                {
                    Log.Error($"sprite is null: {spriteName}");
                }
                return sprite;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return null;
            }
        }

        /// <summary>
        /// 异步加载图集图片资源
        /// </summary>
        /// <OtherParam name="spriteName"></OtherParam>
        /// <returns></returns>
        public static async ETTask<Sprite> LoadIconSpriteAsync(string atlasName,  string spriteName)
        {
            try
            {
                SpriteAtlas spriteAtlas = await ResourcesComponent.Instance.LoadAsync<SpriteAtlas>(atlasName);
                Sprite sprite = spriteAtlas.GetSprite(spriteName);
                if (null == sprite)
                {
                    Log.Error($"sprite is null: {spriteName}");
                }
                return sprite;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return null;
            }
        }
    }
}

