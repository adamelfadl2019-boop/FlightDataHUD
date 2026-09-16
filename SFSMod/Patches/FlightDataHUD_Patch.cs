using HarmonyLib;
using SFS.World;

namespace SFSMod.Patches
{
    /// <summary>
    /// Rocket.FixedUpdate runs once per tick for EVERY rocket/debris object in
    /// the scene, not just the one you're flying. The original version of this
    /// patch called FlightDataHUD.Refresh() on every single one of those calls,
    /// redundantly recalculating orbit data for objects nobody's looking at.
    ///
    /// Fix: use Harmony's __instance parameter to check which specific rocket
    /// is being updated, and only refresh the HUD when it's the one the player
    /// actually controls.
    /// </summary>
    [HarmonyPatch(typeof(Rocket), "FixedUpdate")]
    public static class FlightDataHUD_Patch
    {
        static void Postfix(Rocket __instance)
        {
            if (PlayerController.main != null && PlayerController.main.player.Value == __instance)
            {
                FlightDataHUD.Refresh(__instance);
            }
        }
    }
}
