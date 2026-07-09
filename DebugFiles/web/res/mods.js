var ModsUrl = "/live/mods";

function Mods() {
    var base = this;
    this.events = new EventDisplay("#mods_div_event");

    this.onInit = function () {
        base.updateMods();
    };

    this.onStatusChange = function (online) { };

    this.onDeinit = function () { };

    this.updateMods = function () {
        $.post({
            url: ModsUrl,
            data: '{"Action":"mods_list"}'
        }).done(function (res) {
            base.setMods(jQuery.parseJSON(res));
        });
    };

    this.setMods = function (r) {
        if (!r.Ok) {
            base.events.ShowError(r.Error);
            return;
        }

        var file = "";
        var process = "";
        var fi = 0;
        var pi = 0;

        for (var x in r.Mods) {
            var m = r.Mods[x];
            var row = base.buildRow(m);
            if (m.Type == "process") {
                process += row;
                pi++;
            } else {
                file += row;
                fi++;
            }
        }

        base.fillTable("#mods_tbl_file", "#mods_tr_nofile", file, fi);
        base.fillTable("#mods_tbl_process", "#mods_tr_noprocess", process, pi);

        $(".mods_toggle").change(function () {
            base.toggleMod($(this).data("type"), $(this).data("name"), $(this).prop("checked"));
        });
    };

    this.buildRow = function (m) {
        return "<tr class=\"mods_row\">" +
            "<td>" + m.Name + "</td>" +
            "<td><input type=\"checkbox\" class=\"mods_toggle\" data-type=\"" + m.Type + "\" data-name=\"" + m.Name + "\"" + (m.Enabled ? " checked" : "") + "></td>" +
            "</tr>";
    };

    this.fillTable = function (table, emptyRow, rows, count) {
        $(table + " tbody .mods_row").remove();
        $(table + " tbody").append(rows);
        if (count == 0) {
            $(emptyRow).show();
        } else {
            $(emptyRow).hide();
        }
    };

    this.toggleMod = function (type, name, enabled) {
        var req = { Action: "mods_toggle", Type: type, Name: name, Enabled: enabled };
        $.post({
            url: ModsUrl,
            data: JSON.stringify(req)
        }).done(function (res) {
            base.modToggled(jQuery.parseJSON(res), name, enabled);
        });
    };

    this.modToggled = function (r, name, enabled) {
        if (r.Ok) {
            base.events.ShowInfo("Mod \"" + name + "\" " + (enabled ? "enabled" : "disabled") + ".");
        } else {
            base.events.ShowError(r.Error);
            base.updateMods();
        }
    };
}

mainFrame.setActivePage(new Mods());
