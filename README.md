# Server Auth
Increase the security for your server by adding a new account register and login on player connections, totally server side,

Features:
- Register account system ex: /register 123
- Login account system ex: /login 123

Player unlogged cannot:
- Drop items
- Execute commands
- Break blocks
- Place blocks
- Get dropped items
- Use inventory
- Throw items
- Deal damage
- Receive damage
- Lose saturation
- Move

### Observations
Players not registered will not count as unlogged player.

Players has 25 seconds to login, generally this is smaller because of time to enter in the world, if player not logged will be automatically disconnected from the server.

Too many wrongs password will kick user and ban for some seconds.

Entering too many times in server and not logging will ban the user for some seconds.

When a player enters in the server all the inventory is saved locally and the player inventory is clear, so
the unlogged players will not have access to the inventory, when the player successfully login in, the inventory is restored.

Unfurtunally you can view the players password in servers log if you have acess to it, this is because the native code logs all commands from players, you know how to disable it? feel free to pull request.

### Considerations
This mod change a lot of native code, and can break easily throught updates, please make a backup in your world before adding it.

Performance can be a impacted on the server, because of authentication verification.

### Register
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/0091d753-6329-4d6a-b871-bbb3ef8f3a36) 
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/969828d4-2381-4df5-9fb6-8bc71ea9fd36)
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/1066845b-becf-4e62-9375-ba48e5df3559)

### Login
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/0193f58e-cf56-435f-a300-42dd4cc02746)
![image](https://github.com/LeandroTheDev/server_auth/assets/106118473/6696f3fb-66a1-4bee-94ae-d92f1c7ca6df)


### Building
Learn more about vintage story modding in [Linux](https://github.com/LeandroTheDev/arch_linux/wiki/Games#vintage-story-modding) or [Windows](https://wiki.vintagestory.at/index.php/Modding:Setting_up_your_Development_Environment)

> Linux

Make a symbolic link for fast tests
- ln -s /path/to/project/Releases/serverauth/* /path/to/game/Mods/ServerAuth/

Execute the comamnd ./build.sh, consider downloading dotnet 7 in our linux distribution if you dont have it.

> Windows

Just open the visual studio with ServerAuth.sln

FTM License
