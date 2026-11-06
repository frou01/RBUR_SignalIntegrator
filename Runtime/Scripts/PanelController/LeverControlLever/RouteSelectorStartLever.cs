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
        [UdonSynced][SerializeField] private int Switch_Position;
        [UdonSynced] private bool routeSelected;
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
            if (!routeSelected && routeEnds[Switch_Position].Length > 0)
            {
                Networking.SetOwner(Networking.LocalPlayer,this.gameObject);
                int selectedRoute = 0;
                foreach (RouteSelectorEndButton routeEnd in routeEnds[Switch_Position])
                {
                    if (routeEnd == endButton) break;
                    selectedRoute++;
                }
                if(selectedRoute == routeEnds[Switch_Position].Length)
                {
                    return;
                }
                routeAndSignalPresets[Switch_Position][selectedRoute].SetControlToLevers(1,true);
                routeSelected = true;
                RequestSerialization();
            }
        }
        public void _OnValueChanged()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if (LinkedSlider != null) Switch_Position = (int)LinkedSlider.value;
            SyncUI(Switch_Position, false);
            if(routeEnds[Switch_Position].Length <= 0)
            {
                SwitchDisabledOwnerOnly();
            }
            RequestSerialization();
        }

        private void SwitchDisabledOwnerOnly()
        {
            routeSelected = false;
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
            SyncUI(Switch_Position, true);
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