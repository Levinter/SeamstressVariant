using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    public class SeamstressTrackingSkillDef : SkillDef
    {
        protected class InstanceData : BaseSkillInstanceData
        {
            public SeamstressTracker tracker;
        }

        public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
        {
            return new InstanceData
            {
                tracker = skillSlot.GetComponent<SeamstressTracker>()
            };
        }

        private static bool HasTarget(GenericSkill skillSlot)
        {
            InstanceData instanceData = skillSlot.skillInstanceData as InstanceData;
            return instanceData != null && instanceData.tracker != null && instanceData.tracker.GetTrackingTarget() != null;
        }

        public override bool CanExecute(GenericSkill skillSlot)
        {
            return HasTarget(skillSlot) && base.CanExecute(skillSlot);
        }

        public override bool IsReady(GenericSkill skillSlot)
        {
            return base.IsReady(skillSlot) && HasTarget(skillSlot);
        }
    }
}