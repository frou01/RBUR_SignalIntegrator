using frou01.util;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    [RequireComponent(typeof(RouteSelector_EndToPresetMapHolder))]
    public class RouteSelector_StartLever : UdonSharpBehaviour
    {
        [UdonSynced][SerializeField] private int Switch_Position;
        [UdonSynced] private bool routeSelected;
        [SerializeField] private Animator[] SwitchSideAnimator;
        [SerializeField] private string switchAnimationParamater = "SwitchPosition";
        [HideInInspector][SerializeField] private RouteSelector_EndButton[][] RouteEnds;
        public RouteSelector_EndButton[][] getRouteEnds()
        {
            return RouteEnds;
        }
        [HideInInspector][SerializeField] private MultiLeverController[][] RouteAndSignalPreset;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void AssignMaps()
        {
            GetComponent<RouteSelector_EndToPresetMapHolder>().ApplyEndToPresetArray(out RouteEnds, out RouteAndSignalPreset);
        }
#endif
        Slider slider
        {
            get
            {
                if (m_slider == null)
                {
                    m_slider = GetComponentInChildren<Slider>();
                }
                return m_slider;
            }

            set
            {
                m_slider = value;
            }
        }
        Slider m_slider;

        protected virtual void Start()
        {
            OnDeserialization();
            if (slider) slider.maxValue = RouteEnds.Length;//0 or 1
        }

        private void SwitchDisabled()
        {
            routeSelected = false;
            foreach (MultiLeverController[] routeArray in RouteAndSignalPreset)
            {
                foreach (MultiLeverController route in routeArray)
                {
                    route.SetControlToLevers(0);
                }
            }
        }

        public void SelectRoute(RouteSelector_EndButton endButton)
        {
            if (!routeSelected && RouteEnds[Switch_Position].Length > 0)
            {
                Networking.SetOwner(Networking.LocalPlayer,this.gameObject);
                int SelectedRoute = 0;
                foreach (RouteSelector_EndButton routeEnd in RouteEnds[Switch_Position])
                {
                    if (routeEnd == endButton) break;
                    SelectedRoute++;
                }
                RouteAndSignalPreset[Switch_Position][SelectedRoute].SetControlToLevers(1);
                routeSelected = true;
                RequestSerialization();
            }
        }
        public void OnValueChanged()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if (slider != null) Switch_Position = (int)slider.value;
            SyncUI(Switch_Position, false);
            if(RouteEnds[Switch_Position].Length <= 0)
            {
                SwitchDisabled();
            }
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            SyncUI(Switch_Position, true);
        }
        protected void SyncUI(int switchPosition, bool updateSlider)
        {
            if (updateSlider && slider != null)
            {
                slider.SetValueWithoutNotify(switchPosition);
            }

            foreach (Animator animator in SwitchSideAnimator)
            {
                AnimatorSleeper sleeper = animator.GetComponentInChildren<AnimatorSleeper>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, switchPosition / (RouteEnds.Length > 0 ? RouteEnds.Length-1 : 1));
            }
        }
    }
}