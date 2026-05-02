using RoR2.Projectile;
using UnityEngine;
using RoR2;
using UnityEngine.Events;
using System.Reflection;

namespace SeamstressVariant.Characters.Survivors.SeamstressVariant.Components
{
    public class ProjectileImpactVFXSFX : MonoBehaviour, IProjectileImpactBehavior
    {
        public GameObject impactEffect;
        public GameObject explosionEffect;
        public string impactSoundString;
        public bool logOnly;
        private bool _didDump;

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            string impactPoint = impactInfo.estimatedPointOfImpact.ToString();
            Log.Info($"[SFX DEBUG] Projectile impact on '{gameObject.name}' at {impactPoint}");

            ProjectileImpactExplosion pie = GetComponent<ProjectileImpactExplosion>();
            if (pie && pie.impactEffect)
            {
                EffectComponent fx = pie.impactEffect.GetComponent<EffectComponent>();
                string fxSound = fx ? fx.soundName : "<no EffectComponent>";
                Log.Info($"[SFX DEBUG] PIE impactEffect='{pie.impactEffect.name}' soundName='{fxSound}'");
            }

            if (!_didDump)
            {
                _didDump = true;
                DumpProjectileAudioHints();
            }

            if (logOnly)
            {
                return;
            }

            // Play SFX
            if (!string.IsNullOrEmpty(impactSoundString))
            {
                Util.PlaySound(impactSoundString, gameObject);
                Log.Info($"[SFX DEBUG] Played impact sound '{impactSoundString}' at {transform.position}");
            }

            // Spawn impact VFX
            if (impactEffect)
            {
                EffectManager.SpawnEffect(impactEffect, new EffectData
                {
                    origin = impactInfo.estimatedPointOfImpact,
                    rotation = Quaternion.LookRotation(impactInfo.estimatedImpactNormal),
                }, true);
            }

            // Spawn explosion VFX
            if (explosionEffect)
            {
                EffectManager.SpawnEffect(explosionEffect, new EffectData
                {
                    origin = impactInfo.estimatedPointOfImpact,
                    scale = 1f,
                }, true);
            }
        }

        private void DumpProjectileAudioHints()
        {
            Component[] components = GetComponents<Component>();
            Log.Info($"[SFX DEBUG] Component dump for '{gameObject.name}' ({components.Length} components)");

            foreach (Component component in components)
            {
                if (!component)
                {
                    continue;
                }

                System.Type type = component.GetType();

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(component);
                    if (value == null)
                    {
                        continue;
                    }

                    string fieldName = field.Name.ToLowerInvariant();
                    bool looksAudio = fieldName.Contains("sound") || fieldName.Contains("event") || fieldName.Contains("ak");
                    if (!looksAudio)
                    {
                        continue;
                    }

                    string text = value.ToString();
                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    Log.Info($"[SFX DEBUG] {type.Name}.{field.Name} = '{text}'");
                }

                FieldInfo stickEventField = type.GetField("stickEvent", BindingFlags.Public | BindingFlags.Instance);
                if (stickEventField != null)
                {
                    UnityEvent stickEvent = stickEventField.GetValue(component) as UnityEvent;
                    if (stickEvent != null)
                    {
                        int count = stickEvent.GetPersistentEventCount();
                        Log.Info($"[SFX DEBUG] {type.Name}.stickEvent listeners={count}");
                        for (int i = 0; i < count; i++)
                        {
                            Object target = stickEvent.GetPersistentTarget(i);
                            string method = stickEvent.GetPersistentMethodName(i);
                            string targetName = target ? target.name : "<null>";
                            string targetType = target ? target.GetType().Name : "<null>";
                            Log.Info($"[SFX DEBUG] {type.Name}.stickEvent[{i}] target='{targetName}' type='{targetType}' method='{method}'");
                        }
                    }
                }
            }
        }
    }
}
