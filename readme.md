# SWBF2Admin

A modern, easy-to-use server manager for Star Wars Battlefront II (2005) dedicated servers

## Getting Started

These instructions will get you a minimal SWBF2Admin setup up and running.
SWBF2Admin is highly configurable - if you're interested in customizing your SWBF2Admin installation, please also read "Advanced configuration" for further information.

### Prerequisites

SWBF2Admin requires .NET Framework(or equivalent) v4.6.1 or newer. .NET Framework can be downloaded at
https://www.microsoft.com/net/download/windows

You'll also need a working SWBF2 dedicated server installation.
Dedicated server files can be downloaded at
http://www.moddb.com/games/star-wars-battlefront-ii/downloads/star-wars-battlefront-ii-v11-dedicated-server

Working multiplayer binaries can be downloaded at
http://info.swbfspy.com/

### Minimal setup

- Extract all files to a folder of your choice
- Copy your server installation to <yourfolder>/server (BattlefrontII.exe has to be in that folder)
- Run SWBF2Admin.exe

### Getting a server running
- Open your WebBrowser and navigate to http://localhost:8080/

TODO


## Authors

TODO

## License

This project is licensed under the GNU Public License (GPL) - see the [LICENSE.md](LICENSE.md) file for details.

## Third party software

TODO

### Advanced configuration

## Automatic announce broadcasts

#Configuring the scheduler
Open ./cfg/announce.xml using your favourite text editor.
- Set Enable to true to enable automatic announce scheduling
- Adjust Interval to configure the delay between broadcasts

You can now add as many announces as you like to the "AnnounceList" attribute.
Announces must have the following format:

```
<Announce EnableParser="true/false" Message="YourMessage"/>
```

#Tags
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

Using the {t:(format)} tag:

The {t:(format)} tag can be used to display the current time.
Replace (format) with a format string.
The given formatter has to be a .NET-style format string. (see https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

Example for a broadcast displaying the current time in HH:mm:ss format:
```
<Announce EnableParser="true" Message="Current time {t:HH:mm:ss}"/>
```
## Command configuration
Every command has it's own XML configuration. The files are located under /cfg/cmd

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

```
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
```
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

#LUA object definitions
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




