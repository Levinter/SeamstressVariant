using SeamstressVariant.Survivors.SeamstressVariant.Achievements;
using RoR2;
using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantUnlockables
    {
        public static UnlockableDef characterUnlockableDef = null;
        public static UnlockableDef masterySkinUnlockableDef = null;

        public static void Init()
        {
            masterySkinUnlockableDef = Modules.Content.CreateAndAddUnlockbleDef(
                SeamstressVariantMasteryAchievement.unlockableIdentifier,
                Modules.Tokens.GetAchievementNameToken(SeamstressVariantMasteryAchievement.identifier),
                SeamstressVariantSurvivor.instance.assetBundle.LoadAsset<Sprite>("texMasteryAchievement"));
        }
    }
}
