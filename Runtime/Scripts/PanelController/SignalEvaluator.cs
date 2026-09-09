using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class SignalEvaluator : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [SerializeField] protected UdonSharpBehaviour[] senderInstance;
        [SerializeField] protected int[] ReceiveSignal;

        [Tooltip("False: Prioritize the minimum, True: Prioritize the MAXIMUM")][SerializeField] bool mode; 
        [SerializeField] protected Animator[] SignalSideAnimators;
        [SerializeField] protected string signalAnimationParamater = "SignalRelayPosition";
        [SerializeField] protected int signalLargestLevel;

        [SerializeField] protected SignalEvaluator[] childEvaluators;

        public virtual void updateSignal(UdonSharpBehaviour callingInstance,int signal)
        {
            updateSignal(callingInstance, signal, out int triedIndex);
        }
        public virtual void updateSignal(UdonSharpBehaviour callingInstance, int signal, out int triedIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(updateSignal));
            getCallingBehaviourIndex(callingInstance,out triedIndex);
            if (senderInstance.Length == triedIndex)
            {
                AddNewSender(callingInstance);
            }
            ReceiveSignal[triedIndex] = signal;

            int currentOutSig = mode ? int.MinValue : int.MaxValue;
            foreach(int recieved in ReceiveSignal)
            {
                if (mode)
                {
                    if(recieved > currentOutSig)
                    {
                        currentOutSig = recieved;
                    }
                }
                else
                {
                    if (recieved < currentOutSig)
                    {
                        currentOutSig = recieved;
                    }
                }
            }

            foreach (Animator animator in SignalSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(signalAnimationParamater, (float)currentOutSig / signalLargestLevel);
            }

            foreach (SignalEvaluator Evaluator in childEvaluators)
            {
                Evaluator.updateSignal(this, currentOutSig);
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(updateSignal));
        }
        protected void getCallingBehaviourIndex(UdonSharpBehaviour triedFrom, out int triedIndex)
        {
            triedIndex = 0;
            foreach (UdonSharpBehaviour locker in senderInstance)
            {
                triedIndex++;
                if (triedFrom == locker)
                {
                    triedIndex--;
                    break;
                }
            }
        }
        public virtual bool AddNewSender(UdonSharpBehaviour triedFrom)
        {
            foreach (UdonSharpBehaviour locker in senderInstance)
            {
                if (triedFrom == locker) return false;
            }

            UdonSharpBehaviour[] newSenderScripts = new UdonSharpBehaviour[senderInstance.Length + 1];
            senderInstance.CopyTo(newSenderScripts, 0);
            newSenderScripts[senderInstance.Length] = triedFrom;
            senderInstance = newSenderScripts;

            int[] newReceiveSignal = new int[ReceiveSignal.Length + 1];
            ReceiveSignal.CopyTo(newReceiveSignal, 0);
            newReceiveSignal[ReceiveSignal.Length] = 0;
            ReceiveSignal = newReceiveSignal;

            return true;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in SignalSideAnimators)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), animator.name, guiStyle);
            }
            foreach (SignalEvaluator Evaluator in childEvaluators)
            {
                Gizmos.color = new Color(0.5f, 1f, 0f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(Evaluator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(Evaluator.transform), 0.8f), Evaluator.name, guiStyle);
            }
        }
#endif
    }
}