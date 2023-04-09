using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using System.IO;
using ET;
using UnityEngine;

namespace ClientEditor
{
    class UIEditorController
    {
        [MenuItem("GameObject/生成代码", false, -2)]
        static public void CreateNewCode()
        {
            GameObject go = Selection.activeObject as GameObject;
            UICodeSpawner.SpawnEUICode(go);
        }

        [MenuItem("Assets/AssetBundle/ClearABName")]
        public static void ClearABName()
        {
            UnityEngine.Object[] selectAsset = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.DeepAssets);
            for (int i = 0; i < selectAsset.Length; i++)
            {
                string prefabName = AssetDatabase.GetAssetPath(selectAsset[i]);
                AssetImporter importer = AssetImporter.GetAtPath(prefabName);
                importer.assetBundleName = string.Empty;
                Debug.Log(prefabName);
            }
            AssetDatabase.Refresh();
            AssetDatabase.RemoveUnusedAssetBundleNames();
        }
    }
}
