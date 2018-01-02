function init()
end

function run(player, command, params)
	if(#params < 0) then
		api.Say(api.GetConfig("OnNoParam"), "{usage}", api.GetUsage())
		return false
	end
		
	-- GetServerInfo().GameMode returns ass for eli
	local cmd = string.lower(api.GetServerInfo().GameMode) 
	local maxScore = 0
	
	if (cmd == "ass") then maxScore = 500  
	elseif (cmd == "ctf") then maxScore = 15 
	elseif (cmd == "hunt") then maxScore = 150 
	else
		api.Say(api.GetConfig("OnInvalidGameMode"), "{gamemode}", cmd)		
		return false
	end
	
	local val = tonumber(params[1])
	if(val == nil or val < 0 or val > maxScore) then
		api.Say(api.GetConfig("OnInvalidParam"), "{input}", params[1])		
	end
	
	cmd = cmd .. "scorelimit"	
	api.Say(api.GetConfig("OnSet"), "{admin}", player.Name, "{value}", params[1])			
	api.SendCommand(cmd, tostring(val))
end