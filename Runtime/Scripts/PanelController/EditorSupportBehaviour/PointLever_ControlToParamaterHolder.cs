#if !COMPILER_UDONSHARP && UNITY_EDITOR
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#endif
using UnityEngine;
using static RBUR_SignalIntegrator.PointLever_ControlToRouteIndexHolder;

namespace RBUR_SignalIntegrator
{
    [ExecuteAlways]
    public class PointLever_RouteIndexToParamaterHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] List<To_Paramater> Control_To_ParamaterMaps;
        public float[][] get_Control_to_Paramater_Map()
        {
            return Control_To_ParamaterMaps
                .Select(val => val.Control_To_Paramater)//List<To_ControllerMap> -> List<int[]>
                .ToArray();
        }


        public void Update()
        {
            PointLever_ControlToRouteIndexHolder routeIndexes = GetComponent<PointLever_ControlToRouteIndexHolder>();
            if (!routeIndexes) return;
            routeIndexes.Update();
            int RouteIndexNum = routeIndexes.RouteIndexNum;
            PointControllerLever TargetPointCon;
            TargetPointCon = GetComponent<PointControllerLever>();
            Dictionary<Animator, To_Paramater> synced_Animator_To_Paramater_Map = new Dictionary<Animator, To_Paramater>();
            foreach (To_Paramater syncedControllerMap in Control_To_ParamaterMaps)
            {
                if (syncedControllerMap.linkedAnimator != null && !synced_Animator_To_Paramater_Map.ContainsKey(syncedControllerMap.linkedAnimator))
                    synced_Animator_To_Paramater_Map.Add(syncedControllerMap.linkedAnimator, syncedControllerMap);
            }


            List<To_Paramater> SyncingControl_To_ParamaterMap = new List<To_Paramater>();
            bool isDirty = false;
            int idx = 0;
            foreach (Animator assignedAnimator in TargetPointCon.attachedPointAnimator())
            {
                To_Paramater to_Paramater;
                if (synced_Animator_To_Paramater_Map.TryGetValue(assignedAnimator, out to_Paramater))
                {
                    to_Paramater.onPointConOrder = idx;
                    if (to_Paramater.Control_To_Paramater.Length > RouteIndexNum)
                    {
                        to_Paramater.Control_To_Paramater = to_Paramater.Control_To_Paramater
                            .Select((mapArray, i) => (toConMap: mapArray, i))//get index
                            .Where(val => val.i < RouteIndexNum)//cut index
                            .Select(val => val.toConMap).ToArray();
                        isDirty |= true;
                    }
                    else if (to_Paramater.Control_To_Paramater.Length < RouteIndexNum)
                    {
                        to_Paramater.Control_To_Paramater = to_Paramater.Control_To_Paramater.AddRangeToArray(new float[RouteIndexNum - to_Paramater.Control_To_Paramater.Length]);
                        isDirty |= true;
                    }
                    SyncingControl_To_ParamaterMap.Add(to_Paramater);
                }
                else
                {
                    Control_To_RouteIndexMap routeIndexMap = routeIndexes.control_To_RouteIndexMaps.FirstOrDefault(indexMap => indexMap.linkedPoint.gameObject == assignedAnimator.gameObject);
                    if (assignedAnimator != null)
                        if (routeIndexMap == null)
                        {
                            SyncingControl_To_ParamaterMap.Add(new To_Paramater(idx, assignedAnimator,
                                (new float[RouteIndexNum])
                                .Select((val, i) => (val, i))
                                .Select(val => (float)val.i / (RouteIndexNum > 0 ? (RouteIndexNum - 1) : 1))
                                .ToArray()
                                ));
                            isDirty |= true;
                        }
                        else
                        {
                            SyncingControl_To_ParamaterMap.Add(new To_Paramater(idx, assignedAnimator,
                                (new float[RouteIndexNum])
                                .Select((val, i) => (val, i))
                                .Select(val => (float)routeIndexMap.Control_To_RouteIndex[val.i]/ (RouteIndexNum > 0 ? (RouteIndexNum - 1) : 1))
                                .ToArray()
                                ));
                            isDirty |= true;
                        }
                }
                idx++;
            }
            if (isDirty)
            {
                SyncingControl_To_ParamaterMap.OrderBy(toConMap => toConMap.onPointConOrder);
                Control_To_ParamaterMaps = SyncingControl_To_ParamaterMap;
                EditorUtility.SetDirty(this);
            }
        }

        [System.Serializable]
        class To_Paramater
        {
            [SerializeField] public int onPointConOrder;
            [SerializeField] public Animator linkedAnimator;
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
