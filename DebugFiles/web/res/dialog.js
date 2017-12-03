function Dialog(container, title, buttons) {
  
  var base = this;
  this.container = $(container);
  this.container.wrap('<div class="dialog-overlay"></div>');
  this.container.prepend('<span>'+title+'<a href="#" class="dialog-close"><i class="material-icons">close</i></a></span>');
  this.container.append('<div class="dialog-buttons"></div>');
  this.container.addClass('dialog');
  this.overlay = this.container.parent();
  this.buttons = this.container.find(".dialog-buttons");
  this.tag = null;
  
  base.overlay.hide();
  
  for (var x in buttons) {
    var b = buttons[x]; 
    var a = $('<a href="#" class="button">'+b.Text+'</a>'); 
     
    if (typeof b.Icon != 'undefined') {
      a.prepend($('<i class="material-icons">'+b.Icon+'</i>'));
    }
          
    this.buttons.append(a);
    a.data("callback", b.Callback);
    
    a.click(function(e) {
      e.preventDefault();
      base.close();
      if(base.tag != null) $(this).data("callback")(base.tag);
      else $(this).data("callback")();
      base.tag = null; 
    });
  }
  
  this.container.find(".dialog-close").click(function(e) {
    e.preventDefault();
    base.close(); 
  });
  
  this.close = function() {
    base.onClose();
    base.overlay.fadeOut(200);
  };
  
  this.show = function() {
    base.tag = null;
    base.overlay.fadeIn(200);   
  };
  
  this.show = function(tag) {
    base.tag = tag;  
    base.overlay.fadeIn(200); 
  };  
  
  this.onClose = function() {};   
}