using frou01.util;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class RouteSelector_StartLever : UdonSharpBehaviour
    {
        private bool Switch_Enabled
        {
            get {
                return m_switch_Enabled;
            }
            set {
                m_switch_Enabled = value;
                if (m_switch_Enabled)
                {
                    SwitchEnabled();
                }
                else
                {
                    if (Networking.IsOwner(this.gameObject))SwitchDisabled();
                }
            }
        }
        [UdonSynced, FieldChangeCallback(nameof(Switch_Enabled))] private bool m_switch_Enabled;
        [UdonSynced] private bool routeSelected;
        [SerializeField] private Animator[] SwitchSideAnimator;
        [SerializeField] private string switchAnimationParamater = "SwitchPosition";
        [HideInInspector][SerializeField] private RouteSelector_EndButton[] RouteEnds;
        [HideInInspector][SerializeField] private MultiLeverController[] RouteAndSignalPreset;
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
            if (slider) slider.maxValue = 1;//0 or 1
        }

        private void SwitchEnabled()
        {

        }

        private void SwitchDisabled()
        {
            routeSelected = false;
            foreach (MultiLeverController routes in RouteAndSignalPreset)
            {
                routes.SetControlToLevers(0);
            }
        }

        public void SelectRoute(RouteSelector_EndButton endButton)
        {
            if (Switch_Enabled && !routeSelected)
            {
                int SelectedRoute = 0;
                foreach (RouteSelector_EndButton routeEnd in RouteEnds)
                {
                    if (routeEnd == endButton) break;
                    SelectedRoute++;
                }
                RouteAndSignalPreset[SelectedRoute].SetControlToLevers(1);
                routeSelected = true;
            }
        }
        public void OnValueChanged()
        {
            if (slider != null) Switch_Enabled = (int)slider.value == 1;
            SyncUI(Switch_Enabled, false);
        }

        public override void OnDeserialization()
        {
            SyncUI(Switch_Enabled, true);
        }
        protected void SyncUI(bool switchPosition, bool updateSlider)
        {
            if (updateSlider && slider != null)
            {
                slider.SetValueWithoutNotify(switchPosition ? 1 : 0);
            }

            foreach (Animator animator in SwitchSideAnimator)
            {
                AnimatorSleeper sleeper = animator.GetComponentInChildren<AnimatorSleeper>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, switchPosition ? 1f : 0f);
            }
        }
    }
}