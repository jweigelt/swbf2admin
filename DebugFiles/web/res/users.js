var UsersUrl = "/web/users";

function Users() {
    var base = this;
	this.user = null;
	this.addDialog = null;
	this.editDialog = null;
	this.ID_CREATE = "create";
    this.events = new EventDisplay("#users_div_events");
	
    this.onInit = function () {
		$("#users_a_add").click(function(e) {
			e.preventDefault();
		});
		
		base.updateUsers();
	
	    $(window).blur(base.hideQuickAdmin);
        $(document).click(base.hideQuickAdmin);	
	
		base.editDialog = new Dialog("#users_div_edit", "Edit user", [{ Text: "OK", Callback: base.editDialogOK, Icon: "check" }]);
		base.deleteDialog = new Dialog("#users_div_delete", "Delete user", [
		{ Text: "Yes", Callback: base.deleteDialogOK, Icon: "check" },
		{ Text: "No", Callback: function () { }, Icon: "clear" }]);
	
		$("#users_a_add").click(function(e){
			e.preventDefault();
			$("#users_text_name").val("");
			$("#users_text_password").val("");
			$("#users_text_password_confirm").val("");	
			$("#users_text_password").data("update", true);
			base.editDialog.show(base.ID_CREATE);
		});

		$("#users_a_edit").click(function(e){
			e.preventDefault();
			if(base.user != null) {
			$("#users_text_name").val(base.user.Username);
			$("#users_text_password").val("12345678");
			$("#users_text_password_confirm").val("87654321");	
			$("#users_text_password").data("update", false);
			
			base.editDialog.show(base.user);
				base.user = null;			
			}
		});
	
		$("#users_a_delete").click(function(e){
			e.preventDefault();
			if(base.user != null) {
				base.deleteDialog.show(base.user);
				base.user = null;
			}
		});	
		
		  $("#users_text_password, #users_text_password_confirm").each(function (i, e) {
            $(e).on('input propertychange paste change', function (evt) {
                $(this).data("update", true);
            });
        });		
	};

    this.onStatusChange = function (online) {
		
    };

    this.onDeinit = function () {
    
	};
	
	this.editDialogOK = function (tag) {
		
		var pwd = "";
		var id = (tag == base.ID_CREATE ? 0 : tag.Id);
		
		if($("#users_text_password").data("update")) {
			if($("#users_text_password").val() != $("#users_text_password_confirm").val()) {
				base.events.ShowError("Passwords don't match");
				return;
			}else{
				pwd = $("#users_text_password").val();
			}
		}		
		
		var rq = {
            Action: (tag == base.ID_CREATE ? "users_create" : "users_edit"),
			Id: id,
			Username: $("#users_text_name").val(),
			SpaceInvaders: pwd,
			UpdateSpaceInvaders: $("#users_text_password").data("update")
		};
		
		base.editUser(rq);
				
		base.events.ShowInfo(tag == base.ID_CREATE ? "User created." : "User modified");	//TODO: add feedback
	};
	
	this.deleteDialogOK = function (tag) {
		base.editUser({ Action: "users_delete", Id : tag.Id });		
		base.events.ShowInfo("User removed");	//TODO: add feedback
	};	
	
    this.editUser = function (rq) {
        $.post({
            url: UsersUrl,
            data: JSON.stringify(rq)
        }).done(function (res) {
            base.setUsers(JSON.parse(res));
        });
    };

    this.setUsers = function (r) {
		var tb = "";
		for (var x in r) {
            var u = r[x];
            tb +=
                '<tr data-id="' + u.Id + '" data-username="'+u.Username+'">' +
                "<td>" + u.Id + "</td>" +
                "<td>" + u.Username + "</td>" +
                "<td>" + u.LastVisitStr + "</td>" +
                "</tr>";
        }
		
        $("#users_tbl_users tbody").html(tb);

        $("#users_tbl_users tbody tr").each(function (i, e) {
            $(e).contextmenu(function (e) {
                e.preventDefault();
				if($("#account_id").val() ==$(this).data("id"))
					$("#users_a_delete").hide();				
				else
					$("#users_a_delete").show();	
				
                $("#users_ul_admin").css("left", e.clientX);
                $("#users_ul_admin").css("top", e.clientY);
                $("#users_ul_admin").css("display", "block");
                base.user = { Id: $(this).data("id"), Username: $(this).data("username") };
			});
        });
	
	};

    this.updateUsers = function () {
        $.post({
            url: UsersUrl,
            data: '{"Action":"users_get"}',
        }).done(function (res) {
            base.setUsers(JSON.parse(res));
        });
    };
	
	this.hideQuickAdmin = function () {
        $("#users_ul_admin").css("display", "none");
    };
}

mainFrame.setActivePage(new Users());