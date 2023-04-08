using System;
using System.Collections.Generic;
using System.IO;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AssetBundles
{
    public class AddressablesManager
    {
        public static AddressablesManager Instance { get; private set; } = new AddressablesManager();
        
        //存放的是通过LoadAssetAsync加载的资源( 通俗的讲就是不带皮肤label的资源)
        //key为返回的unity asset, value代表这个asset被load了多少次，每一次都会返回一个handle
        Dictionary<Object, int> dictAssetCaching = new Dictionary<Object, int>();
        //存放的是通过LoadAssetsAsync加载的资源(通俗的讲就是带皮肤label的资源，其加载后返回的result其实是个list)
        //所以这里需要采用list将cache下
        List<KeyValuePair<Object, object>> listSkinAssetCaching = new List<KeyValuePair<Object, object>>();

        private Dictionary<string, Dictionary<string, bool>> assetSkinLabelMap = new Dictionary<string, Dictionary<string, bool>>(); // { address: {skin:true} }
        private string m_curSkinLabel = "skin1"; //当前正在使用的skin的label


        private int processingAddressablesAsyncLoaderCount = 0;


        public bool IsProsessRunning
        {
            get
            {
                return processingAddressablesAsyncLoaderCount > 0;
            }
        }

        #region Addressable 相关接口提供

        //检查catalog的更新
        public ETTask<string> CheckForCatalogUpdates()
        {
            ETTask<string> result = ETTask<string>.Create();
            var handle = Addressables.CheckForCatalogUpdates(false);
            handle.Completed += (res) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (handle.Result != null && handle.Result.Count > 0)
                    {
                        result.SetResult(handle.Result[0]);
                    }
                    else
                    {
                        Debug.LogError("handle.Result == null || handle.Result.Count == 0, Check catalog_1.hash is exist");
                        result.SetResult(null);
                    }
                }
                else
                {
                    Debug.LogError("handle.Status == AsyncOperationStatus.Succeeded");
                    result.SetResult(null);
                }
                Addressables.Release(handle);
            };
            return result;
        }

        //根据key来获取下载大小
        public ETTask<long> GetDownloadSizeAsync(string key)
        {
            ETTask<long> result = ETTask<long>.Create();
            var handle = Addressables.GetDownloadSizeAsync(key);
            handle.Completed += (res) =>
            {
                Addressables.Release(handle);
                if (handle.Status == AsyncOperationStatus.Failed)
                    result.SetResult(-1);
                else
                    result.SetResult(handle.Result);
            };
            return result;
        }
        //下载catalogs
        public ETTask<bool> UpdateCatalogs(string catalog)
        {
            ETTask<bool> result = ETTask<bool>.Create();
            var handle = Addressables.UpdateCatalogs(new string[] { catalog }, false);
            handle.Completed += (res) =>
            {
                Addressables.Release(handle);
                result.SetResult(handle.Status == AsyncOperationStatus.Succeeded);
            };
            return result;
        }
        
        #endregion

        #region clear asset and cache

        public void ClearAssetsCache(Object[] excludeObjects = null)
        {
            Debug.Log("ClearAssetsCache");
            Dictionary<Object, bool> dict_exclude_object = new Dictionary<Object, bool>();
            if (excludeObjects != null)
            {
                foreach (var item in excludeObjects)
                {
                    dict_exclude_object.Add(item, true);
                }
            }

            //清除普通的asset
            List<Object> keys = new List<Object>();
            foreach (var key in dictAssetCaching.Keys)
            {
                if (!dict_exclude_object.ContainsKey(key))
                {
                    var value = dictAssetCaching[key];
                    for (int i = 0; i < value; i++)
                    {
                        Addressables.Release(key);
                    }
                    keys.Add(key);
                }
            }

            foreach (var key in keys)
            {
                dictAssetCaching.Remove(key);
            }

            //清除带skin label的asset
            for (int i = listSkinAssetCaching.Count - 1; i >= 0; i--)
            {
                var item = listSkinAssetCaching[i];
                if (!dict_exclude_object.ContainsKey(item.Key))
                {
                    Addressables.Release(item.Value);
                    listSkinAssetCaching.RemoveAt(i);
                }
            }
            Debug.Log("ClearAssetsCache Over");
        }

        public void ReleaseAsset(Object go)
        {
            if (go==null)
            {
                return;
            }

            bool found = false;
            Debug.Log("ReleaseAsset " + go.name);
            //先从assetCacheing中寻找
            if (dictAssetCaching.TryGetValue(go, out int refCount))
            {
                found = true;
                Debug.Log("开始卸载包：" + go.name);
                Addressables.Release(go);
                Debug.Log("完成卸载包：" + go.name);
                refCount = refCount - 1;
                if (refCount == 0)
                {
                    dictAssetCaching.Remove(go);
                }
                else
                {
                    dictAssetCaching[go] = refCount;
                }
            }

            if (!found)
            {
                for (var i = 0; i < listSkinAssetCaching.Count; i++)
                {
                    if (listSkinAssetCaching[i].Key == go)
                    {
                        found = true;
                        Addressables.Release(listSkinAssetCaching[i].Value);
                        listSkinAssetCaching.RemoveAt(i);
                        break;
                    }
                }
            }

            if (!found)
            {
                Debug.LogError("ReleaseAsset Error " + go.name);
            }
        }
        #endregion

        #region LoadAssetAsync

        public T LoadAssetAsyncObject<T>(string addressPath) where T : Object
        {
            return Addressables.LoadAssetAsync<T>(addressPath).WaitForCompletion();
        }
        
        public ETTask<T> LoadAssetAsync<T>(string addressPath) where T: Object
        {
            ETTask<T> tTask = ETTask<T>.Create();
            var label = GetAssetSkinLabel(addressPath);
            processingAddressablesAsyncLoaderCount += 1;
            if (!string.IsNullOrEmpty(label))
            {
                var res = Addressables.LoadAssetsAsync<T>(new List<string> { addressPath, label }, null, Addressables.MergeMode.Intersection);
                res.Completed += (loader) =>
                {
                    var obj = OnAddressablesAsyncLoaderDone(loader);
                    tTask.SetResult(obj);
                };
            }
            else
            {
                var res = Addressables.LoadAssetAsync<T>(addressPath);
                res.Completed += (loader) =>
                {
                    var obj = OnAddressablesAsyncLoaderDone(loader);
                    tTask.SetResult(obj);
                };
            }
            return tTask;
        }

        public ETTask LoadSceneAsync(string addressPath, bool isAdditive)
        {
            ETTask tTask = ETTask.Create();
            Addressables.LoadSceneAsync(addressPath, isAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single).Completed +=
                    (loader) =>
                    {
                        OnAddressablesAsyncLoaderDone(loader);
                        tTask.SetResult();
                    };
            processingAddressablesAsyncLoaderCount += 1;
            
            return tTask;
        }

        /*
         * loader加载资源完成后主动调用这个接口，来增加asset到cache中
         * note: 原来采用的方式是将loader加入到数组中，update的时候进行遍历，来判断loader是否完成
         * 原来的方式缺点: 1: 无效的检查太多，每帧都会调用update
         */

        public T OnAddressablesAsyncLoaderDone<T>(AsyncOperationHandle<IList<T>> loader) 
        {
            processingAddressablesAsyncLoaderCount -= 1;
            var res = loader.Result as IList<Object>;
            if (res != null)
            {
                listSkinAssetCaching.Add(new KeyValuePair<Object, object>(res[0], res));
                return loader.Result[0];
            }
            return default(T);
        }
        public T OnAddressablesAsyncLoaderDone<T>(AsyncOperationHandle<T> loader)
        {
            processingAddressablesAsyncLoaderCount -= 1;
            var res = loader.Result as Object;
            if (res != null)
            {
                int refCount;
                if (dictAssetCaching.TryGetValue(res, out refCount))
                {
                    dictAssetCaching[res] = refCount + 1;
                }
                else
                {
                    dictAssetCaching.Add(res, 1);
                }
                return loader.Result;
            }
            return default(T);
        }
        
