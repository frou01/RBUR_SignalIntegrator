using frou01.util;
using System.Threading;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer;

namespace frou01.RBUR.SignalIntegrator
{
    [RequireComponent(typeof(MultiLever_SwitchToControlMapsHolder))]
    public class MultiLeverController : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [UdonSynced][SerializeField] private protected int switchPosition;
        [SerializeField] private protected Animator[] SwitchSideAnimator;
        [SerializeField] private protected string switchAnimationParamater = "SwitchPosition";
        [HideInInspector][SerializeField] private AbstractIndirectController[] m_controlledLevers;
        public AbstractIndirectController[] ControlledLevers
        {
            get
            {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
                return GetComponent<MultiLever_SwitchToControlMapsHolder>().GetControllers();
#else
                return new AbstractIndirectController[0];
#endif
            }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
            set
            {
                m_controlledLevers = value;
            }
#endif
        }
        [HideInInspector][OdinSerialize][SerializeField] private protected int[][] m_SwitchToControllerMap = new int[0][];//index:switch, value:controller. -1 is mid(not lever local control)
        public int[][] SwitchToControllerMap
        {
            get
            {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
                return GetComponent<MultiLever_SwitchToControlMapsHolder>().get_Switch_to_Controller_Map();
#else
                return m_SwitchToControllerMap;
#endif
            }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            set
            {
                m_SwitchToControllerMap = value;
            }
#endif
        }
        [HideInInspector][SerializeField] private protected int SwitchPositionNum;
        [HideInInspector][SerializeField] public Interlocking[] ReferingInterlocks;


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
            if (LinkedSlider) LinkedSlider.maxValue = (SwitchPositionNum - 1);
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

            if (LinkedSlider != null) switchPosition = (int)LinkedSlider.value;
            SyncUI(switchPosition, false);
            if (LinkedSlider != null) SetControlToLevers(switchPosition, true);

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
        public virtual void SetControlToLevers(int newSwitchPosition,bool takeOwner)
        {
            if (eventStackHolder != null) eventStackHolder.AddStack(this, nameof(SetControlToLevers));

            if (newSwitchPosition != prevAppliedPos)
            {
                switchPosition = prevAppliedPos = newSwitchPosition;
                if(takeOwner) setControllerOwner();

                //Try control Controllers without interlock update
                int idx = 0;
                foreach (AbstractIndirectController panelController in m_controlledLevers)
                {
                    int nextControl = m_SwitchToControllerMap[idx][switchPosition];

                    if(takeOwner && nextControl != -1) panelController.setControllerOwner();
                    panelController.TrySetControlTarget(this, m_SwitchToControllerMap[idx][switchPosition], false);
                    idx++;
                }


                UpdateInterlocks();

                SyncController();
            }

            if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(SetControlToLevers));
        }

        protected virtual void SyncUI(int switchPosition, bool updateSlider)
        {
            if (updateSlider && LinkedSlider != null)
            {
                LinkedSlider.SetValueWithoutNotify(this.switchPosition);
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