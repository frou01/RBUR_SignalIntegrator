using System.Threading;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer;

namespace RBUR_SignalIntegrator
{
    [RequireComponent(typeof(MultiLeverMappingHolder))]
    public class MultiLeverController : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [UdonSynced][SerializeField] protected int switchPosition;
        protected int PrevSwitchPos = -1;
        [SerializeField] protected Animator[] SwitchSideAnimator;
        [SerializeField] protected string switchAnimationParamater = "SwitchPosition";
        [HideInInspector][OdinSerialize][SerializeField] protected int[][] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)
        [SerializeField][HideInInspector] public Interlocking[] ReferingInterlocks;

        public void Set_switchToControllerMap(int[][] switchToControllerMap)
        {
            this.switchToControllerMap = switchToControllerMap;
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

        [SerializeField] public AbstractPanelController[] controlledLevers;

        public virtual void setControllerOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        public virtual void SyncController()
        {
            this.RequestSerialization();
        }
        public virtual void OnValueChanged()
        {
            eventStackHolder.AddStack(this, nameof(OnValueChanged));

            //Pre-control Interlock update
            foreach(Interlocking interlock in ReferingInterlocks)
            {
                interlock.UpdateInterlock();
            }

            setControllerOwner();
            if (slider != null) switchPosition = (int)slider.value;

            int idx = 0;
            foreach(AbstractPanelController panelController in controlledLevers)
            {
                panelController.setControllerOwner();
                if (switchToControllerMap[idx][switchPosition] != -1)
                {
                    panelController.trySetPosition(switchToControllerMap[idx][switchPosition]);
                }
                idx++;
            }

            foreach (Animator animator in SwitchSideAnimator)
            {
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (switchToControllerMap.Length - 1));
            }
            SyncController();

            //Post-control Interlock update
            foreach (Interlocking interlock in ReferingInterlocks)
            {
                interlock.UpdateInterlock();
            }
            PrevSwitchPos = switchPosition;

            eventStackHolder.RemoveStack(this, nameof(OnValueChanged));
        }
        public override void OnDeserialization()
        {
            eventStackHolder.AddStack(this, nameof(OnDeserialization));

            if (PrevSwitchPos != switchPosition)
            {
                //Pre-control Interlock update
                foreach (Interlocking interlock in ReferingInterlocks)
                {
                    interlock.UpdateInterlock();
                }
            }

            if (slider != null)
            {
                slider.SetValueWithoutNotify(switchPosition);
            }
            foreach (Animator animator in SwitchSideAnimator)
            {
                animator.enabled = true;
                animator.SetFloat(switchAnimationParamater, (float)switchPosition / (switchToControllerMap.Length - 1));
            }

            int idx = 0;
            foreach (AbstractPanelController panelController in controlledLevers)
            {
                if (switchToControllerMap[idx][switchPosition] != -1)
                {
                    panelController.trySetPosition(switchToControllerMap[idx][switchPosition]);
                }
                idx++;
            }

            if (PrevSwitchPos != switchPosition)
            {
                //Post-control Interlock update
                foreach (Interlocking interlock in ReferingInterlocks)
                {
                    interlock.UpdateInterlock();
                }
            }

            eventStackHolder.RemoveStack(this, nameof(OnDeserialization));
        }
        public void PanelLockstateUpdate()
        {
            eventStackHolder.AddStack(this, nameof(PanelLockstateUpdate));

            int idx = 0;
            foreach (AbstractPanelController panelController in controlledLevers)
            {
                if (switchToControllerMap[idx][switchPosition] != -1)
                {
                    panelController.trySetPosition(switchToControllerMap[idx][switchPosition]);
                }
                idx++;
            }
            eventStackHolder.RemoveStack(this, nameof(PanelLockstateUpdate));
        }
    }
}