using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Client;
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
    static private Initialization instance;
    public Harmony overwriter;

    public void OverwriteNativeFunctions(Initialization init)
    {
        instance = init;
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
        // If player is unlogged cannot access inventory: get dropped items or drop items
        if (instance.unloggedPlayers.TryGetValue(player.PlayerUID, out _)) return false;
        else return true;
    }

    // Overwrite the server command system
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatCommandApi), "Execute", [typeof(string), typeof(IServerPlayer), typeof(int), typeof(string), typeof(Action<TextCommandResult>)])]
    public static bool ExecuteServer(string commandName, IServerPlayer player, int groupId, ref string args, Action<TextCommandResult> onCommandComplete)
    {
        // Login or register command doesn't need to be logged
        if (commandName == "login" || commandName == "register")
            return true;

        // Check if player is logged
        if (instance.unloggedPlayers.TryGetValue(player.PlayerUID, out _))
        {
            player.SendMessage(0, Configuration.ErrorRequestLogin, EnumChatType.Notification);
            return false;
        }
        return true;
    }

    // Overwrite the block break system
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Block), "OnBlockBroken")]
    public static bool OnBlockBroken(Block __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
        // In cases that block broken is not broken by a player
        if (byPlayer == null) return true;

        // If player is unlogged cannot break blocks
        if (instance.unloggedPlayers.TryGetValue(byPlayer.PlayerUID, out _))
        {
            IServerPlayer player = byPlayer as IServerPlayer;
            // This will replace the block the player breaked,
            // in general the block will not be breaked for the server, but the player client can still
            // pass the block hes breaked after login, so we need to replace the block
            // if hes breaked so he cannot pass
            Task.Delay(100).ContinueWith((_) =>
            {
                Debug.Log($"{byPlayer.PlayerUID} tried to break a block in {pos.X} {pos.Y} {pos.Z} but hes is not logged");
                __instance.DoPlaceBlock(world, player, new BlockSelection(pos, BlockFacing.SOUTH, __instance), new ItemStack(__instance, 1));
            });
            return false;
        }
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

        if (player.Player.PlayerUID == null) return true;

        // If player is unlogged cannot receive or deal damage
        if (instance.unloggedPlayers.TryGetValue(player.Player.PlayerUID, out _)) return false;
        else return true;
    }

    // Overwrite the saturation
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EntityBehaviorHunger), "ReduceSaturation")]
    public static bool ReduceSaturation(EntityBehaviorHunger __instance, float satLossMultiplier)
    {
        if (__instance.entity is EntityPlayer)
        {
            EntityPlayer player = __instance.entity as EntityPlayer;
            // If player is unlogged cannot lose saturation
            if (instance.unloggedPlayers.TryGetValue(player.GetName(), out _)) return false;
        }
        return true;
    }
}
