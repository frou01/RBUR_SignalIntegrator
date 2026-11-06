
using UnityEngine;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using frou01.RigidBodyTrain;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using static frou01.RBUR.SignalIntegrator.MultiLeverSwitchToControlMapsHolder;
using static frou01.RBUR.SignalIntegrator.PointLeverControlToRouteIndexHolder;
#endif

namespace frou01.RBUR.SignalIntegrator
{
    /// <summary>
    /// Interlockの進路条件を取得して進路てこのてこ設定に変換する
    /// </summary>
    public class MultiLeverReferRouteLocker : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] public int targetMulConSwitch;

        private static int GetControlFromRoute(PointController pointController, AbstractPointSetter pointSetter, int routeIndex)
        {
            List<ControlToRouteIndexMap> controlToRoute = pointController.GetComponent<PointLeverControlToRouteIndexHolder>().control_To_RouteIndexMaps;
            ControlToRouteIndexMap targetedMap = controlToRoute.First(val => val.linkedPoint == pointSetter);
            if (targetedMap != null)
            {
                return targetedMap.Control_To_RouteIndex.Select((x, i) => (x, i)).FirstOrDefault(map => map.x == routeIndex).i;
            }
            else
            {
                return -1;
            }
        }
        private static void SetControlToMap(MultiLeverSwitchToControlMapsHolder switchMapHolder, PointController pointController, int targetControl, int targetMulConSwitch)
        {
            List<ControlToRouteIndexMap> controlToRoute = pointController.GetComponent<PointLeverControlToRouteIndexHolder>().control_To_RouteIndexMaps;
            ToControllerMap map = switchMapHolder.Switch_To_ControlMaps.FirstOrDefault(val => val.linkedController == pointController);
            if (map != null)
            {
                map.Switch_To_Control[targetMulConSwitch] = targetControl;
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
            foreach (SignalController signalControllerLever in mulCon.ControlledLevers.Where(val => val is SignalController))
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
                int[] targetRoutes = routeLocker.TargetRoute;
                foreach (AbstractPointSetter targetPoint in routeLocker.TargetPoints)
                {
                    PointController pointCorrespondController = pointControllers.FirstOrDefault(val => val.GetPointInstances().Contains(targetPoint));
                    if (pointCorrespondController)
                    {
                        MultiLeverSwitchToControlMapsHolder switchMapHolder = controller.GetComponent<MultiLeverSwitchToControlMapsHolder>();
                        Undo.RecordObject(switchMapHolder, "Fetching Point Controller/Route settings");
                        if (!controller.ControlledLevers.Contains(pointCorrespondController))
                        {
                            int[] multiLeverControlMap = Enumerable.Repeat(-1, switchMapHolder.switchPositionNum).ToArray();
                            switchMapHolder.Switch_To_ControlMaps.Add(new ToControllerMap(pointCorrespondController, multiLeverControlMap));
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
        [CustomEditor(typeof(MultiLeverReferRouteLocker)), CanEditMultipleObjects]
        public class MultiLeverReferRouteLockerEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                MultiLeverReferRouteLocker instance = target as MultiLeverReferRouteLocker;
                base.OnInspectorGUI();
                if (GUILayout.Button("Apply to ControlMap"))
                {
                    instance.ApplyToControlMap();
                    foreach (MultiLeverReferRouteLocker childInstance in targets)
                    {
                        childInstance.ApplyToControlMap();
                    }
                }
            }
        }
#endif
    }
}
