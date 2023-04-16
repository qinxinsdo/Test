using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LuaInterface;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Framework
{
    public class LuaSocketClient
    {
        public System.Action<LuaSocketClient> onConnectEvent = null;
        public System.Action<LuaSocketClient, int> onConnectErrorEvent = null;
        public System.Action<LuaSocketClient> onCloseEvent = null;

        // 是否协议压缩
        public static bool COMPRESS = false;
        // 是否协议加密
        public static bool ENCRYPT = false;
        // 压缩长度
        public static int COMPRESS_MIN_LEN = 127;
        // 缓冲区长度
        public const int BUFFER_SIZE = 65535;
        // 包头长度
        public const int HEADER_LENGTH = 4;
        public const int PROTOCOL_LENGTH = 6;

        // Socket属性
        protected string _id;
        protected string _host;
        protected int _port;

        protected Socket _socket;
        protected Thread _threadReceive;
        protected Thread _threadSend;
        protected byte[] _bufferReceive;

        // 数据缓冲
        protected Queue<byte[]> _sendQueue;
        protected Queue<Message> _receiveQueue;

        // 断线重连缓冲使用
        public static bool HandleReconnectCacheMsg = false;
        protected List<Message> _receiveQueueCached = new List<Message>();

        protected ByteArray _bufferBytes;
        protected ByteArray _cacheBytes;
        protected ByteArray _sendBytes;
        protected ByteArray _tmpBytes;
        protected bool _connected;
        protected int _errorCode;

        protected bool _readHead;
        protected int _length;

        protected LuaTable _luaTable;
        protected LuaFunction _luaOnConnect;
        protected LuaFunction _luaOnClose;
        protected LuaFunction _luaOnReceive;
        protected LuaFunction _luaOnConnectError;

        // 做个原子锁的变量
        protected int _checkErrorCodeLock = 0;

        //协议类型枚举
        enum ADDRESSFAM
        {
            IPv4,
            IPv6
        }
#if UNITY_IPHONE && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string getIPv6(string host);
#endif
        private string GetIPv6(string host)
        {
#if UNITY_IPHONE && !UNITY_EDITOR
		    return getIPv6 (host);
#else
            return host + "&&ipv4";
#endif
        }

        /// <summary>
        /// 获取IP地址和类型
        /// </summary>
        /// <param name="serverIp"></param>
        /// <param name="newServerIp"></param>
        /// <param name="IPType"></param>
        private void GetIPType(string serverIp, out string newServerIp, out AddressFamily IPType)
        {
            IPType = AddressFamily.InterNetwork;
            newServerIp = serverIp;
            try
            {
                string IPv6 = GetIPv6(serverIp);
                if (!string.IsNullOrEmpty(IPv6))
                {
                    string[] tmp = Regex.Split(IPv6, "&&");
                    if (tmp != null && tmp.Length >= 2)
                    {
                        string type = tmp[1];
                        if (type == "ipv6")
                        {
                            newServerIp = tmp[0];
                            IPType = AddressFamily.InterNetworkV6;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                App.logManager.Info("GetIPv6 error:" + e.Message);
            }
        }

        /// <summary>
        /// 通过类型获取ID地址
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="AF"></param>
        /// <returns></returns>
        private string GetIPAddress(string hostName, ADDRESSFAM AF)
        {
            if (AF == ADDRESSFAM.IPv6 && !Socket.OSSupportsIPv6)
                return null;
            if (string.IsNullOrEmpty(hostName))
                return null;
            IPHostEntry host;
            string connectIP = "";
            try
            {
                host = Dns.GetHostEntry(hostName);
                foreach (IPAddress ip in host.AddressList)
                {
                    if (AF == ADDRESSFAM.IPv4)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            connectIP = ip.ToString();
                            break;
                        }
                    }
                    else if (AF == ADDRESSFAM.IPv6)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            connectIP = ip.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                App.logManager.Info("GetIPAddress error: " + e.Message);
            }
            return connectIP;
        }

        // 检查是否IP地址
        bool IsIPAddress(string data)
        {
            Match match = Regex.Match(data, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            return match.Success;
        }

        public LuaSocketClient(string id)
        {
            _id = id;
            _bufferReceive = new byte[BUFFER_SIZE];

            _bufferBytes = new ByteArray();
            _cacheBytes = new ByteArray();
            _sendBytes = new ByteArray();
			_tmpBytes = new ByteArray();

            _sendQueue = new Queue<byte[]>();
            _receiveQueue = new Queue<Message>();

            _luaTable = App.luaManager.GetLuaTable("SocketManager");
            _luaOnConnect = _luaTable.GetLuaFunction("OnConnect");
            _luaOnClose = _luaTable.GetLuaFunction("OnClose");
            _luaOnReceive = _luaTable.GetLuaFunction("OnReceive");
            _luaOnConnectError = _luaTable.GetLuaFunction("OnConnectError");
        }

        /// <summary>
        /// 唯一ID
        /// </summary>
        public string id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Host
        /// </summary>
        public string host
        {
            get { return _host; }
            set { _host = value; }
        }

        /// <summary>
        /// Port
        /// </summary>
        public int port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool connected
        {
            get { return _connected; }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public virtual void Connect(string host, int port)
        {
            _host = host;
            _port = port;
            Connect();
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public virtual void Connect()
        {
            try
            {
                //if (Main.BreakNetwork)
                //{
                //    return;
                //}
                string connectionHost = _host;
                string convertedHost = "";
                AddressFamily convertedFamily = AddressFamily.InterNetwork;
                if (IsIPAddress(_host))
                {
                    GetIPType(_host, out convertedHost, out convertedFamily);
                    if (!string.IsNullOrEmpty(convertedHost))
                        connectionHost = convertedHost;
                }
                else
                {
                    convertedHost = GetIPAddress(_host, ADDRESSFAM.IPv6);
                    if (string.IsNullOrEmpty(convertedHost))
                        convertedHost = GetIPAddress(_host, ADDRESSFAM.IPv4);
                    else
                        convertedFamily = AddressFamily.InterNetworkV6;
                    if (string.IsNullOrEmpty(convertedHost))
                    {
                        App.logManager.Info("Can't get IP address");
                        return;
                    }
                    else
                        connectionHost = convertedHost;
                }
                App.logManager.Info("Connecting to : " + connectionHost + " | protocol : " + convertedFamily);

                Close();
                _socket = new Socket(convertedFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.ReceiveTimeout = 60000;
                _socket.NoDelay = true;
                _socket.BeginConnect(connectionHost, _port, ConnectCallback, _socket);
            }
            catch (SocketException exception)
            {
                App.logManager.Error("LuaSocketClient.Connect Error:" + exception.SocketErrorCode + " " + exception.Message);
                _errorCode = (int)exception.SocketErrorCode;
                App.timerManager.RegisterFrameOnce(1, HandleConnectFaild);
            }
        }

        private void ConnectCallback(IAsyncResult asyncResult)
        {
            try
            {
                _socket.EndConnect(asyncResult);
                if (_socket.Connected)
                {
                    App.timerManager.RegisterFrameLoop(1, HandleMessage);

                    _threadReceive = new Thread(ReceiveDataThread);
                    _threadReceive.IsBackground = true;
                    _threadReceive.Start();
                    _threadSend = new Thread(SendDataThread);
                    _threadSend.IsBackground = true;
                    _threadSend.Start();
                }
            }
            catch (SocketException exception)
            {
                UnityEngine.Debug.LogError("LuaSocketClient.ConnectCallback Error:" + exception.SocketErrorCode + " " + exception.Message);
                _errorCode = (int)exception.SocketErrorCode;

                App.timerManager.RegisterFrameOnce(1, HandleConnectFaild);
            }
        }

        /// <summary>
        /// 为了WebSocket重载
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsSocketConnected()
        {
            if (_socket == null)
                return false;
            if (_socket.Connected == false)
                return false;

            return true;
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        protected virtual void HandleMessage()
        {
            if (_socket == null)
                return;

            if (_socket.Connected == false)
            {
                Close();
                CallLuaClose(_id);
                return;
            }
            else if (_connected == false)
            {
                _connected = true;
                CallLuaOnConnect(_id);
            }

            //// TODO:CC 测试断网以及断线重连
            //if (Main.BreakNetwork)
            //{
            //    Close();
            //    CallLuaClose(_id);
            //    return;
            //}

            //if (Main.NetworkTimeout)
            //{
            //    return;
            //}

            // MARK:CC 是否处理缓冲区的接受信息，有潜在逻辑问题，比如一帧内多次处理角色的外观表现
            if (HandleReconnectCacheMsg)
            {
                TryHandleCacheReconnectMsgs();
            }

            while (_receiveQueue.Count > 0)
            {
                Message msg;
                lock (_receiveQueue)
                    msg = _receiveQueue.Dequeue();
                CallLuaOnReceive(msg.protocolID, msg.sequenceNumber, _id, msg.bytes);
            }
        }

        /// <summary>
        /// 如果逻辑需要，处理断线重连缓冲数据
        /// </summary>
        void TryHandleCacheReconnectMsgs()
        {
            if (_receiveQueueCached.Count > 0)
            {
                UnityEngine.Debug.LogWarning("处理消息函数，有断线重连后的缓存数据数量：" + _receiveQueueCached.Count);
                lock (_receiveQueue)
                {
                    int lnum = _receiveQueue.Count;

                    _receiveQueueCached.AddRange(_receiveQueue);
                    _receiveQueue.Clear();

                    foreach (var v in _receiveQueueCached)
                    {
                        _receiveQueue.Enqueue(v);
                    }
                    _receiveQueueCached.Clear();

                    int lnum1 = _receiveQueue.Count;
                    UnityEngine.Debug.LogWarning("处理消息函数，有断线重连后的缓存+当前数，构造前：" + lnum + " 构造后:" + lnum1);

                    StringBuilder sbb = new StringBuilder();
                    foreach (var v in _receiveQueue)
                    {
                        sbb.AppendLine(string.Format("protocolID:{0} sequenceNumber:{1}", v.protocolID, v.sequenceNumber));
                    }
                    UnityEngine.Debug.LogWarning("处理消息函数，有断线重连后的所有消息打印：" + sbb.ToString());
                }
            }
        }

        /// <summary>
        /// 如果是断线关闭Socket时特意检测
        /// </summary>
        void TryCacheReconnectMsgs()
        {
            if (_sendQueue.Count > 0)
            {
                UnityEngine.Debug.LogWarning("关闭Socket， 发送队列有消息数量：" + _sendQueue.Count);
            }
            if (_receiveQueue.Count > 0)
            {
                _receiveQueueCached.Clear();
                _receiveQueueCached.AddRange(_receiveQueue);

                UnityEngine.Debug.LogWarning("关闭Socket， 接受队列有消息数量：" + _receiveQueue.Count);

                StringBuilder sbb = new StringBuilder();
                foreach (var v in _receiveQueue)
                {
                    sbb.AppendLine(string.Format("protocolID:{0} sequenceNumber:{1}", v.protocolID, v.sequenceNumber));
                }
                UnityEngine.Debug.LogWarning("关闭Socket， 接受队列有消息打印：" + sbb.ToString());
            }

            if (_bufferBytes.Length > 0)
            {
                UnityEngine.Debug.LogWarning("关闭Socket， _bufferBytes 字节数组长度：" + _bufferBytes.Length);
            }
            if (_cacheBytes.Length > 0)
            {
                UnityEngine.Debug.LogWarning("关闭Socket， _cacheBytes 字节数组长度：" + _cacheBytes.Length);
            }
            if (_sendBytes.Length > 0)
            {
                UnityEngine.Debug.LogWarning("关闭Socket， _sendBytes 字节数组长度：" + _sendBytes.Length);
            }
        }

        /// <summary>
        /// 处理连接失败
        /// </summary>
        protected virtual void HandleConnectFaild()
        {
            if (_errorCode != 0)
            {
                Close();
                CallLuaOnConnectError(_id, _errorCode);
                _errorCode = 0;
                return;
            }
        }

        public virtual void Send(byte[] bytes)
        {
            // 将协议数据放入队列
            lock (_sendQueue)
            {
                _sendQueue.Enqueue(bytes);
            }
        }

        /// <summary>
        /// 发送协议
        /// </summary>
        /// <param name="protocolID"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="bytes"></param>
        public virtual void Send(int protocolID, uint sequenceNumber, ByteArray bytes)
        {
            //if (Main.BreakNetwork)
            //{
            //    return;
            //}

            //if (Main.NetworkTimeout)
            //{
            //    return;
            //}

            if (!IsSocketConnected())
            {
                return;
            }

            bytes.Position = 0;
            // 清空发送数据
            _sendBytes.Clear();
			
			_tmpBytes.Clear();
            // 写入协议ID
            _tmpBytes.WriteUShort((ushort)protocolID);
            // 写入序列号
            _tmpBytes.WriteUInt(sequenceNumber);
            // 写入数据
            _tmpBytes.WriteBytes(bytes.ReadBytes());
            _tmpBytes.Position = 0;
            byte[] contentBytes = _tmpBytes.ReadBytes();

			

            if (COMPRESS)
            {
                bool compress = contentBytes.Length > COMPRESS_MIN_LEN;
                if (compress)
                    contentBytes = ZipUtil.DeflateCompress(contentBytes);
                if (ENCRYPT)
                    EncryptUtil.encode(contentBytes);

                _sendBytes.WriteInt(contentBytes.Length + 1);
                _sendBytes.WriteBoolean(compress);
                _sendBytes.WriteBytes(contentBytes);
            }
            else
            {
                if (ENCRYPT)
                    EncryptUtil.encode(contentBytes);

                _sendBytes.WriteInt(contentBytes.Length);
                _sendBytes.WriteBytes(contentBytes);
            }
            // 读出数据
            _sendBytes.Position = 0;
            byte[] data = _sendBytes.ReadBytes((int)_sendBytes.Length);

            // 将协议数据放入队列
            lock (_sendQueue)
            {
                SocketCheck.segmentationMsg(data, (retBytes) =>
                {
                    _sendQueue.Enqueue(retBytes);
                });
            }
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        protected virtual void Receive(byte[] bytes, int length)
        {
            if (_cacheBytes.BytesAvailable > 0)
            {
                _bufferBytes.WriteBytes(_cacheBytes.ReadBytes());
                _cacheBytes.Clear();
            }
            // 写入协议数据到缓冲区
            _bufferBytes.WriteBytes(bytes, 0, length);
            _bufferBytes.Position = 0;
            // 循环处理协议数据
            while (_readHead || _bufferBytes.BytesAvailable > HEADER_LENGTH)
            {
                // 未读取协议包头
                if (_readHead == false)
                {
                    _length = _bufferBytes.ReadInt();
                    _readHead = true;
                }
                // 已读取协议包头
                if (_readHead && _bufferBytes.BytesAvailable >= _length)
                {
                    _readHead = false;
                    // 清空包体数据
                    ByteArray contextBytes = new ByteArray();
                    // 判断包体是否有数据
					byte[] contentBytes = null;
                    if (COMPRESS)
                    {
                        bool compress = _bufferBytes.ReadBoolean();
                        contentBytes = _bufferBytes.ReadBytes(_length - 1);
                        if (ENCRYPT)
                            EncryptUtil.decode(contentBytes);
                        if (compress)
                            contentBytes = ZipUtil.DeflateDecompress(contentBytes);
                    }
                    else
                    {
                        contentBytes = _bufferBytes.ReadBytes(_length);
                        if (ENCRYPT)
                            EncryptUtil.decode(contentBytes);
                    }
                    contextBytes.WriteBytes(contentBytes);
                    contextBytes.Position = 0;
                    // 读取协议ID
                    ushort protocolID = contextBytes.ReadUshort();
                    // 读取序列号
                    uint sequenceNumber = contextBytes.ReadUInt();
					
                    // 发送协议到lua处理
                    lock (_receiveQueue)
                        _receiveQueue.Enqueue(new Message(protocolID, sequenceNumber, contextBytes));
                }
                else
                    break;
            }
            if (_bufferBytes.BytesAvailable > 0)
            {
                _cacheBytes.WriteBytes(_bufferBytes.ReadBytes());
                _cacheBytes.Position = 0;
            }
            _bufferBytes.Clear();
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public virtual void Close()
        {
            try
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
                _connected = false;
            }
            catch (SocketException exception)
            {
                App.logManager.Error("LuaSocketClient.Close Error:" + exception.Message);
            }

            if (_threadReceive != null)
            {
                try
                {
                    if (_threadReceive.IsAlive)
                        _threadReceive.Abort();
                }
                catch (ThreadAbortException exception)
                {
                    App.logManager.Error("SocketClient.Close Error:" + exception.Message);
                }
                _threadReceive = null;
            }

            if (_threadSend != null)
            {
                try
                {
                    if (_threadSend.IsAlive)
                        _threadSend.Abort();
                }
                catch (ThreadAbortException exception)
                {
                    App.logManager.Error("LuaSocketClient.Close Error:" + exception.Message);
                }
                _threadSend = null;
            }

            if (HandleReconnectCacheMsg)
            {
                TryCacheReconnectMsgs();
            }

            _sendQueue.Clear();
            _receiveQueue.Clear();
            _bufferBytes.Clear();
            _cacheBytes.Clear();
            _sendBytes.Clear();
			_readHead = false;
			_length = 0;

            Interlocked.Exchange(ref _checkErrorCodeLock, 0);
            App.timerManager.Unregister(HandleMessage);
            App.timerManager.Unregister(HandleConnectFaild);
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public virtual void Dispose()
        {
            Close();
        }

        /// <summary>
        /// 接收数据线程
        /// </summary>
        protected virtual void ReceiveDataThread()
        {
            while (true)
            {
                if (!IsSocketConnected())
                {
                    break;
                }

                try
                {
                    int length = _socket.Receive(_bufferReceive);
                    if (length > 0)
                        Receive(_bufferReceive, length);
                }
                catch (ThreadAbortException exc)
                {
                    UnityEngine.Debug.LogWarning(string.Format("LuaSocketClient.ReceiveData ThreadAbortException:{0}", exc.Message));
                }
                catch (SocketException exception)
                {
                    UnityEngine.Debug.LogWarning(string.Format("LuaSocketClient.ReceiveData ErrorCode:{0} ErrorMessage:{1}", exception.ErrorCode, exception.Message));
                    Interlocked.Exchange(ref _checkErrorCodeLock, exception.ErrorCode);
                    //break;
                }

                try
                {
                    Thread.Sleep(1);
                }
                catch (ThreadAbortException exc)
                {
                    UnityEngine.Debug.LogWarning(string.Format("LuaSocketClient.ReceiveData ThreadAbortException:{0}", exc.Message));
                }
                catch (SocketException exception)
                {
                    UnityEngine.Debug.LogError("SocketClient.ReceiveData Error:" + exception.Message);
                    //break;
                }
            }
        }

        /// <summary>
        /// 发送数据线程
        /// </summary>
        protected virtual void SendDataThread()
        {
            while (true)
            {
                if (!IsSocketConnected())
                {
                    break;
                }

                try
                {
                    while (_sendQueue.Count > 0)
                        lock (_sendQueue)
                            _socket.Send(_sendQueue.Dequeue());

                }
                catch (ThreadAbortException)
                {

                }
                catch (SocketException exception)
                {
                    UnityEngine.Debug.LogWarning(string.Format("LuaSocketClient.SendData ErrorCode:{0} ErrorMessage:{1}", exception.ErrorCode, exception.Message));
                    Interlocked.Exchange(ref _checkErrorCodeLock, exception.ErrorCode);
                    //break;
                }

                try
                {
                    Thread.Sleep(1);
                }
                catch (ThreadAbortException)
                {

                }
                catch (SocketException exception)
                {
                    UnityEngine.Debug.LogError("SocketClient.SendData Error:" + exception.Message);
                    //break;
                }
            }
        }

        protected void CallLuaOnConnect(string socketId)
        {
            if (_luaOnConnect != null)
            {
                _luaOnConnect.BeginPCall();
                _luaOnConnect.Push(_luaTable);
                _luaOnConnect.Push(socketId);
                _luaOnConnect.PCall();
                _luaOnConnect.EndPCall();
            }
        }

        protected void CallLuaClose(string socketId)
        {
            if (_luaOnClose != null)
            {
                _luaOnClose.BeginPCall();
                _luaOnClose.Push(_luaTable);
                _luaOnClose.Push(socketId);
                _luaOnClose.PCall();
                _luaOnClose.EndPCall();
            }
        }

        protected void CallLuaOnReceive(int protocolID, uint sequenceNumber, string socketId, ByteArray bytes)
        {
            if (_luaOnReceive != null)
            {
                _luaOnReceive.BeginPCall();
                _luaOnReceive.Push(_luaTable);
                _luaOnReceive.Push(protocolID);
                _luaOnReceive.Push(sequenceNumber);
                _luaOnReceive.Push(socketId);
                _luaOnReceive.Push(bytes);
                _luaOnReceive.PCall();
                _luaOnReceive.EndPCall();
            }
        }

        protected void CallLuaOnConnectError(string socketId, int errorCode)
        {
            if (_luaOnConnectError != null)
            {
                _luaOnConnectError.BeginPCall();
                _luaOnConnectError.Push(_luaTable);
                _luaOnConnectError.Push(socketId);
                _luaOnConnectError.Push(errorCode);
                _luaOnConnectError.PCall();
                _luaOnConnectError.EndPCall();
            }
        }

        protected struct Message
        {
            public int protocolID;
            public uint sequenceNumber;
            public ByteArray bytes;

            public Message(int protocolID, uint sequenceNumber, ByteArray bytes)
            {
                this.protocolID = protocolID;
                this.sequenceNumber = sequenceNumber;
                this.bytes = bytes;
            }
        }
    }
}