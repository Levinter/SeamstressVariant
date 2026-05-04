using RoR2;
using SeamstressMod.Seamstress.Content;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Continuously searches for the best enemy target in front of the character and displays
    /// the Huntress tracking indicator over it. Mirrors the pattern used by HuntressTracker.
    ///
    /// FireScissors reads GetTrackingTarget() on cast instead of running its own BullseyeSearch,
    /// so the locked-on enemy is already visible to the player before they press the skill.
    /// </summary>
    public class SeamstressTracker : MonoBehaviour
    {
        // Search parameters — match FireScissors static fields so the cone is identical.
        public float maxTrackingDistance = 60f;
        public float maxTrackingAngle    = 30f;

        // How many times per second the search is refreshed.
        public float trackerUpdateFrequency = 10f;

        private CharacterBody _body;
        private TeamComponent _teamComponent;
        private InputBankTest _inputBank;

        private HurtBox _trackingTarget;
        private Indicator _indicator;

        private float _trackerUpdateStopwatch;
        private readonly BullseyeSearch _search = new BullseyeSearch();

        // Prefab loaded once and shared across all instances.
        private static GameObject _trackingPrefab;

        private void Awake()
        {
            _body          = GetComponent<CharacterBody>();
            _teamComponent = GetComponent<TeamComponent>();
            _inputBank     = GetComponent<InputBankTest>();

            // Use OG Seamstress normal tracker visuals.
            if (_trackingPrefab == null)
            {
                _trackingPrefab = SeamstressAssets.telekinesisTracker;
                if (_trackingPrefab == null)
                {
                    _trackingPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/HuntressTrackingIndicator");
                    Log.Warning("SeamstressTracker: SeamstressAssets.telekinesisTracker was null, falling back to HuntressTrackingIndicator.");
                }
            }

            _indicator = new Indicator(gameObject, _trackingPrefab);
        }

        private void OnEnable()
        {
            _indicator.active = true;
        }

        private void OnDisable()
        {
            _indicator.active = false;
        }

        private void FixedUpdate()
        {
            _trackerUpdateStopwatch += Time.fixedDeltaTime;
            if (_trackerUpdateStopwatch >= 1f / trackerUpdateFrequency)
            {
                _trackerUpdateStopwatch -= 1f / trackerUpdateFrequency;
                SearchForTarget();
            }

            // Keep the indicator pointing at the current target every frame regardless of
            // search frequency so it moves smoothly when the enemy moves.
            _indicator.targetTransform = _trackingTarget != null ? _trackingTarget.transform : null;
        }

        private void SearchForTarget()
        {
            // Only the local player benefits from seeing the indicator; skip on simulated clients.
            // Server/authority still needs to track so FireScissors can read the target.
            Ray aimRay = GetAimRay();

            _search.searchOrigin      = aimRay.origin;
            _search.searchDirection   = aimRay.direction;
            _search.maxDistanceFilter = maxTrackingDistance;
            _search.maxAngleFilter    = maxTrackingAngle;
            _search.teamMaskFilter    = TeamMask.allButNeutral;
            _search.teamMaskFilter.RemoveTeam(TeamComponent.GetObjectTeam(gameObject));
            _search.filterByLoS = true;
            _search.sortMode    = BullseyeSearch.SortMode.DistanceAndAngle;
            _search.RefreshCandidates();

            System.Collections.Generic.IEnumerable<HurtBox> results = _search.GetResults();
            HurtBox newTarget = null;
            foreach (HurtBox hb in results)
            {
                newTarget = hb;
                break;
            }
            _trackingTarget = newTarget;
        }

        private Ray GetAimRay()
        {
            if (_inputBank)
                return new Ray(_inputBank.aimOrigin, _inputBank.aimDirection);
            return new Ray(transform.position, transform.forward);
        }

        /// <summary>Returns the currently tracked hurtbox, or null if none.</summary>
        public HurtBox GetTrackingTarget() => _trackingTarget;
    }
}
