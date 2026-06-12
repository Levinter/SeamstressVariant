using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using SeamstressMod.Seamstress.Content;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Redirects incoming healing into Heart that can be used as a resource for skills.
    /// The Heart builds up from healing received and can be consumed by abilities.
    /// </summary>
    internal class BleedingHeartComponent : NetworkBehaviour
    {
        private HealthComponent healthComponent;
        private CharacterBody body;
        private Material destealthMaterial;
        private GameObject trailEffectR;
        private GameObject trailEffectL;
        private bool sustainedVisualActive;
        private TemporaryOverlayInstance persistentDefianceOverlay;
        private EffectManagerHelper defianceBleedEffect;

        // Heart settings
        [SyncVar(hook = nameof(OnMaxHeartChanged))]
        private float MaxHeart = 110f;
        [SyncVar(hook = nameof(OnCurrentHeartChanged))]
        private float currentHeart = 0f;
        [SyncVar(hook = nameof(OnDefianceVisualsActiveChanged))]
        private bool defianceVisualsActive;
        [SyncVar(hook = nameof(OnDefiantStartupFreezeActiveChanged))]
        private bool defiantStartupFreezeActive;
        private int activeBleedStacks = 0;
        private int nearbyEnemyCount = 0;
        private const float NearbyEnemyRadius = 30f;
        private float scanTimer = 0f;
        private float healTimer = 0f;
        private const float ScanInterval = 1f;
        private const float HealInterval = 0.20f;
        private const float HealPerBleedStack = 1f;
        private const int HeartPerBleedChancePercent = 50;
        private bool startupMoveLockApplied;
        private bool cachedDisableAirControlUntilCollision;
        private bool cachedDisableAirControlUntilCollisionValid;
        private bool startupAntiGravityApplied;
        private bool startupFlightApplied;

        private bool isInitialized = false;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            body = GetComponent<CharacterBody>();
            destealthMaterial = SeamstressAssets.destealthMaterial;

            if (healthComponent != null)
            {
                if (NetworkServer.active)
                {
                    MaxHeart = healthComponent.fullHealth;
                }
                isInitialized = true;
            }
        }

        private void Update()
        {
            if (defiantStartupFreezeActive)
            {
                ApplyDefiantStartupFreezeLocal();
            }

            if (!NetworkServer.active || body == null || healthComponent == null || !healthComponent.alive)
            {
                return;
            }

            scanTimer -= Time.deltaTime;
            healTimer -= Time.deltaTime;

            if (scanTimer <= 0f)
            {
                scanTimer = ScanInterval;
                ScanNearbyEnemies();
                //Log.Debug("NEARBY ENEMIES = " + nearbyEnemyCount + " | BLEED STACKS = " + activeBleedStacks);
            }

            if (healTimer <= 0f)
            {
                healTimer = HealInterval;
                ApplyPassiveHeal();
            }
        }

        private void OnEnable()
        {
            if (body != null)
            {
                body.onRecalculateStats += OnBodyRecalculateStates;
            }
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.onRecalculateStats -= OnBodyRecalculateStates;
            }

            RemoveDefiantStartupFreezeLocal();
            RemoveDefianceVisualsLocal();
        }

        // Update maxHeart when maxHealth increased
        private void OnBodyRecalculateStates(CharacterBody body)
        {
            if (!NetworkServer.active) return;
            MaxHeart = body.maxHealth;
            //Log.Debug("MaxHeart = " + MaxHeart);
        }

        // Add Health to heart from healing
        public void AddToHeart(float amount)
        {
            if (isInitialized && healthComponent != null && healthComponent.alive)
            {
                currentHeart = Mathf.Min(currentHeart + amount, MaxHeart);
                //Log.Debug("AMOUNT IN HEART = " + currentHeart);
                body.MarkAllStatsDirty();
            }
        }

        // return the current amount of health on heart
        public float GetHeart()
        {
            return currentHeart;
        }

        public float ConsumeHeart(float amount)
        {
            if (!isInitialized || amount <= 0f)
            {
                return 0f;
            }

            float previousHeart = currentHeart;
            currentHeart = Mathf.Max(0f, currentHeart - amount);
            float consumed = previousHeart - currentHeart;

            if (consumed > 0f)
            {
                //Log.Debug("CONSUMED HEART = " + consumed + " | REMAINING = " + currentHeart);
                body.MarkAllStatsDirty();
            }

            return consumed;
        }

        public bool CanSustainDefiantHeart()
        {
            return currentHeart > 1f;
        }

        public float GetMaxHeart()
        {
            return MaxHeart;
        }

        public int GetBleedChanceBonusFromHeart()
        {
            return (int)(currentHeart / HeartPerBleedChancePercent);
        }

        public bool IsHeartFull()
        {
            return currentHeart >= MaxHeart;
        }

        public int GetActiveBleedStacks()
        {
            return activeBleedStacks;
        }

        public void OnCurrentHeartChanged(float newValue)
        {
            currentHeart = newValue;
            if(body)
            {
                body.MarkAllStatsDirty();
            }
        }

        public void OnMaxHeartChanged(float newValue)
        {
            MaxHeart = newValue;
        }

        private void OnDefianceVisualsActiveChanged(bool newValue)
        {
            defianceVisualsActive = newValue;

            if (newValue)
            {
                ApplyDefianceVisualsLocal();
            }
            else
            {
                RemoveDefianceVisualsLocal();
            }
        }

        private void OnDefiantStartupFreezeActiveChanged(bool newValue)
        {
            defiantStartupFreezeActive = newValue;

            if (newValue)
            {
                ApplyDefiantStartupFreezeLocal();
            }
            else
            {
                RemoveDefiantStartupFreezeLocal();
            }
        }

        public void RequestSetDefianceVisualsActive(bool active)
        {
            if (NetworkServer.active)
            {
                SetDefianceVisualsActiveServer(active);
                return;
            }

            CmdRequestSetDefianceVisualsActive(active);
        }

        [Command]
        private void CmdRequestSetDefianceVisualsActive(bool active)
        {
            SetDefianceVisualsActiveServer(active);
        }

        private void SetDefianceVisualsActiveServer(bool active)
        {
            if (!NetworkServer.active || defianceVisualsActive == active)
            {
                return;
            }

            defianceVisualsActive = active;
            OnDefianceVisualsActiveChanged(active);
        }

        public void RequestSetDefiantStartupFreezeActive(bool active)
        {
            if (NetworkServer.active)
            {
                SetDefiantStartupFreezeActiveServer(active);
                return;
            }

            CmdRequestSetDefiantStartupFreezeActive(active);
        }

        [Command]
        private void CmdRequestSetDefiantStartupFreezeActive(bool active)
        {
            SetDefiantStartupFreezeActiveServer(active);
        }

        private void SetDefiantStartupFreezeActiveServer(bool active)
        {
            if (!NetworkServer.active || defiantStartupFreezeActive == active)
            {
                return;
            }

            defiantStartupFreezeActive = active;
            OnDefiantStartupFreezeActiveChanged(active);
        }
        
        public int GetNearbyEnemyCount()
        {
            return nearbyEnemyCount;
        }

        private void ScanNearbyEnemies()
        {
            if (body == null)
            {
                return;
            }

            Vector3 center = body.footPosition;
            float radiusSqr = NearbyEnemyRadius * NearbyEnemyRadius;
            int enemyCount = 0;
            int bleedCount = 0;

            foreach (CharacterBody otherBody in CharacterBody.readOnlyInstancesList)
            {
                /*if (!CountsAsNearbyCharacter(otherBody))
                {
                    continue;
                }*/

                //if (otherBody.teamComponent.teamIndex != TeamIndex.Player)
                //{
                    Vector3 delta = otherBody.footPosition - center;
                    if (delta.sqrMagnitude <= radiusSqr)
                    {
                        enemyCount++;
                        bleedCount += otherBody.GetBuffCount(RoR2Content.Buffs.Bleeding);
                        bleedCount += otherBody.GetBuffCount(RoR2Content.Buffs.SuperBleed);
                    }
                //}
            }

            nearbyEnemyCount = enemyCount;
            activeBleedStacks = bleedCount;
        }

        private void ApplyPassiveHeal()
        {
            if (activeBleedStacks <= 0 || healthComponent == null || !healthComponent.alive)
            {
                return;
            }

            float healAmount = HealPerBleedStack * activeBleedStacks;
            healthComponent.Heal(healAmount, default);
            //Log.Debug("Passive heal: " + healAmount + " (" + activeBleedStacks + " stacks)");
        }

        private bool CountsAsNearbyCharacter(CharacterBody otherBody)
        {
            if (otherBody == null || otherBody == body || otherBody.healthComponent == null || !otherBody.healthComponent.alive)
            {
                return false;
            }

            return true;
        }

        private void ApplyDefianceBleedEffect()
        {
            if (defianceBleedEffect)
            {
                return;
            }

            GameObject bleedEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/BleedEffect");
            if (!bleedEffectPrefab)
            {
                return;
            }

            defianceBleedEffect = EffectManager.GetAndActivatePooledEffect(bleedEffectPrefab, transform, true);
        }

        private void RemoveDefianceBleedEffect()
        {
            if (!defianceBleedEffect)
            {
                return;
            }

            if (defianceBleedEffect.OwningPool != null)
            {
                defianceBleedEffect.transform.SetParent(null);
                defianceBleedEffect.ReturnToPool();
            }
            else
            {
                Object.Destroy(defianceBleedEffect.gameObject);
            }

            defianceBleedEffect = null;
        }

        private Transform FindModelChild(string childName)
        {
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
            {
                return null;
            }

            ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
            if (!childLocator)
            {
                return null;
            }

            return childLocator.FindChild(childName);
        }

        private Animator GetModelAnimator()
        {
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
            {
                return null;
            }

            return modelLocator.modelTransform.GetComponent<Animator>();
        }

        private Transform GetModelTransform()
        {
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            return modelLocator ? modelLocator.modelTransform : null;
        }

        private void ApplyDefianceVisualsLocal()
        {
            if (sustainedVisualActive)
            {
                return;
            }

            sustainedVisualActive = true;
            //ApplyDefianceBleedEffect();

            Animator anim = GetModelAnimator();
            if (anim && destealthMaterial && persistentDefianceOverlay == null)
            {
                persistentDefianceOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                persistentDefianceOverlay.duration = 9999f;
                persistentDefianceOverlay.destroyComponentOnEnd = true;
                persistentDefianceOverlay.originalMaterial = destealthMaterial;
                persistentDefianceOverlay.inspectorCharacterModel = anim.gameObject.GetComponent<CharacterModel>();
                persistentDefianceOverlay.alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
                persistentDefianceOverlay.animateShaderAlpha = true;
            }

            if (SeamstressAssets.trailEffectHands)
            {
                Transform handR = FindModelChild("HandR");
                Transform handL = FindModelChild("HandL");
                if (handR)
                {
                    trailEffectR = Object.Instantiate(SeamstressAssets.trailEffectHands, handR);
                }
                if (handL)
                {
                    trailEffectL = Object.Instantiate(SeamstressAssets.trailEffectHands, handL);
                }
            }
        }

        private void RemoveDefianceVisualsLocal()
        {
            if (!sustainedVisualActive)
            {
                return;
            }

            sustainedVisualActive = false;
            //RemoveDefianceBleedEffect();

            if (persistentDefianceOverlay != null)
            {
                persistentDefianceOverlay.Destroy();
                persistentDefianceOverlay = null;
            }

            if (trailEffectR)
            {
                Object.Destroy(trailEffectR);
                trailEffectR = null;
            }

            if (trailEffectL)
            {
                Object.Destroy(trailEffectL);
                trailEffectL = null;
            }

            Transform modelTransform = GetModelTransform();
            if (modelTransform && destealthMaterial)
            {
                TemporaryOverlayInstance exitOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                exitOverlay.duration = 1f;
                exitOverlay.destroyComponentOnEnd = true;
                exitOverlay.originalMaterial = destealthMaterial;
                exitOverlay.inspectorCharacterModel = modelTransform.GetComponent<CharacterModel>();
                exitOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                exitOverlay.animateShaderAlpha = true;
            }

            Util.PlaySound("Play_voidman_transform_return", gameObject);
        }

        private void ApplyDefiantStartupFreezeLocal()
        {
            CharacterMotor characterMotor = body ? body.characterMotor : null;
            if (characterMotor)
            {
                if (!startupMoveLockApplied)
                {
                    cachedDisableAirControlUntilCollision = characterMotor.disableAirControlUntilCollision;
                    cachedDisableAirControlUntilCollisionValid = true;
                    characterMotor.disableAirControlUntilCollision = true;
                    startupMoveLockApplied = true;
                }

                if (!startupAntiGravityApplied)
                {
                    CharacterGravityParameters gravityParameters = characterMotor.gravityParameters;
                    gravityParameters.channeledAntiGravityGranterCount++;
                    characterMotor.gravityParameters = gravityParameters;
                    startupAntiGravityApplied = true;
                }

                if (!startupFlightApplied)
                {
                    CharacterFlightParameters flightParameters = characterMotor.flightParameters;
                    flightParameters.channeledFlightGranterCount++;
                    characterMotor.flightParameters = flightParameters;
                    startupFlightApplied = true;
                }

                characterMotor.velocity = Vector3.zero;
            }

            if (body && body.characterDirection)
            {
                body.characterDirection.moveVector = Vector3.zero;
            }

            InputBankTest inputBank = GetComponent<InputBankTest>();
            if (inputBank)
            {
                inputBank.moveVector = Vector3.zero;
            }
        }

        private void RemoveDefiantStartupFreezeLocal()
        {
            CharacterMotor characterMotor = body ? body.characterMotor : null;
            if (!characterMotor)
            {
                startupMoveLockApplied = false;
                cachedDisableAirControlUntilCollisionValid = false;
                startupAntiGravityApplied = false;
                startupFlightApplied = false;
                return;
            }

            if (startupMoveLockApplied && cachedDisableAirControlUntilCollisionValid)
            {
                characterMotor.disableAirControlUntilCollision = cachedDisableAirControlUntilCollision;
            }

            startupMoveLockApplied = false;
            cachedDisableAirControlUntilCollisionValid = false;

            if (startupFlightApplied)
            {
                CharacterFlightParameters flightParameters = characterMotor.flightParameters;
                flightParameters.channeledFlightGranterCount--;
                characterMotor.flightParameters = flightParameters;
                startupFlightApplied = false;
            }

            if (startupAntiGravityApplied)
            {
                CharacterGravityParameters gravityParameters = characterMotor.gravityParameters;
                gravityParameters.channeledAntiGravityGranterCount--;
                characterMotor.gravityParameters = gravityParameters;
                startupAntiGravityApplied = false;
            }
        }
    }
}
