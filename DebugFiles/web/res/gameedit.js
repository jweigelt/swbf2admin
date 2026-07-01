var GameEditUrl = "/live/gameedit";
var GameEditUpdateInterval = 3000;

function GameEdit() {
  var base = this;
  this.timer = null;

  this.onInit = function() {
    base.timer = setInterval(base.updateStatus, GameEditUpdateInterval);
    this.updateStatus();
    
    // Array of buttons and their corresponding time values
    const timeButtons = [
      { id: "#gameedit_btn_add_time_15", time: 15 },
      { id: "#gameedit_btn_add_time_30", time: 30 },
      { id: "#gameedit_btn_add_time_45", time: 45 },
      { id: "#gameedit_btn_add_time_60", time: 60 },
      { id: "#gameedit_btn_del_time_15", time: -15 },
      { id: "#gameedit_btn_del_time_30", time: -30 },
      { id: "#gameedit_btn_del_time_45", time: -45 },
      { id: "#gameedit_btn_del_time_60", time: -60 }
      ];

      timeButtons.forEach(button => {
          $(button.id).click(function(e) {
              e.preventDefault();
              base.addTime(button.time);
          });
      });

      // Array of flags and their corresponding values
      const flagButtons = [
          { id: "#gameedit_btn_add_flag_1", team: 1 , score: 1},
          { id: "#gameedit_btn_add_flag_2", team: 2 , score: 1},
          { id: "#gameedit_btn_sub_flag_1", team: 1 , score: -1},
          { id: "#gameedit_btn_sub_flag_2", team: 2 , score: -1}
      ];

      flagButtons.forEach(button => {
          $(button.id).click(function(e) {
              e.preventDefault();
              base.addFlag(button.team, button.score);
          });
      });
  };

  this.onDeinit = function() {
    if(base.timer != null) clearInterval(base.timer);
  };

  this.updateStatus = function(s) {
    $.post({
      url: DashboardUrl,
      data: '{"action":"status_get"}',
      contentType: "application/json",
      dataType:"json",
    }).done(function(res) {
      $("#team1_score").text(`Score: ${res.Team1Score}`);
      $("#team2_score").text(`Score: ${res.Team2Score}`);
    });  
  };

  this.addTime = function(time) {
    $.post({
      url: GameEditUrl,
      data: JSON.stringify({ action: "add_time", TimeToAdd: time}),
    })
      .done(function(res) {
        base.updateStatus();
      })
      .fail(function() {
        alert("Failed to add time!");
      });
  };

  this.addFlag = function(team, score) {
    $.post({
      url: GameEditUrl,
      data: JSON.stringify({action: "add_flag", team: team, score: score}),
    })
    .done(function(res){
      base.updateStatus();
    })
    .fail(function() {
      alert("Failed to add flag!");
    })
  };

  this.addPlayerPoints = function(slot, score){
    $.post({
      url: GameEditUrl,
      data: JSON.stringify({action:"add_player_points", slot: slot, score: score}),
    })
    .done(function(res){
    })
    .fail(function() {
      alert("Failed to add flag!");
    })
  };
}

// Register the GameEdit page
mainFrame.setActivePage(new GameEdit());
