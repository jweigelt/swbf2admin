var ChatUrl = "/live/chat";
var ChatUpdateInterval = 2000;

function Chat() {
  var base = this;
  this.timer = null;

  this.onInit = function() {
    $("#chat_btn_send").click(base.sendChat);
  };

  this.onStatusChange = function(online) {
    if(online) {
      if(base.timer == null) base.timer = setInterval(base.updateChat, ChatUpdateInterval);
      base.updateChat(); 
      $("#chat_div_chat").removeClass("disabled");  
    }else{
      if(base.timer != null) clearInterval(base.timer);  
      $("#chat_div_chat").addClass("disabled");
    }
  };
  
  this.onDeinit = function() {
    if(base.timer != null) clearInterval(base.timer);
  }; 

  this.setChat = function(r) {
   var t = $("#chat_tbl_chat");   
    
    for (var x in r) {
      var rw = $("<tr><td>#"+r[x].PlayerName+": "+r[x].Message+"</td></tr>").hide();
      t.append(rw);
      rw.show("normal");
    }
        
    $('#chat_div_scroll').scrollTop($('#chat_div_scroll')[0].scrollHeight);
  };
  
  this.updateChat = function(s) {
    $.post({
      url: ChatUrl,
      data: '{"Action":"chat_update"}',
    }).done(function(res) {
      base.setChat(jQuery.parseJSON(res));
    });      
  };
  
  this.sendChat = function() {
    var msg = $("#chat_input").val();
    if(msg == "") return;
    $("#chat_input").val("");
    $.post({
      url: ChatUrl,
      data: '{"Action":"chat_send","Message":"'+msg+'"}',
    }).done(function(res) {
      base.setChat(jQuery.parseJSON(res));
    });     
  }
}

mainFrame.setActivePage(new Chat());