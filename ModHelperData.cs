using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using Il2CppAssets.Scripts.Unity.UI_New.ChallengeEditor;

namespace SalvageIt;

public static class ModHelperData
{
    public const string WorksOnVersion = "44.0";
    public const string Version = "1.0.0";
    public const string Name = "Salvage It";

    public const string Description =
        "A mod that helps you load another player's map save or send yours to others, preserving the exact setup for a perfect salvage :)\n\n" +
        "Drag the salvage file into the right folder which you can open by goint to the mod settings and clicking the \"Open Folder\" button. To save a salvage, " +
        "just click the \"Save Salvage\" button In-Game, and it will be saved right away.";

    public const string RepoOwner = "Trixep"; // TODO add your github username hero, also in the download url in README.md
    public const string RepoName = "SalvageIt"; // TODO add your repo name here, also in the download url in README.md
}