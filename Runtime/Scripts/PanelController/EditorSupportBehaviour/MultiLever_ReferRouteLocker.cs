
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
    /// <summary>
    /// 進路てこに対し、Interlockの進路条件を取得しててこ設定に変換する
    /// </summary>
    public class MultiLever_ReferRouteLocker : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] int targetMulConSwitch;

        private static int GetControlFromRoute(PointController pointController, AbstractPointSetter pointSetter, int routeIndex)
        {
            List<Control_To_RouteIndexMap> controlToRoute = pointController.GetComponent<PointLever_ControlToRouteIndexHolder>().control_To_RouteIndexMaps;
            Control_To_RouteIndexMap targetedMap = controlToRoute.First(val => val.linkedPoint == pointSetter);
            if (targetedMap != null)
            {
                return targetedMap.Control_To_RouteIndex.Select((x, i) => (x, i)).FirstOrDefault(map => map.x == routeIndex).i;
            }
            else
            {
                return -1;
            }
        }
        private static void SetControlToMap(MultiLever_SwitchToControlMapsHolder switchMapHolder, PointController pointController, int TargetControl, int targetMulConSwitch)
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
            List<PointController> pointControllers = new List<PointController>();
            List<Interlocking> interlocks = new List<Interlocking>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                pointControllers.AddRange(obj.GetComponentsInChildren<PointController>(true));
                interlocks.AddRange(obj.GetComponentsInChildren<Interlocking>(true));
            }
            MultiLeverController mulCon = GetComponent<MultiLeverController>();
            foreach (SignalController signalControllerLever in mulCon.controlledLevers.Where(val => val is SignalController))
            {
                Interlocking correspondInterlock = interlocks.FirstOrDefault(val => val.GetFromLocker() == signalControllerLever);
                ApplyToControlMap(mulCon, pointControllers, correspondInterlock.GetRouteLocker());
            }
        }
        public void ApplyToControlMap(MultiLeverController controller, List<PointController> pointControllers, RouteLocker routeLocker)
        {
            if (controller && routeLocker)
            {
                int pointIdx = 0;
                int[] targetRoutes = routeLocker.getTargetRoute();
                foreach (AbstractPointSetter targetPoint in routeLocker.GetTargetPoints())
                {
                    PointController pointCorrespondController = pointControllers.FirstOrDefault(val => val.getPointInstances().Contains(targetPoint));
                    if (pointCorrespondController)
                    {
                        MultiLever_SwitchToControlMapsHolder switchMapHolder = controller.GetComponent<MultiLever_SwitchToControlMapsHolder>();
                        Undo.RecordObject(switchMapHolder, "Fetching Point Controller/Route settings");
                        if (!controller.controlledLevers.Contains(pointCorrespondController))
                        {
                            int[] MultileverControlMap = Enumerable.Repeat(-1, switchMapHolder.switchPositionNum).ToArray();
                            switchMapHolder.Switch_To_ControlMaps.Add(new To_ControllerMap(-1, pointCorrespondController, MultileverControlMap));
                        }
                        int controlTarget = GetControlFromRoute(pointCorrespondController, targetPoint, targetRoutes[pointIdx]);
                        if (controlTarget != -1)
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
