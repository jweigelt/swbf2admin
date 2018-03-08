# SWBF2Admin

A modern, easy-to-use server manager for Star Wars Battlefront II (2005) dedicated servers

## Getting Started

These instructions will get you a minimal SWBF2Admin setup up and running.
SWBF2Admin is highly configurable - if you're interested in customizing your SWBF2Admin installation, please also check out "Advanced configuration" for further information.

### Prerequisites

SWBF2Admin requires .NET Framework(or equivalent) v4.6.1 or newer. .NET Framework can be downloaded at
https://www.microsoft.com/net/download/windows

### Creating a working server installation:

As SWBF2Admin is only a server management tool, the gameserver has to be installed first.

If you want to host a SWBFSpy dedicated server:

- Download dedicated server files from http://www.moddb.com/games/star-wars-battlefront-ii/downloads/star-wars-battlefront-ii-v11-dedicated-server
- Download multiplayer server binaries from http://info.swbfspy.com/
- Replace the server executable with the one downloaded from swbfspy

If you want to host a Steam (GoG) server:

- Install Steam on your server machine
- Install the latest Star Wars Battlefront II (Classic, 2005) version
- Right click the game in your steam library, set launch options to /autonet /win /nointro <map name (e.g tat2g_eli)>
- Download dedicated server files from http://www.moddb.com/games/star-wars-battlefront-ii/downloads/star-wars-battlefront-ii-v11-dedicated-server
- Copy the data folder from the dedicated server installation to your SWBF2 installation. Replace all files except shell.lvl!
- Copy the contents of the GameData folder that came with SWBF2Admin to your server's GameData folder

### Minimal setup

Extract all files to a folder of your choice
- EITHER Copy your server installation to <yourfolder>/server (BattlefrontII.exe has to be in that folder)
- OR run SWBF2Admin.exe once, close it again, open core.xml, set  <ServerPath> to the (absolute) path to your server installation

- If you want to host a Steam Server: open core.xml, set SteamMode
```xml
<EnableSteamMode>true</EnableSteamMode>
```

- If you want to enable ingame commands / player list / web chat etc.: set EnableRuntime
```xml
<EnableRuntime>true</EnableRuntime>
```

- Run SWBF2Admin.exe
- You will be asked to enter credentials which will be used to access the web panel later on. Enter credentials of your choice.
- Access the web panel and start your server (see Using the web panel)
- Join your server and enter !gimmeadmin in chat

### Steam ingame server sustainer

If you want to use the Steam version in Remote Desktop mode (required for most rental machines), you have to to a bit of extra setup 
to keep it from crashing.

- Create an additional account on your server machine
- log in to your new account
- Run Remote Desktop and connect to your gameserver account, save user name and password.
- Get IngameControllerServer.exe from your SWBF2Admin installation, run it and leave it open

IngameControllerServer.exe does not have to be located in the SWBF2Admin installation folder,
so you can move it to a new location if you like.

## Using the web panel

### Accessing the panel
By default, your web panel's URL is set to http://localhost:8080/ and can only be accessed locally.
If you want to access your panel from a remote computer, you can adjust the panel's URL in ./cfg/core.xml.

```xml
<WebAdminPrefix>http://localhost:8080/</WebAdminPrefix>
```

Notes:
- If you're using a address different from localhost, SWBF2Admin will ask for admin permissions during the first startup using the new URL. This is done because each URL has to be registered before it can be used. SWBF2Admin will register your new URL and continue operation in user Mode.
- HTTPS is supported if a valid certificate is installed and assigned to SWBF2Admin's application id (see https://stackoverflow.com/questions/11403333/httplistener-with-https-support)
- If you want to access your web panel from WAN, I'd recommend using a non-standard port in the high range

User authentication is done via HTTP basic auth. You'll be prompted for a username and password. 
If you forget your credentials, running "SWBF2admin.exe --reset-webcredentials" will reset all web credentials.

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

TODO

### Settings / Game

TODO

### Settings / Map Rotation

This page let's you adjust your servers map rotation.
Simply grab the map you want to add from the table on the left. Drag&drop it to the table on the right.
A dialog will open, asking you to select the game modes you want to add. After doing so, click OK and the maps will be added.
If you want to cancel the dialog, just click on the red cross in the top right corner. This will leave your map rotation untouched.

