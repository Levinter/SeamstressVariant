using RoR2;
using RoR2.Skills;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public class BlinkSkillDef : SkillDef
    {
        public override bool CanExecute(GenericSkill skillSlot)
        {
            if (!base.CanExecute(skillSlot))
                return false;

            CharacterBody body = skillSlot.characterBody;
            if (body == null)
                return true;

            float cost = SeamstressVariantConfig.GetBlinkHealthCostForLevel(body.level);
            if (cost <= 0f)
                return true;

            HealthComponent hc = body.healthComponent;
            if (hc == null)
                return true;

            float availableHealth = Mathf.Max(hc.health - 1f, 0f);
            BleedingHeartComponent heart = body.GetComponent<BleedingHeartComponent>();
            float heartAmount = heart != null ? heart.GetHeart() : 0f;

            return availableHealth + heartAmount >= cost;
        }
    }
}
