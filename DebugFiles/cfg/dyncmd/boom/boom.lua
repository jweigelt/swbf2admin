function init()

end

function run(player, command, params)
	-- spawn 25 time bombs around 0,0
	local s =
		"for i=0,5 do\n" ..
			"for j=0,5 do\n" ..
				"local m = CreateMatrix(0,0,0,0,j*10,15,i*10)\n" ..
				"CreateEntity(\"cis_weap_inf_timebomb_ord\", m)\n" ..
			"end\n" ..
		"end"
		
	api.IngameLua(s)
	api.Say("Kaboom!")
end