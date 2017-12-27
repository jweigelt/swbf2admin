function init()
end

function run(player, command, params)
	api.Say(api.GetConfig("OnNextMap"), "{map}", api.GetServerInfo().NextMap)			
end