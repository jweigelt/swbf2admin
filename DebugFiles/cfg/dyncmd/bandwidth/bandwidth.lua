function init()
end

function run(player, command, params)
	api.Say(api.GetConfig("OnBandwidth"), "{bandwidth}", api.SendCommand("bandwidth"))			
end