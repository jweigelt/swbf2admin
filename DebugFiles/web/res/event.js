var EventTimeout = 5000;

function EventDisplay(container) {
  var base = this;
  
  this.infoIcon = $('<span><i class="material-icons">info_outline</i>Info </span>');
  this.errorIcon =   $('<span><i class="material-icons">error_outline</i>Exception </span>');
  this.txt = $('<div class="text"></div>');
  
  this.container = $(container);
  this.container.addClass("item");
  this.container.hide();
  
  this.timeout = null;
  
  this.ShowError = function(message) {
    base.Show(base.infoIcon, message, "error");
  };
  
  this.ShowInfo = function(message) {
    base.Show(base.infoIcon, message, "info");
  };
  
  this.Show = function(ico, msg, cls) {
    base.container.empty();
    base.container.append(ico); 
    base.container.append(base.txt);         
    base.container.removeClass("error"); 
    base.container.removeClass("info"); 
    base.container.addClass(cls);
    base.txt.html(msg);
    base.container.slideDown();
    
    if(base.timeout != null) clearTimeout(base.timeout);
    base.timeout = setTimeout(function(){base.Hide();}, EventTimeout);
  };
  
  this.Hide = function() {
     base.container.slideUp(); 
  }; 
}