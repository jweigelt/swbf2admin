var GameUrl = "/settings/game";

function General() {
  var base = this;
 
  this.events = new EventDisplay("#game_div_error"); 
  this.tabs = new Tab("#game_tabs");
  
  this.onInit = function() {       
    $("#game_btn_save").click(function(){ base.saveSettings(); });    
    base.updateSettings();   
    $("#game_div_container input").each(function(i,e) {
      $(e).change(function(evt){
        base.setSaved(false);
      });   
    });    
  };  
  
  this.onStatusChange = function(online) {    
    $(".fixed input, .fixed select").each(function(i,e){ $(e).prop( "disabled", online ); });
  };
  
  this.onDeinit = function() { }; 
  
  this.setSaved = function(r) {
    if(r != false) {
      if(r.Ok) {
        $("#game_txt_status").attr("class", "online");
        $("#game_txt_status").html("Settings saved");
        return;
      }else{
        base.events.ShowError(r.Error);        
      }
    }
    $("#game_txt_status").attr("class", "offline");
    $("#game_txt_status").html("Not saved");   
  };

  this.saveSettings = function() {
    $("#game_txt_status").attr("class", "loading");
    $("#game_txt_status").html("Saving...");
    
    var settings = {
      Action: "game_set",
      Settings: { 
        Heroes: $("#game_input_hero_enable").prop("checked"),
        HrUnlock: $("#game_select_hero_unlock_trigger").val(),
        HrUnlockValue: $("#game_input_hero_unlock_value").val(),
        HrPlayer: $("#game_select_hero_player").val(),
        HrTeam: $("#game_select_hero_team").val(),
        HrRespawn: $("#game_input_hero_timeout").val(),
                 
        TeamDamage: $("#game_input_ff_enable").prop("checked"),
        Awards: $("#game_input_awards_enable").prop("checked"),
        Shownames: $("#game_input_names_enable").prop("checked"), 
        AutoAssignTeams: $("#game_select_teams").val(),
        PreGameTime: $("#game_input_warmup").val(),
        Spawn: $("#game_input_spawn").val(),  
              
        KickVoteThreshold: $("#game_input_kick_ratio").val(),
        TeamVoteThreshold: $("#game_input_kick_team_ratio").val(),    
                   
        ConReinforcements: $("#game_input_con_reinforcements").val(),        
        ConTimeLimit: $("#game_input_com_time").val(),
        ConAiperTeam: $("#game_input_con_aiperteam").val(),
        
        CTFScoreLimit: $("#game_input_ctf_score").val(), 
        CTFTimeLimit: $("#game_input_ctf_time").val(),
        CTFAiPerTeam: $("#game_input_ctf_aiperteam").val(),
        
        AssReinforcements: $("#game_input_ass_reinforcements").val(), 
        AssScoreLimit: $("#game_input_ass_score").val(),
        AssAiPerTeam: $("#game_input_ass_aiperteam").val(),
                
        EliTimeLimit: $("#game_input_eli_time").val(),
        EliAiPerTeam: $("#game_input_eli_aiperteam").val(),  
                      
        HuntScoreLimit: $("#game_input_hunt_score").val(),
        HunTimeLimit: $("#game_input_hunt_time").val()        
      }
    };
    
    $.post({
      url: GameUrl,
      data: JSON.stringify(settings)
    }).done(function(res) {
      base.setSaved(JSON.parse(res));
    });  
  };    

  this.setSettings = function(r) {       
    var s = r.Settings;
    $("#game_input_hero_enable").prop("checked", s.Heroes);
    $("#game_select_hero_unlock_trigger").val(s.HrUnlock);
    $("#game_input_hero_unlock_value").val(s.HrUnlockValue);
    $("#game_select_hero_team").val(s.HrTeam);
    $("#game_select_hero_player").val(s.HrPlayer);
    $("#game_input_hero_timeout").val(s.HrRespawn);                
        
    $("#game_input_ff_enable").prop("checked", s.TeamDamage);
    $("#game_input_awards_enable").prop("checked", s.Awards);          
    $("#game_input_names_enable").prop("checked", s.Shownames);        
    $("#game_select_teams").val(s.AutoAssignTeams.toString());
    $("#game_input_warmup").val(s.PreGameTime);
    $("#game_input_spawn").val(s.Spawn);       
    
    $("#game_input_kick_ratio").val(s.KickVoteThreshold);
    $("#game_input_kick_team_ratio").val(s.TeamVoteThreshold);
    
    $("#game_input_con_reinforcements").val(s.ConReinforcements);
    $("#game_input_com_time").val(s.ConTimeLimit);
    $("#game_input_con_aiperteam").val(s.ConAiperTeam);
    
    $("#game_input_ctf_score").val(s.CTFScoreLimit);
    $("#game_input_ctf_time").val(s.CTFTimeLimit);
    $("#game_input_ctf_aiperteam").val(s.CTFAiPerTeam);
    
    $("#game_input_ass_reinforcements").val(s.AssReinforcements);
    $("#game_input_ass_score").val(s.AssScoreLimit);
    $("#game_input_ass_aiperteam").val(s.AssAiPerTeam);
    
    $("#game_input_eli_time").val(s.EliTimeLimit);
    $("#game_input_eli_aiperteam").val(s.EliAiPerTeam);
    
    $("#game_input_hunt_score").val(s.HuntScoreLimit);
    $("#game_input_hunt_time").val(s.HunTimeLimit);    
  };
  
  this.updateSettings = function() {
    $.post({
      url: GameUrl,
      data: '{"Action":"game_get"}',
    }).done(function(res) {
      base.setSettings(JSON.parse(res));
    });  
  };
}

mainFrame.setActivePage(new General());