using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class InterlockStateLocker : UdonSharpBehaviour
    {
        [SerializeField] protected AbstractLockerConsolidater locker;
        [SerializeField] protected int[] MeetPositionIndex;
        [HideInInspector][SerializeField] protected Interlocking interlocking;
        public AbstractLockerConsolidater getLocker()
        {
            return locker;
        }
        public void setParentInterlock(Interlocking val)
        {
            interlocking = val;
        }
        public virtual bool Check()
        {
            foreach(int meet in MeetPositionIndex)
            {
                if (locker.GetCurrentPosition() == meet) return true;
            }
            return false;
        }

        //return: Sucess?
        public virtual bool UpdateLock(bool isLocking)
        {
            if (Check())
            {
                return locker.tryUpdateLocking(this, isLocking, locker.GetCurrentPosition());
            }
            else
            {
                return locker.tryUpdateLocking(this, isLocking, MeetPositionIndex[0]);
            }
        }
        public void RelayFailSafe()
        {
            if (!interlocking.FailSafeCalled)
                locker.RelayFailSafe();
        }
        public void SetupLocker()
        {
            locker.AddNewLocker(this);
        }
    }
}