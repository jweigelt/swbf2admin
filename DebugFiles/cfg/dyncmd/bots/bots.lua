function init()
end

function run(player, command, params)
	if(#params < 0) then
		api.Say(api.GetConfig("OnNoParam"), "{usage}", api.GetUsage())
		return false
	end
	
	local val = tonumber(params[1])
	if(val == nil) then
		api.Say(api.GetConfig("OnInvalidParam"), "{input}", params[1])		
	end
	
	local cmd = string.lower(api.GetServerInfo().GameMode) .. "aiperteam"	
	
	api.Say(api.GetConfig("OnSet"), "{admin}", player.Name, "{value}", params[1])			
	api.SendCommand(cmd, tostring(val))
end