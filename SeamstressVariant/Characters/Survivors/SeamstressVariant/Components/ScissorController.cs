using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Owns the survivor's independent scissor state.
    /// Extends NetworkBehaviour so scissor presence is synced to all clients via SyncVar.
    /// The server manages buff add/remove and updates the SyncVars; hooks on clients directly
    /// toggle model visibility without polling.
    /// FireScissors notifies this controller when a blade is launched; remote clients request
    /// consumption through a Command so server state remains authoritative.
    /// When stock changes on an authority client, it requests server reconciliation so
    /// multiplayer clients can still restore scissors after recharge.
    /// </summary>
    public class ScissorController : NetworkBehaviour
    {
        private CharacterBody characterBody;

        // Cached references to the scissor model child GameObjects for visibility toggling.
        private GameObject scissorLModel;
        private GameObject scissorRModel;

        // SyncVars replicate scissor state to all clients. Hooks update model visibility directly,
        // so no per-frame polling is needed on clients.
        [SyncVar(hook = nameof(OnHasLeftScissorChanged))]
        private bool hasLeftScissor;

        [SyncVar(hook = nameof(OnHasRightScissorChanged))]
        private bool hasRightScissor;

        //Used in claw attacks to determine whether to apply scissor damage bonuses.
        public bool HasLeftScissor => hasLeftScissor;
        public bool HasRightScissor => hasRightScissor;

        // Server-side stock snapshot used for lightweight reconciliation.
        private int _lastVisualStock = -1;

        private void Awake()
        {
            characterBody = GetComponent<CharacterBody>();
        }

        private void Start()
        {
            TryCacheScissorModels();

            if (NetworkServer.active)
            {
                SetLeftScissor(true);
                SetRightScissor(true);
                Log.Info("ScissorController: initialized both scissors on server.");
            }

            ApplyVisualState();
        }

        private void FixedUpdate()
        {
            if (characterBody == null)
            {
                return;
            }

            var secondary = characterBody.skillLocator?.secondary;
            if (secondary == null)
            {
                return;
            }

            // Only the base 2 stocks map to scissor visuals.
            int visualStock = Mathf.Clamp(secondary.stock, 0, 2);
            if (visualStock == _lastVisualStock)
            {
                return;
            }

            if (NetworkServer.active)
            {
                ReconcileScissorsFromStock(visualStock);
            }
            else if (Util.HasEffectiveAuthority(gameObject))
            {
                CmdRequestScissorReconcile(visualStock);
            }

            ApplyVisualState();
            _lastVisualStock = visualStock;
        }

        private void ReconcileScissorsFromStock(int visualStock)
        {
            bool wantLeft = visualStock >= 2;
            bool wantRight = visualStock >= 1;

            if (hasLeftScissor != wantLeft)
            {
                SetLeftScissor(wantLeft);
            }

            if (hasRightScissor != wantRight)
            {
                SetRightScissor(wantRight);
            }

            Log.Info($"ScissorController: reconciled from stock={visualStock} left={hasLeftScissor} right={hasRightScissor}");
        }

        private void TryCacheScissorModels()
        {
            if (scissorLModel != null && scissorRModel != null) return;

            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
            {
                return;
            }

            ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
            if (!childLocator)
            {
                return;
            }

            if (scissorLModel == null)
            {
                Transform scissorL = childLocator.FindChild("ScissorLModel");
                if (scissorL)
                {
                    scissorLModel = scissorL.gameObject;
                    Log.Info($"ScissorController: cached left scissor model '{scissorLModel.name}'.");
                }
            }

            if (scissorRModel == null)
            {
                Transform scissorR = childLocator.FindChild("ScissorRModel");
                if (scissorR)
                {
                    scissorRModel = scissorR.gameObject;
                    Log.Info($"ScissorController: cached right scissor model '{scissorRModel.name}'.");
                }
            }
        }

        private void ApplyVisualState()
        {   
            Log.Warning("ApplyVisualState: Applying visual state");
            SetScissorVisual(scissorLModel, hasLeftScissor);
            SetScissorVisual(scissorRModel, hasRightScissor);
        }

        private static void SetScissorVisual(GameObject model, bool active)
        {
            Log.Warning("Setting scissor visual: " + (model != null ? model.name : "null") + " active=" + active);

            if (model == null)
            {
                return;
            }

            if (model.activeSelf != active)
            {
                Log.Warning("Activating model: " + model.name);
                model.SetActive(active);
                Log.Warning("Model active state is now: " + model.activeSelf);
            }
        }

        // SyncVar hooks — called on clients whenever the server changes the value.
        private void OnHasLeftScissorChanged(bool newValue)
        {
            hasLeftScissor = newValue;
            TryCacheScissorModels();
            SetScissorVisual(scissorLModel, newValue);
            Log.Debug($"ScissorController: left sync -> {newValue}");
        }

        private void OnHasRightScissorChanged(bool newValue)
        {
            hasRightScissor = newValue;
            TryCacheScissorModels();
            SetScissorVisual(scissorRModel, newValue);
            Log.Debug($"ScissorController: right sync -> {newValue}");
        }

        /// <summary>
        /// Called by FireScissors when a blade is launched.
        /// Server mutates state directly; remote authority clients send a consume request.
        /// </summary>
        public void OnScissorFired(bool isLeft)
        {
            NotifyScissorFired(isLeft);
        }

        public void NotifyScissorFired(bool isLeft)
        {
            if (NetworkServer.active)
            {
                ApplyScissorFiredServer(isLeft);
                return;
            }
            CmdRequestScissorFired(isLeft);

            Log.Warning($"ScissorController: ignored consume request without authority side={(isLeft ? "L" : "R")}");
        }

        [Command]
        private void CmdRequestScissorReconcile(int visualStock)
        {
            ReconcileScissorsFromStock(Mathf.Clamp(visualStock, 0, 2));
        }

        [Command]
        private void CmdRequestScissorFired(bool isLeft)
        {
            ApplyScissorFiredServer(isLeft);
        }

        private void ApplyScissorFiredServer(bool isLeft)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            bool alreadyMissing = isLeft ? !hasLeftScissor : !hasRightScissor;
            if (alreadyMissing)
            {
                Log.Warning($"ScissorController: consume ignored, side already missing side={(isLeft ? "L" : "R")}");
                return;
            }

            if (isLeft)
            {
                SetLeftScissor(false);
            }
            else
            {
                SetRightScissor(false);
            }

            Log.Info($"ScissorController: consumed side={(isLeft ? "L" : "R")} left={hasLeftScissor} right={hasRightScissor}");
        }

        public void SetLeftScissor(bool active)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("ScissorController: SetLeftScissor ignored on non-server instance.");
                return;
            }

            if (characterBody == null)
            {
                Log.Error("ScissorController: missing CharacterBody while setting left scissor.");
                return;
            }

            if (SeamstressVariantBuffs.scissorLeftBuff == null)
            {
                Log.Error("ScissorController: left buff definition is null.");
                return;
            }

            bool previous = hasLeftScissor;

            hasLeftScissor = active;
            // SyncVar hooks are not invoked on the server when the server sets the value — call directly.
            OnHasLeftScissorChanged(active);

            bool hasBuff = characterBody.HasBuff(SeamstressVariantBuffs.scissorLeftBuff);
            if (active)
            {
                if (!hasBuff) characterBody.AddBuff(SeamstressVariantBuffs.scissorLeftBuff);
            }
            else
            {
                if (hasBuff) characterBody.RemoveBuff(SeamstressVariantBuffs.scissorLeftBuff);
            }

            if (previous != active)
            {
                Log.Info($"ScissorController: left state {previous} -> {active}");
            }
        }

        public void SetRightScissor(bool active)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("ScissorController: SetRightScissor ignored on non-server instance.");
                return;
            }

            if (characterBody == null)
            {
                Log.Error("ScissorController: missing CharacterBody while setting right scissor.");
                return;
            }

            if (SeamstressVariantBuffs.scissorRightBuff == null)
            {
                Log.Error("ScissorController: right buff definition is null.");
                return;
            }

            bool previous = hasRightScissor;

            hasRightScissor = active;
            // SyncVar hooks are not invoked on the server when the server sets the value — call directly.
            OnHasRightScissorChanged(active);

            bool hasBuff = characterBody.HasBuff(SeamstressVariantBuffs.scissorRightBuff);
            if (active)
            {
                if (!hasBuff) characterBody.AddBuff(SeamstressVariantBuffs.scissorRightBuff);
            }
            else
            {
                if (hasBuff) characterBody.RemoveBuff(SeamstressVariantBuffs.scissorRightBuff);
            }

            if (previous != active)
            {
                Log.Info($"ScissorController: right state {previous} -> {active}");
            }
        }
    }
}
