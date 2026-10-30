#if !COMPILER_UDONSHARP && UNITY_EDITOR
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#endif
using UnityEngine;
using UnityEngine.Serialization;
using static RBUR_SignalIntegrator.PointLever_ControlToRouteIndexHolder;

namespace RBUR_SignalIntegrator
{
    [ExecuteAlways]
    public class PointLever_ControlToParamaterHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [FormerlySerializedAs("Route_To_ParamaterMaps")]
        [SerializeField] List<To_Paramater> Control_To_ParamaterMaps;
        public float[][] get_Control_to_Paramater_Map()
        {
            return Control_To_ParamaterMaps
                .Select(val => val.Control_To_Paramater)//List<To_ControllerMap> -> List<int[]>
                .ToArray();
        }
        

        [System.Serializable]
        class To_Paramater
        {
            [SerializeField] public int onPointConOrder;
            [SerializeField] public Animator linkedAnimator;
            [FormerlySerializedAs("Route_To_Paramater")]
            [SerializeField] public float[] Control_To_Paramater;

            public To_Paramater(int onPointConOrder, Animator linkedAnimator, float[] Control_To_Paramater)
            {
                this.onPointConOrder = onPointConOrder;
                this.linkedAnimator = linkedAnimator;
                this.Control_To_Paramater = Control_To_Paramater;
            }
        }
#endif
    }
}
