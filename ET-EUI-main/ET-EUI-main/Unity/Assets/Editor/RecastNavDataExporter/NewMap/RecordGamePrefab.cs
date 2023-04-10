using System;
using UnityEditor;
using UnityEngine;
using Game;
using System.Collections.Generic;
using System.IO;
using ET;
using UnityEditor.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

public class RecordGamePrefab
{
    // 记录Prefab引用的次数
    private static Dictionary<string, int> PrefabUseCountDict = new Dictionary<string, int>();

    // 当前scene节点
    private static Scene curScene;
    private static Transform sectionTrans;
    private static Transform curRootNodeTrans;
    
    [MenuItem("美术工具/场景资源更新")]
    public static void CheckGameScene()
    {
        // 地图
        CheckScene();
        EditorSceneManager.OpenScene(Application.dataPath + "/Scenes/Init.unity");

        AssetDatabase.Refresh();
        Debug.LogError($" -------------------  场景资源更新完毕!  ");
    }

    [MenuItem("美术工具/其他资源更新")]
    public static void CheckGameRes()
    {
        // Resources
        CheckResources();
        AssetDatabase.Refresh();

        Debug.LogError($" -------------------  其他资源更新完毕!  ");
    }


    public static void CheckScene()
    {
        string _GameDataPath = "Assets/_GameData/";
        string[] sceneFiles = AssetDatabase.FindAssets("t:Scene", new string[] {
            _GameDataPath + "scene_out_e" 
        });

        List<string> sceneList = new List<string>(); 
        for (int i = 0; i < sceneFiles.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneFiles[i]);
            sceneList.Add(scenePath);
            Debug.Log(" --------------------------- " + scenePath);
        }
        if (sceneList.Count < 0)
            return;

        FileHelper.DeleteDirectory(Application.dataPath + "/_GameData/Resources/Map/MapJson", true);
        FileHelper.DeleteDirectory(Application.dataPath + "/_GameData/Resources/Map/MapPrefab", true);

