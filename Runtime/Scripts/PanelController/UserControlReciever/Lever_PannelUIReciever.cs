using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class Lever_PannelUIReciever : UdonSharpBehaviour
    {
        //TODO currentUserControlに現時点のユーザー側制御を送り込む
        //TODO 参照はReciever->Controllerの片方向にする
        //TODO これAbstractClassいらないね
        [SerializeField] private protected int[] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)
        [SerializeField] private protected AbstractIndirectController targetController;//index:switch, value:controller. -1 is mid(not lever local control)
        [SerializeField] private protected Animator[] SwitchSideAnimator = new Animator[0];
        [SerializeField] private protected string switchAnimationParamater = "SwitchPosition";
        [SerializeField][UdonSynced] private protected int switchPosition;
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
            set { m_slider = value; }
        }
        Slider m_slider;
        private void Start()
        {
            if (slider) slider.maxValue = (switchToControllerMap.Length - 1);
            OnDeserialization();
        }

        public override void OnDeserialization()
        {
            SyncUI(true);
            targetController.TrySetControlTarget(this, switchToControllerMap[switchPosition], true);
        }
        public virtual void OnValueChanged()//Call by UI
        {
            if (slider != null) switchPosition = (int)slider.value;

            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, targetController.gameObject);

            SyncUI(false);

            targetController.TrySetControlTarget(this,switchToControllerMap[switchPosition],true);

            RequestSerialization();
        }

        protected virtual void SyncUI(bool updateSlider)
        {
            if (updateSlider && slider != null)
            {
                slider.SetValueWithoutNotify(this.switchPosition);
            }

            foreach (Animator animator in SwitchSideAnimator)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (switchToControllerMap.Length - 1));
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in SwitchSideAnimator)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), animator.name, guiStyle);
            }
        }
#endif
    }
}