# SWBF2Admin
A modern, easy-to-use server manager for Star Wars Battlefront II (2005) dedicated servers

## Getting Started
These instructions will get you a minimal SWBF2Admin setup up and running.
SWBF2Admin is highly configurable - for advanced configuration techniques or more detailed instructions, please also check out the "Advanced" section of this document.

### Prerequisites
SWBF2Admin requires the following software to be installed on the host machine:

- .NET Framework(or equivalent) v4.6.1 or newer (<https://www.microsoft.com/net/download/windows>)
- Visual C++ Redistributable x86 2015: (<https://www.microsoft.com/en-us/download/details.aspx?id=48145>)

If you are planning on hosting a GoG/Steam server you will also need a GOG Galaxy account owning SWBF2.

> Using the Steam client for hosting is currently not supported.

### Minimal setup
Extract all files to a destination of your choice, run SWBF2Admin.exe. You will be prompted to set webadmin credentials. Enter username and password of your choice. Close SWBF2Admin afterwards.

Navigate to the `./server` folder in SWBF2Admin's installation directory. Right click `RconServer.dll -> Properties`. Check the `Unblock` option in the lower section of the dialog. Do the same for `dlloader.exe`.

> Should you ever forget your credentials, run reset_webcredentials.bat.
> This will delete all web admin accounts and prompt you for new default credentials.

##### Optional: using the original server package
If you want to run the old dedicated server, open `./cfg/core.xml`, set
```xml
  <ServerType>Gamespy</ServerType>
```

##### Optional: allowing remote access to webadmin
By default, the web panel will only be accessible from the local machine. To change this, open core.xml and adjust
```xml
  <WebAdminPrefix>http://localhost:8080/</WebAdminPrefix>
```
If you do not have a domain pointing to your server, you can also just use the server's IP-Address, for example
```xml
  <WebAdminPrefix>http://192.168.1.234:8080/</WebAdminPrefix>
```
If you have any active firewall, the webadmin port (8080 TCP in this case) has to be unblocked.

If you prefer to use an encrypted connection, you may change the protocol specified in `WebAdminPrefix` from `http://` to `https://`. Note that when using HTTPS, a matching SSL certificate has to be installed into your machine's certificate store.

##### Optional: enabling runtime managament
If you want to use features like ingame commands, announce broadcast, statistics ..., runtime management has be enabled.
To enable runtime management, open `./cfg/core.xml`, set
```xml
  <EnableRuntime>true</EnableRuntime>
```
```diff
! When using runtime management, the !gimmeadmin command will add the first user to execute
! it to the "Admin" group. Make sure you are the first one! The command is deactivated after one use.
```
```diff
! When using the GOG version, set GamePort & RconPort in Server Settings to the same value.
```

### Preparing the gameserver

Depending on which platform you want to use, EITHER follow the "GoG / Steam" OR the Gamespy / "Swbfspy" guide.

##### GoG / Steam
```diff
! Neither the GOG communications server nor GOG Galaxy works over Windows Remote Desktop. 
! You can use tools like VNC or Chrome Remote Desktop instead. 
! Launching any part of the server over Windows Remote Desktop causes it to not show up in the server listing.
```
1) Install GOG Galaxy (<https://www.gog.com/galaxy>)
2) Using GOG Galaxy, download Star Wars Battlefront II
3) In GOG Galaxy, open Battlefront II in your library. Click on `More -> Manage installation -> Show folder`
4) A Explorer Window will open, open `GameData` and copy all contents to the `./server` folder in SWBF2Admin's installation directory

Do not uninstall the original game or GOG Galaxy after copying the data.

##### Gamespy / "Swbfspy"
1) Install the original dedicated server package
2) Copy the contents of it's installation folder (the one containing `BattlefrontII.exe`) to the `server` folder in SWBF2Admin's installation directory 

### First launch
1) Start `SWBF2Admin.exe`
2) Using your web browser, open the web panel. By default, the web panel is accessible at http://localhost:8080/
3) Go to `Server Settings -> General`, adjust server settings to your liking. Make sure that a network adapter is selected under `bind address`
4) Go to `Server Settings -> Map rotation`. Add maps using drag&drop.
5) Go to `Dashboard`, click on `Server status -> "Start`
6) If you chose to enable runtime management, join your server in game and enter `!gimmeadmin` in chat

