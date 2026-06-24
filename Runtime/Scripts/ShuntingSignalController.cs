
using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    public class ShuntingSignalController : UdonSharpBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] string signalPramName;
        [SerializeField] PointLever_Setter[] targetPoints;
        [SerializeField] bool[] points_OpenState;

        public bool isOpen;

        void Start()
        {
            foreach (PointLever_Setter setter in targetPoints)
            {
                UdonSharpBehaviour[] newArray = new UdonSharpBehaviour[setter.callbackUdons.Length + 1];
                setter.callbackUdons.CopyTo(newArray, 0);
                newArray[setter.callbackUdons.Length] = this;
                setter.callbackUdons = newArray;
            }
            PointUpdate();
        }

        public void PointUpdate()//Call via PointLever_Setter.callbackUdons
        {
            isOpen = true;

            int count = 0;
            foreach (PointLever_Setter setter in targetPoints)
            {
                if (points_OpenState[count] != setter.state)
                {
                    isOpen = false;
                    break;
                }
                count++;
            }
            updateAnimator();
        }

        private void updateAnimator()
        {
            animator.SetBool(signalPramName, isOpen);
        }


#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            int count = 0;
            foreach (PointLever_Setter setter in targetPoints)
            {
                setter.DrawGizmo(1, !points_OpenState[count], points_OpenState[count]);
                count++;
            }
        }
#endif
    }

}