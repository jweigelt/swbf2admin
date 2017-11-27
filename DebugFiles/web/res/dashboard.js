var ServerStatus = {
  Online:    {id: 0, statusText: "Server is running." , textClass: "online", commandText: "Stop", icon: "stop"}, 
  Offline :  {id: 1, statusText: "Server is stopped.", textClass: "offline", commandText: "Start", icon: "play_arrow"}, 
  Starting : {id: 2, statusText: "Server is starting...", textClass: "loading", commandText: "Stop", icon: "stop"},  
  Stopping : {id: 3, statusText: "Server is stopping...", textClass: "loading", commandText: "Start", icon: "play_arrow"}
};
var DashboardUrl = "/live/dashboard";
var DashboardUpdateInterval = 3000;

function Dashboard() {
  var base = this;
  this.timer = null;

  this.onInit = function() {
    $("#dash_btn_startstop").click(function(e) {
      e.preventDefault();
      base.toggleStatus();
    });
    base.updateStatus();    
    base.timer = setInterval(base.updateStatus, DashboardUpdateInterval);
  } 

  this.onStatusChange = function(online) { 
    if(online) {
      $("#dash_div_server_status").removeClass("disabled");
      $("#dash_div_game_status").removeClass("disabled");
      $("#dash_div_game_setup").removeClass("disabled");    
    } else {
      $("#dash_div_server_status").addClass("disabled");
      $("#dash_div_game_status").addClass("disabled");
      $("#dash_div_game_setup").addClass("disabled");   
    } 
  }
  
  this.onDeinit = function() {
    if(base.timer != null) clearInterval(base.timer);
  }

  this.setStatus = function(r) {
    var s = null;
    for (var x in ServerStatus) {
      if(ServerStatus[x].id == r.StatusId)  {
        s = ServerStatus[x];      
        break;
      }
    }
    if(s == null) return;
    
    base.status = s;
    $("#dash_txt_status").html(s.statusText);
    $("#dash_txt_status").attr("class", s.textClass);
    $("#dash_btn_startstop span").html(s.commandText);
    $("#dash_btn_startstop i").html(s.icon);   
      
    $("#dash_txt_ipep").html(r.ServerIP);
    $("#dash_txt_session_name").html(r.ServerName);
    $("#dash_txt_server_version").html(r.Version);
    
    $("#dash_txt_current_map").html(r.CurrentMap);    
    $("#dash_txt_next_map").html(r.NextMap);
    $("#dash_txt_gamemode").html(r.GameMode);
    $("#dash_txt_ff").html(r.FFEnabled);
    $("#dash_txt_heroes").html(r.Heroes);    

    $("#dash_txt_players_max").html(r.MaxPlayers);    
    $("#dash_txt_players_online").html(r.Players);
    $("#dash_txt_scores").html(r.Scores);
    $("#dash_txt_tickets").html(r.Tickets);      
  };
  
  this.updateStatus = function(s) {
    $.post({
      url: DashboardUrl,
      data: '{"action":"status_get"}',
    }).done(function(res) {
      base.setStatus(jQuery.parseJSON(res));
    });  
  };
  
  this.toggleStatus = function() {
    var s = null;
        
    if(base.status == ServerStatus.Online) {
      s = ServerStatus.Offline;
    }else if (base.status == ServerStatus.Offline) {            
      s = ServerStatus.Online;
    }else{
      return;
    }  
    
    $.post({
      url: DashboardUrl,
      data: '{"action":"status_set","NewStatusId":'+s.id+'}',
    }).done(function(res) {
      base.setStatus(jQuery.parseJSON(res));
    });      
  };
   
}

mainFrame.setActivePage(new Dashboard());