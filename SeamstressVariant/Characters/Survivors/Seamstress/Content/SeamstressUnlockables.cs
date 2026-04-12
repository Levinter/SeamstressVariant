using SeamstressVariant.Survivors.Seamstress.Achievements;
using RoR2;
using UnityEngine;

namespace SeamstressVariant.Survivors.Seamstress
{
    public static class SeamstressUnlockables
    {
        public static UnlockableDef characterUnlockableDef = null;
        public static UnlockableDef masterySkinUnlockableDef = null;

        public static void Init()
        {
            masterySkinUnlockableDef = Modules.Content.CreateAndAddUnlockbleDef(
                SeamstressMasteryAchievement.unlockableIdentifier,
                Modules.Tokens.GetAchievementNameToken(SeamstressMasteryAchievement.identifier),
                SeamstressSurvivor.instance.assetBundle.LoadAsset<Sprite>("texMasteryAchievement"));
        }
    }
}
