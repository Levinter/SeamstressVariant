using System;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantStaticValues
    {
        public const float clawDamageCoefficient = 1f;
        public const float meleeScissorDamageCoefficient = 1.5f;

        //the scissor damage is always a crit because of the superbleedoncrit damage type on the prefab, so the value is technically double
        public const float scissorExplosionDamageCoefficient = 2.0f;
        public const float explodeRadius = 20f;
    }
}