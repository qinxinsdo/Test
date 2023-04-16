using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Framework
{
    public class SocketClient
    {
        // 缓冲区长度
        protected const int BUFFER_SIZE = 65535;
        // 包头长度
        protected const int HEADER_LENGTH = 4;
        protected const int PROTOCOL_LENGTH = 6;

        public System.Action<SocketClient> onConnectEvent = null;
        public System.Action<SocketClient, int> onConnectErrorEvent = null;
        public System.Action<SocketClient> onCloseEvent = null;

        // Socket属性
        protected string _id;
        protected string _host;
        protected int _port;
        protected ISocketHandler _socketHandler;

        protected Socket _socket;
        protected Thread _threadReceive;
        protected Thread _threadSend;
        protected byte[] _bufferReceive;

        // 数据缓冲
        protected Queue<byte[]> _sendQueue;
        protected Queue<Message> _receiveQueue;
        protected ByteArray _bufferBytes;
        protected ByteArray _cacheBytes;
        protected ByteArray _sendBytes;
        protected bool _connected;
        protected int _errorCode;

        protected bool _readHead;
        protected int _length;

        // 做个原子锁的变量
        protected int _checkErrorCodeLock = 0;

        //协议类型枚举
        enum ADDRESSFAM
        {
            IPv4,
            IPv6
        }

// #if UNITY_IPHONE && !UNITY_EDITOR
//         [DllImport("__Internal")]
//         private static extern string getIPv6(string host);
// #endif
        private string GetIPv6(string host)
        {
// #if UNITY_IPHONE && !UNITY_EDITOR
// 		    return getIPv6 (host);
// #else
            return host + "&&ipv4";
// #endif
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

        public SocketClient(string id)
        {
            _id = id;
            _bufferReceive = new byte[BUFFER_SIZE];

            _bufferBytes = new ByteArray();
            _cacheBytes = new ByteArray();
            _sendBytes = new ByteArray();

            _sendQueue = new Queue<byte[]>();
            _receiveQueue = new Queue<Message>();
        }

        public SocketClient(ISocketHandler socketHandler) : this("default")
        {
            _socketHandler = socketHandler;
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

        public ISocketHandler SocketHandler
        {
            get { return _socketHandler; }
            set { _socketHandler = value; }
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

                    if (null != onConnectEvent) onConnectEvent(this);
                }
            }
            catch (SocketException exception)
            {
                App.logManager.Error("LuaSocketClient.ConnectCallback Error:" + exception.SocketErrorCode + " " + exception.Message);
                _errorCode = (int)exception.SocketErrorCode;

                if (null != onConnectErrorEvent) onConnectErrorEvent(this, _errorCode);
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
                //CallLuaClose(_id);
                return;
            }
            else if (_connected == false)
            {
                _connected = true;
                //CallLuaOnConnect(_id);
            }

            // MARK:CC 标记有异常发生，立即处理给外部
            int location1 = Interlocked.CompareExchange(ref _checkErrorCodeLock, 0, 0);
            if (location1 > 0)
            {
                _errorCode = location1;
                Interlocked.Exchange(ref _checkErrorCodeLock, 0);
                App.logManager.Warn("网络连接出错，回调给Lua端，ErrorCode:" + _errorCode);
                Close();
                //CallLuaClose(_id);

                //HandleConnectFaild();
                return;
            }

            while (_receiveQueue.Count > 0)
            {
                Message msg;
                lock (_receiveQueue)
                    msg = _receiveQueue.Dequeue();
                //CallLuaOnReceive(msg.protocolID, msg.sequenceNumber, _id, msg.bytes);
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
                //CallLuaOnConnectError(_id, _errorCode);
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
        /// <param name="protocol"></param>
        public void Send(IProtocol protocol)
        {
            if (_socket.Connected == false)
            {
                App.socketManager.Close(this);
                return;
            }
            SocketHandler.Send(protocol);
        }

        /// <summary>
        /// 发送协议
        /// </summary>
        /// <param name="protocolID"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="bytes"></param>
        public virtual void Send(int protocolID, int sequenceNumber, ByteArray bytes)
        {
            if (!IsSocketConnected())
            {
                return;
            }

            bytes.Position = 0;
            // 清空发送数据
            _sendBytes.Clear();
            // 数据长度
            int dataLength = (int)bytes.Length + PROTOCOL_LENGTH;
            // 写入数据长度
            _sendBytes.WriteInt(dataLength);
            // 写入协议ID
            _sendBytes.WriteShort((short)protocolID);
            // 写入序列号
            _sendBytes.WriteInt(sequenceNumber);
            // 写入数据
            _sendBytes.WriteBytes(bytes.ReadBytes());
            // 读出数据
            _sendBytes.Position = 0;
            byte[] data = _sendBytes.ReadBytes((int)_sendBytes.Length);
            // 将协议数据放入队列
            lock (_sendQueue)
                _sendQueue.Enqueue(data);
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
                    // 读取协议ID
                    short protocolID = _bufferBytes.ReadShort();
                    // 读取序列号
                    int sequenceNumber = _bufferBytes.ReadInt();
                    // 清空包体数据
                    ByteArray contextBytes = new ByteArray();
                    // 判断包体是否有数据
                    if (_length > PROTOCOL_LENGTH)
                    {
                        // 数据长度
                        int dataLength = _length - PROTOCOL_LENGTH;
                        // 从缓冲区读取数据并写入到包体数据
                        contextBytes.WriteBytes(_bufferBytes.ReadBytes(dataLength));
                        contextBytes.Position = 0;
                    }
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
                catch (ThreadAbortException)
                {
                }
                catch (SocketException exception)
                {
                    App.logManager.Warn(string.Format("LuaSocketClient.ReceiveData ErrorCode:{0} ErrorMessage:{1}", exception.ErrorCode, exception.Message));
                    //tmp = Interlocked.Add(ref _checkErrorCodeLock, tmp);
                    Interlocked.Exchange(ref _checkErrorCodeLock, exception.ErrorCode);
                    break;
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
                    App.logManager.Error("SocketClient.ReceiveData Error:" + exception.Message);
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
                    App.logManager.Warn(string.Format("LuaSocketClient.SendData ErrorCode:{0} ErrorMessage:{1}", exception.ErrorCode, exception.Message));
                    Interlocked.Exchange(ref _checkErrorCodeLock, exception.ErrorCode);
                    break;
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
                    App.logManager.Error("SocketClient.SendData Error:" + exception.Message);
                    //break;
                }
            }
        }

        protected struct Message
        {
            public int protocolID;
            public int sequenceNumber;
            public ByteArray bytes;

            public Message(int protocolID, int sequenceNumber, ByteArray bytes)
            {
                this.protocolID = protocolID;
                this.sequenceNumber = sequenceNumber;
                this.bytes = bytes;
            }
        }
    }
}
