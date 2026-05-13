using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Owns the survivor's independent scissor state.
    /// Extends NetworkBehaviour so scissor presence is synced to all clients via SyncVar.
    /// The server manages buff add/remove and updates the SyncVars; hooks on clients directly
    /// toggle model visibility without polling.
    /// FixedUpdate on the server watches secondary skill stock to handle both removal (when a
    /// client fires) and restoration (when stock recharges).
    /// </summary>
    public class ScissorController : NetworkBehaviour
    {
        private CharacterBody characterBody;

        // Cached references to the scissor model child GameObjects for visibility toggling.
        private GameObject _scissorLModel;
        private GameObject _scissorRModel;

        // SyncVars replicate scissor state to all clients. Hooks update model visibility directly,
        // so no per-frame polling is needed on clients.
        [SyncVar(hook = nameof(OnHasLeftScissorChanged))]
        private bool _hasLeftScissor;

        [SyncVar(hook = nameof(OnHasRightScissorChanged))]
        private bool _hasRightScissor;

        public bool HasLeftScissor => _hasLeftScissor;
        public bool HasRightScissor => _hasRightScissor;

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

            // Apply current SyncVar state to models. This covers late-joining clients where the
            // SyncVar hook may have fired before Start() had a chance to cache the model refs.
            if (_scissorLModel) _scissorLModel.SetActive(_hasLeftScissor);
            if (_scissorRModel) _scissorRModel.SetActive(_hasRightScissor);
        }

        private void FixedUpdate()
        {
            if (characterBody == null) return;
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
                    // One stock restored — restore whichever scissor is currently absent.
                    // Using buff-state rather than lastFiredLeft so this works for remote clients
                    // whose lastFiredLeft is never synced to the server.
                    if (!HasLeftScissor)
                        SetLeftScissor(true);
                    else if (!HasRightScissor)
                        SetRightScissor(true);
                }
            }
            else if (stock < _lastKnownSecondaryStock)
            {
                // Stock decreased — a scissor was fired. Remove the appropriate buff server-side.
                // This is the primary path for clients: OnScissorFired cannot touch buffs on a
                // non-server machine, so the server watches for stock drops and acts here instead
                // (mirroring OG Seamstress's CmdUpdateScissors approach).
                if (stock == 0)
                {
                    SetLeftScissor(false);
                    SetRightScissor(false);
                }
                else
                {
                    // Remove whichever scissor is still present. Left has priority because
                    // FireScissors fires Left when both are available, so Left will still be
                    // present on the first shot (Right on the second shot).
                    if (HasLeftScissor)
                        SetLeftScissor(false);
                    else if (HasRightScissor)
                        SetRightScissor(false);
                }
            }

            _lastKnownSecondaryStock = stock;
        }

        // SyncVar hooks — called on clients whenever the server changes the value.
        private void OnHasLeftScissorChanged(bool newValue)
        {
            if (_scissorLModel) _scissorLModel.SetActive(newValue);
        }

        private void OnHasRightScissorChanged(bool newValue)
        {
            if (_scissorRModel) _scissorRModel.SetActive(newValue);
        }

        /// <summary>
        /// Called by FireScissors on the authority client when a blade is launched.
        /// On the server/host this immediately removes the scissor. For remote clients the
        /// server's FixedUpdate stock-watch handles removal on the next tick.
        /// </summary>
        public void OnScissorFired(bool isLeft)
        {
            if (!NetworkServer.active) return;

            if (isLeft)
                SetLeftScissor(false);
            else
                SetRightScissor(false);
        }

        public void SetLeftScissor(bool active)
        {
            if (!NetworkServer.active || characterBody == null || SeamstressVariantBuffs.scissorLeftBuff == null)
                return;

            _hasLeftScissor = active;

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

            _hasRightScissor = active;

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