        foreach (string scenePath in sceneList)
        {
            PrefabUseCountDict.Clear();

            EditorSceneManager.OpenScene(scenePath);
            curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.LogWarning(" -------------  scene  " + curScene.name);

            GameObject[] rootObj = curScene.GetRootGameObjects();
            foreach (GameObject obj in rootObj)
            {
                if (obj.name.StartsWith(curScene.name))
                {
                    PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(obj.gameObject);
                    if (status == PrefabInstanceStatus.MissingAsset)
                        continue;

                    if (status == PrefabInstanceStatus.Connected)
                        PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

                    sectionTrans = obj.transform;
                    CheckSection();
                }
            }

            // 记录每张地图引用的prefab
            string jsonOutpath = Application.dataPath + "/_GameData/Resources/Map/MapJson/" + curScene.name;
            string jsonFilePath = jsonOutpath + "/Json" + curScene.name + ".json";
            string jsonData = LitJson.JsonMapper.ToJson(PrefabUseCountDict);
            FileHelper.WriteText(jsonFilePath, jsonData, true);
        }
    }

    private static void CheckSection()
    {
        // 1、记录Section信息
        // key point
        MapSectionPrefab sectionInfo = new MapSectionPrefab();
        sectionInfo.sceneName = curScene.name;
        sectionInfo.scetionName = sectionTrans.name;
        Debug.LogWarning(" -------------------  " + sectionTrans.name);

        Transform PointStart = sectionTrans.Find("PointStart").transform;
        PointStart.gameObject.SetActive(true);
        sectionInfo.pointStart.Init(PointStart);

        Transform PointEnd = sectionTrans.Find("PointEnd").transform;
        PointEnd.gameObject.SetActive(true);
        sectionInfo.pointEnd.Init(PointEnd);

        Transform TrackStartPoint = sectionTrans.Find("TrackStartPoint")?.transform;
        if (TrackStartPoint != null)
        {
            TrackStartPoint.gameObject.SetActive(true);
            sectionInfo.tarckStartPoint.Init(TrackStartPoint);
        }
        
        // 1、scene
        Transform sceneTrans = sectionTrans.Find("Scene");
        if (sceneTrans == null)
        {
            Debug.LogError(" ----------- 无 Scene 节点 ");
        }
        else
        {
            sectionInfo.SceneItems = CheckMapPrefabNode(sceneTrans);
        }

        // 2、terrain
        Transform terrainTrans = sectionTrans.Find("Terrain");
        if (terrainTrans == null)
        {
            Debug.LogError(" ----------- 无 Terrain 节点 ");
        }
        else
        {
            sectionInfo.TerrainItems = CheckMapPrefabNode(terrainTrans);
        }

        PointStart.gameObject.SetActive(false);
        PointEnd.gameObject.SetActive(false);

        string jsonOutpath = Application.dataPath + "/_GameData/Resources/Map/MapJson/" + curScene.name;
        Debug.LogWarning(" -------------  jsonOutpath  " + jsonOutpath);
        FileHelper.CreateDirectory(jsonOutpath);

        string jsonFilePath = jsonOutpath + "/Json" + sectionTrans.name + ".json";
        string jsonData = LitJson.JsonMapper.ToJson(sectionInfo);
        FileHelper.WriteText(jsonFilePath, jsonData, true);
    }

    private static List<MapItemPrefab> CheckMapPrefabNode(Transform nodeTrans)
    {
        curRootNodeTrans = nodeTrans;

        List<MapItemPrefab> itemInfoList = new List<MapItemPrefab>();
        if (nodeTrans == null)
        {
            return itemInfoList;
        }
        nodeTrans.gameObject.SetActive(true);

        // 重置父节点
        for (int i = nodeTrans.childCount - 1; i >= 0; i--)
        {
            Transform childTrans = nodeTrans.GetChild(i);
            ResetParent(childTrans);
        }

        // 记录信息
        itemInfoList = RecoredMapItem(nodeTrans);
        itemInfoList.Sort((itemA, itemB) =>
        {
            return itemA.boundMinTrackLength < itemB.boundMinTrackLength ? -1 : 1;
        });

        return itemInfoList;
    }

    // TODO
    public static void DestorySafe(UnityEngine.Object obj)
    {
        if (Application.isPlaying)
        {
            if (Application.isEditor)
            {
                GameObject.DestroyImmediate(obj, true);
            }
            else
            {
                GameObject.Destroy(obj);
            }
        }
        else
        {
            GameObject.DestroyImmediate(obj);
        }
    }
    
    /// <summary>
    /// 重置父节点
    /// </summary>
    /// <param name="nodeTrans"></param>
    private static void ResetParent(Transform nodeTrans)
    {
        if (nodeTrans.gameObject.activeSelf == false)
        {
            DestorySafe(nodeTrans.gameObject);
            return;
        }

        PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(nodeTrans.gameObject);
        if (status == PrefabInstanceStatus.Connected && nodeTrans.name.Contains("zuhe_"))
        {
            status = PrefabInstanceStatus.NotAPrefab;
            PrefabUtility.UnpackPrefabInstance(nodeTrans.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        }

        if (status == PrefabInstanceStatus.NotAPrefab)
        {
            for (int i = nodeTrans.childCount - 1; i >= 0; i--)
            {
                Transform childTrans2 = nodeTrans.GetChild(i);
                ResetParent(childTrans2);
            }

            DestorySafe(nodeTrans.gameObject);
            return;
        }

        nodeTrans.SetParent(curRootNodeTrans);
        nodeTrans.SetAsLastSibling();
    }

    /// <summary>
    /// 记录地图信息
    /// </summary>
    /// <param name="nodeTrans"></param>
    /// <returns></returns>
    private static List<MapItemPrefab> RecoredMapItem(Transform nodeTrans)
    {
        List<MapItemPrefab> itemInfoList = new List<MapItemPrefab>();

        for (int i = 0; i < nodeTrans.childCount; i++)
        {
            Transform trans = nodeTrans.GetChild(i);
            if (trans.gameObject.activeSelf == false)
                continue;

            PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(trans.gameObject);
            if (status == PrefabInstanceStatus.NotAPrefab || status == PrefabInstanceStatus.MissingAsset)
            {
                Debug.LogError(" -------------------  " + sectionTrans.name + "  " + trans.name + " is not a prefab or missing Asset ! ");
                continue;
            }

            // 1、记录Prefab信息
            UnityEngine.Object parentPrefab = PrefabUtility.GetCorrespondingObjectFromSource(trans.gameObject);
            if (parentPrefab == null)
            {
                Debug.LogError(" -------------------  name " + trans.name);
                continue;
            }

            MapItemPrefab itemInfo = new MapItemPrefab();
            itemInfo.name = trans.name;
            itemInfo.prefabName = parentPrefab.name;
            itemInfo.dtx_transform.Init(trans);
            itemInfoList.Add(itemInfo);

            // 1、复制prefab
            if (PrefabUseCountDict.ContainsKey(itemInfo.prefabName) == false)
            {
                PrefabUseCountDict[itemInfo.prefabName] = 1;

                string sourcePath = AssetDatabase.GetAssetPath(parentPrefab);
                Debug.Log(" -------------------  " + parentPrefab.name + "   sourcePath   " + sourcePath);

                int index = sourcePath.IndexOf("scene_out_");
                int lastIndex = sourcePath.LastIndexOf(".prefab");
                string prefabPath = sourcePath.Substring(index + 12, lastIndex - (index + 12));
                Debug.Log(" -------------------  " + parentPrefab.name + "   prefabPath   " + prefabPath);

                string destPath = Application.dataPath + "/_GameData/Resources/Map/MapPrefab/" + prefabPath + ".prefab";
                FileHelper.CopyFile(sourcePath, destPath, true);
                Debug.LogWarning(" -------------------  " + parentPrefab.name + "   destPath   " + destPath);
                continue;
            }

            PrefabUseCountDict[itemInfo.prefabName] += 1;
        }

        return itemInfoList;
    }

    /// <summary>
    /// Resources资源
    /// </summary>
    public static void CheckResources()
    {
        string ResourcesPath = Application.dataPath + "/_GameData/Resources";
        FileHelper.DeleteFile($"{ResourcesPath}/do_Resources.json");

        Dictionary<string, string> ResPathDict = new Dictionary<string, string>();

        string[] paths = Directory.GetFiles(ResourcesPath, ".", SearchOption.AllDirectories);
        foreach(string path in paths)
        {
            if(path.EndsWith(".meta") == false)
            {
                int lastIndex = path.LastIndexOf('\\');
                int lastIndex2 = path.LastIndexOf('.');
                string file = path.Substring(lastIndex + 1, lastIndex2 - lastIndex - 1);
                //Debug.Log(" ----------------  " + file + "   " + path);
                if (ResPathDict.ContainsKey(file))
                {
                    Debug.LogError(" --------------- 同名资源 " + file);
                }
                else
                {
                    string relativePath = GetRelativePath(path, ResourcesPath);
                    lastIndex2 = relativePath.LastIndexOf('.');
                    relativePath = relativePath.Substring(0, lastIndex2).Replace("\\", "/");
                    //Debug.Log(" ---------------- relativePath " + relativePath);
                    ResPathDict[file] = relativePath;
                }
            }
        }

        string jsonData = LitJson.JsonMapper.ToJson(ResPathDict);
        FileHelper.WriteText($"{ResourcesPath}/do_Resources.json", jsonData, true);
    }
    
    //TODO
    /// <summary>
    /// 获取相对路径
    /// </summary>
    /// <param name="filespec"></param>
    /// <param name="folder"></param>
    /// <returns></returns>
    public static string GetRelativePath(string filespec, string folder)
    {
        Uri pathUri = new Uri(filespec);

        if (!folder.EndsWith("\\"))
        {
            folder += "\\";
        }
        Uri folderUri = new Uri(folder);

        if (pathUri.AbsolutePath.ToCharArray()[0] != folderUri.AbsolutePath.ToCharArray()[0])
            return filespec;
        else
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace("/", "\\"));
    }
}

