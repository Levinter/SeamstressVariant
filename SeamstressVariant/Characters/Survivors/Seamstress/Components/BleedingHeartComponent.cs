using UnityEngine;
using RoR2;

namespace SeamstressVariant.Survivors.Seamstress.Components
{
    /// <summary>
    /// Redirects incoming healing into Heart that can be used as a resource for skills.
    /// The Heart builds up from healing received and can be consumed by abilities.
    /// </summary>
    internal class BleedingHeartComponent : MonoBehaviour
    {
        private HealthComponent healthComponent;
        private CharacterBody body;

        // Heart settings
        private float MaxHeart = 110f;
        public float currentHeart = 0f;

        private bool isInitialized = false;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            body = GetComponent<CharacterBody>();
            
            if (healthComponent != null)
            {
                MaxHeart = healthComponent.fullHealth;
                isInitialized = true;
            }
        }

        private void OnEnable()
        {
            body.onRecalculateStats += onBodyRecalculateStates;
            DotController.onDotInflictedServerGlobal += onDotControllerInflicted;
        }

        private void OnDisable()
        {
            body.onRecalculateStats -= onBodyRecalculateStates;
            DotController.onDotInflictedServerGlobal -= onDotControllerInflicted;
        }


        private void onDotControllerInflicted(DotController dotController, ref InflictDotInfo dotInfo)
        {
        }

        // Update maxHeart when maxHealth increased
        private void onBodyRecalculateStates(CharacterBody body)
        {
            MaxHeart = body.maxHealth;
            Log.Debug("MaxHeart = " + MaxHeart);
        }

        // Add Health to heart from healing
        public void AddToHeart(float amount)
        {
            if (isInitialized)
            {
                currentHeart = Mathf.Min(currentHeart + amount, MaxHeart);
                Log.Debug("AMOUNT IN HEART = " + currentHeart);
                body.MarkAllStatsDirty();
            }
        }

        // return the current amount of health on heart
        public float GetHeart()
        {
            return currentHeart;
        }
    }
}
