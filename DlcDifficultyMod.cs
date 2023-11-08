using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using CommonModNS;
using System.Transactions;

namespace DlcDifficultyModNS
{
    public enum Speed { FASTER, NORMAL, SLOWER }
    public enum Difficulty { EASIER, NORMAL, HARDER }

    [HarmonyPatch]
    public partial class DlcDifficultyMod : Mod
    {
        public static DlcDifficultyMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);

        public static bool AllowNewGameDlc => instance?.allowNewGame.Value ?? false;
        public static bool AllowDlcOnlyGame => instance?.allowNewGame.Value ?? false;
        public static bool NoRabbits => instance?.removeRabbit.Value ?? false;
        public static Difficulty GreedDifficulty => instance?.greedDifficulty.Value ?? Difficulty.NORMAL;
        public static Speed SadnessSpeed => instance?.sadnessSpeed.Value ?? Speed.NORMAL;
        public static bool AllowOldProcreation => instance?.oldFolksInHouses.Value ?? true;
        public static Speed AgingSpeed => instance?.agingSpeed.Value ?? Speed.NORMAL;

        private void Awake()
        {
            instance = this;
            SetupConfig();
            Mod spawn_control_mod = ModManager.LoadedMods.FirstOrDefault(x => x.name == "spawn_control_mod");
            if (spawn_control_mod != null)
            {

            }
            WorldManagerPatches.GetSaveRound += WM_Save;
            WorldManagerPatches.LoadSaveRound += WM_Load;
            WorldManagerPatches.ApplyPatches(Harmony);
            Harmony.PatchAll();
        }

        ConfigEntryBool allowNewGame;
        ConfigEntryBool allowDlcGame;
        ConfigEntryBool removeRabbit;
        ConfigEntryBool oldFolksInHouses;
        ConfigToggledEnum<Difficulty> greedDifficulty;
        ConfigToggledEnum<Speed> sadnessSpeed;
        ConfigToggledEnum<Speed> agingSpeed;

        private void SetupConfig()
        {
            Mod xpileMod = ModManager.LoadedMods.Find(x => x.Manifest.Id == "spawn_control_mod") ?? this;
            SetSpecial_SadDivisor = xpileMod.GetType().GetMethod("SetSadnessDivisor");
            if (xpileMod == this)
            {
                //Harmony.PatchAll(typeof());
            }

            _ = new ConfigFreeText("...", Config, "dlcmod_allworlds", "") { TextAlign = TextAlign.Center, TextColor = Color.red };
            allowDlcGame = new ConfigEntryBool("dlcmod_allowDlcGame", Config, false, new ConfigUI()
            {
                NameTerm = "dlcmod_allowDlcGame",
                TooltipTerm = "dlcmod_allowDlcGame_tooltip"
            })
            { currentValueColor = Color.blue };
            allowNewGame = new ConfigEntryBool("dlcmod_allowNewGame", Config, false, new ConfigUI()
            {
                NameTerm = "dlcmod_allowNewGame",
                TooltipTerm = "dlcmod_allowNewGame_tooltip"
            })
            { currentValueColor = Color.blue };
            removeRabbit = new ConfigEntryBool("dlcmod_remove_rabbit", Config, false, new ConfigUI()
            {
                NameTerm = "dlcmod_remove_rabbit",
                TooltipTerm = "dlcmod_remove_rabbit_tooltip"
            })
            { currentValueColor = Color.blue };

            _ = new ConfigFreeText("...", Config, "dlcmod_greed", "") { TextAlign = TextAlign.Center, TextColor = Color.red };
            greedDifficulty = new ConfigToggledEnum<Difficulty>("dlcmod_greedDifficulty", Config, Difficulty.NORMAL)
            {
                currentValueColor = Color.blue,
                onDisplayText = delegate () { return I.Xlat("dlcmod_greedDifficulty"); },
                onDisplayTooltip = delegate () { return I.Xlat("dlcmod_greedDifficulty_tooltip"); },
                onDisplayEnumText = delegate(Difficulty d)
                {
                    return I.Xlat($"dlcmod_difficulty_{d}");
                }
            };

            _ = new ConfigFreeText("...", Config, "dlcmod_sadness", "") { TextAlign = TextAlign.Center, TextColor = Color.red };
            sadnessSpeed = new ConfigToggledEnum<Speed>("dlcmod_sadnessSpeed", Config, Speed.NORMAL)
            {
                currentValueColor = Color.blue,
                onDisplayText = delegate () { return I.Xlat("dlcmod_sadnessSpeed"); },
                onDisplayTooltip = delegate () { return I.Xlat("dlcmod_sadnessSpeed_tooltip"); },
                onDisplayEnumText = delegate (Speed s)
                {
                    return I.Xlat($"dlcmod_happyspeed_{s}");
                }
            };

            _ = new ConfigFreeText("...", Config, "dlcmod_death", "") { TextAlign = TextAlign.Center, TextColor = Color.red };
            agingSpeed = new ConfigToggledEnum<Speed>("dlcmod_agingSpeed", Config, Speed.NORMAL)
            {
                currentValueColor = Color.blue,
                onDisplayText = delegate () { return I.Xlat("dlcmod_agingSpeed"); },
                onDisplayTooltip = delegate () { return I.Xlat("dlcmod_agingSpeed_tooltip"); },
                onDisplayEnumText = delegate (Speed s)
                {
                    return I.Xlat($"dlcmod_agingspeed_{s}");
                }
            };
            oldFolksInHouses = new ConfigEntryBool("dlcmod_nooldparents", Config, false, new ConfigUI()
            {
                NameTerm = "dlcmod_nooldparents",
                TooltipTerm = "dlcmod_nooldparents_tooltip"
            })
            { currentValueColor = Color.blue };

            Config.OnSave = ApplyConfig;
        }

        public override void Ready()
        {
            Patch_MainMenu_Start();
            ApplyConfig();
            Log("Ready!");
        }
    }
}