## Advanced

## Using the web panel
### Dashboard

The dashboard page show status information on your server. It also provides the big not-so-red button which is used to start and stop your server.
When your server is running, the status information will be updated automatically every 20~30 seconds.

The dashboard's icon in the navigation bar will change color depending on your server's current status.
- red: server is offline
- green: server is online
- yellow: server is starting/stopping

### Players

The players page displays a list of all players who are connected to the server. The list is updated automatically every 10~15 seconds.

Right-clicking a player will bring up a context menu containing buttons for quick administation.
- Swap player's team
- Kick player
- Ban player

If you click on "Ban player", a dialog will open asking you to specify the ban's duration and the ban's type.
If "permanent" is selected, the duration field will be ignored.

### Chat

The chat page provides you a live feed of the server's ingame chat. You can also send messages which will be shown ingame.
Add a /admin prefix to send administrative commands. The command output is sent back to your browser.

### Bans

The bans page lets you manage all banned players. You'll find various filter options in the page's top section.
Text fields don't have to be exact matches. Similar results will be shown as well.

- Player: banned player's nick
- Admin: name of the admin who submitted the ban
- Reason: reason specified for the ban
- Date: only bans which were created after the given date will be shown
- Expired: also show expired bans
- Type: whether the player's IP-Address or CD-Key was banned

To delete a ban, just right click it. A context menu will show up - click on "Delete ban".

### Settings

Once you made any changes, the page will notify you that the changes weren't saved yet. 
Five seconds after you made your last change, all settings will automatically be saved.
Settings are also saved immediately if you change to another page, so you don't have to wait.

### Settings / General

This page lets you change your server's basic parameters.


### Settings / Game

This page lets you change your parameters adjusting the gameplay.

### Settings / Map Rotation

This page let's you adjust your servers map rotation.
Simply grab the map you want to add from the table on the left. Drag&drop it to the table on the right.
A dialog will open, asking you to select the game modes you want to add. After doing so, click OK and the maps will be added. If you want to cancel the dialog, just click on the red cross in the top right corner. This will leave your map rotation untouched.

If you want to remove a map from the rotation, just drag&drop it from the right table to the left table.

### Users

If you want to edit your webadmin username / password or want to add additional users, you can do so on this page.
Right-clicking on any user will give you the options to delete or edit a user or create a new one.

If you're editing an existing user but don't want to change his password, leave the password fields untouched.
The password will not be updated.
If you chose create or edit, a dialog box will open which lets you edit the users properties.

Notes:
- The delete option won't appear for your own account

## Statistics tracking
To enable statistics tracking, open `./cfg/game.xml`, set
```
<EnableGameStatsLogging>true</EnableGameStatsLogging>
```
if you want to track player statistics as well, open `./cfg/players.xml`, set
```
<EnablePlayerStatsLogging>true</EnablePlayerStatsLogging>
```

## Automatic announce broadcasts

### Configuring the announce scheduler
Open `./cfg/announce.xml`
- Set `Enable` to true to enable automatic announce scheduling
- Adjust `Interval` to configure the delay between broadcasts

You can now add as many announces as you like to the `<AnnounceList>` attribute.
Announces must have the following format:

```xml
<Announce EnableParser="true/false" Message="YourMessage"/>
```

### Variables

If EnableParser is set to true, the Announce is parsed before broadcasting it.
The following tags are replaced:

|Tag|Description|
|------|------|
|{s:map}|current map|
|{s:ff}|friendly fire enabled (0/1)|
|{s:gm}|gamemode|
|{s:heroes}|heroes enabled (0/1)|
|{s:maxplayers}|max. players|
|{s:nextmap}|next map|
|{s:password}|server password|
|{s:players}|current playercount|
|{s:ip}	|server's ip-address|
|{s:name}|servers name|
|{s:t1score}|team 1's score (CTF)|
|{s:t2score}|team 2's score (CTF)|
|{s:t1tickets}|team 1's reinforcements (CON/ASS/HUNT)|
|{s:t2tickets}|team 2's reinforcements (CON/ASS/HUNT)|
|{s:version}|server's version|
|{g:nr}|current game's id|
|{banner}|SWBF2Admin's version infos|
|{t:(format)}|current time formatted by given format string|

