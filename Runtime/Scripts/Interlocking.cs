
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class Interlocking : UdonSharpBehaviour
    {
        [System.NonSerialized] public Animator signalAnimator;
        [SerializeField] public Controller_Base signalController;
        [SerializeField] public string signalParamator;
        [SerializeField] public float signalGo;
        [SerializeField] public float signalStop;
        [SerializeField] public bool lockByPoint;
        [System.NonSerialized] public Animator[] pointAnimator;
        [SerializeField] public GACLockerConsolidater[] pointLocker;
        int[] LockerID;
        [SerializeField] public float[] pointThisLine;
        [SerializeField] public float[] pointOtherLine;
        [SerializeField] public string[] pointParamator;

        void Start()
        {
            signalAnimator = signalController.TargetAnimator;
            pointAnimator = new Animator[pointLocker.Length];
            for (int index = 0; index < pointLocker.Length; index++)
            {
                pointAnimator[index] = pointLocker[index].target.TargetAnimator;
            }
            LockerID = new int[pointLocker.Length];
            for (int index = 0; index < pointLocker.Length; index++)
            {
                int rcversLen = 0;
                if (pointLocker[index].RecievedBools != null)
                {
                    rcversLen = pointLocker[index].RecievedBools.Length;
                }
                pointLocker[index].RecievedBools = new bool[rcversLen + 1];
                LockerID[index] = rcversLen;
            }
            LockPerform();
        }
        public void LockPerform()
        {
            signalAnimator.enabled = true;
            float currentSignal = signalAnimator.GetFloat(signalParamator);
            Debug.Log("currentSignal " + currentSignal);
            if ((signalStop - currentSignal) * (signalStop - currentSignal) < (signalGo - currentSignal) * (signalGo - currentSignal))
            {
                for (int index = 0; index < pointAnimator.Length; index++)
                {
                    pointLocker[index].perform(LockerID[index], false);
                }
                Debug.Log("Point UnLock");
            }
            else
            {
                for (int index = 0; index < pointAnimator.Length; index++)
                {
                    pointLocker[index].perform(LockerID[index], true);
                }
                Debug.Log("Point Lock");
            }
            if (lockByPoint)
            {
                bool released = true;

                for (int index = 0; index < pointAnimator.Length; index++)
                {
                    pointAnimator[index].enabled = true;
                    float currentPoint = pointAnimator[index].GetFloat(pointParamator[index]);
                    Debug.Log("currentPoint " + currentPoint);
                    if ((pointThisLine[index] - currentPoint) * (pointThisLine[index] - currentPoint) < (pointOtherLine[index] - currentPoint) * (pointOtherLine[index] - currentPoint))
                    {
                        released &= true;
                    }
                    else
                    {
                        released &= false;
                        break;
                    }
                }
                Debug.Log("signalLever Release " + released);
                if (!released)
                {
                    signalController.SetPosition(signalStop);
                }
                signalController.locked = !released;
            }
        }
        void OnDrawGizmos()
        {
        }


        void OnDrawGizmosSelected()
        {
            if (signalController != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 1f);
                Gizmos.DrawLine(signalController.TargetAnimator.transform.position, this.transform.position);
            }
            if (signalController != null)
            {
                Gizmos.color = new Color(1f, 0f, 1f, 1f);
                Gizmos.DrawLine(signalController.TargetAnimator.transform.position, signalController.transform.position);
            }
            Gizmos.color = new Color(1f, 1f, 0f, 1f);
            for (int index = 0; index < pointLocker.Length; index++)
            {
                Gizmos.DrawLine(this.transform.position, pointLocker[index].target.TargetAnimator.transform.position);
            }
            Gizmos.color = new Color(0.2f, 0.2f, 1f, 1f);
            for (int index = 0; index < pointLocker.Length; index++)
            {
                Gizmos.DrawLine(pointLocker[index].target.TargetAnimator.transform.position, pointLocker[index].target.transform.position);
            }
        }

        public void OnPickup_()
        {
            LockPerform();
        }
        public void OnDrop_()
        {
            LockPerform();
        }
    }

}