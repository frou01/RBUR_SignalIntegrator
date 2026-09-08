
using UnityEngine;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using frou01.RigidBodyTrain;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using static RBUR_SignalIntegrator.MultiLever_SwitchToControlMapsHolder;
using static RBUR_SignalIntegrator.PointLever_ControlToRouteIndexHolder;
#endif

namespace RBUR_SignalIntegrator
{
    public class MultiLever_ReferRouteLocker : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] int targetMulConSwitch;

        private static int GetControlFromRoute(PointControllerLever pointController,AbstractPointSetter pointSetter,int routeIndex)
        {
            List<Control_To_RouteIndexMap> controlToRoute = pointController.GetComponent<PointLever_ControlToRouteIndexHolder>().control_To_RouteIndexMaps;
            Control_To_RouteIndexMap targetedMap = controlToRoute.First(val => val.linkedPoint == pointSetter);
            if (targetedMap != null)
            {
                return targetedMap.Control_To_RouteIndex.Select((x,i)=>(x,i)).FirstOrDefault(map => map.x == routeIndex).i;
            }
            else
            {
                return -1;
            }
        }
        private static void SetControlToMap(MultiLever_SwitchToControlMapsHolder switchMapHolder, PointControllerLever pointController, int TargetControl, int targetMulConSwitch)
        {
            List<Control_To_RouteIndexMap> controlToRoute = pointController.GetComponent<PointLever_ControlToRouteIndexHolder>().control_To_RouteIndexMaps;
            To_ControllerMap map = switchMapHolder.Switch_To_ControlMaps.FirstOrDefault(val => val.linkedController == pointController);
            if (map != null)
            {
                map.Switch_To_Control[targetMulConSwitch] = TargetControl;
            }
        }

        public void ApplyToControlMap()
        {
            Scene scene = gameObject.scene;
            List<PointControllerLever> pointControllers = new List<PointControllerLever>();
            List<Interlocking> interlocks = new List<Interlocking>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                pointControllers.AddRange(obj.GetComponentsInChildren<PointControllerLever>(true));
                interlocks.AddRange(obj.GetComponentsInChildren<Interlocking>(true));
            }
            MultiLeverController mulCon = GetComponent<MultiLeverController>();
            foreach (SignalControllerLever signalControllerLever in mulCon.controlledLevers.Where(val => val is SignalControllerLever))
            {
                Interlocking correspondInterlock = interlocks.FirstOrDefault(val => val.GetFromLocker() == signalControllerLever);
                ApplyToControlMap(mulCon,pointControllers, correspondInterlock.GetRouteLocker());
            }
        }
        public void ApplyToControlMap(MultiLeverController controller, List<PointControllerLever> pointControllers,RouteLocker routeLocker)
        {
            if (controller && routeLocker)
            {
                int pointIdx = 0;
                int[] targetRoutes = routeLocker.getTargetRoute();
                foreach (AbstractPointSetter targetPoint in routeLocker.GetTargetPoints())
                {
                    PointControllerLever pointCorrespondController = pointControllers.FirstOrDefault(val => val.getPointInstances().Contains(targetPoint));
                    if (pointCorrespondController)
                    {
                        MultiLever_SwitchToControlMapsHolder switchMapHolder = controller.GetComponent<MultiLever_SwitchToControlMapsHolder>();
                        if (!controller.controlledLevers.Contains(pointCorrespondController))
                        {
                            controller.controlledLevers = controller.controlledLevers.AddItem(pointCorrespondController).ToArray();
                            int[] MultileverControlMap = Enumerable.Repeat(-1, switchMapHolder.switchPositionNum).ToArray();
                            switchMapHolder.Switch_To_ControlMaps.Add(new To_ControllerMap(-1, pointCorrespondController, MultileverControlMap));
                            EditorUtility.SetDirty(controller);
                        }
                        int controlTarget = GetControlFromRoute(pointCorrespondController, targetPoint, targetRoutes[pointIdx]);
                        if(controlTarget != -1)
                        {
                            SetControlToMap(switchMapHolder, pointCorrespondController, controlTarget, targetMulConSwitch);
                        }
                        EditorUtility.SetDirty(switchMapHolder);
                    }

                    pointIdx++;
                }
            }
        }
        [CustomEditor(typeof(MultiLever_ReferRouteLocker)), CanEditMultipleObjects]
        public class MultiLever_ReferRouteLocker_CustomEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                MultiLever_ReferRouteLocker instance = target as MultiLever_ReferRouteLocker;
                base.OnInspectorGUI();
                if (GUILayout.Button("Apply to ControlMap"))
                {
                    instance.ApplyToControlMap();
                    foreach (MultiLever_ReferRouteLocker childInstance in targets)
                    {
                        childInstance.ApplyToControlMap();
                    }
                }
            }
        }
#endif
    }
}
