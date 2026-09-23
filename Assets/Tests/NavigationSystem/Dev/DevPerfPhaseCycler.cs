using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    [DefaultExecutionOrder(-200)]
    public class DevPerfPhaseCycler : MonoBehaviour
    {
        public const float DefaultPhaseSeconds = 10f;
        public const float DefaultWarmupSeconds = 5f;
        private const int PhaseAll = 0;
        private const int PhaseNoArrows = 1;
        private const int PhaseNoMovingMarkers = 2;
        private const int PhaseNoPerfMarkers = 3;
        private const int PhaseNoRouteLine = 4;
        private const int PhaseNoNavUi = 5;

        private readonly string[] stepNames =
        {
            "Warmup",
            "Closed_All",
            "Closed_NoArrows",
            "Closed_NoMovingMarkers",
            "Closed_NoPerfMarkers",
            "Closed_NoRouteLine",
            "Open_All",
            "Open_NoArrows",
            "Open_NoMovingMarkers",
            "Open_NoPerfMarkers",
            "Open_NoRouteLine",
            "NoNavUi",
            "Finished"
        };
        private readonly int[] stepPhases =
        {
            PhaseAll,
            PhaseAll,
            PhaseNoArrows,
            PhaseNoMovingMarkers,
            PhaseNoPerfMarkers,
            PhaseNoRouteLine,
            PhaseAll,
            PhaseNoArrows,
            PhaseNoMovingMarkers,
            PhaseNoPerfMarkers,
            PhaseNoRouteLine,
            PhaseNoNavUi,
            PhaseNoNavUi
        };
        private readonly bool[] stepFullMapOpen =
        {
            false,
            false,
            false,
            false,
            false,
            false,
            true,
            true,
            true,
            true,
            true,
            false,
            false
        };

        [SerializeField] private GameObject minimapRoot;
        [SerializeField] private MapViewInteractive fullMap;
        [SerializeField] private GameObject staticMarkersRoot;
        [SerializeField] private GameObject movingMarkersRoot;
        [SerializeField] private float phaseSeconds = DefaultPhaseSeconds;
        [SerializeField] private float warmupSeconds = DefaultWarmupSeconds;
        private float stepElapsed;
        private int stepIndex;
        private bool applied;

        public string CurrentStepName { get { return stepNames[stepIndex]; } }
        public int CurrentStep { get { return stepIndex; } }
        public int StepCount { get { return stepNames.Length; } }
        public bool IsFinished { get { return stepIndex == stepNames.Length - 1; } }

        public void Configure(GameObject minimapRootValue, MapViewInteractive fullMapValue, GameObject staticMarkersValue, GameObject movingMarkersValue)
        {
            minimapRoot = minimapRootValue;
            fullMap = fullMapValue;
            staticMarkersRoot = staticMarkersValue;
            movingMarkersRoot = movingMarkersValue;
        }

        private void OnEnable()
        {
            stepIndex = 0;
            stepElapsed = 0f;
            applied = false;
        }

        private void Update()
        {
            UpdatePhaseCyclerLogic(Time.unscaledDeltaTime);
        }

        public void UpdatePhaseCyclerLogic(float deltaTime)
        {
            if (!applied)
            {
                ApplyStep();
                applied = true;
            }

            if (IsFinished)
            {
                return;
            }

            stepElapsed += deltaTime;
            float duration = phaseSeconds;
            if (stepIndex == 0)
            {
                duration = warmupSeconds;
            }
            if (stepElapsed < duration)
            {
                return;
            }

            stepElapsed = 0f;
            stepIndex++;
            ApplyStep();
        }

        public string GetStepName(int index)
        {
            if (index < 0 || index >= stepNames.Length)
            {
                return string.Empty;
            }
            return stepNames[index];
        }

        private void ApplyStep()
        {
            int phase = stepPhases[stepIndex];
            bool navUiVisible = phase != PhaseNoNavUi;

            SetActive(minimapRoot, navUiVisible);
            ApplyFullMapOpen(stepFullMapOpen[stepIndex] && navUiVisible);

            SetActive(staticMarkersRoot, phase != PhaseNoPerfMarkers);
            SetActive(movingMarkersRoot, phase != PhaseNoPerfMarkers && phase != PhaseNoMovingMarkers);

            bool arrows = phase != PhaseNoArrows;
            bool routeLine = phase != PhaseNoRouteLine;
            ApplyToView(GetMinimapView(), arrows, routeLine);
            if (fullMap != null)
            {
                ApplyToView(fullMap.GetComponent<MapView>(), arrows, routeLine);
            }
        }

        private void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
            {
                target.SetActive(value);
            }
        }

        private void ApplyFullMapOpen(bool open)
        {
            if (fullMap == null)
            {
                return;
            }

            if (open && !fullMap.gameObject.activeSelf)
            {
                fullMap.Open();
            }
            else if (!open && fullMap.gameObject.activeSelf)
            {
                fullMap.Close();
            }
        }

        private MapView GetMinimapView()
        {
            if (minimapRoot == null)
            {
                return null;
            }
            return minimapRoot.GetComponentInChildren<MapView>(true);
        }

        private void ApplyToView(MapView view, bool arrows, bool routeLine)
        {
            if (view == null)
            {
                return;
            }

            view.SetShowOffScreenArrows(arrows);
            SetRendererActive(view.ActiveRouteRenderer, routeLine);
            SetRendererActive(view.PreviewRouteRenderer, routeLine);
        }

        private void SetRendererActive(RouteLineRenderer routeRenderer, bool value)
        {
            if (routeRenderer != null)
            {
                SetActive(routeRenderer.gameObject, value);
            }
        }
    }
}
