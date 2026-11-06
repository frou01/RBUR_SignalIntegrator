using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class LeverPanelUIReceiver : UdonSharpBehaviour
    {
        [SerializeField] private protected int[] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)
        [SerializeField] private protected AbstractIndirectController targetController;//index:switch, value:controller. -1 is mid(not lever local control)
        [FormerlySerializedAs("SwitchSideAnimator")][SerializeField] private protected Animator[] switchSideAnimators = new Animator[0];
        [FormerlySerializedAs("switchAnimationParamater")][SerializeField] private protected string switchAnimationParamName = "SwitchPosition";
        [FormerlySerializedAs("switchPosition")][SerializeField][UdonSynced] private protected int syncedSwitchPosition;
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
            set { linkedSlider = value; }
        }
        Slider linkedSlider;
        private void Start()
        {
            if (LinkedSlider) LinkedSlider.maxValue = (switchToControllerMap.Length - 1);
            OnDeserialization();
        }

        public override void OnDeserialization()
        {
            SyncUI(true);
            targetController.TrySetControlTarget(this, switchToControllerMap[syncedSwitchPosition], true);
        }
        public virtual void _OnValueChanged()//Call by UI
        {
            if (LinkedSlider != null) syncedSwitchPosition = (int)LinkedSlider.value;

            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, targetController.gameObject);

            SyncUI(false);

            if (targetController.TrySetControlTarget(this, switchToControllerMap[syncedSwitchPosition], true))
            {
                targetController.UpdateInterlocks();
            }

            RequestSerialization();
        }

        protected virtual void SyncUI(bool updateSlider)
        {
            if (updateSlider && LinkedSlider != null)
            {
                LinkedSlider.SetValueWithoutNotify(this.syncedSwitchPosition);
            }

            foreach (Animator animator in switchSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamName, (float)syncedSwitchPosition / (switchToControllerMap.Length - 1));
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in switchSideAnimators)
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