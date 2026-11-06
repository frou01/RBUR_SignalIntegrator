
using frou01.GrabController;
using UnityEngine;
using VRC.SDKBase;

namespace frou01.RBUR.SignalIntegrator
{
    public class GACLockerConsolidator : AbstractLockerConsolidator
    {
        [SerializeField] private protected float[] lockPositions;
        [SerializeField] private protected Controller_Base target;

        public Controller_Base GetGACController()
        {
            return target;
        }

        //Return: Controller positon (On Analog controller, return nearest lock point)
        public override int GetCurrentPosition()
        {
            float controllerPos = target.controllerPosition;
            float minDiff = float.MaxValue;
            int res = -1;
            for (int idx = 0; idx < lockPositions.Length; idx++)
            {
                float diff = Mathf.Abs(lockPositions[idx] - controllerPos);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    res = idx;
                }
            }
            return res;
        }

        protected override void ApplyLockToController(bool state)
        {
            target.locked = state;
        }
        protected override void ApplyPositionToController(int posIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(ApplyPositionToController));
            if (target.controllerPosition_Exposed[0] != lockPositions[posIndex])
            {
                target.SetPosition(lockPositions[posIndex]);
                SyncController();
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(ApplyPositionToController));
        }
        public override bool IsControllerOwner()
        {
            return Networking.IsOwner(target.gameObject);
        }
        public override void SetControllerOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, target.gameObject);
        }
        protected override void SyncController()
        {
            if (IsControllerOwner())
            {
                target.RequestSerialization();
            }
        }
    }
}
