﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ServerAuth;

public class Initialization : ModSystem
{
    ICoreServerAPI api;
    public Dictionary<string, IServerPlayer> unloggedPlayers = [];
    private readonly Dictionary<string, int> timeoutPlayers = [];
    private readonly Dictionary<string, PlayerFreeze> freezePlayers = [];
    private readonly Dictionary<string, bool> unregisteredFreezePlayers = [];
    private readonly Dictionary<string, bool> deadUnloggedPlayers = [];

    private readonly OverwriteNetwork overwriteNetwork = new();
    public override void StartServerSide(ICoreServerAPI _api)
    {
        api = _api;
        base.StartServerSide(api);
        // Create register command
        api.ChatCommands.Create("register")
        // Description
        .WithDescription("Register the account to the server")
        // Chat privilege
        .RequiresPrivilege(Privilege.chat)
        // Only if is a valid player
        .RequiresPlayer()
        // Need a argument called password
        .WithArgs(new StringArgParser("password", false))
        // Function Handle
        .HandleWith(Configuration.UIDAuthentication ? RegisterPlayerSecurePlayerUID : RegisterPlayerUnsecurePlayerName);

        Debug.Log("Register command registered");

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
        .HandleWith(Configuration.UIDAuthentication ? LoginPlayerSecurePlayerUID : LoginPlayerUnsecurePlayerName);

        Debug.Log("Login command registered");

        // Create change password command
        api.ChatCommands.Create("changepassword")
        // Description
        .WithDescription("Change the password of account authentication")
        // Chat privilege
        .RequiresPrivilege(Privilege.chat)
        // Only if is a valid player
        .RequiresPlayer()
        // Need a argument called password
        .WithArgs(new StringArgParser("password", false))
        // Function Handle
        .HandleWith(Configuration.UIDAuthentication ? ChangePasswordSecurePlayerUID : ChangePasswordUnsecurePlayerName);

        Debug.Log("Change password command registered");

        // Create admin change password command
        api.ChatCommands.Create("forcechangepassword")
        // Description
        .WithDescription("Admin command to change password from a player")
        // Chat privilege
        .RequiresPrivilege(Privilege.root)
        // Need two arguments called player and password
        .WithArgs(new StringArgParser("player password", false))
        // Function Handle
        .HandleWith(Configuration.UIDAuthentication ? AdminChangePasswordSecurePlayerUID : AdminChangePasswordUnsecurePlayerName);

        Debug.Log("Admin change password command registered");

        if (!Configuration.UIDAuthentication)
            Debug.Log("WARNING YOU ARE HOSTING IN ANY UNSECURE CONFIGURATION, IF YOU DON'T KNOW WHAT YOU ARE DOING CONSIDER CHANGING \"uidAuthentication\" TO TRUE");

        if (Configuration.UIDAuthentication)
            api.Event.PlayerJoin += PlayerJoinSecurePlayerUID;
        else
            api.Event.PlayerJoin += PlayerJoinUnsecurePlayerName;
        if (Configuration.UIDAuthentication)
            api.Event.PlayerDisconnect += PlayerDisconnectSecurePlayerUID;
        else
            api.Event.PlayerDisconnect += PlayerDisconnectUnsecurePlayerName;
        if (Configuration.UIDAuthentication)
            api.Event.PlayerNowPlaying += PlayerReadySecurePlayerUID;
        else
            api.Event.PlayerNowPlaying += PlayerReadyUnsecurePlayerName;
        if (Configuration.UIDAuthentication)
            api.Event.PlayerRespawn += PlayerRespawnSecurePlayerUID;
        else
            api.Event.PlayerRespawn += PlayerRespawnUnsecurePlayerName;
        api.Event.RegisterGameTickListener(ReduceTimeoutPenalty, Configuration.TimeToReducePlayerAttempts);
        api.Event.RegisterGameTickListener(FreezeUnloggedPlayers, 100);

        Debug.Log("Connections events registered");
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Debug.LoadLogger(api.Logger);
        Debug.Log("Running on Version: 1.0.6");
        overwriteNetwork.OverwriteNativeFunctions(this);
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

    public override double ExecuteOrder()
    {
        return 0;
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        Configuration.PopulateConfigurations(api);
    }

    #region events
    private void PlayerDisconnectUnsecurePlayerName(IServerPlayer player)
    {
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        #region restore_inventory
        // Finded the player freezed and the player is registered, we need to restore the inventory in this case
        if (freezePlayers.TryGetValue(player.PlayerName, out _) && savedPasswords.TryGetValue(player.PlayerName, out _))
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
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED ON HOTBAR");
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
                        if (!freezePlayers[player.PlayerName].backpack.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED ON BACKPACK");
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
                        if (!freezePlayers[player.PlayerName].ground.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED ON GROUND");
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
                        if (!freezePlayers[player.PlayerName].mouse.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED ON MOUSE");
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
                        if (!freezePlayers[player.PlayerName].crafting.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED CRAFTING");
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
                        if (!freezePlayers[player.PlayerName].character.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED CHARACTER");
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
        deadUnloggedPlayers.Remove(player.PlayerName);
    }

    private void PlayerDisconnectSecurePlayerUID(IServerPlayer player)
    {
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        #region restore_inventory
        // Finded the player freezed and the player is registered, we need to restore the inventory in this case
        if (freezePlayers.TryGetValue(player.PlayerUID, out _) && savedPasswords.TryGetValue(player.PlayerUID, out _))
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
                        if (!freezePlayers[player.PlayerUID].hotbar.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED ON HOTBAR");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerUID].hotbar[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerBackPacks"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerUID].backpack.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED ON BACKPACK");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerUID].backpack[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerGround"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerUID].ground.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED ON GROUND");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerUID].ground[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryPlayerMouseCursor"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerUID].mouse.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED ON MOUSE");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerUID].mouse[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryCraftingGrid"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerUID].crafting.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED CRAFTING");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerUID].crafting[index];
                        index++;
                    }
                }
                else if (inventoryType.Contains("InventoryCharacter"))
                {
                    int index = 0;
                    foreach (ItemSlot item in playerInventory)
                    {
                        // If not found alert the server the item has been corrupted
                        if (!freezePlayers[player.PlayerUID].character.TryGetValue(index, out _))
                        {
                            Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED CHARACTER");
                            continue;
                        }
                        item.Itemstack = freezePlayers[player.PlayerUID].character[index];
                        index++;
                    }
                }
            }
        }
        #endregion
        unloggedPlayers.Remove(player.PlayerUID);
        freezePlayers.Remove(player.PlayerUID);
        deadUnloggedPlayers.Remove(player.PlayerUID);
    }

    private void PlayerJoinUnsecurePlayerName(IServerPlayer player)
    {
        // Add new player to the unlogged state
        unloggedPlayers[player.PlayerName] = player;

        //Timeout checker
        if (!timeoutPlayers.TryGetValue(player.PlayerName, out _)) timeoutPlayers[player.PlayerName] = 0;
        timeoutPlayers[player.PlayerName] += 1;
        if (timeoutPlayers[player.PlayerName] >= Configuration.MaxAttemptsToBanPlayer) player.Disconnect(Configuration.ErrorTooManyAttempts);

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is already registered, if yes freeze the player
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
        }
        // Check for the freeze unregistered players option
        else if (Configuration.FreezeNonRegisteredPlayer)
            // Create a new instance of freeze player for unregistered player
            unregisteredFreezePlayers[player.PlayerName] = true;
        // If player doesnt have account simple remove it from unlogged players
        else unloggedPlayers.Remove(player.PlayerName);
    }

    private void PlayerJoinSecurePlayerUID(IServerPlayer player)
    {
        // Add new player to the unlogged state
        unloggedPlayers[player.PlayerUID] = player;

        //Timeout checker
        if (!timeoutPlayers.TryGetValue(player.PlayerUID, out _)) timeoutPlayers[player.PlayerUID] = 0;
        timeoutPlayers[player.PlayerUID] += 1;
        if (timeoutPlayers[player.PlayerUID] >= Configuration.MaxAttemptsToBanPlayer) player.Disconnect(Configuration.ErrorTooManyAttempts);

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is already registered, if yes freeze the player
        if (savedPasswords.TryGetValue(player.PlayerUID, out _))
        {
            // Create a new instance of freeze player
            freezePlayers[player.PlayerUID] = new PlayerFreeze(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z);
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
                            freezePlayers[player.PlayerUID].hotbar.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from hotbar and cloning into freeze player
                        freezePlayers[player.PlayerUID].hotbar.Add(index, item.Itemstack.Clone());
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
                            freezePlayers[player.PlayerUID].backpack.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from backpack and cloning into freeze player
                        freezePlayers[player.PlayerUID].backpack.Add(index, item.Itemstack.Clone());
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
                            freezePlayers[player.PlayerUID].ground.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from ground and cloning into freeze player
                        freezePlayers[player.PlayerUID].ground.Add(index, item.Itemstack.Clone());
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
                            freezePlayers[player.PlayerUID].mouse.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from mouse and cloning into freeze player
                        freezePlayers[player.PlayerUID].mouse.Add(index, item.Itemstack.Clone());
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
                            freezePlayers[player.PlayerUID].crafting.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from crafting and cloning into freeze player
                        freezePlayers[player.PlayerUID].crafting.Add(index, item.Itemstack.Clone());
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
                            freezePlayers[player.PlayerUID].character.Add(index, null);
                            index++;
                            continue;
                        }
                        // Getting the item from character and cloning into freeze player
                        freezePlayers[player.PlayerUID].character.Add(index, item.Itemstack.Clone());
                        index++;

                        // Remove the item from player inventory
                        item.Itemstack = null;
                    }
                }
            }
        }
        // Check for the freeze unregistered players option
        else if (Configuration.FreezeNonRegisteredPlayer)
            // Create a new instance of freeze player for unregistered player
            unregisteredFreezePlayers[player.PlayerUID] = true;
        // If player doesnt have account simple remove it from unlogged players
        else unloggedPlayers.Remove(player.PlayerUID);
    }

    private void PlayerReadyUnsecurePlayerName(IServerPlayer player)
    {
        // Check if player is dead
        if (!player.Entity.Alive)
        {
            deadUnloggedPlayers.Add(player.PlayerName, true);
            Debug.Log($"{player.PlayerName} has joined the server while dead");
        }

        // Check for unregistered players
        if (unregisteredFreezePlayers.TryGetValue(player.PlayerName, out _))
        {
            // If player is unregistered we need to freeze completly now
            // this is necessary because the first join in the world
            // the player spawns in the edge of the world
            // and the wait is necessary to player spawn in the world spawn

            // Create a new instance of freeze player
            Task.Delay(Configuration.TimeToFreezeUnregisteredPlayersAfterJoin).ContinueWith((_) =>
            {
                freezePlayers[player.PlayerName] = new PlayerFreeze(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z);
            });

            // Clear the unregistered list
            unregisteredFreezePlayers.Remove(player.PlayerName);
        }
        // After 20s checks if player is still unlogged then disconnects it
        Task.Delay(Configuration.TimeUntilKickUnloggedPlayer).ContinueWith((_) => DisconnectPlayerIfIsUnlogged(player));

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();

        Debug.Log($"{player.PlayerName} has joined the server with the UID: {player.PlayerUID}");
        // Check if player is already registered, if yes ask for login
        if (savedPasswords.TryGetValue(player.PlayerName, out _)) player.SendMessage(0, Configuration.LoginMessage, EnumChatType.Notification);
        else player.SendMessage(0, Configuration.RegisterMessage, EnumChatType.Notification);
    }

    private void PlayerReadySecurePlayerUID(IServerPlayer player)
    {
        // Check if player is dead
        if (!player.Entity.Alive)
        {
            deadUnloggedPlayers.Add(player.PlayerUID, true);
            Debug.Log($"{player.PlayerName} has joined the server while dead with the UID: {player.PlayerUID}");
        }
        else
        {
            Debug.Log($"{player.PlayerName} has joined the server with the UID: {player.PlayerUID}");
        }

        // Check for unregistered players
        if (unregisteredFreezePlayers.TryGetValue(player.PlayerUID, out _))
        {
            // If player is unregistered we need to freeze completly now
            // this is necessary because the first join in the world
            // the player spawns in the edge of the world
            // and the wait is necessary to player spawn in the world spawn

            // Create a new instance of freeze player
            Task.Delay(Configuration.TimeToFreezeUnregisteredPlayersAfterJoin).ContinueWith((_) =>
            {
                freezePlayers[player.PlayerUID] = new PlayerFreeze(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z);
            });

            // Clear the unregistered list
            unregisteredFreezePlayers.Remove(player.PlayerUID);
        }
        // After 20s checks if player is still unlogged then disconnects it
        Task.Delay(Configuration.TimeUntilKickUnloggedPlayer).ContinueWith((_) => DisconnectPlayerIfIsUnlogged(player));

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();

        // Check if player is already registered, if yes ask for login
        if (savedPasswords.TryGetValue(player.PlayerUID, out _)) player.SendMessage(0, Configuration.LoginMessage, EnumChatType.Notification);
        else player.SendMessage(0, Configuration.RegisterMessage, EnumChatType.Notification);
    }

    private void PlayerRespawnUnsecurePlayerName(IServerPlayer player)
    {
        freezePlayers[player.PlayerName].X = player.Entity.Pos.X;
        freezePlayers[player.PlayerName].Y = player.Entity.Pos.Y;
        freezePlayers[player.PlayerName].Z = player.Entity.Pos.Z;

        Task.Delay(Configuration.ReviveFreezeDelay)
            .ContinueWith((_) => deadUnloggedPlayers.Remove(player.PlayerName));
    }

    private void PlayerRespawnSecurePlayerUID(IServerPlayer player)
    {
        freezePlayers[player.PlayerUID].X = player.Entity.Pos.X;
        freezePlayers[player.PlayerUID].Y = player.Entity.Pos.Y;
        freezePlayers[player.PlayerUID].Z = player.Entity.Pos.Z;

        Task.Delay(Configuration.ReviveFreezeDelay)
            .ContinueWith((_) => deadUnloggedPlayers.Remove(player.PlayerUID));
    }
    #endregion

    #region commands unsecure
    private TextCommandResult RegisterPlayerUnsecurePlayerName(TextCommandCallingArgs args)
    {
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success(Configuration.ErrorTypePassword, "0");
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is already registered
        if (savedPasswords.TryGetValue(args.Caller.Player.PlayerName, out _)) return TextCommandResult.Success(Configuration.ErrorAlreadyRegistered, "1");

        // Receive player password into saved passwords
        savedPasswords[args.Caller.Player.PlayerName] = args[0] as string;
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        // Unfreze player if is freezed
        if (Configuration.FreezeNonRegisteredPlayer)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            #region cleaning
            // Remove it from unlogged players
            unloggedPlayers.Remove(args.Caller.Player.PlayerName);
            freezePlayers.Remove(args.Caller.Player.PlayerName);

            // Notify player the changes
            player.BroadcastPlayerData(true);

            #endregion
            Debug.Log($"{args.Caller.Player.PlayerName} registered into server");
            return TextCommandResult.Success(Configuration.SuccessRegisteredMessage);
        }

        Debug.Log($"{args.Caller.Player.PlayerName} registered into server");
        return TextCommandResult.Success(Configuration.SuccessRegisteredMessage);
    }

    private TextCommandResult LoginPlayerUnsecurePlayerName(TextCommandCallingArgs args)
    {
        IServerPlayer player = args.Caller.Player as IServerPlayer;
        #region password_check
        if (!unloggedPlayers.TryGetValue(player.PlayerName, out _)) return TextCommandResult.Success(Configuration.ErrorAlreadyLogged, "0");
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success(Configuration.ErrorTypePassword, "0");
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is not registered
        if (!savedPasswords.TryGetValue(player.PlayerName, out _)) return TextCommandResult.Success(Configuration.ErrorNotRegistered, "2");
        // Password check
        if (!(args[0] as string == savedPasswords[player.PlayerName]))
        {
            Debug.Log($"{player.PlayerName} typed wrong password");

            // Increment timeout
            if (!timeoutPlayers.TryGetValue(player.PlayerName, out _)) timeoutPlayers[player.PlayerName] = 0;
            timeoutPlayers[player.PlayerName] += 1;

            // Disconnect the player if timeout exceed
            if (timeoutPlayers[player.PlayerName] >= Configuration.MaxAttemptsToBanPlayer) player.Disconnect(Configuration.ErrorTooManyAttempts);
            return TextCommandResult.Success(Configuration.ErrorInvalidPassword, "3");
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
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED HOTBAR");
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
                    if (!freezePlayers[player.PlayerName].backpack.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED BACKPACK");
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
                    if (!freezePlayers[player.PlayerName].ground.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED GROUND");
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
                    if (!freezePlayers[player.PlayerName].mouse.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED MOUSE");
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
                    if (!freezePlayers[player.PlayerName].crafting.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED CRAFTING");
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
                    if (!freezePlayers[player.PlayerName].character.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerName} INVENTORY RESTORATION HAS BEEN CORRUPTED CHARACTER");
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
        return TextCommandResult.Success(Configuration.SuccessLoggedMessage);
        #endregion
    }

    private TextCommandResult ChangePasswordUnsecurePlayerName(TextCommandCallingArgs args)
    {
        IServerPlayer player = args.Caller.Player as IServerPlayer;
        // Checking if player is not logged
        if (unloggedPlayers.TryGetValue(player.PlayerName, out _)) return TextCommandResult.Success(Configuration.ErrorChangePasswordWithoutLogin, "3");
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success(Configuration.ErrorTypePassword, "0");

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is not registered
        if (!savedPasswords.TryGetValue(player.PlayerName, out _)) return TextCommandResult.Success(Configuration.ErrorNotRegistered, "2");

        // Update password
        savedPasswords[player.PlayerName] = args[0] as string;
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        return TextCommandResult.Success(Configuration.SuccessChangedPasswordMessage, "4");
    }

    private TextCommandResult AdminChangePasswordUnsecurePlayerName(TextCommandCallingArgs args)
    {
        // Check if the player name argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success("Please type the player name and password", "5");
        // Get the player name and password
        string[] namePass = args[0].ToString().Split(" "); // Player name = 0, password = 1
        // Check if exist the password
        if (namePass.Length == 1) return TextCommandResult.Success("Please type the account password", "7");
        // Check if exist more than name and password
        if (namePass.Length > 2) return TextCommandResult.Success("Please type only the account name and password", "7");

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is not registered
        if (!savedPasswords.TryGetValue(namePass[0], out _)) return TextCommandResult.Success($"Account {namePass[0]} doesn't exist", "2");

        // Update password
        savedPasswords[namePass[0]] = namePass[1];
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        return TextCommandResult.Success($"Successfully changed the {namePass[0]} password", "6");
    }
    #endregion

    #region commands secure
    private TextCommandResult RegisterPlayerSecurePlayerUID(TextCommandCallingArgs args)
    {
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success(Configuration.ErrorTypePassword, "0");
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is already registered
        if (savedPasswords.TryGetValue(args.Caller.Player.PlayerUID, out _)) return TextCommandResult.Success(Configuration.ErrorAlreadyRegistered, "1");

        // Receive player password into saved passwords
        savedPasswords[args.Caller.Player.PlayerUID] = args[0] as string;
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        // Unfreze player if is freezed
        if (Configuration.FreezeNonRegisteredPlayer)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            #region cleaning
            // Remove it from unlogged players
            unloggedPlayers.Remove(args.Caller.Player.PlayerUID);
            freezePlayers.Remove(args.Caller.Player.PlayerUID);

            // Notify player the changes
            player.BroadcastPlayerData(true);

            #endregion
            Debug.Log($"{args.Caller.Player.PlayerName} registered into server");
            return TextCommandResult.Success(Configuration.SuccessRegisteredMessage);
        }

        Debug.Log($"{args.Caller.Player.PlayerName} registered into server");
        return TextCommandResult.Success(Configuration.SuccessRegisteredMessage);
    }

    private TextCommandResult LoginPlayerSecurePlayerUID(TextCommandCallingArgs args)
    {
        IServerPlayer player = args.Caller.Player as IServerPlayer;
        #region password_check
        if (!unloggedPlayers.TryGetValue(player.PlayerUID, out _)) return TextCommandResult.Success(Configuration.ErrorAlreadyLogged, "0");
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success(Configuration.ErrorTypePassword, "0");
        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is not registered
        if (!savedPasswords.TryGetValue(player.PlayerUID, out _)) return TextCommandResult.Success(Configuration.ErrorNotRegistered, "2");
        // Password check
        if (!(args[0] as string == savedPasswords[player.PlayerUID]))
        {
            Debug.Log($"{player.PlayerUID} typed wrong password");

            // Increment timeout
            if (!timeoutPlayers.TryGetValue(player.PlayerUID, out _)) timeoutPlayers[player.PlayerUID] = 0;
            timeoutPlayers[player.PlayerUID] += 1;

            // Disconnect the player if timeout exceed
            if (timeoutPlayers[player.PlayerUID] >= Configuration.MaxAttemptsToBanPlayer) player.Disconnect(Configuration.ErrorTooManyAttempts);
            return TextCommandResult.Success(Configuration.ErrorInvalidPassword, "3");
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
                    if (!freezePlayers[player.PlayerUID].hotbar.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED HOTBAR");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerUID].hotbar[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryPlayerBackPacks"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerUID].backpack.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED BACKPACK");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerUID].backpack[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryPlayerGround"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerUID].ground.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED GROUND");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerUID].ground[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryPlayerMouseCursor"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerUID].mouse.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED MOUSE");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerUID].mouse[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryCraftingGrid"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerUID].crafting.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED CRAFTING");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerUID].crafting[index];
                    index++;
                }
            }
            else if (inventoryType.Contains("InventoryCharacter"))
            {
                int index = 0;
                foreach (ItemSlot item in playerInventory)
                {
                    // If not found alert the server the item has been corrupted
                    if (!freezePlayers[player.PlayerUID].character.TryGetValue(index, out _))
                    {
                        Debug.Log($"ALERT {player.PlayerUID} INVENTORY RESTORATION HAS BEEN CORRUPTED CHARACTER");
                        continue;
                    }
                    item.Itemstack = freezePlayers[player.PlayerUID].character[index];
                    index++;
                }
            }
        }
        #endregion

        #region cleaning
        // Remove it from unlogged players
        unloggedPlayers.Remove(args.Caller.Player.PlayerUID);
        freezePlayers.Remove(args.Caller.Player.PlayerUID);

        // Notify player the changes
        player.BroadcastPlayerData(true);

        Debug.Log($"{args.Caller.Player.PlayerName} logged into server");
        return TextCommandResult.Success(Configuration.SuccessLoggedMessage);
        #endregion
    }

    private TextCommandResult ChangePasswordSecurePlayerUID(TextCommandCallingArgs args)
    {
        IServerPlayer player = args.Caller.Player as IServerPlayer;
        // Checking if player is not logged
        if (unloggedPlayers.TryGetValue(player.PlayerUID, out _)) return TextCommandResult.Success(Configuration.ErrorChangePasswordWithoutLogin, "3");
        // Check if the password argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success(Configuration.ErrorTypePassword, "0");

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();
        // Check if player is not registered
        if (!savedPasswords.TryGetValue(player.PlayerUID, out _)) return TextCommandResult.Success(Configuration.ErrorNotRegistered, "2");

        // Update password
        savedPasswords[player.PlayerUID] = args[0] as string;
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        return TextCommandResult.Success(Configuration.SuccessChangedPasswordMessage, "4");
    }

    private TextCommandResult AdminChangePasswordSecurePlayerUID(TextCommandCallingArgs args)
    {
        // Check if the player name argument is valid
        if (args.Parsers[0].IsMissing) return TextCommandResult.Success("Please type the player name or uid and password", "5");
        // Get the player name and password
        string[] namePass = args[0].ToString().Split(" "); // Player name or uid = 0, password = 1
        // Check if exist the password
        if (namePass.Length == 1) return TextCommandResult.Success("Please type the account password", "7");
        // Check if exist more than name and password
        if (namePass.Length > 2) return TextCommandResult.Success("Please type only the account name or uid and password", "7");

        // Get all saved passwords in the server
        Dictionary<string, string> savedPasswords = GetSavedPasswords();

        string uidFromName = null;
        foreach (IPlayer player in api.World.AllPlayers)
        {
            if (player.PlayerName == namePass[0])
            {
                if (uidFromName != null)
                    return TextCommandResult.Success($"Multiple accounts names: \"{namePass[0]}\", cannot proceed, you need to check the UID manually", "2");
                uidFromName = player.PlayerUID;
            };
        }

        // Check if player is not registered
        if (!savedPasswords.TryGetValue(namePass[0], out _))
            // Check if we finded the uid with player name and exist in accounts registered
            if (uidFromName == null || !savedPasswords.TryGetValue(uidFromName, out _))
                return TextCommandResult.Success($"Account {namePass[0]} doesn't exist", "2");
            else // Update the uid based on player name
                namePass[0] = uidFromName;

        // Update password
        savedPasswords[namePass[0]] = namePass[1];
        // Save into the world database
        api.WorldManager.SaveGame.StoreData("ServerAuth_Passwords", JsonSerializer.Serialize(savedPasswords));

        return TextCommandResult.Success($"Successfully changed the {namePass[0]} password", "6");
    }
    #endregion

    #region utils
    private Dictionary<string, string> GetSavedPasswords()
    {
        byte[] dataBytes = api.WorldManager.SaveGame.GetData("ServerAuth_Passwords");
        string data = dataBytes == null ? "{}" : SerializerUtil.Deserialize<string>(dataBytes);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(data);
    }

    private void DisconnectPlayerIfIsUnlogged(IServerPlayer player)
    {
        if (Configuration.UIDAuthentication)
        {
            if (unloggedPlayers.TryGetValue(player?.PlayerUID, out _))
            {
                player.Disconnect("Login timeout");
                Debug.Log($"{player.PlayerUID} kicked from the server after 10s unlogged");
                if (timeoutPlayers.TryGetValue(player.PlayerUID, out _)) timeoutPlayers[player.PlayerUID] = 0;
                timeoutPlayers[player.PlayerUID] += 1;
            }
        }
        else
        {
            if (unloggedPlayers.TryGetValue(player?.PlayerName, out _))
            {
                player.Disconnect("Login timeout");
                Debug.Log($"{player.PlayerName} kicked from the server after 10s unlogged");
                if (timeoutPlayers.TryGetValue(player.PlayerName, out _)) timeoutPlayers[player.PlayerName] = 0;
                timeoutPlayers[player.PlayerName] += 1;
            }
        }
    }

    private void ReduceTimeoutPenalty(float id)
    {
        if (Configuration.UIDAuthentication)
            foreach (string playerUID in timeoutPlayers.Keys)
            {
                timeoutPlayers[playerUID] -= 1;
                if (timeoutPlayers[playerUID] <= 0) timeoutPlayers.Remove(playerUID);
            }
        else
            foreach (string playerName in timeoutPlayers.Keys)
            {
                timeoutPlayers[playerName] -= 1;
                if (timeoutPlayers[playerName] <= 0) timeoutPlayers.Remove(playerName);
            }
    }

    private void FreezeUnloggedPlayers(float id)
    {
        if (freezePlayers.Count == 0) return;

        if (Configuration.UIDAuthentication)
            // Swipe all players freezes positions
            foreach (string freezeUID in freezePlayers.Keys)
            {
                // Swipe all online players
                foreach (IPlayer player in api.World.AllOnlinePlayers)
                {
                    // Check if is the same
                    if (player.PlayerUID == freezeUID)
                    {
                        // If player is still dead not freeze them, used to prevent player respawning in the dead body
                        if (deadUnloggedPlayers.TryGetValue(player.PlayerUID, out _))
                            break;

                        // Reset Position
                        player.Entity.TeleportToDouble(
                            freezePlayers[freezeUID].X,
                            freezePlayers[freezeUID].Y,
                            freezePlayers[freezeUID].Z
                        );
                        break;
                    }
                }
            }
        else
            // Swipe all players freezes positions
            foreach (string freezeName in freezePlayers.Keys)
            {
                // Swipe all online players
                foreach (IPlayer player in api.World.AllOnlinePlayers)
                {
                    // Check if is the same
                    if (player.PlayerName == freezeName)
                    {
                        // If player is still dead not freeze them, used to prevent player respawning in the dead body
                        if (deadUnloggedPlayers.TryGetValue(player.PlayerName, out _))
                            break;

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
    #endregion
}

public class Debug
{
    private static readonly OperatingSystem system = Environment.OSVersion;
    static private ILogger loggerForNonTerminalUsers;

    static public void LoadLogger(ILogger logger) => loggerForNonTerminalUsers = logger;
    static public void Log(string message)
    {
        // Check if is linux or other based system and if the terminal is active for the logs to be show
        if ((system.Platform == PlatformID.Unix || system.Platform == PlatformID.Other) && Environment.UserInteractive)
            // Based terminal users
            Console.WriteLine($"{DateTime.Now:d.M.yyyy HH:mm:ss} [ServerAuth] {message}");
        else
            // Unbased non terminal users
            loggerForNonTerminalUsers?.Log(EnumLogType.Notification, $"[ServerAuth] {message}");
    }
}