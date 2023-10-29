using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using static UnityEngine.TouchScreenKeyboard;
using UnityEngine;

namespace CommonModNS
{
    /**
     *      Features in this file:
     *          Modify times it takes for any StartTimer to run
     *          Modify times it takes for any StartBlueprintTimer to run
     **/

    public class GameCardTimerModifier
    {
        public string actionId;
        public Type myCardDataType = typeof(System.Object);
        public float multiplier; // if >0 time is multiplied by this amount
        public float newTime; // if multiplier is 0 and this >0, time is replaced with this value
        public string hasCardBag;
        public Type cardBagProduces;

        public static bool AnyCardInCardBagIsOfType(CardBag cardBag, Type type)
        {
            List<string> cards = cardBag.GetCardsInBag(I.WM.GameDataLoader);
            foreach (string card in cards)
            {
                if (type.IsAssignableFrom(I.WM.GameDataLoader.GetCardFromId(card).GetType())) return true;
            }
            return false;
        }

        public override bool Equals(System.Object other)
        {
            if (other == null || other is not GameCardTimerModifier) return false;
            return actionId == ((GameCardTimerModifier)other).actionId && 
                   ((GameCardTimerModifier)other).myCardDataType.IsAssignableFrom(myCardDataType);
        }

        public override int GetHashCode()
        {
            return actionId.GetHashCode();
        }
        public void AddToList()
        {
            GameCardStartTimer_Patch.modifiers.Remove(this);
            bool ret = GameCardStartTimer_Patch.modifiers.Add(this);
            I.Log($"GameCardTimerModifier Adding {actionId} {myCardDataType} {ret}");
        }

        public bool CheckCardBag(GameCard card)
        {
            Prior prior = priorResults.FirstOrDefault(x => x.Id == card.CardData.Id && x.type == cardBagProduces);
            if (prior != null)
            {
//                I.Log($"CheckCardBag {prior.Id} {prior.type} prior result {prior.result}");
                return prior.result;
            }

            FieldInfo fi = myCardDataType.GetField(hasCardBag);
            I.Log($"{fi}");
            CardBag cardBag = (CardBag)fi?.GetValue(card.CardData);
            if (cardBag == null) return false;
            I.Log($"Calling AllCardsInCardBagAreOfType for type {cardBagProduces.Name} on card {card.CardData.Id}");
            bool retval = AnyCardInCardBagIsOfType(cardBag, cardBagProduces);
            prior = new Prior() { Id = card.CardData.Id, type = cardBagProduces, result = retval };
            priorResults.Add(prior);
            I.Log(prior.ToString());
            return retval;
        }

        protected class Prior
        {
            public string Id;
            public Type type;
            public bool result;

            public override bool Equals(System.Object other) { return Id == ((Prior)other).Id && type == ((Prior)other).type; }
            public override int GetHashCode() { return Id.GetHashCode() ^ type.ToString().GetHashCode(); }

            public override string ToString() { return $"Prior({Id},{type},{result})"; }
        }

        private readonly static HashSet<Prior> priorResults = new();
    }

    [HarmonyPatch(typeof(GameCard))]
    public class GameCardStartTimer_Patch
    {
        public static HashSet<GameCardTimerModifier> modifiers = new();
//        public static HashSet<string> tagged = new();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameCard.StartTimer))]
        static void StartTimerPrefix(GameCard __instance, ref float time, TimerAction a, string status, string actionId, bool withStatusBar = true)
        {
            GameCardTimerModifier m = modifiers.FirstOrDefault(x => x.actionId == actionId && x.myCardDataType.IsAssignableFrom(__instance.CardData.GetType()));
            if (m != null)
            {
                if (String.IsNullOrEmpty(m.hasCardBag) || m.CheckCardBag(__instance))
                {
//                    I.Log($"ActionId {actionId} time on entry {time} {m.multiplier} {m.newTime} {__instance.CardData.Id}");
                    if (m.multiplier > 0f) { time = time * m.multiplier; }
                    else if (m.newTime > 0f) { time = m.newTime; }
//                    I.Log($"ActionId {actionId} time on exit {time}");
                }
            }
        }

//        [HarmonyPrefix]
//        [HarmonyPatch(nameof(GameCard.CancelTimer))]
        static void CancelTimerPrefix(GameCard __instance)
        {
//            tagged.Remove(__instance.CardData.UniqueId);
        }
    }

    public class BlueprintTimerModifier
    {
        public string blueprintId;
        public int subprintindex; // use -1 for all subprints in the blueprint
        public float multiplier; // if >0 time is multiplied by this amount
        public float newTime; // if multiplier is 0 and this >0, time is replaced with this value

        public override bool Equals(System.Object other)
        {
            if (other == null || other is not BlueprintTimerModifier) return false;
            return blueprintId == ((BlueprintTimerModifier)other).blueprintId && subprintindex == ((BlueprintTimerModifier)other).subprintindex;
        }

        public override int GetHashCode()
        {
            return blueprintId.GetHashCode() ^ subprintindex.GetHashCode(); 
        }

        public void AddToList()
        {
            BlueprintStartTimer_Patch.modifiers.Remove(this);
            BlueprintStartTimer_Patch.modifiers.Add(this);
        }
    }

    [HarmonyPatch(typeof(GameCard), nameof(GameCard.StartBlueprintTimer))]
    public class BlueprintStartTimer_Patch
    {
        public static HashSet<BlueprintTimerModifier> modifiers = new();

        static void Prefix(GameCard __instance, ref float time, TimerAction a, string status, string actionId, string blueprintId, int subprintIndex)
        {
            I.Log($"Blueprint {blueprintId} index {subprintIndex} actionId {actionId}");
            BlueprintTimerModifier m = modifiers.FirstOrDefault(x => x.blueprintId == blueprintId && (x.subprintindex == subprintIndex || x.subprintindex == -1));
            if (m != null)
            {
                I.Log($"Blueprint {blueprintId} index {subprintIndex} time on entry {time} {m.multiplier} {m.newTime}");
                if (m.multiplier > 0f) { time = time * m.multiplier; }
                else if (m.newTime > 0f) { time = m.newTime; }
                I.Log($"Blueprint {blueprintId} index {subprintIndex} time on exit {time}");
            }
        }
    }
}
