using frou01.util;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    [RequireComponent(typeof(RouteSelectorEndToPresetMapHolder))]
    public class RouteSelectorStartLever : UdonSharpBehaviour
    {
        [UdonSynced][SerializeField] private int Switch_Position;
        [UdonSynced] private bool routeSelected;
        [SerializeField] private Animator[] SwitchSideAnimator;
        [SerializeField] private string switchAnimationParamater = "SwitchPosition";
        [SerializeField] private int switchAnimationOffset;
        [HideInInspector][SerializeField] private RouteSelectorEndButton[][] RouteEnds;
        public RouteSelectorEndButton[][] GetRouteEnds()
        {
            return RouteEnds;
        }
        [HideInInspector][SerializeField] private MultiLeverController[][] RouteAndSignalPreset;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void AssignMaps()
        {
            GetComponent<RouteSelectorEndToPresetMapHolder>().ApplyEndToPresetArray(out RouteEnds, out RouteAndSignalPreset);
        }
#endif
        Slider LinkedSlider
        {
            get
            {
                if (linkedSlider == null)
                {
                    linkedSlider = GetComponentInChildren<Slider>();
                }
                return linkedSlider;
            }

            set
            {
                linkedSlider = value;
            }
        }
        Slider linkedSlider;

        protected virtual void Start()
        {
            if (LinkedSlider) LinkedSlider.maxValue = RouteEnds.Length - 1;//0 or 1
            OnDeserialization();
        }

        public void SelectRoute(RouteSelectorEndButton endButton)
        {
            if (!routeSelected && RouteEnds[Switch_Position].Length > 0)
            {
                Networking.SetOwner(Networking.LocalPlayer,this.gameObject);
                int selectedRoute = 0;
                foreach (RouteSelectorEndButton routeEnd in RouteEnds[Switch_Position])
                {
                    if (routeEnd == endButton) break;
                    selectedRoute++;
                }
                if(selectedRoute == RouteEnds[Switch_Position].Length)
                {
                    return;
                }
                RouteAndSignalPreset[Switch_Position][selectedRoute].SetControlToLevers(1,true);
                routeSelected = true;
                RequestSerialization();
            }
        }
        public void OnValueChanged()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if (LinkedSlider != null) Switch_Position = (int)LinkedSlider.value;
            SyncUI(Switch_Position, false);
            if(RouteEnds[Switch_Position].Length <= 0)
            {
                SwitchDisabledOwnerOnly();
            }
            RequestSerialization();
        }

        private void SwitchDisabledOwnerOnly()
        {
            routeSelected = false;
            foreach (MultiLeverController[] routeArray in RouteAndSignalPreset)
            {
                foreach (MultiLeverController route in routeArray)
                {
                    route.SetControlToLevers(0, true);
                }
            }
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            SyncUI(Switch_Position, true);
        }
        protected void SyncUI(int switchPosition, bool updateSlider)
        {
            if (updateSlider && LinkedSlider != null)
            {
                LinkedSlider.SetValueWithoutNotify(switchPosition);
            }

            foreach (Animator animator in SwitchSideAnimator)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater,
                    (float)(switchPosition + (switchAnimationOffset > 0 ? switchAnimationOffset : 0)) 
                    /
                    ((RouteEnds.Length > 1 ? RouteEnds.Length - 1 : 1) + Mathf.Abs(switchAnimationOffset)));
            }
        }
    }
}