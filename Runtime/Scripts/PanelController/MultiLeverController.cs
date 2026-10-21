using frou01.util;
using System.Threading;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer;

namespace RBUR_SignalIntegrator
{
    [RequireComponent(typeof(MultiLever_SwitchToControlMapsHolder))]
    public class MultiLeverController : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [UdonSynced][SerializeField] private protected int switchPosition;
        [SerializeField] private protected Animator[] SwitchSideAnimator;
        [SerializeField] private protected string switchAnimationParamater = "SwitchPosition";
        [HideInInspector][SerializeField] public AbstractIndirectController[] controlledLevers;
        [HideInInspector][OdinSerialize][SerializeField] private protected int[][] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)
        [HideInInspector][SerializeField] private protected int SwitchPositionNum;
        [HideInInspector][SerializeField] public Interlocking[] ReferingInterlocks;
        public void Set_switchToControllerMap(int[][] switchToControllerMap)
        {
            this.switchToControllerMap = switchToControllerMap;
            if (switchToControllerMap.Length > 0)
                SwitchPositionNum = switchToControllerMap[0].Length;
        }
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
            if (slider) slider.maxValue = (SwitchPositionNum - 1);
            OnDeserialization();
        }
        public virtual void setControllerOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        public virtual void SyncController()
        {
            if(Networking.IsOwner(this.gameObject))this.RequestSerialization();
        }
        public virtual void OnValueChanged()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(OnValueChanged));

            if (slider != null) switchPosition = (int)slider.value;
            SyncUI(switchPosition, false);
            if (slider != null) SetControlToLevers(switchPosition, true);

            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(OnValueChanged));
        }
        public override void OnDeserialization()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(OnDeserialization));
            //Update Interlock in controlled-levers.

            SyncUI(switchPosition, true);
            //ControlPosition synced by controlled-levers.
            SetControlToLevers(this.switchPosition , false);

            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(OnDeserialization));
        }

        int prevAppliedPos = -2;
        public void SetControlToLevers(int newSwitchPosition,bool TakeOwner)
        {
            if (eventStackHolder != null) eventStackHolder.AddStack(this, nameof(SetControlToLevers));

            if (switchPosition != prevAppliedPos)
            {
                prevAppliedPos = switchPosition;
                if(TakeOwner) setControllerOwner();

                //Try control Controllers without interlock update
                int idx = 0;
                foreach (AbstractIndirectController panelController in controlledLevers)
                {
                    int nextControl = switchToControllerMap[idx][switchPosition];

                    if(TakeOwner && nextControl != -1) panelController.setControllerOwner();
                    panelController.TrySetControlTarget(this, switchToControllerMap[idx][switchPosition], false);
                    idx++;
                }


                UpdateInterlocks();

                SyncController();
            }

            if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(SetControlToLevers));
        }

        protected virtual void SyncUI(int switchPosition, bool updateSlider)
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
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (SwitchPositionNum - 1));
            }
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
    }
}