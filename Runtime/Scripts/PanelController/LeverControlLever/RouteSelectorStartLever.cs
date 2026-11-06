using frou01.util;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    [RequireComponent(typeof(RouteSelectorEndToPresetMapHolder))]
    public class RouteSelectorStartLever : UdonSharpBehaviour
    {
        [FormerlySerializedAs("Switch_Position")][UdonSynced][SerializeField] private int syncedSwitchPosition;
        [UdonSynced] private bool syncedRouteSelected;
        [FormerlySerializedAs("SwitchSideAnimator")][SerializeField] private Animator[] switchSideAnimators;
        [FormerlySerializedAs("switchAnimationParamater")][SerializeField] private string switchAnimationParamName = "SwitchPosition";
        [SerializeField] private int switchAnimationOffset;
        [FormerlySerializedAs("RouteEnds")][HideInInspector][SerializeField] private RouteSelectorEndButton[][] routeEnds;
        public RouteSelectorEndButton[][] GetRouteEnds()
        {
            return routeEnds;
        }
        [FormerlySerializedAs("RouteAndSignalPreset")][HideInInspector][SerializeField] private MultiLeverController[][] routeAndSignalPresets;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void AssignMaps()
        {
            GetComponent<RouteSelectorEndToPresetMapHolder>().ApplyEndToPresetArray(out routeEnds, out routeAndSignalPresets);
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
            if (LinkedSlider) LinkedSlider.maxValue = routeEnds.Length - 1;//0 or 1
            OnDeserialization();
        }

        public void SelectRoute(RouteSelectorEndButton endButton)
        {
            if (!syncedRouteSelected && routeEnds[syncedSwitchPosition].Length > 0)
            {
                Networking.SetOwner(Networking.LocalPlayer,this.gameObject);
                int selectedRoute = 0;
                foreach (RouteSelectorEndButton routeEnd in routeEnds[syncedSwitchPosition])
                {
                    if (routeEnd == endButton) break;
                    selectedRoute++;
                }
                if(selectedRoute == routeEnds[syncedSwitchPosition].Length)
                {
                    return;
                }
                routeAndSignalPresets[syncedSwitchPosition][selectedRoute].SetControlToLevers(1,true);
                syncedRouteSelected = true;
                RequestSerialization();
            }
        }
        public void _OnValueChanged()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if (LinkedSlider != null) syncedSwitchPosition = (int)LinkedSlider.value;
            SyncUI(syncedSwitchPosition, false);
            if(routeEnds[syncedSwitchPosition].Length <= 0)
            {
                SwitchDisabledOwnerOnly();
            }
            RequestSerialization();
        }

        private void SwitchDisabledOwnerOnly()
        {
            syncedRouteSelected = false;
            foreach (MultiLeverController[] routeArray in routeAndSignalPresets)
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
            SyncUI(syncedSwitchPosition, true);
        }
        protected void SyncUI(int switchPosition, bool updateSlider)
        {
            if (updateSlider && LinkedSlider != null)
            {
                LinkedSlider.SetValueWithoutNotify(switchPosition);
            }

            foreach (Animator animator in switchSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamName,
                    (float)(switchPosition + (switchAnimationOffset > 0 ? switchAnimationOffset : 0)) 
                    /
                    ((routeEnds.Length > 1 ? routeEnds.Length - 1 : 1) + Mathf.Abs(switchAnimationOffset)));
            }
        }
    }
}