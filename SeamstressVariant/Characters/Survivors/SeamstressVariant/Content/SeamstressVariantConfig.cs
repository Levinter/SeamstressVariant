using BepInEx.Configuration;
using SeamstressVariant.Modules;
using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantConfig
    {
        // Blink movement time in seconds.
        public static ConfigEntry<float> utilityBlinkDuration;
        // Flat health cost paid on every blink.
        public static ConfigEntry<float> utilityBlinkHealthCost;
        // Additional health cost applied per level (level 1 uses 1x).
        public static ConfigEntry<float> utilityBlinkHealthCostPerLevel;

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
                25f,
                0f,
                25f,
                "Health drained each time Blink is used. Non-lethal (will not reduce below 1 HP).");

            utilityBlinkHealthCostPerLevel = Config.BindAndOptions(
                section,
                "Utility Blink Health Cost Per Level",
                0f,
                0f,
                0f,
                "Additional health drained per level when Blink is used. Starts applying at level 1.");
        }

        public static float GetBlinkHealthCostForLevel(float level)
        {
            float baseCost = Mathf.Max(utilityBlinkHealthCost.Value, 0f);
            //float perLevelCost = Mathf.Max(utilityBlinkHealthCostPerLevel.Value, 0f);
            //float levelMultiplier = Mathf.Max(level, 0f);
    
            // Total blink cost = base + perLevel * level.
            return baseCost; // + perLevelCost * levelMultiplier;
        }
    }
}
