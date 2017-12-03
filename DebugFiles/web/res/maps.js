var MapsUrl = "/settings/maps";

var MapFlags = {
  GCWCon : (1 << 0), GCWCTF : (1 << 1), GCW1Flag : (1 << 2), GCWHunt : (1 << 3), GCWEli : (1 << 4),
  CWCon : (1 << 10), CWCTF : (1 << 11), CW1Flag : (1 << 12), CWHunt : (1 << 13), CWEli : (1 << 14)
};

function Maps() {
  var base = this;
  this.dialog = null;
  this.mapList = [];
  this.events = new EventDisplay("#maps_div_error");

  this.onInit = function() {       
    base.updateInstalledMaps();
      
    $("#maps_table_rotation").on("drop",function(e) {
      base.addMap(JSON.parse(e.originalEvent.dataTransfer.getData("map")));       
    });

    $("#maps_table_installed").on("drop",function(e) {
      base.dropMap(JSON.parse(e.originalEvent.dataTransfer.getData("map")));  
    });
      
    $("#maps_table_rotation").on("dragover", function(e) {
      e.preventDefault();
    });

    $("#maps_table_installed").on("dragover", function(e) {
      e.preventDefault();
    });
      
    $("#maps_btn_save").click(function(){
      base.saveMaps();  
    });
      
    base.dialog = new Dialog("#maps_div_add", "Pick Gamemodes", [{Text: "OK", Callback : base.dialogOK, Icon : "check"}]);      
  };  
  
  this.onStatusChange = function(online) {
  
  };
  
  this.onDeinit = function() {
    if(base.timer != null) clearInterval(base.timer);
  }; 
   
  this.setSaved = function(r) {
    if(r != false) {
      if(r.Ok) {
        $("#maps_txt_status").attr("class", "online");
        $("#maps_txt_status").html("Maps saved");
        return;
      }else{
        base.events.ShowError(r.Error);        
      }
    }
    $("#maps_txt_status").attr("class", "offline");
    $("#maps_txt_status").html("Not saved");   
  };
  
  this.saveMaps = function() {
    $("#maps_txt_status").attr("class", "loading");
    $("#maps_txt_status").html("Saving...");
  
    var req = { Action: "maps_save", Maps: []};
  
    $("#maps_table_rotation tr").each(function(i,e){
      req.Maps.push($(e).data("name"));  
    });    
    
    $.post({
      url: MapsUrl,
      data: JSON.stringify(req),
    }).done(function(res) {
      base.setSaved(jQuery.parseJSON(res));
    });
  };
  
  this.dialogOK = function(m) {  
    var tb = $("#maps_table_rotation");        
    $("#maps_div_add input").each(function(i,e) {
      if($(e).prop('checked')) {
        var tr = $(
        '<tr data-id="'+m.id+'" data-flags="'+m.flags+'" data-name="'+m.name+$(e).data("map")+'" data-nicename="'+m.nicename+'" draggable="true">' + 
        "<td>"+m.nicename+"</td>" + 
        "<td>"+m.name+$(e).data("map")+"</td>" + 
        "</tr>");
        tb.append(tr);
      
        tr.on("dragstart", function(e) {
          var m = {id: $(this).data("id"), flags: $(this).data("flags"),name: $(this).data("name"), nicename: $(this).data("nicename") };
          e.originalEvent.dataTransfer.setData("map",JSON.stringify(m)); 
        });         
      }
    }); 
  };
  
  this.addMap=function(map) {
    base.setSaved(false); 
    $("#maps_div_add input").each(function(i,e) {
      $(e).prop('checked', false);
      if((parseInt(map.flags) & MapFlags[$(e).data("flag")]) > 0) $(e).prop("disabled", false);
      else $(e).prop("disabled", true);
    });       
    base.dialog.show(map);
  };
  
  this.dropMap=function(map) {
    base.setSaved(false); 
    $("#maps_table_rotation tr").each(function(i,e) {
      if($(e).data("name") == map.name) {
        $(e).remove();
        return false;
      }
    });       
  };

  this.setInstalledMaps = function(r) {
    base.mapList = r;
    
    var tb = $("#maps_table_installed");
    tb.html("");
    for (var x in r) {
      var m = r[x];
      var tr = $(
      '<tr data-id="'+m.DatabaseId+'" data-flags="'+m.Flags+'" data-name="'+m.Name+'" data-nicename="'+m.NiceName+'" draggable="true">' + 
      "<td>"+m.NiceName+"</td>" + 
      "<td>"+m.Name+"</td>" + 
      "</tr>");
      tb.append(tr);
      
      tr.on("dragstart", function(e) {
        var m = {id: $(this).data("id"), flags: $(this).data("flags"),name: $(this).data("name"), nicename: $(this).data("nicename") };
        e.originalEvent.dataTransfer.setData("map",JSON.stringify(m)); 
      }); 
    }
    
    base.updateMapRotation();    
  };
  
  this.updateInstalledMaps = function() {
    $.post({
      url: MapsUrl,
      data: '{"Action":"maps_installed"}',
    }).done(function(res) {
      base.setInstalledMaps(jQuery.parseJSON(res));
    });  
  };

  this.setMapRotation = function(r) {
    if(!r.Ok) {
      base.events.ShowError(r.Error);
      return;
    }    
    
    var tb = $("#maps_table_rotation");
    tb.html("");
    
    for (var x in r.Maps) {
      var name = r.Maps[x];
      var m = base.getMap(name);    
      
      var tr = $(
        '<tr data-id="'+m.DatabaseId+'" data-flags="'+m.Flags+'" data-name="'+name+'" data-nicename="'+m.NiceName+'" draggable="true">' + 
        "<td>"+m.NiceName+"</td>" + 
        "<td>"+name+"</td>" + 
        "</tr>");
        tb.append(tr);
      
        tr.on("dragstart", function(e) {
          var m = {id: $(this).data("id"), flags: $(this).data("flags"),name: $(this).data("name"), nicename: $(this).data("nicename") };
          e.originalEvent.dataTransfer.setData("map",JSON.stringify(m)); 
        });         
            
      tb.append(tr);
    }         
  };
  
  this.getMap=function(name) {
    var sn = name.split("_")[0];
    var r = null;
    sn = sn.substring(0,sn.length-1);
    $.each(base.mapList, function(i,e) {
      if(e.Name === sn) {
        r = e;
        return false;
      }
    });  
    return r;
  };
  
  this.updateMapRotation = function() {
    $.post({
      url: MapsUrl,
      data: '{"Action":"maps_rotation"}',
    }).done(function(res) {
      base.setMapRotation(jQuery.parseJSON(res));
    });  
  };   
}

mainFrame.setActivePage(new Maps());