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

Unfurtunally you can view the players password in servers log if you have acess to it, this is because the native code logs all commands from players, you know how to disable it? feel free to pull request.

The players password is saved based in the player name, if the player changes the name it will lost that password and will need to register again.

Server Auth is completly server side so no needs to clients have this mod installed to play on the server.

### Considerations
This mod change a lot of native code, and can break easily throught updates, please make a backup in your world before adding it.

Performance can be a impacted on the server, because of authentication verification.

To much unlogged players can cause performances leaks because of forcing stop moviment.

### About Server Auth
Server Auth is open source project and can easily be accessed on the github, all contents from this mod is completly free.

If you want to contribute into the project you can access the project github and make your pull request.

You are free to fork the project and make your own version of Server Auth, as long the name is changed

### Register
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/0091d753-6329-4d6a-b871-bbb3ef8f3a36) 
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/969828d4-2381-4df5-9fb6-8bc71ea9fd36)
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/1066845b-becf-4e62-9375-ba48e5df3559)

### Login
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/0193f58e-cf56-435f-a300-42dd4cc02746)
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/6696f3fb-66a1-4bee-94ae-d92f1c7ca6df)

https://github.com/LeandroTheDev/server_auth/assets/106118473/58a9501c-2da2-494f-84cc-0af1da27bce3

https://github.com/LeandroTheDev/server_auth/assets/106118473/2e39be0e-544a-4a5d-957e-615a8ea784bb




### Building
Learn more about vintage story modding in [Linux](https://github.com/LeandroTheDev/arch_linux/wiki/Games#vintage-story-modding) or [Windows](https://wiki.vintagestory.at/index.php/Modding:Setting_up_your_Development_Environment)

Create a template with name ServerAuth, and paste all contents from this project in there

> Linux

Make a symbolic link for fast tests
- ln -s /path/to/project/Releases/serverauth/* /path/to/game/Mods/ServerAuth/

Execute the comamnd ./build.sh, consider downloading dotnet 7 in our linux distribution if you dont have it.

> Windows

Just open the visual studio with ServerAuth.sln

FTM License
