using RoR2;
using UnityEngine;

namespace SeamstressVariant.Survivors.Seamstress
{
    public static class SeamstressBuffs
    {
        // armor buff gained during roll
        public static BuffDef armorBuff;
        
        // heart visualization buff
        public static BuffDef heartBuff;

        // bleed aura count visualization buff
        public static BuffDef bleedAuraBuff;

        public static void Init(AssetBundle assetBundle)
        {
            armorBuff = Modules.Content.CreateAndAddBuff("HenryArmorBuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.white,
                false,
                false);

            // Create buff for visualizing bleed aura count
            bleedAuraBuff = Modules.Content.CreateAndAddBuff("SeamstressBleedAura",
                assetBundle.LoadAsset<Sprite>("texPassiveIcon"), // Use the passive icon or create an aura-specific one
                new Color(0.9f, 0.1f, 0.1f), // Dark red color for bleed aura
                true, // canStack = true so we can show the bleed count
                false); // not a debuff

        }
    }
}
