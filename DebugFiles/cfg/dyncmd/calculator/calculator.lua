--[[
	Dynamic command scripting calculator example
]]--

local val = 0

function init()
end

function run(player, command, params)
	if(#params < 1) then
		syntaxError()
		return false
	end		
	
	if(params[1] == "c") then
		val = 0
		api.Say(api.GetConfig("OnClear"))
		return true
	elseif(params[1] == "r") then
		api.Say(api.GetConfig("OnCalc"), "{value}", tostring(val))	
		return true
	end

	if(#params < 2) then
		syntaxError()
		return false
	end
	
	local lv = tonumber(params[2])
	
	if(lv == nil) then
		api.Say(api.GetConfig("OnInvalidNumber"), "{input}", params[2])
		return false
	end
			
	if(params[1] == "+")	 then val = val + lv
	elseif(params[1] == "-") then val = val - lv
	elseif(params[1] == "*") then val = val * lv
	elseif(params[1] == "/") then val = val / lv
	else
		syntaxError()
		return false
	end
	api.Say(api.GetConfig("OnCalc"), "{value}", tostring(val))
end

function syntaxError()
	api.Say(api.getConfig("OnSyntaxError"), "{usage}", api.getUsage())
end