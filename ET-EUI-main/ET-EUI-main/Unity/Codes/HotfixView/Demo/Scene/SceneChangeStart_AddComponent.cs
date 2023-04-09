namespace ET
{
    public class SceneChangeStart_AddComponent: AEvent<EventType.SceneChangeStart>
    {
        protected override void Run(EventType.SceneChangeStart args)
        {
            RunAsync(args).Coroutine();
        }
        
        private async ETTask RunAsync(EventType.SceneChangeStart args)
        {
            Scene currentScene = args.ZoneScene.CurrentScene();
            
            // 加载场景资源
            await ResourcesComponent.Instance.LoadSceneAsync(currentScene.Name, false);
            // await ResourcesComponent.Instance.LoadSceneAsync(SceneManagerComponent.Instance.GetSceneConfigByName(SceneNames.Loading).SceneAddress, false);
            Log.Info($"加载场景{currentScene.Name}资源 Over");//await ResourcesComponent.Instance.LoadBundleAsync($"{currentScene.Name}.unity3d");
            // 切换到map场景

            SceneChangeComponent sceneChangeComponent = null;
            try
            {
                sceneChangeComponent = Game.Scene.AddComponent<SceneChangeComponent>();
                {
                    await sceneChangeComponent.ChangeSceneAsync(currentScene.Name);
                }
            }
            finally
            {
                sceneChangeComponent?.Dispose();
            }
			

            currentScene.AddComponent<OperaComponent>();

        }
    }
}