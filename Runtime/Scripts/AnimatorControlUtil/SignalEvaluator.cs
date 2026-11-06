using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class SignalEvaluator : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [FormerlySerializedAs("senderInstance")][HideInInspector][SerializeField] protected UdonSharpBehaviour[] senderInstances = new UdonSharpBehaviour[0];
        [FormerlySerializedAs("ReceiveSignal")][HideInInspector][SerializeField] protected float[] receivedSignals = new float[0];

        [FormerlySerializedAs("mode")][Tooltip("False: Prioritize the minimum, True: Prioritize the MAXIMUM")][SerializeField] bool shouldPrioritizeMaximum; 
        [FormerlySerializedAs("SignalSideAnimators")][SerializeField] protected Animator[] signalSideAnimators;
        [FormerlySerializedAs("signalAnimationParamater")][SerializeField] protected string signalAnimationParamName = "SignalRelayPosition";
        [FormerlySerializedAs("SignalMultiplier")][SerializeField] protected float signalMultiplier = 1;

        [SerializeField] protected SignalEvaluator[] childEvaluators;

        public virtual void UpdateSignal(UdonSharpBehaviour callingInstance,float signal)
        {
            UpdateSignal(callingInstance, signal, out int triedIndex);
        }
        public virtual void UpdateSignal(UdonSharpBehaviour callingInstance, float signal, out int triedIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateSignal));
            GetCallingBehaviourIndex(callingInstance,out triedIndex);
            if (senderInstances.Length == triedIndex)
            {
                AddNewSender(callingInstance);
            }
            receivedSignals[triedIndex] = signal;

            int currentOutSig = shouldPrioritizeMaximum ? int.MinValue : int.MaxValue;
            foreach(int received in receivedSignals)
            {
                if (shouldPrioritizeMaximum)
                {
                    if(received > currentOutSig)
                    {
                        currentOutSig = received;
                    }
                }
                else
                {
                    if (received < currentOutSig)
                    {
                        currentOutSig = received;
                    }
                }
            }

            foreach (Animator animator in signalSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(signalAnimationParamName, currentOutSig * signalMultiplier);
            }

            foreach (SignalEvaluator evaluator in childEvaluators)
            {
                evaluator.UpdateSignal(this, currentOutSig * signalMultiplier);
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateSignal));
        }
        protected void GetCallingBehaviourIndex(UdonSharpBehaviour triedFrom, out int triedIndex)
        {
            triedIndex = 0;
            foreach (UdonSharpBehaviour locker in senderInstances)
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
            if (eventStackHolder != null) eventStackHolder.AddStack(this, nameof(AddNewSender));
            foreach (UdonSharpBehaviour locker in senderInstances)
            {
                if (triedFrom == locker)
                {
                    if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(AddNewSender));
                    return false;
                }
            }

            UdonSharpBehaviour[] newSenderScripts = new UdonSharpBehaviour[senderInstances.Length + 1];
            senderInstances.CopyTo(newSenderScripts, 0);
            newSenderScripts[senderInstances.Length] = triedFrom;
            senderInstances = newSenderScripts;

            float[] newReceiveSignal = new float[senderInstances.Length];
            receivedSignals.CopyTo(newReceiveSignal, 0);
            newReceiveSignal[receivedSignals.Length] = 0;
            receivedSignals = newReceiveSignal;

            if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(AddNewSender));
            return true;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in signalSideAnimators)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), animator.name, guiStyle);
            }
            foreach (SignalEvaluator evaluator in childEvaluators)
            {
                Gizmos.color = new Color(0.5f, 1f, 0f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(evaluator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(evaluator.transform), 0.8f), evaluator.name, guiStyle);
            }
        }
#endif
    }
}