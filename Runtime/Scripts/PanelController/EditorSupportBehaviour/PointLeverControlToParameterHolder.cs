#if !COMPILER_UDONSHARP && UNITY_EDITOR
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#endif
using UnityEngine;
using UnityEngine.Serialization;
using static frou01.RBUR.SignalIntegrator.PointLeverControlToRouteIndexHolder;

namespace frou01.RBUR.SignalIntegrator
{
    [ExecuteAlways]
    public class PointLeverControlToParameterHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [FormerlySerializedAs("Route_To_ParamaterMaps")]
        [FormerlySerializedAs("Control_To_ParamaterMaps")][SerializeField] public List<ToParameter> controlToParameterMaps;
        public void ApplyToUdonBehaviour()
        {
            PointController pointController = GetComponent<PointController>();
            if (pointController == null)
            {
                Debug.LogError("PointController not found on the same GameObject.", this);
                return;
            }
            pointController.SetControlToParameterMap(GetControlToParameterMap());
            pointController.SetAttachedPointAnimator(GetAttachedAnimators());
        }
        public float[][] GetControlToParameterMap()
        {
            return controlToParameterMaps
                .Select(val => val.controlToParameter)//List<ToControllerMap> -> List<int[]>
                .ToArray();
        }
        public Animator[] GetAttachedAnimators()
        {
            return controlToParameterMaps
                .Select(val => val.linkedAnimator)//List<ToControllerMap> -> List<int[]>
                .ToArray();
        }


        [System.Serializable]
        public class ToParameter
        {
            [SerializeField] public Animator linkedAnimator;
            [FormerlySerializedAs("Route_To_Paramater")]
            [FormerlySerializedAs("Control_To_Paramater")][SerializeField] public float[] controlToParameter;

            public ToParameter(Animator linkedAnimator, float[] controlToParameter)
            {
                this.linkedAnimator = linkedAnimator;
                this.controlToParameter = controlToParameter;
            }
        }
#endif
    }
}
