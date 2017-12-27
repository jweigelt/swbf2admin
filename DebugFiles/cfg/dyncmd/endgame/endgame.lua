function init()
end

function run(player, command, params)
	api.Say(api.GetConfig("OnEnd"), "{admin}", player.Name)			
	api.SendCommand("endgame")
end