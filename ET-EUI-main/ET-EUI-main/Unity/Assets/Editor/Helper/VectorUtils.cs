using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class VectorUtils : MonoBehaviour
    {
        /// <summary>
        /// 判断目标是否在角色前方
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="target"></param>
        public static bool JudgeForward(Transform trans, Transform target)
        {
            Vector3 dir = target.position - trans.position;
            float forward = Vector3.Dot(trans.forward, dir);
            return forward > 0;
        }

        /// <summary>
        /// 取固定位
        /// </summary>
        public static Vector3 RoundVector3(Vector3 vector, int digits = 2)
        {
            float x = (float)Math.Round(vector.x, digits);
            float y = (float)Math.Round(vector.y, digits);
            float z = (float)Math.Round(vector.z, digits);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 两个向量相乘，比如旋转或者缩放
        /// </summary>
        public static Vector3 MultiplyVector(Vector3 vec1, Vector3 vec2)
        {
            float x = vec1.x * vec2.x;
            float y = vec1.y * vec2.y;
            float z = vec1.z * vec2.z;
            return new Vector3(x, y, z);
        }
    }
}
