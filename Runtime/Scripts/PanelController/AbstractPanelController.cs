using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using frou01.util;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
#endif

namespace RBUR_SignalIntegrator
{
    //virtualization controller for machine<Point,Signal,Sign>
    public class AbstractPanelController : AbstractLockerConsolidater
    {
        [SerializeField][HideInInspector] public Interlocking[] ReferingInterlocks;
        int PannelCon_prevControllingPosition = -1;
        [SerializeField][UdonSynced] protected int controllingPosition;//machine controlling position
        [SerializeField] protected int[] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public int[] getSwitchToControllerMap()
        {
            return switchToControllerMap;
        }
#endif

        [SerializeField] protected Animator[] SwitchSideAnimator;
        [SerializeField] protected string switchAnimationParamater = "SwitchPosition";
        [SerializeField] public UdonBehaviour[] callbackBehaviours;

        protected bool UpdateInterlockBySwitching = true;

        [UdonSynced][SerializeField] protected int switchPosition;
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

        protected override void Start()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(Start));
            base.Start();
            OnDeserialization();
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(Start));
        }
        public override bool tryUpdateLocking(UdonSharpBehaviour triedFromInstance, bool lockState, int lockPositionSelector, out int triedInstanceIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(tryUpdateLocking));
            bool prevLock = isLocked();
            bool res = base.tryUpdateLocking(triedFromInstance, lockState, lockPositionSelector, out triedInstanceIndex);
            if (!isLocked())
            {
                trySetPosition(switchToControllerMap[switchPosition]);
            }
            if(isLocked() != prevLock)
            {
                foreach (UdonBehaviour beh in callbackBehaviours)
                {
                    beh.SendCustomEvent("PanelLockstateUpdate");
                }
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(tryUpdateLocking));

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
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(applyPositionToController));
            if (posIndex != -1)
            {
                if (isControllerOwner() && posIndex != controllingPosition)
                {
                    controllingPosition = posIndex;
                    if (PannelCon_prevControllingPosition != controllingPosition) SyncController();
                }
                else
                {
                    controllingPosition = posIndex;
                }
            }


            PannelCon_prevControllingPosition = controllingPosition;
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(applyPositionToController));
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
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(SyncController));
            if(isControllerOwner()) this.RequestSerialization();
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(SyncController));
        }
        public virtual void OnValueChanged()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(OnValueChanged));
            //Pre-control Interlock update
            setControllerOwner();
            UpdateInterlocks();

            if (slider != null) switchPosition = (int)slider.value;
            //if (isLocked())
            //{
            //    DebugLock();
            //}
            trySetPosition(switchToControllerMap[switchPosition]);
            foreach (Animator animator in SwitchSideAnimator)
            {
                AnimatorSleeper sleeper = animator.GetComponentInChildren<AnimatorSleeper>(); if(sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (switchToControllerMap.Length-1));
            }
            SyncController();

            //Post-control Interlock update
            if (UpdateInterlockBySwitching)
            {
                UpdateInterlocks();
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(OnValueChanged));
        }
        public override void OnDeserialization()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(OnDeserialization));


            //Pre-control Interlock update
            if (UpdateInterlockBySwitching)
            {
                UpdateInterlocks();
            }

            bool updateControllingPosition = PannelCon_prevControllingPosition != controllingPosition;

            SyncUI(switchPosition);

            applyPosition(controllingPosition);


            //Post-control Interlock update
            if (updateControllingPosition)
            {
                UpdateInterlocks();
            }

            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(OnDeserialization));
        }

        public virtual void UpdateInterlocks()
        {
            int loopLimit = 10;
            bool needNextUpdate;
            do
            {
                loopLimit--;
                needNextUpdate = false;
                foreach (Interlocking interlock in ReferingInterlocks)
                {
                    needNextUpdate |= interlock.UpdateInterlock(false);
                }
            } while (needNextUpdate && loopLimit > 0);
            if (loopLimit <= 0)
            {
                Debug.LogError(this.name + ": Interlock Update Looping. Interlock Settings is inconsistency." + this.name, this);
                foreach (Interlocking interlock in ReferingInterlocks)
                {
                    Debug.LogError(this.name + ":    " + interlock.name, interlock);
                }
            }
        }

        protected virtual void SyncUI(int switchPosition)
        {
            this.switchPosition = switchPosition;
            if (slider != null)
            {
                slider.SetValueWithoutNotify(this.switchPosition);
            }
            foreach (Animator animator in SwitchSideAnimator)
            {
                AnimatorSleeper sleeper = animator.GetComponentInChildren<AnimatorSleeper>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (switchToControllerMap.Length - 1));
            }
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