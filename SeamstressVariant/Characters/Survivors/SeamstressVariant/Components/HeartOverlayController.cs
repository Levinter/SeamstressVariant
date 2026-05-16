using System.Collections.Generic;
using System.Diagnostics;
using RoR2;
using RoR2.HudOverlay;
using RoR2.UI;
using SeamstressMod.Seamstress.Content;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    internal class HeartOverlayController : NetworkBehaviour
    {
        private sealed class OverlayThemeCache
        {
            public GameObject instance;
            public Image[] filledImages;
            public TextMeshProUGUI[] texts;
            public ChildLocator childLocator;
            public Animator animator;
        }

        private BleedingHeartComponent heartComponent;
        private OverlayController overlayController;
        private readonly List<ImageFillController> fillUiList = new List<ImageFillController>();
        private readonly List<TextMeshProUGUI> textList = new List<TextMeshProUGUI>();
        private readonly List<OverlayThemeCache> overlayThemeCaches = new List<OverlayThemeCache>();

        private static GameObject overlayPrefab;
        private static string overlayChildLocatorEntry;
        private static bool overlayAssetsLoaded = false;
        private static bool EnableThemePerfLogging = false;
        private const float ThemePerfLogInterval = 10f;
        private static readonly int overlayValueParamHash = Animator.StringToHash("corruption");
        private static readonly int overlayDrainStateParamHash = Animator.StringToHash("isCorrupted");
        private static readonly Color overlayWineColor = new Color32(196, 66, 82, 255);
        private static readonly Color overlayDrainColor = Color.red;

        private int themeApplyCallCount;
        private float themeApplyTotalMs;
        private float themeApplyMaxMs;
        private float nextThemePerfLogTime;
        [SyncVar(hook = nameof(OnHeartDrainActiveChanged))]
        private bool heartDrainActive;

        private void Awake()
        {
            heartComponent = GetComponent<BleedingHeartComponent>();
        }

        private void Start()
        {
            EnsureOverlayAssetsLoaded();
            RegisterOverlay();

            nextThemePerfLogTime = Time.unscaledTime + ThemePerfLogInterval;
        }

        private void OnHeartDrainActiveChanged(bool newValue)
        {
            heartDrainActive = newValue;
        }

        internal void SetHeartDrainActive(bool active)
        {
            if (heartDrainActive == active)
            {
                Log.Warning("Heart drain active state is already " + active);
                return;
            }

            heartDrainActive = active;
            Log.Warning("Setting heart drain active state to " + active);
            ApplyThemeToOverlayInstances();
            ApplyOverlayStateToTrackedInstances();
        }

        private static void EnsureOverlayAssetsLoaded()
        {
            if (overlayAssetsLoaded) return;
            GameObject voidBody = Addressables
                .LoadAssetAsync<GameObject>("RoR2/DLC1/VoidSurvivor/VoidSurvivorBody.prefab")
                .WaitForCompletion();
            if (voidBody != null)
            {
                var voidController = voidBody.GetComponent<VoidSurvivorController>();
                if (voidController != null)
                {
                    overlayPrefab = CreateOverlayClone(voidController.overlayPrefab);
                    overlayChildLocatorEntry = voidController.overlayChildLocatorEntry;
                    overlayAssetsLoaded = overlayPrefab != null;
                }
            }
        }

        private static GameObject CreateOverlayClone(GameObject sourceOverlayPrefab)
        {
            if (sourceOverlayPrefab == null)
            {
                return null;
            }

            GameObject overlayClone = Object.Instantiate(sourceOverlayPrefab);
            overlayClone.name = "SeamstressHeartOverlayPrefab";
            overlayClone.SetActive(false);
            Object.DontDestroyOnLoad(overlayClone);

            ApplyOverlayTheme(overlayClone);
            return overlayClone;
        }

        private static void ApplyOverlayTheme(GameObject overlayRoot)
        {
            foreach (Image image in overlayRoot.GetComponentsInChildren<Image>(true))
            {
                if (image.type == Image.Type.Filled)
                {
                    Color fillColor = image.color;
                    image.color = new Color(overlayWineColor.r, overlayWineColor.g, overlayWineColor.b, fillColor.a);
                }
            }

            foreach (TextMeshProUGUI valueText in overlayRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (valueText != null)
                {
                    Color textColor = valueText.color;
                    valueText.color = new Color(overlayWineColor.r, overlayWineColor.g, overlayWineColor.b, textColor.a);
                }
            }
        }

        private void RegisterOverlay()
        {
            if (overlayPrefab == null) return;
            OverlayCreationParams overlayCreationParams = new OverlayCreationParams
            {
                prefab = overlayPrefab,
                childLocatorEntry = overlayChildLocatorEntry
            };
            overlayController = HudOverlayManager.AddOverlay(gameObject, overlayCreationParams);
            overlayController.onInstanceAdded += OnOverlayInstanceAdded;
            overlayController.onInstanceRemove += OnOverlayInstanceRemoved;

            // Overlay instances can exist before callbacks are attached; backfill tracked state.
            foreach (GameObject existingInstance in overlayController.instancesList)
            {
                OnOverlayInstanceAdded(overlayController, existingInstance);
            }
        }

        private void OnDisable()
        {
            if (overlayController != null)
            {
                overlayController.onInstanceAdded -= OnOverlayInstanceAdded;
                overlayController.onInstanceRemove -= OnOverlayInstanceRemoved;
                fillUiList.Clear();
                textList.Clear();
                overlayThemeCaches.Clear();
                HudOverlayManager.RemoveOverlay(overlayController);
                overlayController = null;
            }

            heartDrainActive = false;
        }

        private void Update()
        {
            if (heartComponent == null) return;

            EnsureTrackedOverlayInstances();

            float max = heartComponent.GetMaxHeart();
            float current = heartComponent.GetHeart();
            float fraction = max > 0f ? current / max : 0f;
            foreach (ImageFillController fillUi in fillUiList)
            {
                fillUi.SetTValue(fraction);
            }
            int displayValue = Mathf.FloorToInt(current);
            foreach (TextMeshProUGUI text in textList)
            {
                text.SetText(displayValue.ToString());
            }

            ApplyOverlayStateToTrackedInstances();
        }

        private void LateUpdate()
        {
            if (heartComponent == null) return;

            if (EnableThemePerfLogging)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                ApplyThemeToOverlayInstances();
                stopwatch.Stop();
                RecordThemePerfSample((float)stopwatch.Elapsed.TotalMilliseconds);
            }
            else
            {
                ApplyThemeToOverlayInstances();
            }

            if (EnableThemePerfLogging && Time.unscaledTime >= nextThemePerfLogTime)
            {
                FlushThemePerfLog();
                nextThemePerfLogTime = Time.unscaledTime + ThemePerfLogInterval;
            }
        }

        private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
        {
            var fill = instance.GetComponent<ImageFillController>();
            if (fill != null && !fillUiList.Contains(fill))
            {
                fillUiList.Add(fill);
            }
            var text = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null && !textList.Contains(text))
            {
                textList.Add(text);
            }

            if (FindThemeCacheIndex(instance) == -1)
            {
                Image[] allImages = instance.GetComponentsInChildren<Image>(true);
                List<Image> filledImages = new List<Image>();
                foreach (Image image in allImages)
                {
                    if (image != null && image.type == Image.Type.Filled)
                    {
                        filledImages.Add(image);
                    }
                }

                overlayThemeCaches.Add(new OverlayThemeCache
                {
                    instance = instance,
                    filledImages = filledImages.ToArray(),
                    texts = instance.GetComponentsInChildren<TextMeshProUGUI>(true),
                    childLocator = instance.GetComponent<ChildLocator>(),
                    animator = instance.GetComponent<Animator>()
                });
            }

            ApplyThemeToOverlayInstance(instance);
            ApplyOverlayStateToTrackedInstances();
        }

        private void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
        {
            var fill = instance.GetComponent<ImageFillController>();
            if (fill != null)
            {
                fillUiList.Remove(fill);
            }
            var text = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                textList.Remove(text);
            }

            int cacheIndex = FindThemeCacheIndex(instance);
            if (cacheIndex >= 0)
            {
                overlayThemeCaches.RemoveAt(cacheIndex);
            }
        }

        private void ApplyThemeToOverlayInstances()
        {
            Color targetColor = heartDrainActive ? overlayDrainColor : overlayWineColor;

            for (int i = overlayThemeCaches.Count - 1; i >= 0; i--)
            {
                OverlayThemeCache cache = overlayThemeCaches[i];
                if (cache.instance == null)
                {
                    overlayThemeCaches.RemoveAt(i);
                    continue;
                }

                ApplyThemeToOverlayCache(cache, targetColor, targetColor);
            }
        }

        private void EnsureTrackedOverlayInstances()
        {
            if (overlayController == null)
            {
                return;
            }

            foreach (GameObject overlayInstance in overlayController.instancesList)
            {
                if (overlayInstance != null && FindThemeCacheIndex(overlayInstance) == -1)
                {
                    OnOverlayInstanceAdded(overlayController, overlayInstance);
                }
            }
        }

        private int FindThemeCacheIndex(GameObject overlayInstance)
        {
            for (int i = 0; i < overlayThemeCaches.Count; i++)
            {
                if (overlayThemeCaches[i].instance == overlayInstance)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ApplyThemeToOverlayInstance(GameObject instance)
        {
            Image[] allImages = instance.GetComponentsInChildren<Image>(true);
            List<Image> filledImages = new List<Image>();
            foreach (Image image in allImages)
            {
                if (image != null && image.type == Image.Type.Filled)
                {
                    filledImages.Add(image);
                }
            }

            OverlayThemeCache tempCache = new OverlayThemeCache
            {
                instance = instance,
                filledImages = filledImages.ToArray(),
                texts = instance.GetComponentsInChildren<TextMeshProUGUI>(true)
            };

            Color targetColor = heartDrainActive ? overlayDrainColor : overlayWineColor;
            ApplyThemeToOverlayCache(tempCache, targetColor, targetColor);
        }

        private static void ApplyThemeToOverlayCache(OverlayThemeCache cache, Color fillTargetColor, Color textTargetColor)
        {
            foreach (Image image in cache.filledImages)
            {
                if (image == null) continue;

                Color fillColor = image.color;
                Color themedFillColor = new Color(fillTargetColor.r, fillTargetColor.g, fillTargetColor.b, fillColor.a);
                if (fillColor != themedFillColor)
                {
                    image.color = themedFillColor;
                }

                image.canvasRenderer.SetColor(themedFillColor);
            }

            foreach (TextMeshProUGUI valueText in cache.texts)
            {
                if (valueText == null) continue;

                Color textColor = valueText.color;
                Color themedTextColor = new Color(textTargetColor.r, textTargetColor.g, textTargetColor.b, textColor.a);
                if (textColor != themedTextColor)
                {
                    valueText.color = themedTextColor;
                }

                Color32 faceColor = valueText.faceColor;
                Color32 themedFaceColor = new Color32(
                    (byte)Mathf.RoundToInt(textTargetColor.r * 255f),
                    (byte)Mathf.RoundToInt(textTargetColor.g * 255f),
                    (byte)Mathf.RoundToInt(textTargetColor.b * 255f),
                    faceColor.a);
                if (!faceColor.Equals(themedFaceColor))
                {
                    valueText.faceColor = themedFaceColor;
                }

                valueText.canvasRenderer.SetColor(themedTextColor);
            }
        }

        private void ApplyOverlayStateToTrackedInstances()
        {
            float maxHeart = heartComponent != null ? heartComponent.GetMaxHeart() : 0f;
            float currentHeart = heartComponent != null ? heartComponent.GetHeart() : 0f;
            float normalizedHeart = maxHeart > 0f ? currentHeart / maxHeart : 0f;

            for (int i = overlayThemeCaches.Count - 1; i >= 0; i--)
            {
                OverlayThemeCache cache = overlayThemeCaches[i];
                if (cache.instance == null)
                {
                    overlayThemeCaches.RemoveAt(i);
                    continue;
                }

                ApplyOverlayState(cache, currentHeart, normalizedHeart);
            }
        }

        private void ApplyOverlayState(OverlayThemeCache cache, float currentHeart, float normalizedHeart)
        {
            if (cache.childLocator != null)
            {
                Transform heartThreshold = cache.childLocator.FindChild("CorruptionThreshold");
                if (heartThreshold != null)
                {
                    heartThreshold.rotation = Quaternion.Euler(0f, 0f, normalizedHeart * -360f);
                }

                Transform minimumThreshold = cache.childLocator.FindChild("MinCorruptionThreshold");
                if (minimumThreshold != null)
                {
                    minimumThreshold.rotation = Quaternion.identity;
                }
            }

            if (cache.animator != null)
            {
                cache.animator.SetFloat(overlayValueParamHash, currentHeart);
                cache.animator.SetBool(overlayDrainStateParamHash, heartDrainActive);
            }
        }

        private void RecordThemePerfSample(float elapsedMs)
        {
            themeApplyCallCount++;
            themeApplyTotalMs += elapsedMs;
            if (elapsedMs > themeApplyMaxMs)
            {
                themeApplyMaxMs = elapsedMs;
            }
        }

        private void FlushThemePerfLog()
        {
            if (themeApplyCallCount <= 0)
            {
                Log.Info("HeartOverlay theme perf: no apply calls in sample window.");
                return;
            }

            float avgMs = themeApplyTotalMs / themeApplyCallCount;
            Log.Info($"HeartOverlay theme perf: calls={themeApplyCallCount}, avgMs={avgMs:F4}, maxMs={themeApplyMaxMs:F4}, totalMs={themeApplyTotalMs:F4}, caches={overlayThemeCaches.Count}");

            themeApplyCallCount = 0;
            themeApplyTotalMs = 0f;
            themeApplyMaxMs = 0f;
        }
    }
}
