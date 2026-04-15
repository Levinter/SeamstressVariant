using System.Collections.Generic;
using RoR2;
using RoR2.HudOverlay;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SeamstressVariant.Survivors.Seamstress.Components
{
    internal class HeartOverlayController : MonoBehaviour
    {
        private BleedingHeartComponent heartComponent;
        private OverlayController overlayController;
        private readonly List<ImageFillController> fillUiList = new List<ImageFillController>();
        private readonly List<TextMeshProUGUI> textList = new List<TextMeshProUGUI>();

        private static GameObject overlayPrefab;
        private static string overlayChildLocatorEntry;
        private static bool overlayAssetsLoaded = false;

        private void Awake()
        {
            heartComponent = GetComponent<BleedingHeartComponent>();
        }

        private void Start()
        {
            EnsureOverlayAssetsLoaded();
            RegisterOverlay();
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
                    overlayPrefab = voidController.overlayPrefab;
                    overlayChildLocatorEntry = voidController.overlayChildLocatorEntry;
                    overlayAssetsLoaded = true;
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
        }

        private void OnDisable()
        {
            if (overlayController != null)
            {
                overlayController.onInstanceAdded -= OnOverlayInstanceAdded;
                overlayController.onInstanceRemove -= OnOverlayInstanceRemoved;
                fillUiList.Clear();
                textList.Clear();
                HudOverlayManager.RemoveOverlay(overlayController);
                overlayController = null;
            }
        }

        private void Update()
        {
            if (heartComponent == null || fillUiList.Count == 0) return;
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
        }

        private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
        {
            var fill = instance.GetComponent<ImageFillController>();
            if (fill != null)
            {
                fillUiList.Add(fill);
            }
            var text = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                textList.Add(text);
            }
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
        }
    }
}
