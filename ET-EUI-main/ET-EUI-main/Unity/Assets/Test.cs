using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button Btn_Load;
    
    public Button Btn_Destory;
    
    public Button Btn_UnLoad;
    
    private GameObject Cube;
    
    private AsyncOperationHandle handle;
    
    void Start()
    {
        Addressables.InitializeAsync(); 
        
        Btn_Load.onClick.AddListener(LoadGameObject);
        Btn_Destory.onClick.AddListener(OnClickDestroyObj);
        Btn_UnLoad.onClick.AddListener(ReleaseGameObject);
        
    }

    /// <summary>
    /// 加载物体 
    /// </summary>
    void LoadGameObject()
    {
        // Addressables.InstantiateAsync("Cube").Completed += (hal) =>
        // {
        //     Cube = hal.Result;
        // };
        
        Addressables.LoadAssetAsync<GameObject>("Cube").Completed += (hal) =>
        {
            Cube = Instantiate(hal.Result);
            handle = hal;
        };
    }
    

    /// <summary>
    /// 释放
    /// </summary>
    void ReleaseGameObject()
    {
        Addressables.Release(Cube);
    }

    /// <summary>
    /// 销毁
    /// </summary>
    void OnClickDestroyObj()
    {
        // Destroy(Cube);
        // 会自动调用Destroy，销毁物体
        Addressables.ReleaseInstance(handle);
        // Destroy(Cube);
    }
}