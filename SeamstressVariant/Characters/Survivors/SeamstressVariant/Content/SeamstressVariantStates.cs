using SeamstressVariant.Survivors.SeamstressVariant.SkillStates;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantStates
    {
        public static void Init()
        {
            Modules.Content.AddEntityState(typeof(SeamstressSpawnState));

            Modules.Content.AddEntityState(typeof(ClawCombo));

            Modules.Content.AddEntityState(typeof(Blink));

            Modules.Content.AddEntityState(typeof(DefiantHeart));

            Modules.Content.AddEntityState(typeof(HealingHeart));

            Modules.Content.AddEntityState(typeof(FireScissors));
        }
    }
}