##### Using the {t:(format)} tag

The `{t:(format)}` tag can be used to display the current time. Replace `(format)` with a format string.
The given formatter has to be a .NET-style format string. (see <https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings> for reference)

Example for a broadcast displaying the current time in HH:mm:ss format:
```xml
<Announce EnableParser="true" Message="Current time {t:HH:mm:ss}"/>
```

## Automatic conditional broadcasts

Conditional broadcasts can be used to trigger custom messages if a special event is observed. Special events can for example be a player reaching a specific number of kills without dying.

SWBF2Admin lets you specify unlimited messages for one event. When the event is triggered, one line is chosen from the message pool at random.

(see `./cfg/players.xml -> ConditionalMessages` for setup syntax)

## Ingame commands

### Getting permissions

After you freshly installed SWBF2Admin, join your server and enter `!gimmeadmin` in chat. This will add your player account to the Administrator group.

By default SWBF2Admin has three user groups configured:
- Player
- Moderator
- Admin

To add players to a specific group use the putgroup command:

```
!putgroup <name> <group>
```

If you want to remove a player from a specific group, you can do so using the !rmgroup command
```
!rmgroup <name> <group>
```

Each user group is assigned a hierarchy value ("group level"). By default SWBF2Admin will not allow users to add players to or remove players from groups that a higher in hierarchy than the highest ranking group the acting user is a member of.
This behaviour can be disabled by setting
```xml
<CheckLevel>false</CheckLevel>
```
in `./cfg/cmd/putgroup.xml` and `./cfg/cmd/rmgroup.xml.`

### Basic administrative commands
SWBF2Admin supports all basic administrative commands.
All commands that expect a player name to be given feature automatic player name detection. This means that you only have to enter parts of a player's name. If the expression provided is ambiguous, the `-n <number>` argument can be used. This will kick the n-th matching player from the score board list.

|Command syntax|Description|
|--------------|-----------|
|!swap \<player\> [\<reason\>]|Changes the player's team|
|!kick \<player\> [\<reason\>]|Kicks the player from the server|
|!ban \<player\> [\<reason\>]|(Permanently) bans the player from the server|
|!ipban \<player\> [\<reason\>]|Bans the player's IP-address (does not work on GoG)|
|!tempban \<player\> \<duration\> [\<reason\>]|Bans the player for a specific amount of time. (Give time in seconds)|
|!tempipban \<player\> \<duration\> [\<reason\>]|Bans the player for a specific amount of time.<br/>The ban is IP-Address based. (Give time in seconds, does not work on GoG)|

### Map commands
Like player commands, all map commands also feature automatic name completion.<br/>
A map can be specified using the following format:
```
<map name> <era>_<mode>
```
You can either use the 3-letter code of the map or a part of the "nice" name of a map.
Map names and 3-letter codes can also be viewed in web admin.

|Command syntax|Description|
|--------------|-----------|
|!map \<map\>|Adds the map to the map rotation (if it is not contained already)<br/>and immediately switches to the new map|
|!addmap \<map\>|Adds the map to the map rotation (if it is not contained already)|
|!removemap \<map\>|Removes the map to the map rotation|
|!setnextmap \<map\>|Adds the map to the map rotation (if it is not contained already). Sets the next map to the new map|

### Misc commands
|Command syntax|Description|
|--------------|-----------|
|!endgane|Ends the current game and loads the next map|
|!applymods \<enable/disable\> \<mod\>|Applies/Removes the given hexedit mod to/from the server's local level files|
|!bots \<count\>|Adjusts the number of AI players to the given value|
|!timer \<value\>|Sets the map timer to the given value|
|!score \<value\>|Adjusts the ticket count / score limit / flag limit for the current map|
|!boom|Spawns a grid of time bombs around (0, 0, 0)|

### Public commands
The following commands are configured to be available to anyone playing on the server.
|Command syntax|Description|
|--------------|-----------|
|!nextmap|Prints the next map in chat|
|!calc|An ingame calculator - never need to TAB out to calculate your K/D again|
|!stats \<section\>|Prints a player's own statistics<br/>Game & Player statistics tracking has to be enabled|
|!hello|Greets a player|