#endregion

        #region skin change begin
        public void InitAssetSkinLabelText(string text)
        {
            string[] lines = GameUtility.StringToArrary(text);
            Dictionary<string, Dictionary<string, bool>> dict = new Dictionary<string, Dictionary<string, bool>>();
            //为了防止路径里面带有空格，这里区分不采用空格来划分而采用&&
            for (var i = 0; i < lines.Length; i++)
            {
                string[] splits = lines[i].Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length > 0)
                {
                    Dictionary<string, bool> labelMap = new Dictionary<string, bool>();
                    var address = splits[0];
                    for (var j = 1; j < splits.Length; j++)
                    {
                        var label = splits[j];
                        labelMap.Add(label, true);
                    }
                    dict.Add(address, labelMap);
                }
            }
            assetSkinLabelMap = dict;
        }

        public void SetCurSkinLabel(string label)
        {
            m_curSkinLabel = label;
        }

        public string GetCurSkinLabel()
        {
            return m_curSkinLabel;
        }

        //获取asset的skin label
        public string GetAssetSkinLabel(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return "";
            }

            Dictionary<string, bool> dict_label;
            if (assetSkinLabelMap.TryGetValue(address, out dict_label))
            {
                bool found;
                if (dict_label.TryGetValue(m_curSkinLabel, out found))
                {
                    //该asset在当前skin下是拥有资源的
                    return m_curSkinLabel;
                }
                else
                {
                    //这里其实是异常了，该asset在当前skin漏添加了资源
                    //TODO 看需不需要直接返回个label
                    return "";
                }
            }
            else
            {
                //证明该asset是不需要皮肤的其label应该是default
                return "";
            }
        }
#endregion

        public Dictionary<string, TextAsset> LoadAllTextAsset()
        {
            Dictionary<string, TextAsset> res = new Dictionary<string, TextAsset>();

            var fullPath = "Assets/Bundles/Config/";
            if (Directory.Exists(fullPath)){
                DirectoryInfo direction = new DirectoryInfo(fullPath);
                FileInfo[] files = direction.GetFiles("*",SearchOption.AllDirectories);
                for(int i=0;i<files.Length;i++){
                    if (files[i].Name.EndsWith(".meta")){
                        continue;
                    }
                    var asset = Addressables.LoadAssetAsync<TextAsset>(files[i].Name).WaitForCompletion();
                    Debug.LogError($"加载配置表：{files[i].Name}");
                    res.Add(files[i].Name, asset);
                }  

            }
            return res;
        }
        
        public TextAsset LoadTextAsset(string addressPath)
        {
            Debug.LogError($"单独加载一个配置表：{addressPath}");
            TextAsset asset = Addressables.LoadAssetAsync<TextAsset>(addressPath).WaitForCompletion();
            if (asset == null)
            {
                Debug.LogError("LoadTextAsset fail, path: " + addressPath);
            }
            return asset;
        }
    }
}