using System.Threading;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer;

namespace RBUR_SignalIntegrator
{
    public class MultiLeverController : UdonSharpBehaviour
    {
        [UdonSynced][SerializeField] protected int switchPosition;
        [SerializeField] protected Animator[] SwitchSideAnimator;
        [SerializeField] protected string switchAnimationParamater = "SwitchPosition";
        [OdinSerialize][SerializeField] protected int[][] switchToControllerMap;//index:switch, value:controller. -1 is mid(not lever local control)
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
        }
        public override void OnDeserialization()
        {
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
        }
        public void PanelLeverUpdate()
        {
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
        }
    }
}