If you want to remove a map from the rotation, just drag&drop it from the right table to the left table.

### Groups

TODO 

### Users

If you want to edit your web admin username / password or want to add additional users, you can do so on this page.
Right-clicking on any user will give you the options to delete or edit a user or create a new one.

If you're editing an existing user but don't want to change his password, leave the password fields untouched.
The password will not be updated.
If you chose create or edit, a dialog box will open which lets you edit the users properties.

Notes:
- The delete option won't appear for your own account

## Ingame commands

### Getting permissions

After you freshly installed SWBF2Admin, join your server and enter !gimmeadmin in chat. This will add your player account to the Administrator group.

TODO

## Authors

This project was written by Jan "LeKeks" Weigelt (https://github.com/jweigelt) and Yoni (https://github.com/yonilerner).

## License

This project is licensed under the GNU Public License (GPL) - see the [LICENSE.md](LICENSE.md) file for details.

## Third party software

SWBF2Admin uses several pieces of third party software.
All licenses for third party software can be found in the licenses folder supplied with SWBF2Admin.

## Advanced configuration

### Automatic announce broadcasts

#### Configuring the scheduler
Open ./cfg/announce.xml
- Set Enable to true to enable automatic announce scheduling
- Adjust Interval to configure the delay between broadcasts

You can now add as many announces as you like to the "AnnounceList" attribute.
Announces must have the following format:

```xml
<Announce EnableParser="true/false" Message="YourMessage"/>
```

#### Variables

If EnableParser is set to true, the Announce is parsed before broadcasting it.
The following tags are available:
```
{s:map}			current map
{s:ff}			friendly fire enabled (0/1)
{s:gm}			gamemode
{s:heroes}		heroes enabled (0/1)
{s:maxplayers}	max. players
{s:nextmap}		next map
{s:password}	server password
{s:players}		current playercount
{s:ip}			server's ip-address
{s:name}		servers name
{s:t1score}		team 1's score (CTF)
{s:t2score}		team 2's score (CTF)
{s:t1tickets}	team 1's reinforcements (CON/ASS/HUNT)
{s:t2tickets}	team 2's reinforcements (CON/ASS/HUNT)
{s:version}		server's version
{g:nr}			current game's id
{banner}		SWBF2Admin's version infos
{t:(format)}	current time formatted by given format string
```

#### Using the {t:(format)} tag:

The {t:(format)} tag can be used to display the current time.
Replace (format) with a format string.
The given formatter has to be a .NET-style format string. (see https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

Example for a broadcast displaying the current time in HH:mm:ss format:
```xml
<Announce EnableParser="true" Message="Current time {t:HH:mm:ss}"/>
```

## Command configuration
Every command has it's own XML configuration. The files are located in ./cfg/cmd.

TODO

## Custom commands
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
run() is called everytime your command is invoked.
player: (Player) player who invoked your command
command: (string) your command alias
params: (table(of string)) paramaters given by the player

LUA object definitions

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

TODO

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

The API is exported to LUA as a superglobal called "api".
```
Table<Player> GetPlayers() - Gets a table containing all connected players
Table<Player> FindPlayers(string exp, bool ignoreCase = true, bool exact = false) - Gets a table containg all players whose names match the given expression 
void KickPlayer(Player player) - boots a player from the server
void SwapPlayer(Player player) - changes a players team
void Pm(string message, Player player, params string[] p) - sends a private message to a player (!: sending PMs too fast might slow down or freeze the chat)
Table<PlayerBan> GetBans(string playerExp, string adminExp, string reasonExp, bool expired, int banType, number timestamp, number maxRows)
void InsertBan(Player player, Player admin, string reason, bool ip, number duration = -1)

ServerInfo GetServerInfo()
GameInfo GetGameInfo()

string SendCommand(string cmd, params string[] args)
void SendCommandNoResponse(string cmd, params string[] args)
void Say(string message, params string[] p)

string GetConfig(string name)
string GetUsage()
string GetAlias()

(const number)LogLevel_Verbose 
(const number)LogLevel_Info 
(const number)LogLevel_Warning 
(const number)LogLevel_Error 
void Log(number level, string message, params string[] p)

Table<LvlMod> GetMods()
void ApplyMod(LvlMod mod)
void RevertMod(LvlMod mod)
void RevertAllMods()
```
