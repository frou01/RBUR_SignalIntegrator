
using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    public class ShuntingSignalController : RouteChecker
    {
        [SerializeField] Animator animator;
        [SerializeField] string signalPramName;

        public override void PointUpdate()//Call via PointLever_Setter.callbackUdons
        {
            base.PointUpdate();
            updateAnimator();
        }

        private void updateAnimator()
        {
            animator.SetBool(signalPramName, isRouteOpen());
        }
    }

}