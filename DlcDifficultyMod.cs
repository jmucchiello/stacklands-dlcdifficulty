using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace DlcDifficultyModNS
{
    public partial class DlcDifficultyMod : Mod
    {
        public static DlcDifficultyMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);
        private void Awake()
        {
            instance = this;
            SetupConfig();
            //Harmony.PatchAll();
        }

        private void SetupConfig()
        {
            Mod xpileMod = ModManager.LoadedMods.Find(x => x.name == "spawn_control_mod") ?? this;
            SetSpecial_SadDivisor = xpileMod.GetType().GetMethod("SetSadnessDivisor");
            if (xpileMod == this)
            {
                //Harmony.PatchAll(typeof());
            }


        }

        public void ApplyConfig()
        {

        }

        public override void Ready()
        {
            ApplyConfig();
            Logger.Log("Ready!");
        }
    }
}