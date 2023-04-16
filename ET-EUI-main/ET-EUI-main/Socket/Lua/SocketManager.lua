local SocketManager = {}

-- socket集合
local _sockets = {}
-- 协议处理集合
local _handlers = {}
-- 协议缓存
local _protocols = {}
-- 协议类型
local _protocolTypes = nil

-- 协议数据
local _protocolBytes = ByteArray.New()

-- 协议序列号
SocketManager.sequenceNumber_send = 0
SocketManager.sequenceNumber_receive = 0

-- Socket事件
SocketManager.startConnectEvent = event("startConnectEvent")
SocketManager.connectEvent = event("connectEvent")
SocketManager.closeEvent = event("closeEvent")
SocketManager.connectErrorEvent = event("connectErrorEvent")
SocketManager.sendEvent = event("sendEvent")
SocketManager.receiveEvent = event("receiveEvent")

SocketManager.postHandleProtocolEvent = event("postHandleProtocolEvent")

--- MARK:CC 判断用的那种Socket处理协议
SocketManager.useWebSocket = false
SocketManager.islogin = false
-- 设置协议类型
function SocketManager:SetProtocolTypes(protocolTypes)
	_protocolTypes = protocolTypes
end

-- 注册Socket
function SocketManager:RegisterSocket(socketID)
	local socket = _sockets[socketID]
	assert(socket == nil, "Socket \"" .. socketID .. " has already registered!")

	if SocketManager.useWebSocket then
		socket = LuaWebSocketClient.New(socketID)
	else
		socket = LuaSocketClient.New(socketID)
	end


	_sockets[socketID] = socket
	return socket
end

-- 注销Socket
function SocketManager:UnregisterSocket(socketID)
	local socket = _sockets[socketID]
	if socket ~= nil then
		_sockets[socketID] = nil
		return socket
	end
	return nil
end

-- 获取Socket
function SocketManager:GetSocket(socketID)
	return _sockets[socketID]
end

-- 连接
function SocketManager:Connect(socketID, host, port)
	print("SocketManager:连接 Connect")
	local socket = _sockets[socketID]
	assert(socket ~= nil, "Socket \"" .. socketID .. " is not exist!")
	-- assert(socket.connected == false, "Socket \"" .. socketID .. " has already connected!")
    if socket.connected then
        self:Close(socketID)
    end
	socket:Connect(host, port)
	self.startConnectEvent:Call(socket)
end

-- 关闭连接
function SocketManager:Close(socketID)
	print("SocketManager:Close")
	local socket = _sockets[socketID]
	assert(socket ~= nil, "Socket \"" .. socketID .. " is not exist!")
	socket:Close()
	self:OnClose(socketID)
end

-- 销毁Socket
function SocketManager:Destroy(socketID)
	local socket = _sockets[socketID]
	assert(socket ~= nil, "Socket \"" .. socketID .. " is not exist!")
	socket:Dispose()
	_sockets[socketID] = nil
end

-- 发送协议
function SocketManager:Send(protocol, socketID)
	if SocketManager.islogin == false then
		if protocol.protocolID ~= Protocols.login_req and protocol.protocolID ~= Protocols.reconnect_req then
			--Log.Info("no login ok send protocolID:"..protocol.protocolID)
			return
		end
	end
	local socket = _sockets[socketID]
	assert(socket ~= nil, "Socket \"" .. socketID .. " is not exist!")
	-- 清空协议数据
	_protocolBytes:Clear()
	-- 写入协议数据
	protocol:Encode(_protocolBytes)
	-- 发送数据
	socket:Send(protocol.protocolID, self.sequenceNumber_receive, _protocolBytes)
	protocol:Reset()
	self.sendEvent:Call(socket)
end

-- 注册协议回调
function SocketManager:RegisterHandler(protocolID, socketID, func)
	local handlerDict = _handlers[socketID]
	if handlerDict == nil then
		handlerDict = {}
		_handlers[socketID] = handlerDict
	end
	local handlerList = handlerDict[protocolID]
	if handlerList == nil then
		handlerList = {}
		handlerDict[protocolID] = handlerList
	end
	table.insert(handlerList, func)
end

-- 注销协议回调
function SocketManager:UnregisterHandler(protocolID, socketID, func)
	local handlerDict = _handlers[socketID]
	if handlerDict ~= nil then
		local handlerList = handlerDict[protocolID]
		if handlerList ~= nil then
			table.removeElement(handlerList, func)
		end
	end
end

-- 获取协议（接收时不重置，获取时重置）
function SocketManager:GetProtocolOnReceive(protocolID)
	local protocol = _protocols[protocolID]
	if protocol ~= nil then
		protocol:Reset()
	else
		local protocolType = _protocolTypes[protocolID]
		if protocolType ~= nil then
			protocol = protocolType.new()
			_protocols[protocolID] = protocol
		end
	end
	return protocol
end

-- 获取协议（发送完成重置，获取时不重置）
function SocketManager:GetProtocolOnSend(protocolID)
	local protocol = _protocols[protocolID]
	if protocol == nil then
		local protocolType = _protocolTypes[protocolID]
		if protocolType ~= nil then
			protocol = protocolType.new()
			_protocols[protocolID] = protocol
		end
	end
	return protocol
end

-- 销毁
function SocketManager:OnDestroy()
	for k, v in pairs(_sockets) do
		self:Destroy(k)
	end
end

-- C#端调用

-- 开始连接
function SocketManager:OnConnect(socketID)
	local socket = _sockets[socketID]
	assert(socket ~= nil, "Socket \"" .. socketID .. " is not exist!")
	self.connectEvent:Call(socket)
end

-- 开始连接
function SocketManager:OnClose(socketID)
	local socket = _sockets[socketID]
	assert(socket ~= nil, "Socket \"" .. socketID .. " is not exist!")
	self.closeEvent:Call(socket)
end

-- 接收数据
function SocketManager:OnReceive(protocolID, sequenceNumber, socketID, bytes)
	self.sequenceNumber_receive = sequenceNumber
    local socket = _sockets[socketID]
    if socket ~= nil then
        local protocol = self:GetProtocolOnReceive(protocolID)
        if protocol == nil then
            Log.Error("Unrecognized protocolID:" .. protocolID)
        else
            protocol:Decode(bytes)
            local handlerDict = _handlers[socketID]
            if handlerDict ~= nil then
                local handlerList = handlerDict[protocolID]
				if handlerList ~= nil then
					--Log.Info("Check protocolID:" .. protocolID)
					for i, v in ipairs(handlerList) do
						v(protocol)
					end

					---- MARK:CC 统一处理错误码
					self.postHandleProtocolEvent:Call(protocolID, protocol)
				end
            end
        end

        self.receiveEvent:Call(socket)
    end
end

-- 开始连接
function SocketManager:OnConnectError(socketID, errorCode)
	local socket = _sockets[socketID]
	assert(socket ~= nil, "Socket \"" .. socketID .. " is not exist!")
	self.connectErrorEvent:Call(socket, errorCode)
end

return SocketManager