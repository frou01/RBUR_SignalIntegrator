using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class Interlock_ToLockerAndMeetPosition : UdonSharpBehaviour
    {
        [SerializeField] public EventStackHolder eventStackHolder;
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
            foreach (int meet in MeetPositionIndex)
            {
                if (locker.GetCurrentPosition() == meet)
                {
                    return true;
                }
            }
            return false;
        }

        //return: Sucess?
        public virtual bool UpdateLock(bool isLocking)
        {
            eventStackHolder.AddStack(this, nameof(UpdateLock));
            bool res;
            if (Check())
            {
                res = locker.tryUpdateLocking(this, isLocking, locker.GetCurrentPosition());
            }
            else
            {
                if (isLocking && !locker.isControllerOwner()) res = false;
                else
                {
                    res = locker.tryUpdateLocking(this, isLocking, MeetPositionIndex[0]);
                }
            }
            eventStackHolder.RemoveStack(this, nameof(UpdateLock));
            return res;
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