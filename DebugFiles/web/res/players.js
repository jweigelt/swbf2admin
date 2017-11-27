var PlayersUrl = "/live/players";
var PlayersUpdateInterval = 10000;

function Players() {
  var base = this;
  this.timer = null;

  this.onInit = function() {    
    $(window).blur(base.hideQuickAdmin);
    $(document).click(base.hideQuickAdmin);
  }  
  
  this.onStatusChange = function(online) {
    if(online) {
      if(base.timer == null) base.timer = setInterval(base.updatePlayers, PlayersUpdateInterval);
      base.updatePlayers(); 
      $("#players_div_players").removeClass("disabled");  
    }else{
      if(base.timer != null) clearInterval(base.timer);  
      $("#players_tbl_players tbody").html("");
      $("#players_td_noplayers").show();
      $("#players_div_players").addClass("disabled");  
    }
  }
  
  this.onDeinit = function() {
    if(base.timer != null) clearInterval(base.timer);
  } 

  this.setPlayers = function(r) {
    var tb="";
    var i = 0;
    for (var x in r) {
      var p = r[x];
      tb +=
      "<tr>" + 
      "<td>"+p.Slot+"</td>" + 
      "<td>"+p.Name+"</td>" + 
      "<td>"+p.Team+"</td>" + 
      "<td>"+p.Score+"</td>" +       
      "<td>"+p.Kills+"</td>" + 
      "<td>"+p.Deaths+"</td>" +
      "<td>"+p.Ping+"</td>" +
      "<td>"+p.RemoteAddressStr+"</td>" +
      "<td>"+p.KeyHash+"</td>" +
      "<td>"+"Group"+"</td>" +
      "</tr>";
      i++;
    }
    
    $("#players_tbl_players tbody").html(tb);
    
    $("#players_tbl_players tbody tr").each(function(i,e) {
      $(e).contextmenu(function(e) {
        e.preventDefault();
        $("#players_ul_admin").css("left",e.clientX);
        $("#players_ul_admin").css("top",e.clientY);
        $("#players_ul_admin").css("display","block");
      });
    });   
    
    if(i == 0){
      $("#players_td_noplayers").show();
    }else{
      $("#players_td_noplayers").hide();
    }    
  };
  
  this.updatePlayers = function(s) {
    $.post({
      url: PlayersUrl,
      data: '{"Action":"players_update"}',
    }).done(function(res) {
      base.setPlayers(jQuery.parseJSON(res));
    });  
  };
  
  this.hideQuickAdmin = function() {
    $("#players_ul_admin").css("display","none");
  } 
}

mainFrame.setActivePage(new Players());