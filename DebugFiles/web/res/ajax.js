var MainFrameUrl = "/";
var MainFrameUpdateInterval = 3000;

function MainFrame() {
  var base = this;
  this.statusTimer = null;
  this.online = false;
  this.activePage = null;
  
  this.setActivePage = function(p) {
    base.activePage = p;
    base.activePage.onInit();
    base.activePage.onStatusChange(base.online);
  }

  this.isOnline = function() { return base.online; };

  this.loadPage = function(href) {
    $.get(href, function( data ) {
      $("td#content").html( data );
    });
  };
  
  this.init = function() {
    $("td#navigation a").each(function(i,e) {
      $(e).click(function(e) {
        e.preventDefault(); 
        if(base.activePage != null) base.activePage.onDeinit();
        base.loadPage($(this).attr("href"));         
        $("td#navigation a").each(function(i,e) {
          $(e).removeClass("active");  
        });
        $(this).addClass("active");
      });
    }); 
    statusTimer = setInterval(base.updateStatus, MainFrameUpdateInterval);   
    
    base.loadPage("/live/dashboard");    
    base.updateStatus();
  };
  
  this.updateStatus = function() {
    $.post({
      url: MainFrameUrl,
      data: '{"Action":"status_get"}',
    }).done(function(res) {
      base.setStatus(jQuery.parseJSON(res));
    });      
  };
 
  this.setStatus = function(s) {  
    if(base.activePage != null && base.online != s.Online) base.activePage.onStatusChange(s.Online);  
    base.online = s.Online;
    $("i#status").css("color",(base.online  ?"#7fd173":"#ff5e42"));    
  }; 
}

var mainFrame = new MainFrame();

$(document).ready(function() {
  mainFrame.init();  
});