using frou01.RigidBodyTrain;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#endif
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    [ExecuteAlways]
    public class PointLever_ControlToRouteIndexHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] public List<Control_To_RouteIndexMap> control_To_RouteIndexMaps;
        public int[][] get_Control_to_RouteIndex_Map()
        {
            return control_To_RouteIndexMaps
                .Select(val => val.Control_To_RouteIndex)//List<To_ControllerMap> -> List<int[]>
                .ToArray();
        }

        [SerializeField] public int RouteIndexNum;

        public void Update()
        {
            //TODO RouteIndexNumに合わせてControl_To_RouteIndexの配列長を揃える処理を入れる
            bool isDirty = false;
            if (isDirty)
            {
                EditorUtility.SetDirty(this);
            }
        }
        [System.Serializable]
        public class Control_To_RouteIndexMap
        {
            [SerializeField] public AbstractPointSetter linkedPoint;
            [SerializeField] public int[] Control_To_RouteIndex;

            public Control_To_RouteIndexMap(AbstractPointSetter linkedPoint, int[] Control_To_RouteIndexMap)
            {
                this.linkedPoint = linkedPoint;
                this.Control_To_RouteIndex = Control_To_RouteIndexMap;
            }
        }
#endif
    }
}
