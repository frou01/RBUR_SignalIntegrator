using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class SignalEvaluator : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [HideInInspector][SerializeField] protected UdonSharpBehaviour[] senderInstance = new UdonSharpBehaviour[0];
        [HideInInspector][SerializeField] protected float[] ReceiveSignal = new float[0];

        [Tooltip("False: Prioritize the minimum, True: Prioritize the MAXIMUM")][SerializeField] bool mode; 
        [SerializeField] protected Animator[] SignalSideAnimators;
        [SerializeField] protected string signalAnimationParamater = "SignalRelayPosition";
        [SerializeField] protected float SignalMultiplier = 1;

        [SerializeField] protected SignalEvaluator[] childEvaluators;

        public virtual void UpdateSignal(UdonSharpBehaviour callingInstance,float signal)
        {
            UpdateSignal(callingInstance, signal, out int triedIndex);
        }
        public virtual void UpdateSignal(UdonSharpBehaviour callingInstance, float signal, out int triedIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateSignal));
            GetCallingBehaviourIndex(callingInstance,out triedIndex);
            if (senderInstance.Length == triedIndex)
            {
                AddNewSender(callingInstance);
            }
            ReceiveSignal[triedIndex] = signal;

            int currentOutSig = mode ? int.MinValue : int.MaxValue;
            foreach(int received in ReceiveSignal)
            {
                if (mode)
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

            foreach (Animator animator in SignalSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(signalAnimationParamater, currentOutSig * SignalMultiplier);
            }

            foreach (SignalEvaluator evaluator in childEvaluators)
            {
                evaluator.UpdateSignal(this, currentOutSig * SignalMultiplier);
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateSignal));
        }
        protected void GetCallingBehaviourIndex(UdonSharpBehaviour triedFrom, out int triedIndex)
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
            if (eventStackHolder != null) eventStackHolder.AddStack(this, nameof(AddNewSender));
            foreach (UdonSharpBehaviour locker in senderInstance)
            {
                if (triedFrom == locker)
                {
                    if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(AddNewSender));
                    return false;
                }
            }

            UdonSharpBehaviour[] newSenderScripts = new UdonSharpBehaviour[senderInstance.Length + 1];
            senderInstance.CopyTo(newSenderScripts, 0);
            newSenderScripts[senderInstance.Length] = triedFrom;
            senderInstance = newSenderScripts;

            float[] newReceiveSignal = new float[senderInstance.Length];
            ReceiveSignal.CopyTo(newReceiveSignal, 0);
            newReceiveSignal[ReceiveSignal.Length] = 0;
            ReceiveSignal = newReceiveSignal;

            if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(AddNewSender));
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