### Command configuration
Every command has it's own XML configuration. The files are located in ./cfg/cmd.

All templates are kept configurable so you can adjust ingame messages to your liking.

### Custom commands
To create a custom command, navigate to ./cfg/dyncmd and create a new folder for your command.
Create an empty XML and LUA file in that folder. Rename both files so their names match your folder's name.

```
/dyncmd
	/yourcommand
		/yourcommand.xml
		/yourcommand.lua
```

Use the following template for your XML file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<DynamicCommand xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Enabled>true</Enabled>
  <Alias>youralias</Alias>
  <Permission>yourpermission</Permission>
  <Usage>youralias (param1) (param2) ...</Usage>
  <UserConfig>
	<CustomConfig>...</CustomConfig>
	...
  </UserConfig>  
</DynamicCommand>
```

Enabled: setting enabled to false will disable your command. This can be useful if you want to disable a command without deleting it's files.
Alias: the ingame alias which will be used to call your command (without prefix!)
Usage: A small explanation on how to use your command
UserConfig: You can add custom child nodes to UserConfig to configure your command. These can later be accessed by LUA.

Copy the following template to your LUA file:
```lua
function init()

end

function run(player, command, params)
		
end
```

The init function is called once when SWBF2Admin has finished parsing your script.
```
run() is called everytime your command is invoked.
player: (Player) player who invoked your command
command: (string) your command alias
params: (table(of string)) paramaters given by the player
```

### Invoking ingame lua
SWBF2Admin's command system allows to run lua code in in-game context. This allows to invoke all game-specific lua functions externally without having to munge files.
Simply use
```lua
api.IngameLua("<lua code here>")
```
in your custom command script. (Example command: `!boom`, see `./cfg/dyncmd/boom.lua`)
For additional information on how to write ingame lua scripts consult the mod tools documentation or <https://github.com/marth8880/SWBF2-Lua-API>.

### LUA API documentation
The API is exported to LUA as a superglobal called "api".

|function name| returns | description |
|-------------|---------|-------------|
|`GetPlayers` |`Table<Player>`|Gets a table containing all connected players|
|`FindPlayers`  |`Table<Player>`|Gets a table containg all players whose names match the given expression<br/>Arguments:<br/>`string expression`<br/>`bool ignoreCase = true`<br/>`bool exact = false`|
|`KickPlayer`||Boots a player from the server<br/>Arguments:<br/>`Player player`|
|`SwapPlayer`||Changes a players team<br/>Arguments:<br/>`Player player`|
|`Pm`||Sends a private message to a player<br /> **Sending PMs too fast can slow down or freeze the chat**|
|`GetBans`|`Table<PlayerBan>`|Gets a list of bans from the database filtered by the given parameters<br/>Arguments:<br/>`string playerExp`<br/>`string adminExp`<br/>`string reasonExp`<br/>`bool expired`<br/>`number banType`<br/>`number timestamp`<br/>`number maxRows`|
|`InsertBan`||Saves a ban in the database. The ban is enabled immediately.<br/>Arguments:<br/>`Player player`<br/>`Player admin`<br/>`string reason`<br/>`bool ip`<br/>`number duration = -1` (-1: permanent ban)|
|`GetServerInfo`|`ServerInfo`|Gets the gameservers current status|
|`GameInfo`|`GetGameInfo`|Gets information about the current game|
|`SendCommand`|`string`|Sends a command to the server's remote console "rcon". The server's response is returned.<br/>Arguments:<br/>`string cmd`<br/>`params string[] args`|
|`SendCommandNoResponse`||Sends a command to the server's remote console "rcon" SWBF2Admin will not wait for a response from the server. Use this method for commands that do not return any text.<br/>Arguments:<br/>`string cmd`<br/>`params string[] args`|
|`Say`||Broadcasts a message in the server<br/>Arguments:<br/>`string message`|
|`IngameLua`||Executes lua code in the gameserver's lua context<br/>Arguments:<br/>`string lua`|
|`GetConfig`|`string`|Gets a parameter from the command.xml user configuration section<br/>Arguments:<br/>`string name`|
|`GetUsage`|`string`|Gets the usage field from the command configuration file|
|`GetAlias`|`string`|Gets the alias field from the command configuration file|
|`Log`||Passes a log message to SWBF2Admin's event logging system<br/>Available levels are: LogLevel_Verbose, LogLevel_Info, LogLevel_Warning, LogLevel_Error|<br/>Arguments:<br/>`number level`<br/>`string message`<br/>`params string[] p`|
|`GetMods`|`Table<LvlMod>`|Gets a list of available hex-edit mods|
|`ApplyMod`||Applies the given mod to the servers local level files.<br/>Arguments:<br/>`LvlMod mod`|
|`RevertMod`||Removes the given mod to the servers local level files.<br/>Arguments:<br/>`LvlMod mod`|
|`RevertAllMods`||Removes all hex edit mods from the servers local level files.||
|`RegisterEventListener`||Associates a closure with a given event name. Event names can user-defined.<br/>Arguments:<br/>`string eventName`<br/>`Closure callback`|
|`InvokeEvent`||Invokes a given event. Args are forwarded to the registered closure.<br/>Arguments:<br/>`string eventName`<br/>`params object[] args`|
##### LUA object definitions
Player
```
(number)Slot - player's slot (shown in /admin /players)
(number)Ping - player's ping in ms
(number)Kills - number of kills
(number)Deaths - number of deaths
(number)Score - score
(string)Team - team name
(number)Name - player's name
(number)KeyHash - md5 hash of the player's cd-key
(boolean)IsBanned - true if player is banned (!: player will be autokicked soon)
(string)RemoteAddressStr - player's ip address
(number)DatabaseId - player's unique database id
```

BanType
```
ShowAll = -1
Keyhash = 0
IPAddress = 1
```
 
PlayerBan
```
(const number)DURATION_PERMANENT = -1
(number)DatabaseId 
(string)DateStr 
(number)Duration - ban duration in seconds
(boolean)Expired - true if the ban expired
(number) TypeId - ban type (see BanType def.)
(string)PlayerName - player's last name
(string)PlayerKeyhash - player's keyhash
(string)PlayerIPAddress - player's ip during ban
(string)AdminName - name of the banning admin
(string)Reason - reason, "" if not specified
(number)PlayerDatabaseId - player's unique database id
(number)AdminDatabaseId - admin's unique database id
```

PlayerBan
```
(const number)DURATION_PERMANENT = -1
(number)DatabaseId 
(string)DateStr 
(number)Duration - ban duration in seconds
(boolean)Expired - true if the ban expired
(number) TypeId - ban type (see BanType def.)
(string)PlayerName - player's last name
(string)PlayerKeyhash - player's keyhash
(string)PlayerIPAddress - player's ip during ban
(string)AdminName - name of the banning admin
(string)Reason - reason, "" if not specified
(number)PlayerDatabaseId - player's unique database id
(number)AdminDatabaseId - admin's unique database id
```

ServerStatus
```
Online = 0
Offline = 1
Starting = 2
Stopping = 3
```

ServerInfo
```
(number)StatusId - see ServerStatus
(string)ServerName
(string)ServerIP
(string)Version
(string)MaxPlayers
(string)Password
(string)CurrentMap
(string)NextMap
(string)GameMode
(string)Players
(string)Scores
(string)Tickets
(number)Team1Score
(number)Team2Score
(number)Team1Tickets
(number)Team2Tickets
(string)FFEnabled
(string)Heroes
```

GameInfo
```
(string)Map
(number)Team1Score
(number)Team2Score
(number)Team1Tickets
(number)Team2Tickets
(string)GameStarted
(number)DatabaseId
```

## Authors

This project was written by Jan "LeKeks" Weigelt (https://github.com/jweigelt), Yoni (https://github.com/yonilerner) and AsLan (https://github.com/SWBF2AsLan).

## License

This project is licensed under the GNU Public License (GPL) - see the [LICENSE.md](LICENSE.md) file for details.

## Third party software

SWBF2Admin uses several pieces of third party software.
All licenses for third party software can be found in the licenses folder supplied with SWBF2Admin.
