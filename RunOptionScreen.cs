using CommonModNS;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DlcDifficultyModNS
{
    public partial class DlcDifficultyMod
    {
        private static readonly string SaveGameKey_DlcGameMode = "dlcgamemode";
        public bool GameIsDlcOnlyRun { get; private set; }

        void WM_Save(WorldManager _, SaveRound round)
        {
            round.ExtraKeyValues.SetOrAdd(SaveGameKey_DlcGameMode, GameIsDlcOnlyRun ? "1" : "0");
        }

        void WM_Load(WorldManager _, SaveRound round)
        {
            string value = round.ExtraKeyValues.Find(x => x.Key == SaveGameKey_DlcGameMode)?.Value;
            GameIsDlcOnlyRun = String.IsNullOrEmpty(value) || value == "0" ? false : true;
        }

        void StartNewDlcRun()
        {
            GameIsDlcOnlyRun = true;
            instance.myROS.SetDlcMode(true);
            GameCanvas.instance.SetScreen<RunOptionsScreen>();
        }

        [HarmonyPatch(typeof(MainMenu),"StartNewRun")]
        [HarmonyPrefix]
        private static void MM_StartNewRun(MainMenu __instance)
        {
            instance.GameIsDlcOnlyRun = false;
            instance.myROS.SetDlcMode(false);
        }

        public enum SpiritWorld { NONE, GREED, SADNESS, DEATH };

        public SpiritWorld DlcRunSpiritWorld { get; private set; }

        private CustomButton ButtonNewDlcGame;

        private void Patch_MainMenu_Start()
        {
            try
            {
                MainMenu menu = (MainMenu)GameCanvas.instance.GetScreen<MainMenu>();
                Transform mainMenuButtons = GameCanvas.instance.transform.Find("MenuScreen/Background/Buttons");
                List<Transform> list = new List<Transform>();
                for (int i = 0; i < mainMenuButtons.childCount; ++i)
                {
                    list.Add(mainMenuButtons.GetChild(i));
                }
                mainMenuButtons.DetachChildren();
                ButtonNewDlcGame = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
                foreach (Transform t in list)
                {
                    t.SetParent(mainMenuButtons);
                    if (t.name == "NewRun")
                    {
                        ButtonNewDlcGame.transform.SetParent(mainMenuButtons);
                    }
                }
                SetupButton(ButtonNewDlcGame, I.Xlat("dlcmod_newdlcrun"), I.Xlat("dlcmod_newdlcrun_tooltip"));
                ButtonNewDlcGame.name = "NewCurseRun";
                ButtonNewDlcGame.Clicked += delegate
                {
                    if (I.WM.CurrentSave.LastPlayedRound != null)
                    {
                        GameCanvas.instance.ShowStartNewRunModal(delegate
                        {
                            StartNewDlcRun();
                        });
                    }
                    else
                    {
                        StartNewDlcRun();
                    }
                };
            }
            catch (Exception e)
            {
                Log("Exception caught modifying MainMenu.Start" + e.Message);
            }
        }

        private RunOptionsScreenPatching myROS;

        public class RunOptionsScreenPatching
        {
            Transform DefaultPlayButton;
            CustomButton MyPlayButton;
            Transform CurseOptions; // the normal play with curses group
            GameObject CurseChoice; // button group
            CustomButton DeathButton;
            CustomButton HappinessButton;
            CustomButton GreedButton;

            CustomButton AddedSadness;
            CustomButton AddedDeath;

            bool attached = false;

            public bool DlcOnlyMode { get => _dlcOnlyMode; set => _dlcOnlyMode = SetDlcMode(value); }
            private bool _dlcOnlyMode;

            public bool SetDlcMode(bool dlcMode)
            {
                CurseOptions.transform.gameObject.SetActive(!dlcMode);
                DefaultPlayButton.gameObject.SetActive(!dlcMode);

                CurseChoice.SetActive(dlcMode);
                MyPlayButton.gameObject.SetActive(dlcMode);
                return dlcMode;
            }

            public void Attach(RunOptionsScreen ros)
            {
                if (attached) return;
                attached = true;

                AddedSadness = ros.HappinessButton;
                AddedDeath = ros.DeathButton;

                Transform rosEntriesParent = ros.transform.Find("Background/Buttons");
                List<Transform> list = new List<Transform>();

                for (int i = 0, cnt = rosEntriesParent.childCount; i < cnt; ++i)
                {
                    Transform child = rosEntriesParent.GetChild(i);
                    if (child.name == "CurseOptions")
                    {
                        CurseOptions = child;
                        CurseChoice = UnityEngine.Object.Instantiate<GameObject>(CurseOptions.gameObject);
                        list.Add(CurseChoice.transform);
                    }
                    list.Add(child);
                    if (child.name == "PlayButton")
                    {
                        DefaultPlayButton = child;
                        MyPlayButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
                        list.Add(MyPlayButton.transform);
                    }
                }
                rosEntriesParent.DetachChildren();
                foreach (Transform t in list)
                {
                    t.SetParent(rosEntriesParent);
                }
                CurseChoice.name = "ChooseCurse";
                CurseChoice.GetComponent<VerticalLayoutGroup>();
                Transform label = CurseChoice.transform.GetChild(0);
                Transform spacer = CurseChoice.transform.GetChild(2);
                CurseChoice.transform.DetachChildren();
                label.SetParentClean(CurseChoice.transform);
                label.GetComponent<TextMeshProUGUI>().text = I.Xlat("dlcmodnew_choosecurse");
                //label.GetComponent<Tooltip>().TextMesh.text = I.Xlat("dlcmodnew_choosecurse_tooltip");
                CurseChoice.transform.localPosition = Vector3.zero;
                CurseChoice.transform.localRotation = Quaternion.identity;
                CurseChoice.transform.localScale = Vector3.one;

                GreedButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab, CurseChoice.transform);
                GreedButton.name = "GreedButton";
                HappinessButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab, CurseChoice.transform);
                HappinessButton.name = "SadnessButton";
                DeathButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab, CurseChoice.transform);
                DeathButton.name = "DeathButton";
                spacer.SetParentClean(CurseChoice.transform);

                string greedText = I.Xlat("dlcmodnew_greed");
                string sadnessText = I.Xlat("dlcmodnew_sadness");
                string deathText = I.Xlat("dlcmodnew_death");

                instance.DlcRunSpiritWorld = SpiritWorld.GREED;
                SetupButton(GreedButton, ConfigEntryHelper.ColorText(Color.blue, greedText));
                SetupButton(HappinessButton, sadnessText);
                SetupButton(DeathButton, deathText);
                GreedButton.Clicked += delegate ()
                {
                    instance.DlcRunSpiritWorld = SpiritWorld.GREED;
                    GreedButton.TextMeshPro.text = ConfigEntryHelper.ColorText(Color.blue, greedText);
                    HappinessButton.TextMeshPro.text = sadnessText;
                    DeathButton.TextMeshPro.text = deathText;
                    AddedDeath.enabled = true;
                    AddedSadness.enabled = true;
                };
                HappinessButton.Clicked += delegate ()
                {
                    instance.DlcRunSpiritWorld = SpiritWorld.SADNESS;
                    GreedButton.TextMeshPro.text = greedText;
                    HappinessButton.TextMeshPro.text = ConfigEntryHelper.ColorText(Color.blue, sadnessText);
                    DeathButton.TextMeshPro.text = deathText;
                    AddedDeath.enabled = false;
                    AddedSadness.enabled = true;
                };
                DeathButton.Clicked += delegate ()
                {
                    instance.DlcRunSpiritWorld = SpiritWorld.DEATH;
                    GreedButton.TextMeshPro.text = greedText;
                    HappinessButton.TextMeshPro.text = sadnessText;
                    DeathButton.TextMeshPro.text = ConfigEntryHelper.ColorText(Color.blue, deathText);
                    AddedDeath.enabled = false;
                    AddedSadness.enabled = true;
                };

                SetupButton(MyPlayButton, I.Xlat("label_start_run"));
                MyPlayButton.Clicked += delegate ()
                {
                    PlayButtonClick(ros);
                };
            }

            private void PlayButtonClick(RunOptionsScreen ros)
            {
                TransitionScreen.instance.StartTransition(delegate
                {
                    RunOptions ro = WorldManager.instance.CurrentRunOptions = new RunOptions
                    {
                        MoonLength = ros.CurMoonLength,
                        IsPeacefulMode = ros.PeacefulMode,
                        IsGreedEnabled = instance.DlcRunSpiritWorld == SpiritWorld.GREED,
                        IsHappinessEnabled = instance.DlcRunSpiritWorld == SpiritWorld.SADNESS || AddedSadness.enabled && AddedSadness.TextMeshPro.text.EndsWith(I.Xlat("label_on")),
                        IsDeathEnabled = instance.DlcRunSpiritWorld == SpiritWorld.DEATH || AddedDeath.enabled && AddedDeath.TextMeshPro.text.EndsWith(I.Xlat("label_on"))
                    };
                    I.Log($"IsGreedEnabled {ro.IsGreedEnabled}, IsHappinessEnabled {ro.IsHappinessEnabled}, IsDeathEnabled {ro.IsDeathEnabled}");
                    I.WM.CurrentRunVariables = new RunVariables();
                    WorldManager.instance.RoundExtraKeyValues = new List<SerializedKeyValuePair>();
                    MethodInfo ClearRound = AccessTools.Method(typeof(WorldManager), "ClearRound");
                    Log($"3 {(ClearRound == null?"isnull":"notnull")}");
                    ClearRound?.Invoke(I.WM,null);
                    Log($"4 {I.WM.BoardMonths.ForestMonth} {I.WM.BoardMonths.DeathMonth}");
                    GameBoard main = I.WM.GetBoardWithId("main");
                    Log($"main {(main == null?"isnull":"notnull")}");
                    Traverse t = Traverse.Create(I.WM).Property("CurrentBoard").SetValue(main);
                    I.Log($"set current board {t.PropertyExists()}");
                    I.Log($"{I.WM.CurrentBoard.Id}");

                    GameCanvas.instance.SetScreen<GameScreen>();
                    Log("6");
                    Log($"7-1 currentboardid {(I.WM.CurrentBoard == null ? "isnull" : "isnotnull")}");
                    Log("7");
                    I.WM.CurrentGameState = WorldManager.GameState.Playing;
                    Log("8-1");

                    GameBoard newBoard = I.WM.GetBoardWithId(instance.DlcRunSpiritWorld switch
                    {
                        SpiritWorld.GREED => "greed",
                        SpiritWorld.SADNESS => "happiness",
                        SpiritWorld.DEATH => "death",
                        _ => throw new Exception("No cursed world selected")
                    });
                    GameCanvas.instance.SetScreen<EmptyScreen>();
                    I.WM.GoToBoard(newBoard, delegate
                    {
                        GameCanvas.instance.SetScreen<GameScreen>();
                    }, "spirit");
                    //I.WM.CreateBoosterpack(I.WM.MiddleOfBoard(), "starter");

                    Log("9-1");
                    GameCamera.instance.CenterOnBoard(I.WM.CurrentBoard);
                    Log("9");
                    QuestManager.instance.CheckPacksUnlocked();
                    Log("10");
                    I.WM.UpdateCardTargets();
                    Log("11");

                    BoosterpackData pb = I.WM.BoosterPackDatas.Find(x => x.name == "starter");
                    if (pb != null)
                    {
                        foreach (var b in pb.BoosterAdditions)
                        {
                            Log($"start addition {b} {b.CardBags.Count()}");
                        }
                    }
                    else Log($"no starter");
                });
            }
        }

        private static void SetupButton(CustomButton button, string text = null, string tooltip = null)
        {
            button.transform.localPosition = Vector3.zero;
            button.transform.localRotation = Quaternion.identity;
            button.transform.localScale = Vector3.one;
            if (!String.IsNullOrEmpty(text)) button.TextMeshPro.text = text;
            if (!String.IsNullOrEmpty(tooltip)) button.TooltipText = tooltip;
        }


        [HarmonyPatch(typeof(RunOptionsScreen), "Start")]
        [HarmonyPostfix]
        static void RunOptionsScreen_Start(RunOptionsScreen __instance)
        {
            try
            {
                ref RunOptionsScreenPatching ros = ref DlcDifficultyMod.instance.myROS;
                if (ros == null)
                {
                    ros = new RunOptionsScreenPatching();
                    ros.Attach(__instance);
                }
                ros.DlcOnlyMode = DlcDifficultyMod.instance.GameIsDlcOnlyRun;
            }
            catch (Exception ex)
            {
                Log("Exception caught modifying RunOptionScreen.Start" + ex.Message);
            }
        }

        [HarmonyPatch(typeof(RunOptionsScreen), "SetCurseButton")]
        [HarmonyPrefix]
        static bool RunOptionsScreen_SetCurseButton(RunOptionsScreen __instance, ref bool curseUnlocked)
        {
            if (DlcDifficultyMod.AllowNewGameDlc) curseUnlocked = true;
            return true;
        }

        [HarmonyPatch(typeof(Cutscenes),nameof(Cutscenes.EveryoneInSpiritWorldDead))]
        [HarmonyPrefix]
        static bool Cutscenes_EveryoneInSpiritWorldDead()
        {
            if (instance.GameIsDlcOnlyRun)
            {
                GameCanvas.instance.SetScreen<GameOverScreen>();
                return false;
            }
            return true;
        }
    }
}
