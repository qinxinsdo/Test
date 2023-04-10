using System;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class DTx_Transform
    {
        public Vector3 localPosition { get; set; }
        public Vector3 localRotation { get; set; }
        public Vector3 localScale { get; set; }

        public DTx_Transform()
        {
            localScale = Vector3.one;
            localPosition = Vector3.zero;
            localRotation = Vector3.zero;
        }

        public void Init(Transform transform)
        {
            this.localPosition = VectorUtils.RoundVector3(transform.localPosition);
            this.localRotation = VectorUtils.RoundVector3(transform.localRotation.eulerAngles);
            this.localScale = VectorUtils.RoundVector3(transform.localScale);
        }
    }

    [Serializable]
    public class TrackPoint
    {
        public Vector3 position { get; set; }
        public float trackLength { get; set; }

        public TrackPoint(Vector3 position)
        {
            this.trackLength = 0;
            this.position = position;
        }
    }

    [Serializable]
    public class MapItemPrefab
    {
        public string name { get; set; }
        public string prefabName { get; set; }
        public DTx_Transform dtx_transform { get; set; }

        // 包围盒对应的导航线距离
        public float boundMaxTrackLength { get; set; }
        public float boundMinTrackLength { get; set; }

        public MapItemPrefab()
        {
            name = string.Empty;
            prefabName = string.Empty;
            dtx_transform = new DTx_Transform();

            boundMaxTrackLength = 0;
            boundMinTrackLength = 0;
        }
    }
}
