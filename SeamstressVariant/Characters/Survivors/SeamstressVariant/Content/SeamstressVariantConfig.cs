using BepInEx.Configuration;
using SeamstressVariant.Modules;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantConfig
    {
        public static ConfigEntry<float> utilityBlinkDuration;

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
        }
    }
}
