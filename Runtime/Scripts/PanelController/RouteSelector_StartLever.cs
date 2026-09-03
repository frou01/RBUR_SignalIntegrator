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
        [UdonSynced][SerializeField] private protected bool switchPosition;
        [SerializeField] private protected Animator[] SwitchSideAnimator;
        [SerializeField] private protected string switchAnimationParamater = "SwitchPosition";
        private protected int SelectedRoute;
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

        public virtual void OnValueChanged()
        {
            if (slider != null) switchPosition = (int)slider.value == 1;
        }
        public override void OnDeserialization()
        {
            SyncUI(switchPosition, true);
        }
        protected virtual void SyncUI(bool switchPosition, bool updateSlider)
        {
            this.switchPosition = slider.value == 1;
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