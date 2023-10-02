using UnityEngine;
using UnityEngine.UI;

namespace DlcDifficultyModNS
{
    public class ConfigFreeText : ConfigEntryHelper
    {
        public string Text;
        public override object BoxedValue { get => new object(); set => _ = value; }

        public Action<ConfigEntryBase, CustomButton> Clicked;

        /**
         *  Create a header line in the config screen. Also useful for stuff like "Close" in ModalScreen
         **/
        public ConfigFreeText(string name, ConfigFile config, string text, string tooltip = null)
        {
            Name = name;
            Config = config;
            ValueType = typeof(object);
            UI = new ConfigUI()
            {
                NameTerm = text,
                Hidden = true,
                OnUI = delegate (ConfigEntryBase c)
                {
                    CustomButton btn = UnityEngine.Object.Instantiate(I.PFM.ButtonPrefab, I.MOS.ButtonsParent);
                    btn.transform.localScale = Vector3.one;
                    btn.transform.localPosition = Vector3.zero;
                    btn.transform.localRotation = Quaternion.identity;
                    btn.TextMeshPro.text = RightAlign(I.Xlat(UI.NameTerm));
                    btn.TooltipText = I.Xlat(tooltip);
                    btn.EnableUnderline = false;
                    btn.Clicked += delegate ()
                    {
                        Clicked?.Invoke(this, btn);
                    };
                }
            };
            config.Entries.Add(this);
        }
    }
}
