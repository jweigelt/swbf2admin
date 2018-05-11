var PlayersUrl = "/live/players";
var PlayersUpdateInterval = 10000;

function Players() {
    var base = this;
    this.timer = null;
    this.events = new EventDisplay("#players_div_events");
    this.player = null;
    this.dialog = null;
    this.action = "";


    this.onInit = function () {
        $(window).blur(base.hideQuickAdmin);
        $(document).click(base.hideQuickAdmin);

        $("#players_a_swap").click(function (e) {
            e.preventDefault();
            base.quickAdmin({ Action: "players_swap" });
        });

        $("#players_a_kick").click(function (e) {
            e.preventDefault();
            base.quickAdmin({ Action: "players_kick" });
        });

        $("#players_a_ban").click(function (e) {
            e.preventDefault();
            base.dialog.show(base.player);
        });

        $("#players_num_ban_duration").on('input propertychange paste', function (evt) {
            $("#players_ban_permanent").prop("checked", false);
        });


        base.dialog = new Dialog("#players_div_ban", "Ban player", [{ Text: "OK", Callback: base.dialogOK, Icon: "check" }]);
    }

    this.dialogOK = function (tag) {
        var duration = ($("#players_ban_permanent").prop("checked") ? -1 : $("#players_num_ban_duration").val());
        base.quickAdmin({
            Action: "players_ban",
            BanDuration: duration,
            BanTypeId: $("#players_select_ban_type").val()
        });
    };

    this.onStatusChange = function (online) {
        if (online) {
            if (base.timer == null) base.timer = setInterval(base.updatePlayers, PlayersUpdateInterval);
            base.updatePlayers();
            $("#players_div_players").removeClass("disabled");
        } else {
            if (base.timer != null) clearInterval(base.timer);
            $("#players_tbl_players tbody").html("");
            $("#players_td_noplayers").show();
            $("#players_div_players").addClass("disabled");
        }
    }

    this.onDeinit = function () {
        if (base.timer != null) clearInterval(base.timer);
    }

    this.setPlayers = function (r) {
        var tb = "";
        var i = 0;
        for (var x in r) {
            var p = r[x];
            tb +=
                "<tr data-slot=\"" + p.Slot + "\">" +
                "<td>" + p.Slot + "</td>" +
                "<td>" + p.Name + "</td>" +
                "<td>" + p.Team + "</td>" +
                "<td>" + p.Score + "</td>" +
                "<td>" + p.Kills + "</td>" +
                "<td>" + p.Deaths + "</td>" +
                "<td>" + p.Ping + "</td>" +
                "<td>" + p.RemoteAddressStr + "</td>" +
                "<td>" + p.KeyHash + "</td>" +
                "<td>" + p.GroupName + "</td>" +
                "</tr>";
            i++;
        }

        $("#players_tbl_players tbody").html(tb);

        $("#players_tbl_players tbody tr").each(function (i, e) {
            $(e).contextmenu(function (e) {
                e.preventDefault();
                $("#players_ul_admin").css("left", e.pageX);
                $("#players_ul_admin").css("top", e.pageY);
                $("#players_ul_admin").css("display", "block");
                base.player = $(this).data("slot");
            });
        });

        if (i == 0) {
            $("#players_td_noplayers").show();
        } else {
            $("#players_td_noplayers").hide();
        }
    };

    this.updatePlayers = function (s) {
        $.post({
            url: PlayersUrl,
            data: '{"Action":"players_update"}',
        }).done(function (res) {
            base.setPlayers(jQuery.parseJSON(res));
        });
    };

    this.hideQuickAdmin = function () {
        $("#players_ul_admin").css("display", "none");
    };

    this.setQuickAdmin = function (r) {
        base.events.ShowInfo("Done.");
    };

    this.quickAdmin = function (data) {
        if (base.player != null) {
            data.PlayerId = base.player;
            $.post({
                url: PlayersUrl,
                data: JSON.stringify(data),
            }).done(function (res) {
                base.setQuickAdmin(jQuery.parseJSON(res));
            });
        }
        base.player = null;
    };
}

mainFrame.setActivePage(new Players());