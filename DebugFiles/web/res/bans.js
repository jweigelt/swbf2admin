var BansUrl = "/db/bans";
var BansFilterTimeout = 300;

var BanTypes = [
    "Key",
    "IP",
    "Alias"
]

function Bans() {
    var base = this;
    this.quickAdminId = null;

    this.onInit = function () {
        $(window).blur(base.hideQuickAdmin);
        $(document).click(base.hideQuickAdmin);

        base.deleteDialog = new Dialog("#bans_div_delete", "Delete ban", [{ Text: "Yes", Callback: base.deleteDialogOK, Icon: "check" }, { Text: "No", Callback: function () { }, Icon: "clear" }]);

        base.events = new EventDisplay("#bans_div_event");

        $(".bans_autorf").on('input', function () {
            clearTimeout($(this).data('timer'));
            $(this).data('timer', setTimeout(function () {
                base.updateBans();
            }, BansFilterTimeout));
        });

        $("#bans_input_expired, #bans_select_type").change(function () {
            base.updateBans();
        });

        $(".bans_autorf").on('keyup', function (e) {
            if (e.keyCode == 13) {
                clearTimeout($(this).data('timer'));
                base.updateBans();
            }
        });

        $("#bans_input_date").change(function (e) {
            base.updateBans();
        });

        $("#bans_a_delete").click(function (e) {
            e.preventDefault();
            base.deleteDialog.show(base.quickAdminId);
        });

        $("#bans_a_edit").click(function (e) {
            e.preventDefault();
            console.log("edit ban");
        });

        base.updateBans();
    };

    this.onStatusChange = function (online) {

    };

    this.onDeinit = function () {

    };

    this.setBans = function (r) {
        var tb = "";
        var i = 0;
        var legend = false;

        for (var x in r) {
            var b = r[x];
            tb +=
                "<tr" + (b.Expired ? ' class="expired"' : "") + ' data-id="' + b.DatabaseId + '">' +
                "<td>" + b.DatabaseId + "</td>" +
                "<td>" + b.PlayerName + "</td>" +
                "<td>" + b.AdminName + "</td>" +
                "<td>" + b.Reason + "</td>" +
                "<td>" + b.DateStr + "</td>" +
                "<td>" + (b.Duration < 0 ? "permanent" : b.Duration) + "</td>" +
                "<td>" + b.PlayerKeyhash + "</td>" +
                "<td>" + b.PlayerIPAddress + "</td>" +
                "<td>" + BanTypes[b.TypeId] + "</td>" +
                "</tr>";
            i++;
            legend |= b.Expired;
        }

        $("#bans_tbl_bans tbody").html(tb);

        if (i == 0) {
            $("#bans_td_nobans").show();
        } else {
            $("#bans_td_nobans").hide();
        }

        if (legend) {
            $("#bans_div_legend").show();
        } else {
            $("#bans_div_legend").hide();
        }

        $("#bans_tbl_bans tbody tr").each(function (i, e) {
            $(e).contextmenu(function (e) {
                e.preventDefault();
                base.quickAdminId = $(this).closest("tr").data("id");
                $("#bans_ul_admin").css("left", e.pageX);
                $("#bans_ul_admin").css("top", e.pageY);
                $("#bans_ul_admin").css("display", "block");
            });
        });
    };

    this.updateBans = function () {
        var filter = {
            Action: "bans_update",
            PlayerNameExp: $("#bans_input_player").val(),
            AdminNameExp: $("#bans_input_admin").val(),
            ReasonExp: $("#bans_input_reason").val(),
            StartDateStr: $("#bans_input_date").val(),
            Expired: $("#bans_input_expired").is(":checked"),
            Type: $("#bans_select_type").val()
        };

        $.post({
            url: BansUrl,
            data: JSON.stringify(filter)
        }).done(function (res) {
            base.setBans(JSON.parse(res));
        });
    };

    this.hideQuickAdmin = function () {
        $("#bans_ul_admin").css("display", "none");
    };

    this.banDeleted = function (res) {
        if (res.Ok) {
            base.updateBans();
            base.events.ShowInfo("Ban removed.");
        }
    };

    this.deleteDialogOK = function (tag) {
        var rq = { Action: "bans_delete", DatabaseId: tag };
        $.post({
            url: BansUrl,
            data: JSON.stringify(rq)
        }).done(function (res) {
            base.banDeleted(JSON.parse(res));
        });
    };
}

mainFrame.setActivePage(new Bans());