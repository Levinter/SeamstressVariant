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

        // bleed stack counter buff to visualize active enemy bleeds
        public static BuffDef bleedStackCounterBuff;

        // active special buff: while present, damage cannot reduce health below 1
        public static BuffDef defianceBuff;

        public static void Init(AssetBundle assetBundle)
        {
            armorBuff = Modules.Content.CreateAndAddBuff("HenryArmorBuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.white,
                false,
                false);

            // Create buff for visualizing active enemy bleed stacks
            bleedStackCounterBuff = Modules.Content.CreateAndAddBuff("SeamstressBleedStackCounter",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/Bleeding").iconSprite, // Use bleed icon
                new Color(1f, 0.2f, 0.2f), // Bright red for stack visualization
                true, // canStack = true to show count
                false); // not a debuff

            defianceBuff = Modules.Content.CreateAndAddBuff("SeamstressDefiance",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                new Color(0.9f, 0.1f, 0.1f),
                false,
                false);
        }
    }
}
