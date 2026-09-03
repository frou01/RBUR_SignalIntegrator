using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using frou01.util;
using VRC.Udon.Common;
using UnityEngine.Serialization;




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
        [SerializeField][UdonSynced] private protected int controllingPosition;//machine controlling position
        [SerializeField] private protected int[] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public int[] getSwitchToControllerMap()
        {
            return switchToControllerMap;
        }
#endif

        [SerializeField] private protected Animator[] SwitchSideAnimator;
        [SerializeField] private protected string switchAnimationParamater = "SwitchPosition";
        [FormerlySerializedAs("callbackBehaviours")]
        [Tooltip("Auto assign by buildProcess")][SerializeField] public UdonBehaviour[] LockstateCallbackBehaviours;

        private protected bool UpdateInterlockBySwitching = true;

        [UdonSynced][SerializeField] private protected int switchPosition;
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
            this.enabled = false;
            base.Start();
            if (slider) slider.maxValue = (switchToControllerMap.Length - 1);
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
                foreach (UdonBehaviour beh in LockstateCallbackBehaviours)
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
        public override bool isControllerOwner()
        {
            return Networking.IsOwner(this.gameObject);
        }
        public virtual bool isLocalOverride()
        {
            return switchToControllerMap[switchPosition] != -1;
        }
        private protected override void applyLockToController(bool state)
        {
            //There is No Other instance for apply lock. bool array has updated.
        }
        private protected override void applyPositionToController(int posIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(applyPositionToController));
            if (posIndex != -1 && posIndex != controllingPosition)
            {
                controllingPosition = posIndex;
                if (isControllerOwner())
                {
                    if (PannelCon_prevControllingPosition != controllingPosition) SyncController();
                }
            }

            PannelCon_prevControllingPosition = controllingPosition;
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(applyPositionToController));
        }
        public override void setControllerOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        private protected override void SyncController()
        {
            if (isControllerOwner())
            {
                if (!this.enabled) this.enabled = true;
                this.RequestSerialization();
            }
        }
        public virtual void OnValueChanged()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(OnValueChanged));
            setControllerOwner();

            //Pre-control Interlock update
            UpdateInterlocks();

            if (slider != null) switchPosition = (int)slider.value;

            SyncUI(switchPosition, false);

            trySetPosition(switchToControllerMap[switchPosition]);

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
            if (eventStackHolder != null) eventStackHolder.AddStack(this, nameof(OnDeserialization));

            SyncUI(switchPosition, true);

            applyPosition(controllingPosition);//Force Apply Control

            Debug.Log("" + this.name + " , " + controllingPosition);

            //Post-control Interlock update
            if (UpdateInterlockBySwitching)
            {
                UpdateInterlocks();
            }

            if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(OnDeserialization));
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success)
            {
                DisableAfterSync();
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(_RetrySync),Random.Range(0,3));
            }
        }

        public void _RetrySync()
        {
            SyncController();
        }
        protected virtual void DisableAfterSync()
        {
            this.enabled = false;
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

        protected virtual void SyncUI(int switchPosition,bool updateSlider)
        {
            this.switchPosition = switchPosition;
            if (updateSlider && slider != null)
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