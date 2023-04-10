using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class MapItemVo
    {
        public MapItemPrefab info { get; set; }

        // 包围盒对应的实际导航线距离
        public float boundRealMaxTrackLength { get; set; }
        public float boundRealMinTrackLength { get; set; }
        public GameObject gameObject { get; set; }

        public EnumGameObjectState state { get; set; }

        public EnumMapItemType itemType { get; set; }

        public MapItemVo(MapItemPrefab itemInfo)
        {
            Reset();
            this.info = itemInfo;
            this.state = EnumGameObjectState.Init;
            this.itemType = EnumMapItemType.None;
        }

        public void Reset()
        {
            info = null;
            gameObject = null;
            boundRealMaxTrackLength = 0f;
            boundRealMinTrackLength = 0f;
            state = EnumGameObjectState.None;
            itemType = EnumMapItemType.None;
        }
    }
}
