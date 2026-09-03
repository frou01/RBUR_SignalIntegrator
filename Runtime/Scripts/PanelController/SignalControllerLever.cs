using UnityEngine;
using frou01.util;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
#endif

namespace RBUR_SignalIntegrator
{
    public class SignalControllerLever : AbstractPanelController
    {
        [SerializeField] Animator[] SignalSideAnimators;
        [SerializeField] string signalAnimationParamater = "SignalRelayPosition";
        [SerializeField] int signalLargestLevel;
        [SerializeField] protected SignalEvaluator[] Evaluators;

        private protected override void applyPositionToController(int posIndex)
        {

            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(applyPositionToController));
            base.applyPositionToController(posIndex);

            foreach (Animator animator in SignalSideAnimators)
            {
                AnimatorSleeper sleeper = animator.GetComponentInChildren<AnimatorSleeper>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(signalAnimationParamater, (float)controllingPosition / signalLargestLevel);
            }
            foreach (SignalEvaluator Evaluator in Evaluators)
            {
                Evaluator.updateSignal(this, controllingPosition);
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(applyPositionToController));
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base .OnDrawGizmosSelected();
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in SignalSideAnimators)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), this.gameObject.name + ".SignalSideAnimator", guiStyle);
            }
            foreach (SignalEvaluator Evaluator in Evaluators)
            {
                Gizmos.color = new Color(0.5f, 1f, 0f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(Evaluator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(Evaluator.transform), 0.8f), this.gameObject.name + ".SignalEvaluator", guiStyle);
            }
        }
#endif
    }
}