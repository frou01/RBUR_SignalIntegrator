
using frou01.GrabController;
using UnityEngine;
using VRC.SDKBase;

namespace RBUR_SignalIntegrator
{
    public class GACLockerConsolidater : AbstractLockerConsolidater
    {
        [SerializeField] protected float[] lockPositions;
        [SerializeField] protected Controller_Base target;



        //Return: Controller positon (On Analog controller, return nearest lock point)
        public override int GetCurrentPosition()
        {
            float controllerPos = target.controllerPosition;
            float diffhistr = float.MaxValue;
            int res = -1;
            for (int idx = 0; idx < lockPositions.Length; idx++)
            {
                float diff = Mathf.Abs(lockPositions[idx] - controllerPos);
                if (diff < diffhistr)
                {
                    diffhistr = diff;
                    res = idx;
                }
            }
            return res;
        }

        protected override void applyLockToController(bool state)
        {
            target.locked = state;
        }
        protected override void applyPositionToController(int posIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(applyPositionToController));
            if (target.controllerPosition_Exposed[0] != lockPositions[posIndex])
            {
                target.SetPosition(lockPositions[posIndex]);
                SyncController();
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(applyPositionToController));
        }
        public override bool isControllerOwner()
        {
            return Networking.IsOwner(target.gameObject);
        }
        public override void setControllerOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, target.gameObject);
        }
        public override void SyncController()
        {
            if (isControllerOwner())
            {
                target.RequestSerialization();
            }
        }
    }
}
