using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Utils;
using Il2CppNewtonsoft.Json;
using MelonLoader;
using SalvageIt;
using UnityEngine;
using Directory = System.IO.Directory;
using DirectoryInfo = System.IO.DirectoryInfo;
using File = System.IO.File;
using Path = System.IO.Path;
using FileInfo = System.IO.FileInfo;
using MemoryStream = System.IO.MemoryStream;
using SearchOption = System.IO.SearchOption;
using TaskScheduler = BTD_Mod_Helper.Api.TaskScheduler;

[assembly: MelonInfo(typeof(SalvageIt.SalvageIt), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SalvageIt;

public class SalvageIt : BloonsTD6Mod
{
    private static ModHelperOption? deleteOption;

    public const string SavesFolderName = "SalvageItSaves";

    public static string SavesFolder => Path.Combine(Game.instance.playerService.configuration.playerDataRootPath, SavesFolderName, Game.Player.Data.ownerID ?? "");

    public static string FilePathFor(string mapName) => Path.Combine(SavesFolder, mapName.ToString());

    public static int highestCompletedRound;

    internal static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
    };


    public static readonly ModSettingButton OpenSavesFolder = new(() => Process.Start(new ProcessStartInfo
    {
        FileName = SavesFolder,
        UseShellExecute = true,
        Verb = "open"
    }))
    {
        displayName = "Open salvages folder",
        buttonText = "Open"
    };

    public static readonly ModSettingButton DeleteData = new(() =>
        PopupScreen.instance.ShowPopup(PopupScreen.Placement.menuCenter, "Delete Data",
            "Are you sure you want to delete all Salvage data?", new Action(ClearData), "Delete", null,
            "Cancel", Popup.TransitionAnim.Scale))

    {
        displayName = "Calculating...",
        buttonText = "Delete All Salvages",
        buttonSprite = VanillaSprites.RedBtnLong,
        modifyOption = CalcSize
    };

    public static void CalcSize(ModHelperOption? option = null)
    {
        if (option != null) deleteOption = option;

        var folder = new DirectoryInfo(SavesFolder);
        if (!folder.Exists || deleteOption == null) return;

        Task.Run(() => folder.EnumerateFiles("*", SearchOption.AllDirectories).Sum(info => info.Length))
            .ContinueWith(task => TaskScheduler.ScheduleTask(() =>
            {
                if (deleteOption != null)
                {
                    deleteOption.TopRow.GetComponentInChildren<ModHelperText>()
                        .SetText($"Storing {task.Result / 1000000.0:N1} mb of data");
                }
            }));
    }

    public static void ClearData()
    {
        try
        {
            Directory.Delete(SavesFolder, true);
            Directory.CreateDirectory(SavesFolder);
        }
        catch (Exception e)
        {
            ModHelper.Warning<SalvageIt>(e);
        }

        CalcSize();
    }

    public override void OnMainMenu()
    {
        var folder = new DirectoryInfo(SavesFolder);
        
        if (!folder.Exists) 
        {
            try
            {
                Directory.CreateDirectory(SavesFolder);
            }
            catch (Exception e)
            {
                ModHelper.Warning<SalvageIt>(e);
            }
        }
    }

    public static void SaveGame()
    {
        var path = FilePathFor($"{InGame.instance.MapDataSaveId} {InGame.instance.SelectedMode} - Round {InGame.instance.bridge.GetCurrentRound()}");
        var saveModel = InGame.instance.CreateCurrentMapSave(InGame.instance.bridge.GetCurrentRound() - 1, InGame.instance.MapDataSaveId);
        var text = JsonConvert.SerializeObject(saveModel, Settings);
        var bytes = Encoding.UTF8.GetBytes(text);

        using var outputStream = new MemoryStream(bytes);
        using (var zlibStream = new ZLibStream(outputStream, CompressionMode.Compress))
        {
            zlibStream.Write(bytes, 0, bytes.Length);
        }

        Directory.CreateDirectory(new FileInfo(path).DirectoryName!);
        File.WriteAllBytes(path, outputStream.ToArray());

        PopupScreen.instance.SafelyQueue(screen => screen.ShowPopup(PopupScreen.Placement.menuCenter, "Salvage It!", "The file has been saved.",
            new Action(() => Process.Start(new ProcessStartInfo { FileName = SavesFolder, UseShellExecute = true, Verb = "open" })), "Open Folder", null, "Close",
            Popup.TransitionAnim.Scale, PopupScreen.BackGround.Grey));
    }

    public static void LoadSave(string fileToLoad)
    {
        if (InGame.instance == null) return;

        var file = FilePathFor(fileToLoad);

        string text;

        if (File.Exists(file))
        {
            var bytes = File.ReadAllBytes(file);
            using var inputStream = new MemoryStream(bytes);
            using var outputStream = new MemoryStream();
            using (var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress))
            {
                zlibStream.CopyTo(outputStream);
            }
            text = Encoding.UTF8.GetString(outputStream.ToArray());
        }
        else if (File.Exists(file + ".json"))
        {
            text = File.ReadAllText(file + ".json");
        }
        else
        {
            ModHelper.Warning<SalvageIt>($"No data for {file}");
            return;
        }

        var saveModel = JsonConvert.DeserializeObject<MapSaveDataModel>(text, Settings);

        InGame.Bridge.ExecuteContinueFromCheckpoint(InGame.Bridge.MyPlayerNumber, new KonFuze(), ref saveModel,
            true, false);

        Game.Player.Data.SetSavedMap(saveModel.savedMapsId, saveModel);
    }

    public static void SalvageUI(GameObject mainPanel)
    {
        var sidePanel = mainPanel.AddModHelperPanel(new Info("Salvage Info", 0, -1150, 2500, 150));

        var btn = mainPanel.AddModHelperComponent(ModHelperButton.Create(new Info("SaveSalvageBtn", 1700, -700, 700, 250), VanillaSprites.BlueBtnLong, 
            new Action(() => SaveGame())));
        btn.AddText(new Info("Text", InfoPreset.FillParent), "Save Salvage", 80);

        var btn2 = mainPanel.AddModHelperComponent(ModHelperButton.Create(new Info("LoadSalvageBtn", 1700, -1000, 700, 250), VanillaSprites.GreenBtnLong, 
            new Action(() => SalvageUISaves(mainPanel))));
        btn2.AddText(new Info("Text", InfoPreset.FillParent), "Load Salvage", 80);
    }

    public static void SalvageUISaves(GameObject mainPanel)
    {
        var folder = new DirectoryInfo(SavesFolder);
        var saves = folder.GetFiles().Select(fileInfo => Path.GetFileNameWithoutExtension(fileInfo.Name)).ToList();

        var salvagesPanel = mainPanel.AddModHelperScrollPanel(new Info("Salvage Info", 0, 0, 800, 2000), RectTransform.Axis.Vertical, VanillaSprites.MainBgPanelHematite, 50, 25);
        ModHelperButton closeBtn = null;
        closeBtn = mainPanel.AddModHelperComponent(ModHelperButton.Create(new Info("CloseSalvageBtn", 500, 950, 200), VanillaSprites.CloseBtn,
            new Action(() => CLoseSalvagePanel(salvagesPanel.gameObject, closeBtn.gameObject))));
        //it works, okay? :)

        foreach (var save in saves)
        {
            var text = $"Load \"{save}\" save file?";
            var btnImage = VanillaSprites.BlueBtnLong;

            var btn = salvagesPanel.ScrollContent.AddButton(
                new Info($"Btn{save}", 650, 200), btnImage,
                new Action(() =>
                {
                    MenuManager.instance.buttonClick3Sound.Play("ClickSounds");
                    PopupScreen.instance.SafelyQueue(screen => screen.ShowPopup(PopupScreen.Placement.menuCenter, "Salvage It!", text,
                        new Action(() => LoadSave(save)), "Yes", null, "No", Popup.TransitionAnim.Scale, PopupScreen.BackGround.Grey));
                })
            );

            btn.AddText(new Info("Text", InfoPreset.FillParent), save.ToString(), 40);
        }
    }

    public static void CLoseSalvagePanel(GameObject salvagePanel, GameObject closeButton)
    {
        GameObject.Destroy(salvagePanel);
        GameObject.Destroy(closeButton);
    }
}