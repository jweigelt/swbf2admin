function init()
end

function showTotal(stats, player)
	api.Say(api.GetConfig("OnTotal"),
		"{score}", tostring(stats.TotalScore),
		"{kills}", tostring(stats.TotalKills),
		"{deaths}", tostring(stats.TotalDeaths),
		"{joined}", tostring(player.TotalVisits))
end

function showAvg(stats)
	api.Say(api.GetConfig("OnAVG"),
		"{kd}", string.format("%.2f", stats.TotalKDRatio),
		"{sg}", string.format("%.2f", stats.TotalSGRatio),
		"{kg}", string.format("%.2f", stats.TotalKGRatio),
		"{dg}", string.format("%.2f", stats.TotalDGRatio))
end

function showGame(stats)
	api.Say(api.GetConfig("OnGame"),		
		"{won}", tostring(stats.GamesWon),
		"{lost}", tostring(stats.GamesLost),
		"{quit}", tostring(stats.GamesQuit))
end

function run(player, command, params)
	
	local show = api.GetConfig("ArgAll")
	
	if((#params > 0)) then
		show = params[1] 
	end
	
	
	local stats = api.GetPlayerStats(player)
	if(stats == nil) then
		api.Say(api.GetConfig("OnNoStats"), "{player}", player.Name)	  
		return false
	end
	
	if(show == api.GetConfig("ArgAll")) then
		showTotal(stats, player)
		showAvg(stats)
		showGame(stats, player)
	elseif(show == api.GetConfig("ArgTotal")) then
		showTotal(stats, player)
	elseif(show == api.GetConfig("ArgAVG")) then	
		showAvg(stats)
	elseif(show == api.GetConfig("ArgGame")) then
		showGame(stats)
	else
		api.Say(api.GetConfig("OnInvalidArg"),
			"{input}", show,
			"{arg_total}", api.GetConfig("ArgTotal"),
			"{arg_avg}", api.GetConfig("ArgAVG"),
			"{arg_game}", api.GetConfig("ArgGame"))			
	end

	return true
end