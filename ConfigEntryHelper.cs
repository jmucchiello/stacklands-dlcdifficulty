using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CommonModNS;

namespace CommonModNS
{
    public enum TextAlign { Left, Center, Right }

    public abstract class ConfigEntryHelper : ConfigEntryBase
    {
        public virtual void SetDefaults() { }

        public static string AlignText(TextAlign align, string txt)
        {
            return $"<align={align.ToString().ToLower()}>{txt}</align>";
        }

        public static string CenterAlign(string txt)
        {
            return AlignText(TextAlign.Center, txt);
        }

        public static string RightAlign(string txt)
        {
            return AlignText(TextAlign.Right, txt);
        }

        public static string ColorText(string color, string txt)
        {
            return $"<color={color}>" + txt + "</color>";
        }

        public static string ColorText(Color color, string txt)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>" + txt + "</color>";
        }

        public static string SizeText(int pixels, string txt)
        {
            if (pixels <= 0) return txt;
            return $"<size={pixels}>" + txt + "</size>";
        }

        public CustomButton DefaultButton(RectTransform parent, string text, string tooltip = null)
        {
            CustomButton btn = UnityEngine.Object.Instantiate(I.PFM.ButtonPrefab);
            btn.transform.SetParent(parent);
            btn.transform.localPosition = Vector3.zero;
            btn.transform.localScale = Vector3.one;
            btn.transform.localRotation = Quaternion.identity;
            btn.TextMeshPro.text = text;
            btn.TooltipText = tooltip;
            return btn;
        }

        public T LoadConfigEntry<T>(string key, T defValue)
        {
            if (Config.Data.TryGetValue(key, out JToken value))
            {
                return value.Value<T>();
            }
            return defValue;
        }
    }

    public class ConfigEmtySpace : ConfigEntryBase
    {
        private RectTransform spacer1, spacer2;
        public override object BoxedValue { get => new object(); set => _ = value; }

        public ConfigEmtySpace(ConfigFile Config)
        {
            Name = "none";
            ValueType = typeof(object);
            Config.Entries.Add(this);
            UI = new ConfigUI()
            {
                Hidden = true,
                OnUI = delegate {
                    spacer1 = UnityEngine.Object.Instantiate(I.MOS.SpacerPrefab, I.MOS.ButtonsParent);
                    spacer2 = UnityEngine.Object.Instantiate(I.MOS.SpacerPrefab, I.MOS.ButtonsParent);
                }
            };
        }

    }
}
