using System;
using System.Collections.Generic;

namespace Game
{
    /// <summary>
    /// 地图片段Prefab配置信息
    /// </summary>
    [Serializable]
    public class MapSectionPrefab
    {
        // 场景名称
        public string sceneName { get; set; }

        // 地图片段名称
        public string scetionName { get; set; }

        // 地图重要节点
        public DTx_Transform pointStart { get; set; }
        public DTx_Transform pointEnd { get; set; }
        public DTx_Transform tarckStartPoint { get; set; }

        // 导航点
        public List<TrackPoint> trackLine { get; set; }
        public float realTrackLength { get; set; }

        // 地形
        public List<MapItemPrefab> TerrainItems { get; set; }

        // 场景物体
        public List<MapItemPrefab> SceneItems { get; set; }

        public MapSectionPrefab()
        {
            sceneName = string.Empty;
            scetionName = string.Empty;

            pointStart = new DTx_Transform();
            pointEnd = new DTx_Transform();
            tarckStartPoint = new DTx_Transform();

            trackLine = new List<TrackPoint>();
            TerrainItems = new List<MapItemPrefab>();
            SceneItems = new List<MapItemPrefab>();
        }
    }
}
