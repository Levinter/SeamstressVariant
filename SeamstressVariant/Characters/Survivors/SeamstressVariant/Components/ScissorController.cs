using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Owns the survivor's independent scissor state.
    /// Fires are tracked via OnScissorFired(bool isLeft). FixedUpdate watches the secondary skill
    /// stock on the server; each time a stock restores, it re-enables the opposite-of-last-fired
    /// scissor first (mirroring the OG Seamstress pattern).
    /// </summary>
    public class ScissorController : MonoBehaviour
    {
        private CharacterBody characterBody;

        // Cached references to the scissor model child GameObjects for visibility toggling.
        private GameObject _scissorLModel;
        private GameObject _scissorRModel;
        private bool? _prevHasLeftScissor;
        private bool? _prevHasRightScissor;

        public bool HasLeftScissor =>
            characterBody != null
            && SeamstressVariantBuffs.scissorLeftBuff != null
            && characterBody.HasBuff(SeamstressVariantBuffs.scissorLeftBuff);

        public bool HasRightScissor =>
            characterBody != null
            && SeamstressVariantBuffs.scissorRightBuff != null
            && characterBody.HasBuff(SeamstressVariantBuffs.scissorRightBuff);

        // True when the last fired scissor was the left one.
        // Used by FixedUpdate to decide which buff to restore first.
        public bool lastFiredLeft = true;

        private int _lastKnownSecondaryStock = -1;

        private void Awake()
        {
            characterBody = GetComponent<CharacterBody>();
        }

        private void Start()
        {
            // Cache scissor model child GameObjects from the ChildLocator.
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (modelLocator && modelLocator.modelTransform)
            {
                ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
                if (childLocator)
                {
                    Transform scissorL = childLocator.FindChild("ScissorLModel");
                    if (scissorL) _scissorLModel = scissorL.gameObject;

                    Transform scissorR = childLocator.FindChild("ScissorRModel");
                    if (scissorR) _scissorRModel = scissorR.gameObject;
                }
            }

            if (NetworkServer.active)
            {
                SetLeftScissor(true);
                SetRightScissor(true);
            }

            UpdateScissorModelVisibility();
        }

        private void FixedUpdate()
        {
            if (characterBody == null) return;

            UpdateScissorModelVisibility();

            if (!NetworkServer.active) return;

            GenericSkill secondary = characterBody.skillLocator?.secondary;
            if (secondary == null) return;

            int stock = secondary.stock;

            // First frame with a valid skill — just capture the baseline stock.
            if (_lastKnownSecondaryStock < 0)
            {
                _lastKnownSecondaryStock = stock;
                return;
            }

            if (stock > _lastKnownSecondaryStock)
            {
                if (stock >= 2)
                {
                    // Both stocks restored — bring both scissors back.
                    SetLeftScissor(true);
                    SetRightScissor(true);
                }
                else
                {
                    // One stock restored — restore the scissor that was NOT the last one fired.
                    if (lastFiredLeft)
                        SetRightScissor(true);
                    else
                        SetLeftScissor(true);
                }
            }

            _lastKnownSecondaryStock = stock;
        }

        private void UpdateScissorModelVisibility()
        {
            bool hasLeft = HasLeftScissor;
            bool hasRight = HasRightScissor;

            if (_scissorLModel && (_prevHasLeftScissor == null || _prevHasLeftScissor.Value != hasLeft))
            {
                _scissorLModel.SetActive(hasLeft);
            }

            if (_scissorRModel && (_prevHasRightScissor == null || _prevHasRightScissor.Value != hasRight))
            {
                _scissorRModel.SetActive(hasRight);
            }

            _prevHasLeftScissor = hasLeft;
            _prevHasRightScissor = hasRight;
        }

        /// <summary>
        /// Called by FireScissors when a blade is launched. Records which side was fired for
        /// restoration ordering, and only removes a scissor if we're in the final two stocks.
        /// This keeps both scissors available while surplus stocks (3+) are being spent.
        /// </summary>
        public void OnScissorFired(bool isLeft)
        {
            lastFiredLeft = isLeft;

            GenericSkill secondary = characterBody?.skillLocator?.secondary;
            if (secondary != null && secondary.stock >= 2)
                return;

            if (isLeft)
                SetLeftScissor(false);
            else
                SetRightScissor(false);
        }

        public void SetLeftScissor(bool active)
        {
            if (!NetworkServer.active || characterBody == null || SeamstressVariantBuffs.scissorLeftBuff == null)
                return;

            bool hasBuff = characterBody.HasBuff(SeamstressVariantBuffs.scissorLeftBuff);
            if (active)
            {
                if (!hasBuff) characterBody.AddBuff(SeamstressVariantBuffs.scissorLeftBuff);
            }
            else
            {
                if (hasBuff) characterBody.RemoveBuff(SeamstressVariantBuffs.scissorLeftBuff);
            }
        }

        public void SetRightScissor(bool active)
        {
            if (!NetworkServer.active || characterBody == null || SeamstressVariantBuffs.scissorRightBuff == null)
                return;

            bool hasBuff = characterBody.HasBuff(SeamstressVariantBuffs.scissorRightBuff);
            if (active)
            {
                if (!hasBuff) characterBody.AddBuff(SeamstressVariantBuffs.scissorRightBuff);
            }
            else
            {
                if (hasBuff) characterBody.RemoveBuff(SeamstressVariantBuffs.scissorRightBuff);
            }
        }
    }
}
