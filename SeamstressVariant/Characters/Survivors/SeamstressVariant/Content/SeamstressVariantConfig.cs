using BepInEx.Configuration;
using SeamstressVariant.Modules;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantConfig
    {
        public static ConfigEntry<float> utilityBlinkDuration;
        public static ConfigEntry<float> utilityBlinkHealthCost;

        public static void Init()
        {
            string section = "Seamstress";

            utilityBlinkDuration = Config.BindAndOptions(
                section,
                "Utility Blink Duration",
                0.1f,
                0.05f,
                0.5f,
                "Duration of Seamstress Variant blink in seconds.");

            utilityBlinkHealthCost = Config.BindAndOptions(
                section,
                "Utility Blink Health Cost",
                10f,
                0f,
                30f,
                "Health drained each time Blink is used. Non-lethal (will not reduce below 1 HP).");
        }
    }
}
