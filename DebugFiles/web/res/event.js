var EventTimeout = 5000;

function EventDisplay(container) {
  var base = this;
  
  this.infoIcon = $('<span><i class="material-icons">cloud_done</i>Info </span>');
  this.errorIcon =   $('<span><i class="material-icons">error_outline</i>Exception </span>');
  this.warnIcon =   $('<span><i class="material-icons">cloud_queue</i>Warning </span>');
  this.txt = $('<div class="text"></div>');
  
  this.container = $(container);
  this.container.addClass("item");
  this.container.addClass("event");
  this.container.hide();
  
  this.timeout = null;
  
  this.ShowError = function(message) {
    base.Show(base.errorIcon, message, "error", false, false);
  };
  
  this.ShowInfo = function(message) {
    base.Show(base.infoIcon, message, "info", true, false);
  };
  
  this.ShowWarning = function(message) {
    base.Show(base.warnIcon, message, "error", false, false);
  }
  
  this.Show = function(ico, msg, cls, autohide, r) {
    if(base.container.is(":visible") && !base.container.hasClass(cls)) {
      base.container.fadeOut(100, function(){
        base.Show(ico,msg,cls,autohide,true);
      });
    }else{
      base.container.empty();
      base.container.append(ico); 
      base.container.append(base.txt);         
      base.container.removeClass("error"); 
      base.container.removeClass("info"); 
      base.container.addClass(cls);
      base.txt.html(msg);
      if(r) base.container.fadeIn(); else base.container.slideDown();     
    }
    
    if(base.timeout != null) clearTimeout(base.timeout);
    if(autohide) base.timeout = setTimeout(function(){base.Hide();}, EventTimeout);
  };
  
  this.Hide = function() {
     base.container.slideUp(); 
  }; 
}