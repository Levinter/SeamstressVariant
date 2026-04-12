using RoR2;
using SeamstressVariant.Modules.Achievements;

namespace SeamstressVariant.Survivors.Seamstress.Achievements
{
    //automatically creates language tokens "ACHIEVMENT_{identifier.ToUpper()}_NAME" and "ACHIEVMENT_{identifier.ToUpper()}_DESCRIPTION" 
    [RegisterAchievement(identifier, unlockableIdentifier, null, 10, null)]
    public class SeamstressMasteryAchievement : BaseMasteryAchievement
    {
        public const string identifier = SeamstressSurvivor.HENRY_PREFIX + "masteryAchievement";
        public const string unlockableIdentifier = SeamstressSurvivor.HENRY_PREFIX + "masteryUnlockable";

        public override string RequiredCharacterBody => SeamstressSurvivor.instance.bodyName;

        //difficulty coeff 3 is monsoon. 3.5 is typhoon for grandmastery skins
        public override float RequiredDifficultyCoefficient => 3;
    }
}