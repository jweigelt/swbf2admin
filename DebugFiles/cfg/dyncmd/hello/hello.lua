function init()
	api.Log(api.LogLevel_Verbose, "Preparing to greet the world...")
end

function run(player, command, params)
	api.Log(api.LogLevel_Verbose, "Greeting {0}.", player.Name) 
	api.Pm(api.GetConfig("OnHello"), player,"{player}", player.Name) 
end