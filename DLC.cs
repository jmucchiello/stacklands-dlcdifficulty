using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using HarmonyLib;
using CommonModNS;
using UnityEngine;

namespace DlcDifficultyModNS
{
    public partial class DlcDifficultyMod : Mod
    {
        MethodInfo SetSpecial_SadDivisor;

        public void SetSadnessDivisor(int value)
        {
            //SpecialEvents_Patch.SadEventDivisor = Math.Clamp(value, 1, 8);
        }

        public void ApplyConfig()
        {
            SetupHappinessCards();
            SetupDeathLifestages();
            ButtonNewDlcGame?.gameObject.SetActive(AllowNewGameDlc);
        }

        public void SetupHappinessCards()
        {
            float happinessModifier = SadnessSpeed switch
            {
                Speed.FASTER => 0.75f,
                Speed.SLOWER => 1.25f,
                _ => 1f
            };
            if (happinessModifier != 1f)
            {
                new List<BlueprintTimerModifier>() {
                    new BlueprintTimerModifier() { blueprintId = "blueprint_admire_coin", subprintindex = 0, multiplier = happinessModifier },
                    new BlueprintTimerModifier() { blueprintId = "blueprint_euphoria", subprintindex = 0, multiplier = happinessModifier },
                    new BlueprintTimerModifier() { blueprintId = "blueprint_happiness", subprintindex = 0, multiplier = happinessModifier },
                    new BlueprintTimerModifier() { blueprintId = "blueprint_tavern", subprintindex = 0, multiplier = happinessModifier }
                }.ForEach(x => x.AddToList());

                new List<GameCardTimerModifier>() {
                    new GameCardTimerModifier() { actionId = "complete_charity", myCardDataType = typeof(Charity), multiplier = happinessModifier },
                    new GameCardTimerModifier() { actionId = "research_food", myCardDataType = typeof(Tavern), multiplier = happinessModifier },
                    new GameCardTimerModifier() { actionId = "complete_petting", myCardDataType = typeof(PettingZoo), multiplier = happinessModifier }
                }.ForEach(x => x.AddToList());
            }

            GameCardStartTimer_Patch.modifiers.Add(new GameCardTimerModifier()
            {
                actionId = "research_food",
                myCardDataType = typeof(Tavern),
                multiplier = happinessModifier
            });
            Log($"Happiness creation multiplier {happinessModifier}");
        }

        public void SetupDeathLifestages()
        {
            AgingDetermination.Teenager = AgingSpeed switch { Speed.FASTER => 1, Speed.SLOWER => 3, _ => 2 };
            AgingDetermination.Adult    = AgingSpeed switch { Speed.FASTER => 5, Speed.SLOWER => 9, _ => 6 };
            AgingDetermination.Elderly  = AgingSpeed switch { Speed.FASTER => 6, Speed.SLOWER => 12, _ => 8 };
            Log($"Death Curse Lifespans - Teens: {AgingDetermination.Teenager}, Adults: {AgingDetermination.Adult}, Elderly: {AgingDetermination.Elderly}");
        }
#if UNDEFINED
        public void SetupSadEventFrequency()
        {
            switch (difficulty)
            {
                case DifficultyType.VeryEasy:
                    SpecialEvents_Patch.SadEventDivisor = 5;
                    SpecialEvents_Patch.SadEventMinMonth = 5;
                    break;
                case >= DifficultyType.VeryHard:
                    SpecialEvents_Patch.SadEventDivisor = 3;
                    SpecialEvents_Patch.SadEventMinMonth = 3;
                    break;
                default:
                    SpecialEvents_Patch.SadEventDivisor = 4;
                    SpecialEvents_Patch.SadEventMinMonth = 4;
                    break;
            }
            if (!AllowSadEvents) SpecialEvents_Patch.SadEventMinMonth = 1000000;
            I.Log($"Sad Event Frequency {SpecialEvents_Patch.SadEventDivisor} Min Month {SpecialEvents_Patch.SadEventMinMonth}");
        }
#endif
    }

