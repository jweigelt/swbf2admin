function init()
end

function run(player, command, params)
	if(#params < 0) then
		api.Say(api.GetConfig("OnNoParam"), "{usage}", api.GetUsage())
		return false
	end
	
	local val = tonumber(params[1])
	if(val == nil or val < 0 or val > 60) then
		api.Say(api.GetConfig("OnInvalidParam"), "{input}", params[1])		
	end
	
	local cmd = string.lower(api.GetServerInfo().GameMode) 	
	
	
	if cmd == "eli" then
		mode = "ctf"
    end
	
	cmd = cmd .. "timelimit"
	
	api.Say(api.GetConfig("OnSet"), "{admin}", player.Name, "{value}", params[1])			
	api.SendCommand(cmd, tostring(val))
end