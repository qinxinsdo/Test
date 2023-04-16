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

using WebSocketSharp;

namespace Framework
{
    public class LuaWebSocketClient : LuaSocketClient
    {
        protected string _url;
        protected WebSocket _webSocket;

        public LuaWebSocketClient(string id) : base(id)
        {
        }

        public string url
        {
            get { return _url; }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public override void Connect(string host, int port)
        {
            _host = host;
            _port = port;
            Connect(string.Format("ws://{0}:{1}", host, port));
        }

        public void Connect(string url)
        {
            _url = url;
            Connect();
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public override void Connect()
        {
            try
            {
                App.logManager.Info("Connecting to : " + this.url);

                Close();

                _webSocket = new WebSocket(this.url);
                _webSocket.Log.Output = WebSocketLog;
                _webSocket.OnOpen += OnOpen;
                _webSocket.OnError += OnError;
                _webSocket.OnMessage += OnMessage;
                _webSocket.OnClose += OnClose;

                //_webSocket.Connect();
                _webSocket.ConnectAsync();
            }
            catch (SocketException exception)
            {
                App.logManager.Error("LuaSocketClient.Connect Error:" + exception.SocketErrorCode + " " + exception.Message);
                _errorCode = (int)exception.SocketErrorCode;
                App.timerManager.RegisterFrameOnce(1, HandleConnectFaild);
            }
        }

        #region WebSocket回调
        private void OnOpen(object sender, EventArgs e)
        {
            App.logManager.Info("Websocket连接成功，当前threadID=" + Thread.CurrentThread.ManagedThreadId + " 主线程ID=" + App.mainThreadID);

            // 进入主线程中处理
            App.RunInMainThread(() =>
            {
                if (_webSocket.IsConnected)
                {
                    App.timerManager.RegisterFrameLoop(1, HandleMessage);

                    // MARK:CC 这里不需要做接受， WebSocket的OnMessage会处理
                    //_threadReceive = new Thread(ReceiveDataThread);
                    //_threadReceive.IsBackground = true;
                    //_threadReceive.Start();

                    _threadSend = new Thread(SendDataThread);
                    _threadSend.IsBackground = true;
                    _threadSend.Start();

                    App.logManager.Info("创建多线程发送消息，线程threadID=" + _threadSend.ManagedThreadId + " 主线程ID=" + App.mainThreadID);

                    UnityEngine.Debug.Log("连接服务器成功");

                    if (null != onConnectEvent) onConnectEvent(this);
                }
                else
                {
                    if (null != onConnectErrorEvent) onConnectErrorEvent(this, -1);
                }
                // MAKR:CC 这里先不要赋值，在HandleMessage会用到false做判断
                //_connected = _webSocket.IsConnected;
            });
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            if (null != onCloseEvent) onCloseEvent(this);
            _connected = false;
            //_errorCode = e.Reason;
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            //UnityEngine.Debug.Log("接收服务器成功");

            byte[] raws = e.RawData;
            if (raws.Length > 0)
            {
                Receive(raws, raws.Length);
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            HandleConnectFaild();
        }

        private void WebSocketLog(WebSocketSharp.LogData data, string file)
        {
            switch (data.Level)
            {
                case WebSocketSharp.LogLevel.Trace:
                    UnityEngine.Debug.LogError(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Debug:
                    UnityEngine.Debug.Log(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Info:
                    UnityEngine.Debug.Log(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Warn:
                    UnityEngine.Debug.LogWarning(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Error:
                    UnityEngine.Debug.LogError(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Fatal:
                    UnityEngine.Debug.LogError(data.Message);
                    break;
            }
        }
        #endregion WebSocket回调

        /// <summary>
        /// 重载判断
        /// </summary>
        /// <returns></returns>
        protected override bool IsSocketConnected()
        {
            if (_webSocket == null)
                return false;
            if (_webSocket.IsConnected == false)
                return false;

            return true;
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        protected override void HandleMessage()
        {
            if (_webSocket == null)
                return;

            if (_webSocket.IsConnected == false)
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

            //// TODO: CC 测试断网以及断线重连
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

            while (_receiveQueue.Count > 0)
            {
                Message msg;
                lock (_receiveQueue)
                    msg = _receiveQueue.Dequeue();
                CallLuaOnReceive(msg.protocolID, msg.sequenceNumber, _id, msg.bytes);
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public override void Close()
        {
            try
            {
                if (_webSocket != null)
                {
                    // MARK:CC 客户端修改为同步强制关闭
                    //_webSocket.CloseAsync();
                    _webSocket.Close();
                }
                _connected = false;
            }
            catch (SocketException exception)
            {
                App.logManager.Error("LuaSocketClient.Close Error:" + exception.Message);
            }
            finally
            {
                if (null != _webSocket)
                {
                    _webSocket.OnOpen -= OnOpen;
                    _webSocket.OnError -= OnError;
                    _webSocket.OnMessage -= OnMessage;
                    _webSocket.OnClose -= OnClose;
                }
                _webSocket = null;
            }

            base.Close();
        }

        /// <summary>
        /// 发送数据线程
        /// </summary>
        protected override void SendDataThread()
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
                        {
                            //_webSocket.SendAsync(_sendQueue.Dequeue(), (bool sendResult) =>
                            //{
                            //    if (!sendResult)
                            //    {
                            //        UnityEngine.Debug.LogError("发送失败");
                            //    }
                            //});
                            _webSocket.Send(_sendQueue.Dequeue());
                        }
                }
                catch (ThreadAbortException)
                {

                }
                catch (SocketException exception)
                {
                    UnityEngine.Debug.LogWarning(string.Format("LuaSocketClient.SendData ErrorCode:{0} ErrorMessage:{1}", exception.ErrorCode, exception.Message));
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
                    UnityEngine.Debug.LogError("SocketClient.SendData Error:" + exception.Message);
                    //break;
                }
            }
        }
    }
}