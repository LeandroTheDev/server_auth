using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace ServerAuth;

public class Initialization : ModSystem
{
    ICoreServerAPI api;
#pragma warning disable CA2211
    public static Dictionary<string, IServerPlayer> unloggedPlayers = [];
#pragma warning restore CA2211
    private readonly Dictionary<string, int> timeoutPlayers = [];
    private readonly Dictionary<string, PlayerFreeze> freezePlayers = [];

    private readonly OverwriteNetwork overwriteNetwork = new();
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
        .WithArgs(new StringArgParser("password", false))
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
        .WithArgs(new StringArgParser("password", false))
        // Function Handle
        .HandleWith(LoginPlayer);

        Debug.Log("Commands registered");

        api.Event.PlayerJoin += PlayerJoin;
        api.Event.PlayerDisconnect += PlayerDisconnect;
        api.Event.RegisterGameTickListener(ReduceTimeoutPenalty, 10000);
        api.Event.RegisterGameTickListener(FreezeUnloggedPlayers, 100);

        Debug.Log("Connections events registered");
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
        #region restore_inventory
        if (freezePlayers.TryGetValue(player.PlayerName, out _))
        {
            foreach (IInventory playerInventory in player.InventoryManager.Inventories.Values)
            {
                // Get inventory type
                string inventoryType = playerInventory.GetType().ToString();

                // Stores all items inventory into player freeze variable
                if (inventoryType.Contains("InventoryPlayerCreative")) continue;
                else if (inventoryType.Contains("InventoryPlayerHotbar"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerName].hotbar[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerBackPacks"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerName].backpack[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerGround"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerName].ground[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerMouseCursor"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerName].mouse[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryCraftingGrid"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerName].crafting[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryCharacter"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerName].character[index];
                        index++;
                    }
                }
            }
        }
        #endregion
        unloggedPlayers.Remove(player.PlayerName);
        freezePlayers.Remove(player.PlayerName);
    }

    private void PlayerJoin(IServerPlayer player)
    {
        // Add new player to the unlogged state
        unloggedPlayers[player.PlayerName] = player;

        //Timeout checker
        if (!timeoutPlayers.TryGetValue(player.PlayerName, out _)) timeoutPlayers[player.PlayerName] = 0;
        timeoutPlayers[player.PlayerName] += 1;
        if (timeoutPlayers[player.PlayerName] >= 5) player.Disconnect("Too many attempts");

        // After 25s checks if player is still unlogged then disconnects it
        Task.Delay(25000).ContinueWith((_) => DisconnectPlayerIfIsUnlogged(player));

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is already registered, if yes ask for login
        if (savedPasswords.TryGetValue(player.PlayerName, out _))
        {
            // Create a new instance of freeze player
            freezePlayers[player.PlayerName] = new PlayerFreeze(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z);
            // Swipe all inventory that player has
            foreach (IInventory playerInventory in player.InventoryManager.Inventories.Values)
            {
                // Get inventory type
                string inventoryType = playerInventory.GetType().ToString();
                // Stores all items inventory into player freeze variable
                if (inventoryType.Contains("InventoryPlayerCreative")) continue;
                else if (inventoryType.Contains("InventoryPlayerHotbar"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // Check if slot contains item
                        if (item.Itemstack == null)
                        {
                            freezePlayers[player.PlayerName].hotbar.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from hotbar and cloning into freeze player
                        freezePlayers[player.PlayerName].hotbar.Add(index, item.Itemstack.Clone());
                        index++;

                        // Remove the item from player inventory
                        item.Itemstack = null;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerBackPacks"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // Check if slot contains item
                        if (item.Itemstack == null)
                        {
                            freezePlayers[player.PlayerName].backpack.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from backpack and cloning into freeze player
                        freezePlayers[player.PlayerName].backpack.Add(index, item.Itemstack.Clone());
                        index++;

                        // Remove the item from player inventory
                        item.Itemstack = null;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerGround"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // Check if slot contains item
                        if (item.Itemstack == null)
                        {
                            freezePlayers[player.PlayerName].ground.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from ground and cloning into freeze player
                        freezePlayers[player.PlayerName].ground.Add(index, item.Itemstack.Clone());
                        index++;

                        // Remove the item from player inventory
                        item.Itemstack = null;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerMouseCursor"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // Check if slot contains item
                        if (item.Itemstack == null)
                        {
                            freezePlayers[player.PlayerName].mouse.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from mouse and cloning into freeze player
                        freezePlayers[player.PlayerName].mouse.Add(index, item.Itemstack.Clone());
                        index++;

                        // Remove the item from player inventory
                        item.Itemstack = null;
                    }
                }
                else if (inventoryType.Contains("InventoryCraftingGrid"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // Check if slot contains item
                        if (item.Itemstack == null)
                        {
                            freezePlayers[player.PlayerName].crafting.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from crafting and cloning into freeze player
                        freezePlayers[player.PlayerName].crafting.Add(index, item.Itemstack.Clone());
                        index++;

                        // Remove the item from player inventory
                        item.Itemstack = null;
                    }
                }
                else if (inventoryType.Contains("InventoryCharacter"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // Check if slot contains item
                        if (item.Itemstack == null)
                        {
                            freezePlayers[player.PlayerName].character.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from character and cloning into freeze player
                        freezePlayers[player.PlayerName].character.Add(index, item.Itemstack.Clone());
                        index++;

                        // Remove the item from player inventory
                        item.Itemstack = null;
                    }
                }
            }
            player.SendMessage(0, "To continue please login: /login password", EnumChatType.Notification);
        }
        // If not ask for register
        else
        {
            unloggedPlayers.Remove(player.PlayerName);
            player.SendMessage(0, "This servers provides authentication system, consider using: /register password", EnumChatType.Notification);
        }
    }

    private TextCommandResult RegisterPlayer(TextCommandCallingArgs args)
    {
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success("Please type a password", "0");
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is already registered
        if (savedPasswords.TryGetValue(args.Caller.Player.PlayerName, out _)) return TextCommandResult.Success("This account is already registered use /login password", "1");

        // Receive player password into saved passwords
        savedPasswords[args.Caller.Player.PlayerName] = args[0] as string;
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        return TextCommandResult.Success("Successfully registered the account, next time you logging you will need the password");
    }

    private TextCommandResult LoginPlayer(TextCommandCallingArgs args)
    {
        IServerPlayer player = args.Caller.Player as IServerPlayer;
        #region password_check
        if (!unloggedPlayers.TryGetValue(player.PlayerName, out _)) return TextCommandResult.Success("You are already logged", "0");
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success("Please type a password", "0");
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is not registered
        if (!savedPasswords.TryGetValue(player.PlayerName, out _)) return TextCommandResult.Success("This account is not registered yet, register using /register password", "2");
        // Password check
        if (!(args[0] as string == savedPasswords[player.PlayerName]))
        {
            Debug.Log($"{player.PlayerName} typed wrong password");

            // Increment timeout
            if (!timeoutPlayers.TryGetValue(player.PlayerName, out _)) timeoutPlayers[player.PlayerName] = 0;
            timeoutPlayers[player.PlayerName] += 1;

            // Disconnect the player if timeout exceed
            if (timeoutPlayers[player.PlayerName] >= 5) player.Disconnect("Too many attempts");
            return TextCommandResult.Success("Invalid password", "3");
        };
        #endregion

        #region restore_inventory
        foreach (IInventory playerInventory in player.InventoryManager.Inventories.Values)
        {
            // Get inventory type
            string inventoryType = playerInventory.GetType().ToString();

            // Stores all items inventory into player freeze variable
            if (inventoryType.Contains("InventoryPlayerCreative")) continue;
            else if (inventoryType.Contains("InventoryPlayerHotbar"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerName].hotbar[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryPlayerBackPacks"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerName].backpack[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryPlayerGround"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerName].ground[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryPlayerMouseCursor"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerName].mouse[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryCraftingGrid"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerName].crafting[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryCharacter"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerName].hotbar.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerName].character[index];
                    index++;
                }
            }
        }
        #endregion

        #region cleaning
        // Remove it from unlogged players
        unloggedPlayers.Remove(args.Caller.Player.PlayerName);
        freezePlayers.Remove(args.Caller.Player.PlayerName);

        // Notify player the changes
        player.BroadcastPlayerData(true);

        Debug.Log($"{args.Caller.Player.PlayerName} logged into server");
        return TextCommandResult.Success("Successfully logged");
        #endregion
    }

    private Dictionary<string, string> GetSavedPasswords()
    {
        byte[] dataBytes = api.WorldManager.SaveGame.GetData("ServerAuth_Passwords");
        string data = dataBytes == null ? "{}" : SerializerUtil.Deserialize<string>(dataBytes);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(data);
    }

    private void DisconnectPlayerIfIsUnlogged(IServerPlayer player)
    {
        if (unloggedPlayers.TryGetValue(player?.PlayerName, out _))
        {
            player.Disconnect("Login timeout");
            Debug.Log($"{player.PlayerName} kicked from the server after 10s unlogged");
            if (timeoutPlayers.TryGetValue(player.PlayerName, out _)) timeoutPlayers[player.PlayerName] = 0;
            timeoutPlayers[player.PlayerName] += 1;
        }
    }

    private void ReduceTimeoutPenalty(float id)
    {
        foreach (string playerName in timeoutPlayers.Keys)
        {
            timeoutPlayers[playerName] -= 1;
            if (timeoutPlayers[playerName] <= 0) timeoutPlayers.Remove(playerName);
        }
    }

    private void FreezeUnloggedPlayers(float id)
    {
        if (freezePlayers.Count == 0) return;

        // Swipe all players freezes positions
        foreach (string freezeName in freezePlayers.Keys)
        {
            // Swipe all online players
            foreach (IPlayer player in api.World.AllOnlinePlayers)
            {
                // Check if is the same
                if (player.PlayerName == freezeName)
                {
                    // Reset Position
                    player.Entity.TeleportToDouble(
                        freezePlayers[freezeName].X,
                        freezePlayers[freezeName].Y,
                        freezePlayers[freezeName].Z
                    );
                    break;
                }
            }
        }
    }
}

public class Debug
{
    static public void Log(string message)
    {
        Console.WriteLine($"{DateTime.Now:d.M.yyyy HH:mm:ss} [ServerAuth] {message}");
    }
}