    [HarmonyPatch(typeof(BaseVillager), nameof(BaseVillager.DetermineLifeStageFromAge))]
    public class AgingDetermination
    {
        public static int Teenager = 2;
        public static int Adult = 6;
        public static int Elderly = 8;
        static bool Prefix(BaseVillager __instance, ref LifeStage __result, int age)
        {
            if (age < Teenager) __result = LifeStage.Teenager;
            else if (age < Adult) __result = LifeStage.Adult;
            else if (age < Elderly) __result = LifeStage.Elderly;
            else __result = LifeStage.Dead;
            return false;
        }
    }

    [HarmonyPatch]
    public class AlterDemands
    {
        static Demand currentDemand;

        [HarmonyPatch(typeof(DemandManager), nameof(DemandManager.GetDemandById))]
        [HarmonyPostfix]
        static void GetDemandById(DemandManager __instance, ref Demand __result, string demandId)
        {
            if (currentDemand == null) GetDemandToStart(__instance, ref __result);
            __result = currentDemand;
        }

        [HarmonyPatch(typeof(DemandManager), nameof(DemandManager.GetDemandToStart))]
        [HarmonyPostfix]
        static void GetDemandToStart(DemandManager __instance, ref Demand __result)
        {
            if (DlcDifficultyMod.GreedDifficulty == Difficulty.NORMAL || __result.IsFinalDemand)
                return;

            currentDemand = ScriptableObject.CreateInstance<Demand>();
            //I.Log($"CreateInstance<Demand> {(currentDemand == null?"isnull":"notnull")}");
            currentDemand.Amount = __result.Amount;
            currentDemand.Duration = __result.Duration;
            currentDemand.Difficulty = __result.Difficulty;
            currentDemand.DemandId = __result.DemandId;
            currentDemand.CardToGet = __result.CardToGet;
            currentDemand.QuestFailedAnimationStates = __result.QuestFailedAnimationStates;
            currentDemand.QuestStartAnimationStates = __result.QuestStartAnimationStates;
            currentDemand.QuestSuccessAnimationStates = __result.QuestSuccessAnimationStates;
            currentDemand.ShouldDestroyOnComplete = __result.ShouldDestroyOnComplete;
            currentDemand.SuccessCards = __result.SuccessCards;
            currentDemand.FailedCards = __result.FailedCards;
            currentDemand.BlueprintIds = __result.BlueprintIds;
            currentDemand.IsFinalDemand = __result.IsFinalDemand;

            if (DlcDifficultyMod.GreedDifficulty == Difficulty.EASIER)
            {
                if (__result.Amount > 2)
                {
                    --currentDemand.Amount;
                    I.Log($"{currentDemand.DemandId} for {currentDemand.CardToGet} made easier by reducing the number needed to {currentDemand.Amount}.");
                }
                else
                {
                    ++__result.Duration;
                    I.Log($"{currentDemand.DemandId} for {currentDemand.CardToGet} made easier by increasing the duration to {currentDemand.Duration} moons.");
                }
            }
            else if (DlcDifficultyMod.GreedDifficulty == Difficulty.HARDER)
            {
                if (__result.Amount > 2)
                {
                    ++currentDemand.Amount;
                    I.Log($"{currentDemand.DemandId} for {currentDemand.CardToGet} made harder by increasing the number needed to {currentDemand.Amount}.");
                }
                else if (__result.Duration >= 2)
                {
                    --currentDemand.Duration;
                    I.Log($"{currentDemand.DemandId} for {currentDemand.CardToGet} made harder by decreasing the duration to {currentDemand.Duration} moons.");
                }
                else
                {
                    I.Log($"{currentDemand.DemandId} for {currentDemand.CardToGet} could not be made harder.");

                }
            }
            __result = currentDemand;
        }
    }

    [HarmonyPatch(typeof(House), "CanHaveCard")]
    public class NoOldVillagersInHouses
    {
        static bool Prefix(House __instance, ref bool __result, CardData otherCard)
        {
            if (!DlcDifficultyMod.AllowOldProcreation && otherCard is OldVillager)
            {
                __result = false;
                return false;
            }
            return true; // normal processing
        }
    }
}
