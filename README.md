# Server Auth
Increase the security for your server by adding a new account register and login on player connections, totally server side,

Features:
- Register account system ex: /register 123
- Login account system ex: /login 123
- Change password system ex: /changepassword 321
- Admin change password system ex: /forcechangepassword test 321
- Configuration files

Player unlogged cannot:
- Drop items
- Execute commands
- Break blocks
- Get dropped items
- Use inventory
- Deal damage
- Receive damage
- Lose saturation
- Move

### Observations
Players not registered will not count as unlogged player. (Configurable)

Players has 20 seconds to login, if player not logged will be automatically disconnected from the server. (Configurable)

Too many wrongs password will kick user and ban for some seconds. (Configurable)

Entering too many times in server and not logging will ban the user for some seconds. (Configurable)

When a player enters in the server all the inventory is saved locally and the player inventory is clear, so
the unlogged players will not have access to the inventory, when the player successfully login in, the inventory is restored.

Unfurtunally you can view the players passwords in servers log if you have acess to it, this is because the native code logs all commands from players, you know how to disable it? feel free to pull request.

The players password is saved based in the player UID (you can force to use player name instead but this is UNSECURE), if the player changes the name the password for login will be the same.

Server Auth is completly server side so no needs to clients have this mod installed to play on the server.

Changing authentication to uid and player name will cause incosistences with players needing register again, be carefull changing it in mid game

### Considerations
This mod change a lot of native code, and can break easily throught updates, please make a backup in your world before adding it.

Performance can be a impacted on the server, because of authentication verification.

To much unlogged players can cause performances leaks because of forcing stop moviment.

# About Server Auth
Server Auth is open source project and can easily be accessed on the github, all contents from this mod is completly free.

If you want to contribute into the project you can access the project github and make your pull request.

You are free to fork the project and make your own version of Server Auth, as long the name is changed

# Building
- Install .NET in your system, open terminal type: ``dotnet new install VintageStory.Mod.Templates``
- Create a template with the name ``ServerAuth``: ``dotnet new vsmod --AddSolutionFile -o ServerAuth``
- [Clone the repository](https://github.com/LeandroTheDev/server_auth/archive/refs/heads/main.zip)
- Copy the ``CakeBuild`` and ``build.ps1`` or ``build.sh`` and paste inside the repository

Now you can build using the ``build.ps1`` or ``build.sh`` file

FTM License
