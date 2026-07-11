using BepInEx;
using HarmonyLib;
namespace MortarLib
{

        [BepInPlugin("com.severon.mortarlib", "Mortar Lib", "1.0.0")]
        public class Plugin : BaseUnityPlugin
        {
            private void Awake()
            {
                Harmony harmony = new Harmony("com.severon.mortarlib");
                harmony.PatchAll();

                Logger.LogInfo("Mortar Lib loaded successfully, hell yeah.");
            }
        }
        [HarmonyPatch(typeof(M_Gamemode), "Initialize")]
        public static class M_Gamemode_Initialize_Patch
        {
            private static void Postfix(M_Gamemode __instance)
            {
                __instance.allowLeaderboardScoring = false;
                __instance.allowCheatedScores = false;
            }
        }
}
