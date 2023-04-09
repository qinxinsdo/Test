using System;


namespace ET
{
    public static class EnterMapHelper
    {
        public static async ETTask EnterMapAsync(Scene zoneScene)
        {
            try
            {
                zoneScene.GetComponent<PlayerComponent>().MyId = 1;
                
                SceneChangeHelper.SceneChangeTo(zoneScene, "Map1", 1001).Coroutine();
                
                Game.EventSystem.Publish(new EventType.EnterMapFinish() {ZoneScene = zoneScene});
            }
            catch (Exception e)
            {
                Log.Error(e);
            }	
        }
    }
}