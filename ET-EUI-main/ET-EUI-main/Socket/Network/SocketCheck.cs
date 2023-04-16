using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework
{
    public class SocketCheck
    {
        /// <summary>
        /// 发送到客户端包得附加信息长度
        /// </summary>
        public const int toClientPackExtendLen = 10;
        /// <summary>
        /// 发送到客户端包得最大长度
        /// </summary>
        public const int toClientPackMaxLen = int.MaxValue - toClientPackExtendLen;
        /// <summary>
        /// 单个包得最大长度
        /// </summary>
        public const int toClientOncePackMaxLen = 512;

        /// <summary>
        /// 包头长度类型长度
        /// </summary>
        public const int packLenTypeLen = sizeof(int);

        public static void segmentationMsg(byte[] sendMsg,Action<byte[]> callBack)
        {
            if (callBack == null)
            {
                throw new Exception("请添加上回调函数");
            }

            if (sendMsg.Length <= SocketCheck.toClientOncePackMaxLen)
            {
                callBack(sendMsg);
                return;
            }

            var sumCount = sendMsg.Length;
            int curLen = 0;
            do
            {
                var sendLen = System.Math.Min(sumCount, SocketCheck.toClientOncePackMaxLen);
                byte[] sd = new byte[sendLen];
                Array.Copy(sendMsg, curLen, sd, 0, sendLen);
                curLen += sendLen;
                callBack(sd);
                sumCount -= sendLen;
            } while (sumCount > 0);
        }

        /// <summary>
        /// 获取协议包头长度
        /// </summary>
        /// <param name="recvData"></param>
        /// <returns></returns>
        public static int readHeadLen(byte[] recvData,int postion)
        {
            if (recvData.Length < packLenTypeLen)
            {
                //throw new ArgumentOutOfRangeException($"recvData.Length[{recvData.Length}] < packLenTrypLen[{packLenTypeLen}]");
                throw new Exception("");
            }
            int value;
            if (ByteArray.BigEndian)
            {
                value = (recvData[postion] << 24) | (recvData[postion+1] << 16) | (recvData[postion+2] << 8) | recvData[postion+3];
                return value;
            }
            value = BitConverter.ToInt32(recvData, postion);
            return value;

        }
    }
}
