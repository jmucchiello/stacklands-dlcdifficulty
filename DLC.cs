using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace DlcDifficultyModNS
{
    public partial class DlcDifficultyMod : Mod
    {
        MethodInfo SetSpecial_SadDivisor;

        public void SetSadnessDivisor(int value)
        {
            //SpecialEvents_Patch.SadEventDivisor = Math.Clamp(value, 1, 8);
        }

        public void ApplyDLC()
        {
            SetupHappinessCards();
            SetupDeathLifestages();
            SetupSadEventFrequency();
        }

        public void SetupHappinessCards()
        {
            float happinessBlueprintModifier = Difficulty switch
            {
                <= DifficultyType.VeryEasy => 0.8f,
                >= DifficultyType.VeryHard => 1.2f,
                _ => 1f
            };
            if (happinessBlueprintModifier != 1f)
            {
                new List<BlueprintTimerModifier>() {
                    new BlueprintTimerModifier() { blueprintId = "blueprint_admire_coin", subprintindex = 0, multiplier = happinessBlueprintModifier },
                    new BlueprintTimerModifier() { blueprintId = "blueprint_euphoria", subprintindex = 0, multiplier = happinessBlueprintModifier },
                    new BlueprintTimerModifier() { blueprintId = "blueprint_happiness", subprintindex = 0, multiplier = happinessBlueprintModifier },
                    new BlueprintTimerModifier() { blueprintId = "blueprint_tavern", subprintindex = 0, multiplier = happinessBlueprintModifier }
                }.ForEach(x => x.AddToList());
            }
            Log($"Happiness creation multiplier {happinessBlueprintModifier}");
        }

        public void SetupDeathLifestages()
        {
            if (difficulty == DifficultyType.Brutal)
            {
                AgingDetermination.Teenager = 1;
                AgingDetermination.Adult = 5;
                AgingDetermination.Elderly = 6;
            }
            else if (difficulty < DifficultyType.VeryEasy)
            {
                AgingDetermination.Teenager = 3;
                AgingDetermination.Adult = 9;
                AgingDetermination.Elderly = 12;
            }
            Log($"Death Curse Lifespans - Teens: {AgingDetermination.Teenager}, Adults: {AgingDetermination.Adult}, Elderly: {AgingDetermination.Elderly}");
        }

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
            DifficultyMod.Log($"Sad Event Frequency {SpecialEvents_Patch.SadEventDivisor} Min Month {SpecialEvents_Patch.SadEventMinMonth}");
        }
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

    [HarmonyPatch(typeof(DemandManager), nameof(DemandManager.GetDemandToStart))]
    public class AlterDemands
    {
        static void Postfix(DemandManager __instance, ref Demand __result)
        {
            if (DifficultyMod.Difficulty > DifficultyType.VeryEasy || DifficultyMod.Difficulty < DifficultyType.VeryHard)
                return;

            Demand demand = new Demand() { Amount = __result.Amount,
                                           Duration = __result.Duration, 
                                           Difficulty = __result.Difficulty,
                                           DemandId = __result.DemandId,
                                           CardToGet = __result.CardToGet,
                                           QuestFailedAnimationStates = __result.QuestFailedAnimationStates,
                                           QuestStartAnimationStates = __result.QuestStartAnimationStates,
                                           QuestSuccessAnimationStates = __result.QuestSuccessAnimationStates,
                                           ShouldDestroyOnComplete = __result.ShouldDestroyOnComplete,
                                           SuccessCards = __result.SuccessCards,
                                           FailedCards = __result.FailedCards,
                                           BlueprintIds = __result.BlueprintIds,
                                           IsFinalDemand = __result.IsFinalDemand
            };
            if (DifficultyMod.Difficulty <= DifficultyType.VeryEasy)
            {
                if (__result.Amount > 2) demand.Amount--;
                else __result.Duration++;
            }
            else if (DifficultyMod.Difficulty >= DifficultyType.VeryHard)
            {
                if (__result.Amount > 2) demand.Amount++;
                else if (__result.Duration >= 2) demand.Duration--;
            }
            __result = demand;
        }
    }

    [HarmonyPatch(typeof(House), "CanHaveCard")]
    public class NoOldVillagersInHouses
    {
        static bool Prefix(House __instance, ref bool __result, CardData otherCard)
        {
            if (DifficultyMod.Difficulty >= DifficultyType.VeryHard && otherCard.Id == "old_villager")
            {
                __result = false;
                return false;
            }
            return true; // normal processing
        }
    }
}
