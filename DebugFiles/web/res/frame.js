var MainFrameUrl = "/";
var MainFrameUpdateInterval = 3000;

function MainFrame() {
    var base = this;
    this.statusTimer = null;
    this.online = false;
	this.enableRuntime = null;
    this.activePage = null;
    this.stopEvent = null;

    $(document).ajaxStop(function () {
        if (base.stopEvent != null) {
            base.stopEvent();
            base.stopEvent = null;
        }
    });

    this.setActivePage = function (p) {
        base.activePage = p;
        base.activePage.onInit();
        base.activePage.onStatusChange(base.online);
    };

    this.isOnline = function () { return base.online; };

    this.loadPage = function (href) {
        var e = { delay: false };
        if (base.activePage != null) {
            console.log("deinit()");
            base.activePage.onDeinit(e);
        }

        if ($.active > 0) {
            console.log("Waiting for pending ajax requests to finish...");
            //wait for any ajax-requests to finish before unloading the page
            base.stopEvent = function () { $.get(href, function (data) { $("td#content").html(data); }); };
        } else {
            $.get(href, function (data) { $("td#content").html(data); });
        }
    };

    this.init = function () {
        $("td#navigation a").each(function (i, e) {
            $(e).click(function (e) {
                e.preventDefault();
                base.loadPage($(this).attr("href"));
                $("td#navigation a").each(function (i, e) {
                    $(e).removeClass("active");
                });
                $(this).addClass("active");
            });
        });
        statusTimer = setInterval(base.updateStatus, MainFrameUpdateInterval);

        base.loadPage("/live/dashboard");
        base.updateStatus();
    };

    this.updateStatus = function () {
        $.post({
            url: MainFrameUrl,
            data: '{"Action":"status_get"}'
        }).done(function (res) {
            base.setStatus(jQuery.parseJSON(res));
        });
    };

    this.setStatus = function (s) {
        if (base.activePage != null && base.online != s.Online) base.activePage.onStatusChange(s.Online);
        base.online = s.Online;
		
		if(base.enableRuntime != s.EnableRuntime) {
			base.enableRuntime = s.EnableRuntime;		
		
			$("#navigation").children("a").each(function(i,e) {
				if($(e).hasClass("runtime")) {
					if(s.EnableRuntime) 
						$(e).show();
					else $(e).hide();
				}
			});
		}
		
        $("i#status").css("color", (base.online ? "#7fd173" : "#ff5e42"));
    };
}

var mainFrame = new MainFrame();

$(document).ready(function () {
    mainFrame.init();
});