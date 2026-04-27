using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantBuffs
    {
        // heart visualization buff
        public static BuffDef heartBuff;

        // bleed stack counter buff to visualize active enemy bleeds
        public static BuffDef bleedStackCounterBuff;

        // active special buff: while present, damage cannot reduce health below 1
        public static BuffDef defianceBuff;

        // scissor buffs for tracking left and right scissors
        public static BuffDef scissorLeftBuff;
        public static BuffDef scissorRightBuff;

        public static void Init(AssetBundle assetBundle)
        {

            // Create buff for visualizing active enemy bleed stacks
            bleedStackCounterBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantBleedStackCounter",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/Bleeding").iconSprite, // Use bleed icon
                new Color(1f, 0.2f, 0.2f), // Bright red for stack visualization
                true, // canStack = true to show count
                false); // not a debuff

            defianceBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantDefiance",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                new Color(0.9f, 0.1f, 0.1f),
                false,
                false);

            // Create scissor buffs
            scissorLeftBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantScissorLeft",
                Addressables.LoadAssetAsync<Sprite>((object)"RoR2/DLC1/VoidSurvivor/texBuffVoidSurvivorCorruptionIcon.tif").WaitForCompletion(),
                new Color(31f / 51f, 11f / 51f, 11f / 51f),
                false, // canStack = false
                false); // not a debuff

            scissorRightBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantScissorRight",
                Addressables.LoadAssetAsync<Sprite>((object)"RoR2/DLC1/VoidSurvivor/texBuffVoidSurvivorCorruptionIcon.tif").WaitForCompletion(),
                new Color(31f / 51f, 11f / 51f, 11f / 51f),
                false, // canStack = false
                false); // not a debuff
        }
    }
}
