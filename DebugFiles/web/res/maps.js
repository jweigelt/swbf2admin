//TODO: clean up map adding
var MapsUrl = "/settings/maps";
var MapsEditTimeout = 2500;

var MapFlags = {
    GCWCon: (1 << 0), GCWCTF: (1 << 1), GCW1Flag: (1 << 2), GCWHunt: (1 << 3), GCWEli: (1 << 4), GCWAss: (1 << 5),
    CWCon: (1 << 10), CWCTF: (1 << 11), CW1Flag: (1 << 12), CWHunt: (1 << 13), CWEli: (1 << 14), CWAss: (1 << 15)
};

function Maps() {
    var base = this;
    this.dialog = null;
    this.mapList = [];
    this.events = new EventDisplay("#maps_div_events");
    this.pending = false;
    this.draggedRow = null;
    this.dragSource = null;
    this.insertBeforeRow = null;

    this.onInit = function () {
        base.updateInstalledMaps();

        $("#maps_table_rotation tbody").on("dragstart", "tr", function (e) {
            base.draggedRow = this;
            base.dragSource = "rotation";
            $(this).addClass("dragging");
            e.originalEvent.dataTransfer.effectAllowed = "move";
            e.originalEvent.dataTransfer.setData("map", JSON.stringify(base.getRowMap(this)));
        });

        $("#maps_table_installed tbody").on("dragstart", "tr", function (e) {
            base.draggedRow = null;
            base.dragSource = "installed";
            e.originalEvent.dataTransfer.effectAllowed = "copy";
            e.originalEvent.dataTransfer.setData("map", JSON.stringify(base.getRowMap(this)));
        });

        $("#maps_table_rotation").on("dragover", function (e) {
            e.preventDefault();
            base.showDropTarget(e);
        });

        $("#maps_table_installed").on("dragover", function (e) {
            if (base.dragSource == "rotation") e.preventDefault();
        });

        $("#maps_table_rotation").on("drop", function (e) {
            e.preventDefault();
            e.stopPropagation();
            base.dropOnRotation(e);
        });

        $("#maps_table_installed").on("drop", function (e) {
            e.preventDefault();
            if (base.dragSource == "rotation") base.dropMap();
        });

        $("#maps_table_rotation, #maps_table_installed").on("dragend", function () {
            base.clearDropTarget();
            $(base.draggedRow).removeClass("dragging");
            base.draggedRow = null;
            base.dragSource = null;
        });

        $("#maps_input_randomize_enable").change(function(e) {
            base.setSaved(false);    
        });
        
        
        base.dialog = new Dialog("#maps_div_add", "Pick Gamemodes", [{ Text: "OK", Callback: base.dialogOK, Icon: "check" }]);
    };

    this.onStatusChange = function (online) { };

    this.onDeinit = function() {
        if (base.pending == true) {
            if (base.timeout != null) clearTimeout(base.timeout);
            base.pending = true;
            base.saveMaps();
        }
    };

    this.setSaved = function (r) {
        if (r != false) {
            if (r.Ok) {
                if (r.RestartRequired) base.events.ShowWarning("Map order saved. The new order will take effect after the next server restart.");
                else base.events.ShowInfo("Settings saved.");
                base.pending = false;
            } else {
                base.events.ShowError(r.Error);
            }
        } else {
            base.pending = true;
            base.events.ShowWarning("Changes not saved ...");
            if (base.timeout != null) clearTimeout(base.timeout);
            base.timeout = setTimeout(function () { base.saveMaps(); }, MapsEditTimeout);
        }
    };

    this.saveMaps = function () {
        var req = { Action: "maps_save", Maps: [] };
        $("#maps_table_rotation tbody tr").each(function (i, e) {
            req.Maps.push($(e).data("name"));
        });
        req.Randomize = $("#maps_input_randomize_enable").prop("checked");

        $.post({
            url: MapsUrl,
            data: JSON.stringify(req)
        }).done(function (res) {
            base.setSaved(jQuery.parseJSON(res));
        });
    };

    this.getRowMap = function (row) {
        return {
            id: $(row).data("id"),
            flags: $(row).data("flags"),
            name: $(row).data("name"),
            nicename: $(row).data("nicename")
        };
    };

    this.getDropTarget = function (e) {
        var row = $(e.target).closest("#maps_table_rotation tbody tr")[0];
        if (row == null) return { row: null, before: false };

        var rect = row.getBoundingClientRect();
        return { row: row, before: e.originalEvent.clientY < rect.top + rect.height / 2 };
    };

    this.showDropTarget = function (e) {
        var target = base.getDropTarget(e);
        base.clearDropTarget();
        if (target.row != null && target.row != base.draggedRow) {
            $(target.row).addClass(target.before ? "drop-before" : "drop-after");
        }
    };

    this.clearDropTarget = function () {
        $("#maps_table_rotation tbody tr").removeClass("drop-before drop-after");
    };

    this.dropOnRotation = function (e) {
        var target = base.getDropTarget(e);
        var tbody = $("#maps_table_rotation tbody")[0];
        var insertBefore = target.row == null || target.before ? target.row : target.row.nextSibling;

        base.clearDropTarget();
        if (base.dragSource == "rotation" && base.draggedRow != null) {
            var rows = $(tbody).children().toArray();
            tbody.insertBefore(base.draggedRow, insertBefore);

            var changed = false;
            $(tbody).children().each(function (i, row) {
                if (rows[i] != row) changed = true;
            });
            if (changed) base.setSaved(false);
        } else if (base.dragSource == "installed") {
            base.insertBeforeRow = insertBefore;
            base.addMap(JSON.parse(e.originalEvent.dataTransfer.getData("map")));
        }
    };

    this.createRotationRow = function (map, name) {
        var gm = name.split("_")[1];
        return $(
            '<tr data-id="' + map.id + '" data-flags="' + map.flags + '" data-name="' + name + '" data-nicename="' + map.nicename + '" draggable="true">' +
            '<td><span class="' + (gm == "1flag" ? "ctf" : gm) + '">' + gm.toUpperCase() + '</span></td>' +
            "<td>" + map.nicename + "</td>" +
            "<td>" + name + "</td>" +
            "</tr>");
    };

    this.dialogOK = function (m) {
        var tb = $("#maps_table_rotation tbody");
        var added = false;
        $("#maps_div_add input").each(function (i, e) {
            if ($(e).prop('checked')) {
                var name = m.name + $(e).data("map");
                var tr = base.createRotationRow(m, name);
                if (base.insertBeforeRow != null && $.contains(tb[0], base.insertBeforeRow)) {
                    tb[0].insertBefore(tr[0], base.insertBeforeRow);
                } else {
                    tb.append(tr);
                }
                added = true;
            }
        });
        base.insertBeforeRow = null;
        if (added) base.setSaved(false);
    };

    this.addMap = function (map) {
        $("#maps_div_add input").each(function (i, e) {
            $(e).prop('checked', false);
            if ((parseInt(map.flags) & MapFlags[$(e).data("flag")]) > 0) $(e).prop("disabled", false);
            else $(e).prop("disabled", true);
        });
        base.dialog.show(map);
    };

    this.dropMap = function () {
        if (base.draggedRow == null) return;
        $(base.draggedRow).removeClass("dragging");
        $(base.draggedRow).remove();
        base.draggedRow = null;
        base.dragSource = null;
        base.setSaved(false);
    };

    this.setInstalledMaps = function (r) {
        base.mapList = r;

        var tb = $("#maps_table_installed tbody");
        tb.html("");
        for (var x in r) {
            var m = r[x];
            var tr = $(
                '<tr data-id="' + m.DatabaseId + '" data-flags="' + m.Flags + '" data-name="' + m.Name + '" data-nicename="' + m.NiceName + '" draggable="true">' +
                "<td>" + m.NiceName + "</td>" +
                "<td>" + m.Name + "</td>" +
                "<td>" + base.getModeIndicators(m) + "</td>" +
                "</tr>");
            tb.append(tr);
        }

        base.updateMapRotation();
    };
    
    this.getModeIndicators = function(map) {
        var modes = "";
        modes += base.makeIndicator("con", map.Flags & MapFlags.GCWCon || map.Flags & MapFlags.CWCon);
        modes += base.makeIndicator("1flag", map.Flags & MapFlags.GCW1Flag || map.Flags & MapFlags.CW1Flag);
        modes += base.makeIndicator("ctf", map.Flags & MapFlags.GCWCTF || map.Flags & MapFlags.CWCTF);
        modes += base.makeIndicator("hunt", map.Flags & MapFlags.GCWHunt || map.Flags & MapFlags.CWHunt);
        modes += base.makeIndicator("ass", map.Flags & MapFlags.GCWAss || map.Flags & MapFlags.CWAss);        
        modes += base.makeIndicator("eli", map.Flags & MapFlags.GCWEli || map.Flags & MapFlags.CWEli);              
        return modes;
    };
    
    this.makeIndicator = function(mode, hasMode) {
      return '<span class="' + (hasMode ? (mode == "1flag" ? "ctf"  : mode) : "inactive") + '">'+mode.toUpperCase()+'</span> ';      
    };

    this.updateInstalledMaps = function () {
        $.post({
            url: MapsUrl,
            data: '{"Action":"maps_installed"}'
        }).done(function (res) {
            base.setInstalledMaps(jQuery.parseJSON(res));
        });
    };

    this.setMapRotation = function (r) {
        if (!r.Ok) {
            base.events.ShowError(r.Error);
            return;
        }

        var tb = $("#maps_table_rotation tbody");
        tb.html("");

        for (var x in r.Maps) {
            var name = r.Maps[x];
            var m = base.getMap(name);
            var map = { id: m.DatabaseId, flags: m.Flags, nicename: m.NiceName };
            tb.append(base.createRotationRow(map, name));
        }
        $("#maps_input_randomize_enable").prop("checked", r.Randomize);
    };

    this.getMap = function (name) {
        var sn = name.split("_")[0];
        var r = null;
        sn = sn.substring(0, sn.length - 1);
        $.each(base.mapList, function (i, e) {
            if (e.Name === sn) {
                r = e;
                return false;
            }
        });
        return r;
    };

    this.updateMapRotation = function () {
        $.post({
            url: MapsUrl,
            data: '{"Action":"maps_rotation"}'
        }).done(function (res) {
            base.setMapRotation(jQuery.parseJSON(res));
        });
    };
}

mainFrame.setActivePage(new Maps());
