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

using WebSocketSharp;

namespace Framework
{
    public class WebSocketClient : SocketClient
    {
        // Socket属性
        protected string _url;

        protected WebSocket _webSocket;
        public WebSocketClient(string id) : base (id)
        {
        }

        public WebSocketClient(ISocketHandler socketHandler) : this("default")
        {
            _socketHandler = socketHandler;
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

                _webSocket.ConnectAsync();
            }
            catch (SocketException exception)
            {
                App.logManager.Error("LuaSocketClient.Connect Error:" + exception.SocketErrorCode + " " + exception.Message);
                _errorCode = (int)exception.SocketErrorCode;
                App.timerManager.RegisterFrameOnce(1, HandleConnectFaild);
            }
        }

        private void OnOpen(object sender, EventArgs e)
        {
            if (_webSocket.IsConnected)
            {
                App.timerManager.RegisterFrameLoop(1, HandleMessage);

                //_threadReceive = new Thread(ReceiveDataThread);
                //_threadReceive.IsBackground = true;
                //_threadReceive.Start();

                _threadSend = new Thread(SendDataThread);
                _threadSend.IsBackground = true;
                _threadSend.Start();

                App.logManager.Info("连接服务器成功");

                if (null != onConnectEvent) onConnectEvent(this);
            }
            else
            {
                if (null != onConnectErrorEvent) onConnectErrorEvent(this, -1);
            }
            _connected = _webSocket.IsConnected;
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            if (null != onCloseEvent) onCloseEvent(this);
            _connected = false;
            //_errorCode = e.Reason;
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            App.logManager.Info("接收服务器成功");

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
                    App.logManager.Error(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Debug:
                    App.logManager.Info(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Info:
                    App.logManager.Info(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Warn:
                    App.logManager.Warn(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Error:
                    App.logManager.Error(data.Message);
                    break;
                case WebSocketSharp.LogLevel.Fatal:
                    App.logManager.Fatal(data.Message);
                    break;
            }
        }

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
                //CallLuaClose(_id);
                return;
            }

            // MARK:CC 标记有异常发生，立即处理给外部
            int location1 = Interlocked.CompareExchange(ref _checkErrorCodeLock, 0, 0);
            if (location1 > 0)
            {
                _errorCode = location1;
                Interlocked.Exchange(ref _checkErrorCodeLock, 0);
                App.logManager.Warn("网络连接出错，回调给Lua端，ErrorCode:" + _errorCode);
                Close();
                return;
            }

            while (_receiveQueue.Count > 0)
            {
                Message msg;
                lock (_receiveQueue)
                {
                    msg = _receiveQueue.Dequeue();
                }
                if (null != _socketHandler)
                {
                    _socketHandler.Receive(msg.protocolID, msg.sequenceNumber, msg.bytes);
                }
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
                    _webSocket.CloseAsync();
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
                            _webSocket.SendAsync(_sendQueue.Dequeue(), (bool sendResult) =>
                            {
                                if (!sendResult)
                                {
                                    App.logManager.Error("发送失败");
                                }
                            });

                            //_webSocket.Send(_sendQueue.Dequeue());
                        }
                }
                catch (ThreadAbortException exc)
                {
                    App.logManager.Warn(string.Format("LuaSocketClient.ReceiveData ThreadAbortException:{0}", exc.Message));
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
                catch (ThreadAbortException exc)
                {
                    App.logManager.Warn(string.Format("LuaSocketClient.ReceiveData ThreadAbortException:{0}", exc.Message));
                }
                catch (SocketException exception)
                {
                    App.logManager.Error("SocketClient.ReceiveData Error:" + exception.Message);
                    //break;
                }
            }
        }
    }
}
