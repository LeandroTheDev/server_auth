using HarmonyLib;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace ServerAuth;

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

    // Override Package Receive
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkChannel), "OnPacket")]
    public static bool OnPacket(Packet_CustomPacket p, IServerPlayer player)
    {
        Debug.Log($"Channel ID: {p.ChannelId}, Message ID: {p.MessageId}");
        if(Initialization.unloggedPlayers.TryGetValue(player.PlayerName, out _)) Debug.Log("PLAYER IS UNLOGGED");
        return true;
    }
}
