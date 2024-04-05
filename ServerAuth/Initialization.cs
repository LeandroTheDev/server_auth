using System;
using System.Collections.Generic;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ServerAuth;

public class Initialization : ModSystem
{
    ICoreServerAPI api;
    public static Dictionary<string, IServerPlayer> unloggedPlayers = [];

    private OverwriteNetwork overwriteNetwork = new();
    public override void StartServerSide(ICoreServerAPI _api)
    {
        api = _api;
        base.StartServerSide(api);
        // Create register command
        api.ChatCommands.Create("register")
        // Description
        .WithDescription("Register the account name to the server")
        // Chat privilege
        .RequiresPrivilege(Privilege.chat)
        // Only if is a valid player
        .RequiresPlayer()
        // Need a argument called password
        .WithArgs(new StringArgParser("password", true))
        // Function Handle
        .HandleWith(RegisterPlayer);

        // Create login command
        api.ChatCommands.Create("login")
        // Description
        .WithDescription("Login into to the server")
        // Chat privilege
        .RequiresPrivilege(Privilege.chat)
        // Only if is a valid player
        .RequiresPlayer()
        // Need a argument called password
        .WithArgs(new StringArgParser("password", true))
        // Function Handle
        .HandleWith(LoginPlayer);

        api.Event.PlayerJoin += PlayerJoin;
        api.Event.PlayerDisconnect += PlayerDisconnect;
        
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        overwriteNetwork.OverwriteNativeFunctions();
    }

    public override void Dispose()
    {
        base.Dispose();
        overwriteNetwork.overwriter.UnpatchAll();
    }

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Server;
    }

    private void PlayerDisconnect(IServerPlayer player)
    {
        // If player is unlogged yet and disconnect the server remove it from unllogedPlayers
        if (unloggedPlayers.TryGetValue(player.PlayerName, out _)) unloggedPlayers.Remove(player.PlayerName);
    }

    private void PlayerJoin(IServerPlayer player)
    {
        // Add new player to the unlogged state
        unloggedPlayers[player.PlayerName] = player;
        // player.InventoryManager.DiscardAll();
    }

    private TextCommandResult RegisterPlayer(TextCommandCallingArgs args)
    {
        // Check if the password argument is valid
        if (args[0] is not string) return TextCommandResult.Error("Invalid password", "0");
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is already registered
        if (savedPasswords.TryGetValue(args.Caller.Player.PlayerName, out _)) return TextCommandResult.Error("This account is already registered", "1");

        // Receive player password into saved passwords
        savedPasswords[args.Caller.Player.PlayerName] = args[0] as string;
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        // Remove it from unlogged players
        unloggedPlayers.Remove(args.Caller.Player.PlayerName);
        return TextCommandResult.Success("Successfully registered the account");
    }

    private TextCommandResult LoginPlayer(TextCommandCallingArgs args)
    {
        return TextCommandResult.Success("Successfully logged");
    }

    private Dictionary<string, string> GetSavedPasswords()
    {
        byte[] dataBytes = api.WorldManager.SaveGame.GetData("LevelUPData_Axe");
        string data = dataBytes == null ? "{}" : SerializerUtil.Deserialize<string>(dataBytes);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(data);
    }
}

public class Debug
{
    static public void Log(string message)
    {
        Console.WriteLine($"{DateTime.Now:d.M.yyyy HH:mm:ss} [ServerAuth] {message}");
    }
}
