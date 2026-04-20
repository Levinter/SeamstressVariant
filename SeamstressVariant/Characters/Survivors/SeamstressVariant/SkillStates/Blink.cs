using EntityStates;
using KinematicCharacterController;
using RoR2;
using SeamstressMod.Seamstress.Content;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class Blink : BaseSkillState
    {
        protected Transform modelTransform;

        public static string beginSoundString = "Play_imp_attack_blink";
        public string animationLayer = "FullBody, Override";
        public string animString = "Dash";

        public float duration = 0.1f;

        protected CameraTargetParams.AimRequest request;
        protected Vector3 blinkVector;
        protected float speedCoefficient;

        protected CharacterModel characterModel;
        protected HurtBoxGroup hurtboxGroup;
        protected Animator animator;

        protected GameObject dashPrefab;
        protected GameObject blinkPrefab;
        protected Material destealthMaterial;

        public override void OnEnter()
        {
            base.OnEnter();

            duration = SeamstressVariantConfig.utilityBlinkDuration.Value;

            // Keep OG seamstress blink VFX when available.
            dashPrefab = SeamstressAssets.impDashEffect;
            blinkPrefab = SeamstressAssets.smallBlinkEffect;
            destealthMaterial = SeamstressAssets.destealthMaterial;

            Util.PlaySound(beginSoundString, gameObject);

            modelTransform = GetModelTransform();
            if (modelTransform)
            {
                characterModel = modelTransform.GetComponent<CharacterModel>();
                hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
                animator = modelTransform.GetComponent<Animator>();
            }

            if (characterModel)
            {
                characterModel.invisibilityCount++;
            }

            if (hurtboxGroup)
            {
                hurtboxGroup.hurtBoxesDeactivatorCounter++;
            }

            if (cameraTargetParams)
            {
                request = cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
            }

            blinkVector = GetBlinkVector();
            if (characterDirection)
            {
                characterDirection.moveVector = blinkVector;
            }

            CreateBlinkEffect(characterBody.corePosition, true);

            int itemCount = characterBody.inventory ? characterBody.inventory.GetItemCountEffective(RoR2Content.Items.JumpBoost) : 0;
            float sprintBonus = 1f;
            if (itemCount > 0 && characterBody.isSprinting)
            {
                float accelControl = characterBody.acceleration * characterMotor.airControl;
                if (characterBody.moveSpeed > 0f && accelControl > 0f)
                {
                    float stride = Mathf.Sqrt(10f * itemCount / accelControl);
                    float speedRatio = characterBody.moveSpeed / accelControl;
                    sprintBonus = (stride + speedRatio) / speedRatio;
                }
            }

            speedCoefficient = 0.3f * characterBody.jumpPower * Mathf.Clamp(characterBody.moveSpeed * sprintBonus / 4f, 5f, 20f);

            gameObject.layer = LayerIndex.fakeActor.intVal;
            if (characterMotor)
            {
                ((BaseCharacterController)characterMotor).Motor.RebuildCollidableLayers();
            }
        }

        protected virtual Vector3 GetBlinkVector()
        {
            if (!inputBank)
            {
                return characterDirection ? characterDirection.forward : transform.forward;
            }

            Vector3 aimDirection = inputBank.aimDirection;
            aimDirection.y = 0f;

            if (aimDirection.sqrMagnitude <= 0f)
            {
                aimDirection = characterDirection ? characterDirection.forward : transform.forward;
                aimDirection.y = 0f;
            }

            Vector3 axis = -Vector3.Cross(Vector3.up, aimDirection);
            float angle = Vector3.Angle(inputBank.aimDirection, aimDirection);
            if (inputBank.aimDirection.y < 0f)
            {
                angle = -angle;
            }

            Vector3 moveVector = inputBank.moveVector;
            if (moveVector.sqrMagnitude <= 0f)
            {
                moveVector = characterDirection ? characterDirection.forward : transform.forward;
            }

            return Vector3.Normalize(Quaternion.AngleAxis(angle, axis) * moveVector);
        }

        protected void CreateBlinkEffect(Vector3 origin, bool first)
        {
            if (blinkPrefab)
            {
                EffectData effectData = new EffectData
                {
                    rotation = Util.QuaternionSafeLookRotation(blinkVector),
                    origin = origin,
                    scale = 0.15f,
                };

                EffectManager.SpawnEffect(blinkPrefab, effectData, true);

                effectData.scale = 3f;
                if (!first && dashPrefab)
                {
                    EffectManager.SpawnEffect(dashPrefab, effectData, true);
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority && characterMotor && characterDirection)
            {
                ((BaseCharacterController)characterMotor).Motor.ForceUnground(0.1f);
                characterMotor.velocity = blinkVector * speedCoefficient;
            }

            if (isAuthority && fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            gameObject.layer = LayerIndex.defaultLayer.intVal;
            if (characterMotor)
            {
                ((BaseCharacterController)characterMotor).Motor.RebuildCollidableLayers();
            }

            if (!outer.destroying)
            {
                CreateBlinkEffect(characterBody.corePosition, false);

                modelTransform = GetModelTransform();
                if (modelTransform && destealthMaterial && animator)
                {
                    TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(gameObject);
                    temporaryOverlayInstance.duration = 1f;
                    temporaryOverlayInstance.destroyComponentOnEnd = true;
                    temporaryOverlayInstance.originalMaterial = destealthMaterial;
                    temporaryOverlayInstance.inspectorCharacterModel = animator.gameObject.GetComponent<CharacterModel>();
                    temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlayInstance.animateShaderAlpha = true;
                }

                if (characterModel)
                {
                    characterModel.invisibilityCount--;
                }

                if (hurtboxGroup)
                {
                    hurtboxGroup.hurtBoxesDeactivatorCounter--;
                }

                if (cameraTargetParams)
                {
                    request.Dispose();
                }
            }

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(blinkVector);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            blinkVector = reader.ReadVector3();
        }
    }
}
