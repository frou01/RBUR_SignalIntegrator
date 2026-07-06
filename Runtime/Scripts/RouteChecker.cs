using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class RouteChecker : UdonSharpBehaviour
    {
        [SerializeField] AbstractPointSetter[] TargetPoints;
        [SerializeField] Rail_Script[] TargetRoute;

        public bool isOpen;

        public void SetupCallback()
        {
            foreach (AbstractPointSetter setter in TargetPoints)
            {
                UdonSharpBehaviour[] newArray = new UdonSharpBehaviour[setter.callbackUdons.Length + 1];
                setter.callbackUdons.CopyTo(newArray, 0);
                newArray[setter.callbackUdons.Length] = this;
                setter.callbackUdons = newArray;
            }
        }
        void Start()
        {
            SetupCallback();
            PointUpdate();
        }

        public virtual void PointUpdate()//Call via AbstractPointSetter.callbackUdons
        {
            isOpen = true;

            int count = 0;
            foreach (AbstractPointSetter setter in TargetPoints)
            {
                if (TargetRoute[count] != setter.get_current_To())
                {
                    isOpen = false;
                    break;
                }
                count++;
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            int count = 0;
            if (TargetPoints != null)
            {
                foreach (AbstractPointSetter setter in TargetPoints)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 1f);
                    setter.DrawGizmo_From();
                    Gizmos.color = new Color(0f, 1f, 1f, 1f);
                    setter.DrawGizmo_To(TargetRoute[count]);
                    count++;
                }
            }
        }
#endif
    }
}