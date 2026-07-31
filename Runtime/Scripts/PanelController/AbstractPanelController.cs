using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using frou01.util;
#endif

namespace RBUR_SignalIntegrator
{
    //virtualization controller for machine<Point,Signal,Sign>
    public class AbstractPanelController : AbstractLockerConsolidater
    {
        [SerializeField][HideInInspector] public Interlocking[] interlocks;
        [SerializeField][UdonSynced] protected int controllingPosition;//machine controlling position
        [SerializeField] protected int[] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)
        [SerializeField] protected Animator[] SwitchSideAnimator;
        [SerializeField] protected string switchAnimationParamater = "SwitchPosition";

        void Start()
        {

        }
        public override bool tryUpdateLocking(UdonSharpBehaviour triedFromInstance, bool lockState, int lockPositionSelector, out int triedInstanceIndex)
        {
            bool res = base.tryUpdateLocking(triedFromInstance, lockState, lockPositionSelector, out triedInstanceIndex);
            if (!isLocked() && switchToControllerMap[switchPosition] != -1)
            {
                trySetPosition(switchToControllerMap[switchPosition]);
            }

            return res;
        }
        public override int GetCurrentPosition()
        {
            return controllingPosition;
        }
        protected override void applyLockToController(bool state)
        {
            //Lever is not lockup. and validation is excuted on interlock.
        }
        protected override void applyPositionToController(int posIndex)
        {
            if (isControllerOwner() && posIndex != controllingPosition)
            {
                controllingPosition = posIndex;
                SyncController();
            }
            else
            {
                controllingPosition = posIndex;
            }
        }
        public override bool isControllerOwner()
        {
            return Networking.IsOwner(this.gameObject);
        }
        public override void setControllerOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        public override void SyncController()
        {
            this.RequestSerialization();
        }

        [UdonSynced][SerializeField] protected int switchPosition;
        Slider slider
        {
            get
            {
                if(m_slider == null)
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
        public virtual void OnValueChanged()
        {
            foreach (Interlocking interlock in interlocks)
            {
                interlock.UpdateInterlock();
            }
            setControllerOwner();
            if(slider != null) switchPosition = (int)slider.value;
            if (switchToControllerMap[switchPosition] != -1)
            {
                trySetPosition(switchToControllerMap[switchPosition]);
            }
            foreach (Animator animator in SwitchSideAnimator)
            {
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (switchToControllerMap.Length-1));
            }
            foreach (Interlocking interlock in interlocks)
            {
                interlock.UpdateInterlock();
            }
            SyncController();
        }
        public override void OnDeserialization()
        {
            if (slider != null)
            {
                slider.value = switchPosition;
            }
            foreach (Animator animator in SwitchSideAnimator)
            {
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (switchToControllerMap.Length - 1));
            }
            applyPositionToController(controllingPosition);
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
        }
        protected virtual void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in SwitchSideAnimator)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), this.gameObject.name + ".SwitchAnimator", guiStyle);
            }
        }

#endif
    }
}