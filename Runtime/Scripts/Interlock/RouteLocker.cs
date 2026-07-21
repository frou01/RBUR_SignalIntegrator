using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class RouteLocker : RouteChecker
    {
        [HideInInspector][SerializeField] protected AbstractLockerConsolidater[] lockers;
        [HideInInspector][SerializeField] protected int[] lockPositions;
        [HideInInspector][SerializeField] protected Interlocking interlocking;

        public void setParentInterlock(Interlocking val)
        {
            interlocking = val;
        }

        public override void Start()
        {
            base.Start();
        }

        public bool UpdateLockRoute(bool State)
        {
            if (!State)
            {
                foreach (AbstractLockerConsolidater locker in lockers)
                {
                    locker.tryUpdateLocking(this,false,0);
                }
                return true;
            }
            else
            {
                if (!isRouteOpen()) return false;

                bool result = true;

                foreach (AbstractLockerConsolidater locker in lockers)
                {
                    result &= locker.tryUpdateLocking(this, true, locker.GetCurrentPosition());
                }

                return result;
            }
        }

        public void SetupLocker()
        {
            foreach(AbstractLockerConsolidater locker in lockers)
            {
                locker.AddNewLocker(this);
            }
        }


        public void RelayFailSafe()
        {
            if (!interlocking.FailSafeCalled)
                foreach (AbstractLockerConsolidater locker in lockers)
                {
                    locker.RelayFailSafe();
                }
        }
    }
}