function Tab(container) {
  var base = this;
  this.container = $(container);

  this.updateTab = function() {
    $(container).find("a").each(function(i,e) {
      if(!$(e).hasClass("active")) {
        $("#"+$(e).attr("href")).hide();
      }else{
        $("#"+$(e).attr("href")).show();
      }
    });
  };
 
  $(container).find("a").each(function(i,e) {
    $(e).click(function(e) {
      $(container).find("a").each(function(i,e) {
        $(e).removeClass("active");
      });
      $(this).addClass("active");
      base.updateTab();
      e.preventDefault();
    });
  });
  
  this.updateTab();
}