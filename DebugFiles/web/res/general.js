var GeneralUrl = "/settings/general";

function General() {
  var base = this;
 
  this.events = new EventDisplay("#general_div_error"); 

  this.onInit = function() {       
    $("#general_btn_save").click(function(){
      base.saveSettings();  
    });
    
    base.updateSettings();   
  };  
  
  this.onStatusChange = function(online) {    
    $(".fixed input, .fixed select").each(function(i,e){ $(e).prop( "disabled", online ); });
  };
  
  this.onDeinit = function() { }; 
  
  this.setSaved = function(r) {
    if(r != false) {
      if(r.Ok) {
        $("#general_txt_status").attr("class", "online");
        $("#general_txt_status").html("Settings saved");
        return;
      }else{
        base.events.ShowError(r.Error);        
      }
    }
    $("#general_txt_status").attr("class", "offline");
    $("#general_txt_status").html("Not saved");   
  };

  this.saveSettings = function() {
    $("#general_txt_status").attr("class", "loading");
    $("#general_txt_status").html("Saving...");
    
    var settings = {
      Action: "general_set",
      Settings: { 
        GameName: $("#general_input_session_name").val(),
        Password: $("#general_input_password").val(),
        AdminPw: $("#general_input_admin_password").val(),
        PlayerCount: $("#general_input_max_players").val(),
        PlayerLimit: $("#general_input_min_players").val(),
        Tps: $("#general_input_tps").val(), 
        IP: $("#general_select_ipa").val(),
        GamePort: $("#general_input_gameport").val(),
        RconPort: $("#general_input_rconport").val(), 
        Lan: $("#general_select_lan").val(),
        Bandwidth: $("#general_select_bandwidth").val()
      }
    };
    
    $.post({
      url: GeneralUrl,
      data: JSON.stringify(settings)
    }).done(function(res) {
      base.setSaved(JSON.parse(res));
    });  
  };    

  this.setSettings = function(r) {   
    $("#general_select_ipa").empty();
    for (var x in r.NetworkDevices) {
      var iface = r.NetworkDevices[x];
      $("#general_select_ipa").append($('<option value="'+iface.IPAddress+'">'+iface.IPAddress+' ('+iface.Name+')</option>'));          
    }   
    
    var s = r.Settings;
    $("#general_input_session_name").val(s.GameName);
    $("#general_input_password").val(s.Password);
    $("#general_input_admin_password").val(s.AdminPw);
    $("#general_input_max_players").val(s.PlayerCount);
    $("#general_input_min_players").val(s.PlayerLimit);
    $("#general_input_tps").val(s.Tps);     
    $("#general_select_ipa").val(s.IP);
    $("#general_input_gameport").val(s.GamePort);
    $("#general_input_rconport").val(s.RconPort);
    $("#general_select_lan").val(s.Lan.toString());
    $("#general_select_bandwidth").val(s.Bandwidth);
  };
  
  this.updateSettings = function() {
    $.post({
      url: GeneralUrl,
      data: '{"Action":"general_get"}',
    }).done(function(res) {
      base.setSettings(JSON.parse(res));
    });  
  };
}

mainFrame.setActivePage(new General());