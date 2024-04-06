using System;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace ServerAuth;

#pragma warning disable IDE0060
[HarmonyPatchCategory("serverauth_network")]
public class OverwriteNetwork
{
    public Harmony overwriter;

    public void OverwriteNativeFunctions()
    {
        if (!Harmony.HasAnyPatches("serverauth_network"))
        {
            overwriter = new Harmony("serverauth_network");
            overwriter.PatchCategory("serverauth_network");
            Debug.Log("Network system has been overwrited");
        }
        else
        {
            Debug.Log("FATAL ERROR: Cannot overwrite network system, did exist a mod with serverauth_network harmony?");
        }
    }

    // Overwrite the inventory access system
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryBasePlayer), "CanPlayerAccess")]
    public static bool CanPlayerAccess(InventoryBasePlayer __instance, IPlayer player, EntityPos position)
    {
        // If player is unlogged cannot access inventory
        if (Initialization.unloggedPlayers.TryGetValue(player.PlayerName, out _)) return false;
        else return true;
    }

    // Overwrite the command system
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatCommandApi), "Execute", [typeof(string), typeof(IServerPlayer), typeof(int), typeof(string), typeof(Action<TextCommandResult>)])]
    public static bool Execute(string commandName, IServerPlayer player, int groupId, string args, Action<TextCommandResult> onCommandComplete)
    {
        if (commandName == "login" || commandName == "register") return true;
        if (Initialization.unloggedPlayers.TryGetValue(player.PlayerName, out _))
        {
            player.SendMessage(0, "You need to loggin before using commands", EnumChatType.Notification);
            return false;
        }
        else return true;
    }

    // Overwrite the block place system
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Block), "CanPlaceBlock")]
    public static bool CanPlaceBlock(Block __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
    {
        // If player is unlogged cannot place blocks
        if (Initialization.unloggedPlayers.TryGetValue(byPlayer.PlayerName, out _))
        {
            failureCode = "claimed";
            return false;
        }
        else return true;
    }

    // Overwrite the block break system
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Block), "OnBlockBroken")]
    public static bool OnBlockBroken(Block __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
        // If player is unlogged cannot break blocks
        if (Initialization.unloggedPlayers.TryGetValue(byPlayer.PlayerName, out _))
        {
            IServerPlayer player = byPlayer as IServerPlayer;
            // This will replace the block the player breaked,
            // in general the block will not be breaked for the server, but the player client can still
            // pass the block hes breaked after login, so we need to replace the block
            // if hes breaked so he cannot pass
            Task.Delay(100).ContinueWith((_) =>
            {
                Debug.Log($"{byPlayer.PlayerName} tried to break a block in {pos.X} {pos.Y} {pos.Z} but hes is not logged");
                __instance.DoPlaceBlock(world, player, new BlockSelection(pos, BlockFacing.SOUTH, __instance), new ItemStack(__instance, 1));
            });
            return false;
        }
        else return true;
    }


    // Overwrite the spear throw
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemSpear), "OnHeldInteractStart")]
    public static bool OnHeldInteractStartSpear(ItemSpear __instance, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        // If player is unlogged cannot throw spears
        if (Initialization.unloggedPlayers.TryGetValue(byEntity.GetName(), out _)) return false;
        else return true;
    }

    // Overwrite the bow throw
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStart")]
    public static bool OnHeldInteractStartBow(ItemBow __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        // If player is unlogged cannot use bows
        if (Initialization.unloggedPlayers.TryGetValue(byEntity.GetName(), out _)) return false;
        else return true;
    }

    // Overwrite the stone throw
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemStone), "OnHeldInteractStart")]
    public static bool OnHeldInteractStartStone(ItemStone __instance, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        // If player is unlogged cannot throw stones
        if (Initialization.unloggedPlayers.TryGetValue(byEntity.GetName(), out _)) return false;
        else return true;
    }

    // Overwrite the damage
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Entity), "ReceiveDamage")]
    public static bool ReceiveDamage(Entity __instance, DamageSource damageSource, float damage)
    {
        // Get damage sources
        EntityPlayer player;
        if (damageSource.GetCauseEntity() is EntityPlayer) player = damageSource.GetCauseEntity() as EntityPlayer;
        else if (damageSource.SourceEntity is EntityPlayer) player = damageSource.SourceEntity as EntityPlayer;
        else if (__instance is EntityPlayer) player = __instance as EntityPlayer;
        else return true;

        if (player.Player.PlayerName == null) return true;

        // If player is unlogged cannot receive or deal damage
        if (Initialization.unloggedPlayers.TryGetValue(player.Player.PlayerName, out _)) return false;
        else return true;
    }

    // // Overwrite the durability not necessary because player inventory now is cleared
    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(CollectibleObject), "DamageItem")]
    // public static bool DamageItem(CollectibleObject __instance, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
    // {
    //     // If player is unlogged cannot lose item durability
    //     if (Initialization.unloggedPlayers.TryGetValue(byEntity.GetName(), out _)) return false;
    //     else return true;
    // }

    // Overwrite the saturation
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EntityBehaviorHunger), "ReduceSaturation")]
    public static bool ReduceSaturation(EntityBehaviorHunger __instance, float satLossMultiplier)
    {
        if (__instance.entity is EntityPlayer)
        {
            EntityPlayer player = __instance.entity as EntityPlayer;
            // If player is unlogged cannot lose saturation
            if (Initialization.unloggedPlayers.TryGetValue(player.GetName(), out _)) return false;
        }
        return true;
    }
}
