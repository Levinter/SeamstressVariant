using System;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantStaticValues
    {
        public const float clawDamageCoefficient = 1f;
        public const float meleeScissorDamageCoefficient = 1.5f;

        public const float scissorImpactDamageCoefficient = 1.5f;
        public const float scissorExplosionDamageCoefficient = 3.0f;
        public const float scissorExplosionFromImpactMultiplier = scissorExplosionDamageCoefficient / scissorImpactDamageCoefficient;
        public const float dashDamageCoefficient = 3.0f;
        public const float explodeRadius = 20f;
    }
}