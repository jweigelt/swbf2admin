var StatsUrl = "/db/statistics";
var StatsFilterTimeout = 300;
var StatsRowsPerPage = 20;

function Stats() {
    var base = this;

    this.quickAdminId = null;
    this.quickAdminRow = null;
    this.loading = false;
    this.hitEnd = false;
    this.page = 0;
    
    this.onInit = function () {
        $(window).blur(base.hideQuickAdmin);
        $(document).click(base.hideQuickAdmin);

        base.deleteDialog = new Dialog("#stats_div_delete", "Delete match", [{ Text: "Yes", Callback: base.deleteDialogOK, Icon: "check" }, { Text: "No", Callback: function () { }, Icon: "clear" }]);
        base.editDialog = new Dialog("#stats_div_edit", "Edit name",  [{ Text: "Abort", Callback: function () { }, Icon: "clear"},{ Text: "OK", Callback: base.editDialogOK, Icon: "check" }]);
        base.detailsDialog = new Dialog("#stats_div_details", null,[{ Text: "OK", Callback: function () { }, Icon: "check"}]);
        
        base.events = new EventDisplay("#stats_div_event");

        $(".stats_autorf").on('input', function () {
            clearTimeout($(this).data('timer'));
            $(this).data('timer', setTimeout(function () {
                base.updateStats(0);
            }, StatsFilterTimeout));
        });

        $("#stats_input_selected").change(function () {
            base.updateStats(0);
        });

        $(".stats_autorf").on('keyup', function (e) {
            if (e.keyCode == 13) {
                clearTimeout($(this).data('timer'));
                base.updateStats(0);
            }
        });

        $("#stats_input_date1", "#stats_input_date2").change(function (e) {
            base.updateStats(0);
        });

        $("#stats_a_details").click(function (e) {
            e.preventDefault();
            if(base.quickAdminId !== null) base.showPlayers(base.quickAdminId);
        });
        
        $("#stats_a_delete").click(function (e) {
            e.preventDefault();
            if(base.quickAdminId !== null) base.deleteDialog.show(base.quickAdminId);
        });

        $("#stats_a_edit").click(function (e) {
            e.preventDefault();
            if(base.quickAdminId !== null) {
                $("#stats_input_edit_name").val(base.quickAdminRow.data("name"));
                base.editDialog.show(base.quickAdminId);
            }
        });

        $("#stats_a_select").click(function (e) {
            e.preventDefault();
            if(base.quickAdminRow !== null) {
                var row = base.quickAdminRow;
                var selected = !row.data("selected");
                base.setSelected(base.quickAdminId, selected);
                row.data("selected", selected);
                if(row.data("selected")) {
                    row.addClass("stat_selected");
                } else {
                    if(row.hasClass("stat_selected")) row.removeClass("stat_selected");
                }
            }
        });  
        
        $("#stats_a_export_csv").click(function (e) {
            e.preventDefault();
            if(base.quickAdminId !== null) base.downloadCSV(base.quickAdminId);
        });
        
        base.updateStats(0);
          
        $(window).scroll(function() {
            if($(window).scrollTop() + $(window).height() > $(document).height() - 50) {
                if(base.loading === false && base.hitEnd === false) {
                    base.loading = true;
                    base.updateStats(base.page+1);
                }
            }
        });     
    };

    this.onStatusChange = function (online) {

    };

    this.onDeinit = function () {

    };

    this.setStats = function (r) {
        var tb = "";
        var i = 0;
        
        for (var x in r) {
            var b = r[x];
            tb +=
                '<tr data-id="' + b.DatabaseId + '" data-name="' + b.Name + '" data-selected ="'+b.Selected+'"' + (b.Selected ? 'class="stat_selected"' : "") + '>' +
                "<td>" + b.Name + "</td>" +
                "<td>" + b.GameStartedStr + "</td>" +
                "<td>" + b.DurationStr + "</td>" + 
                '<td><span class="'+(b.GameModeStr == "1flag" ? "ctf" : b.GameModeStr)+'">' + b.GameModeStr.toUpperCase() + "</span></td>" +
                "<td>" + b.Map + "</td>" +
                "<td>" + (b.HasScoreMode ? (b.Team1Score + " / " + b.Team2Score) : (b.Team1Tickets + " / " + b.Team2Tickets))  + "</td>" + 
                "</tr>";
            i++;
        }

        if(base.page == 0) $("#stats_table_stats tbody").html("");
        $("#stats_table_stats tbody").append(tb);

        if (i == 0) {
            $("#stats_td_nostats").show();
        } else {
            $("#stats_td_nostats").hide();
        }

        $("#stats_table_stats tbody tr").each(function (i, e) {
            $(e).contextmenu(function (e) {
                e.preventDefault();
                base.quickAdminRow = $(this).closest("tr");
                base.quickAdminId = base.quickAdminRow.data("id");
                $("#stats_ul_admin").css("left", e.pageX);
                $("#stats_ul_admin").css("top", e.pageY);
                $("#stats_ul_admin").css("display", "block");
                $("#stats_a_select").find("i").html(base.quickAdminRow.data("selected") ? "check_box" : "check_box_outline_blank");
                $("#stats_a_select").find("span").html(base.quickAdminRow.data("selected") ? "Deselect" : "Select");   
            });
        });
        $("#stats_td_loading").hide();
        base.loading = false;
        base.hitEnd = (r.length !== StatsRowsPerPage);
    };

    this.updateStats = function (page) {
        $("#stats_td_loading").show();
         base.page = page;
        
        var filter = {
            Action: "stats_update",
            Selected: $("#stats_input_selected").is(":checked"),
            NameExp: $("#stats_input_name").val(),
            MapExp: $("#stats_input_map").val(),
            DateFromStr: $("#stats_input_date1").val(),
            DateUntilStr: $("#stats_input_date2").val(),
            Page: page
        };
       
        
        $.post({
            url: StatsUrl,
            data: JSON.stringify(filter)
        }).done(function (res) {
            base.setStats(JSON.parse(res));
        });
    };

    this.matchSelected = function(r) {
        if(r.Ok) {
            //TODO
        }else{
            base.events.ShowError(r.Error);          
        }        
    };
    
    this.setSelected = function(id, selected) {
        var rr = {
            Action: "stats_select",
            DatabaseId: id,
            Selected: selected
        };
        $.post({
            url: StatsUrl,
            data: JSON.stringify(rr)
        }).done(function (res) {
            base.matchSelected(JSON.parse(res));
        });        
    }
    
    this.matchDeleted = function (r) {
        if (r.Ok) {
            var row = base.getRowById(r.DatabaseId);
            if(row !== null) row.remove();
            base.events.ShowInfo("Match removed.");
        }else{
             base.events.ShowError(r.Error); 
        }
    };

    this.deleteDialogOK = function (tag) {
        var rq = { Action: "stats_delete", DatabaseId: tag };
        $.post({
            url: StatsUrl,
            data: JSON.stringify(rq)
        }).done(function (res) {
            base.matchDeleted(JSON.parse(res));
        });
    };

    this.matchModified = function(r) {
        if(r.Ok) {
            base.events.ShowInfo("Match name updated."); 
            var row = base.getRowById(r.DatabaseId);
            if(row !== null)  {
                row.data("name", r.Name);
                row.children("td").first().html(r.Name);
            }
        }else{
            base.events.ShowError(r.Error); 
        }
    };
    
    this.editDialogOK = function(tag) {
        var rq = { Action: "stats_edit", DatabaseId: tag, NameExp: $("#stats_input_edit_name").val() };
        $.post({
            url: StatsUrl,
            data: JSON.stringify(rq)
        }).done(function (res) {
            base.matchModified(JSON.parse(res));
        });
    }
    
    this.getRowById = function(id) {
        var r = null;
        $("#stats_table_stats tbody tr").each(function (i, e) {
            if($(e).data("id") == id) {
                r = $(e);
                return false; 
            } 
        });
        return r;
    };

    this.setPlayers = function(r) {
        var tb = "";
        var i = 0;
        for (var x in r) {
            var b = r[x];
            tb +=
                '<tr>' +
                "<td>" + b.DatabaseId + "</td>" +
                "<td>" + b.Name + "</td>" +
                "<td>" + b.Team + "</td>" +
                "<td>" + b.Score + "</td>" +
                "<td>" + b.Kills + "</td>" +
                "<td>" + b.Deaths + "</td>" +
                "<td>" + b.KeyHash + "</td>" +
                "</tr>";
            i++;
        }
        $("#stats_table_players tbody").html(tb);
        if (i == 0) $("#stats_td_noplayers").show();
        else $("#stats_td_noplayers").hide();      
        $("#stats_td_players_loading").hide();       
    };
    
    this.showPlayers = function(id) {
        $("#stats_td_players_loading").show();
        $("#stats_td_noplayers").hide();
        $("#stats_table_players tbody").html("");
        base.detailsDialog.show();
        var rq = { Action: "stats_players", DatabaseId: id };
        $.post({
            url: StatsUrl,
            data: JSON.stringify(rq)
        }).done(function (res) {
            base.setPlayers(JSON.parse(res));
        });    
    };
     
    this.downloadCSV = function(id) {
        window.open(StatsUrl + "?type=csv&export_id=" + id);    
    };
    
    this.hideQuickAdmin = function () {
        $("#stats_ul_admin").css("display", "none");
    }; 
}

mainFrame.setActivePage(new Stats());