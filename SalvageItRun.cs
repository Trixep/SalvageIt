using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.GameOver;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Pause;

namespace SalvageIt;


[HarmonyPatch(typeof(PauseScreen), nameof(PauseScreen.Open))]
internal static class PauseScreen_Open
{
    [HarmonyPostfix]
    private static void Postfix(PauseScreen __instance)
    {
        var mainPanel = __instance.sidePanel.transform.parent.gameObject;
        SalvageIt.SalvageUI(mainPanel);
    }
}

[HarmonyPatch(typeof(DefeatScreen), nameof(DefeatScreen.Open))]
internal static class DefeatScreen_Open
{
    [HarmonyPostfix]
    private static void Postfix(DefeatScreen __instance)
    {
        SalvageIt.SalvageUI(__instance.regularObject);
    }
}

[HarmonyPatch(typeof(BossDefeatScreen), nameof(BossDefeatScreen.Open))]
internal static class BossDefeatScreen_Open
{
    [HarmonyPostfix]
    private static void Postfix(BossDefeatScreen __instance)
    {
        SalvageIt.SalvageUI(__instance.commonPanel.gameObject);
    }
}