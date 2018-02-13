var GeneralUrl = "/settings/general";
var GeneralEditTimeout = 2500;

function General() {
    var base = this;

    this.events = new EventDisplay("#general_div_events");
    this.timeout = null;
    this.pending = false;

    this.onInit = function () {
        $("#general_div_container input, #general_div_container select").each(function (i, e) {

            $(e).on('input propertychange paste', function (evt) {
                base.setSaved(false);
            });
        });

        base.updateSettings();
    };

    this.onStatusChange = function (online) {
        $(".fixed input, .fixed select").each(function (i, e) { $(e).prop("disabled", online); });
    };

    this.onDeinit = function () {
        if (base.pending == true) {
            if (base.timeout != null) clearTimeout(base.timeout);
            base.pending = true;
            base.saveSettings();
        }
    };

    this.setSaved = function (r) {
        if (r != false) {
            if (r.Ok) {
                base.pending = false;
                base.events.ShowInfo("Settings saved.");
            } else {
                base.events.ShowError(r.Error);
            }
        } else {
            base.pending = true;
            base.events.ShowWarning("Changes not saved ...");
            if (base.timeout != null) clearTimeout(base.timeout);
            base.timeout = setTimeout(function () { base.saveSettings(); }, GeneralEditTimeout);
        }
    };

    this.saveSettings = function () {
        var settings = {
            Action: "general_set",
            Settings: {
                GameName: $("#general_input_session_name").val(),
                Password: $("#general_input_password").val(),
                AdminPw: $("#general_input_admin_password").val(),
                PlayerCount: $("#general_input_max_players").val(),
                PlayerLimit: $("#general_input_min_players").val(),
                Tps: $("#general_input_tps").val(),
                IP: $("#general_select_ipa").val(),
                GamePort: $("#general_input_gameport").val(),
                RconPort: $("#general_input_rconport").val(),
                Lan: $("#general_select_lan").val(),
                Bandwidth: $("#general_select_bandwidth").val()
            }
        };

        $.post({
            url: GeneralUrl,
            data: JSON.stringify(settings)
        }).done(function (res) {
            base.setSaved(JSON.parse(res));
        });
    };

    this.setSettings = function (r) {
        $("#general_select_ipa").empty();
        for (var x in r.NetworkDevices) {
            var iface = r.NetworkDevices[x];
            $("#general_select_ipa").append($('<option value="' + iface.IPAddress + '">' + iface.IPAddress + ' (' + iface.Name + ')</option>'));
        }

        var s = r.Settings;
        $("#general_input_session_name").val(s.GameName);
        $("#general_input_password").val(s.Password);
        $("#general_input_admin_password").val(s.AdminPw);
        $("#general_input_max_players").val(s.PlayerCount);
        $("#general_input_min_players").val(s.PlayerLimit);
        $("#general_input_tps").val(s.Tps);
        $("#general_select_ipa").val(s.IP);
        $("#general_input_gameport").val(s.GamePort);
        $("#general_input_rconport").val(s.RconPort);
        $("#general_select_lan").val(s.Lan.toString());
        $("#general_select_bandwidth").val(s.Bandwidth);

        if ($("#general_select_ipa option:selected").length < 1) {
            $("#general_select_ipa option:first").prop('selected', true);
        }
    };

    this.updateSettings = function () {
        $.post({
            url: GeneralUrl,
            data: '{"Action":"general_get"}'
        }).done(function (res) {
            base.setSettings(JSON.parse(res));
        });
    };
}

mainFrame.setActivePage(new General());