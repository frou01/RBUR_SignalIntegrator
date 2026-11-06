using UnityEngine;
using UnityEngine.Serialization;
using frou01.util;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
#endif

namespace frou01.RBUR.SignalIntegrator
{
    public class SignalController : AbstractIndirectController
    {
        [FormerlySerializedAs("SignalSideAnimators")][SerializeField] Animator[] signalSideAnimators;
        [FormerlySerializedAs("signalAnimationParamater")][SerializeField] string signalAnimationParamName = "SignalRelayPosition";
        [SerializeField] int signalLargestLevel;
        [FormerlySerializedAs("Evaluators")][SerializeField] protected SignalEvaluator[] evaluators;

        protected override void ApplyPositionToController(int posIndex)
        {

            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(ApplyPositionToController));
            base.ApplyPositionToController(posIndex);

            if (isLogging) Debug.Log("Debug [on " + this.name + "," + nameof(ApplyPositionToController) + "] : controllingPosition = " + controllingPosition, this);
            foreach (Animator animator in signalSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(signalAnimationParamName, (float)controllingPosition / signalLargestLevel);
            }
            foreach (SignalEvaluator evaluator in evaluators)
            {
                evaluator.UpdateSignal(this, controllingPosition);
            }
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(ApplyPositionToController));
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base .OnDrawGizmosSelected();
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in signalSideAnimators)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), animator.name, guiStyle);
            }
            foreach (SignalEvaluator evaluator in evaluators)
            {
                Gizmos.color = new Color(0.5f, 1f, 0f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(evaluator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(evaluator.transform), 0.8f), evaluator.name, guiStyle);
                evaluator.OnDrawGizmosSelected();
            }
        }
#endif
    }
}