using frou01.RigidBodyTrain;
using HarmonyLib;
using RBUR_SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace RBUR_SignalIntegrator_Editor
{
    public class Interlock_Pannel_BuildProcess : IProcessSceneWithReport
    {

        public int callbackOrder => -101;

        public void OnProcessScene(Scene scene, BuildReport report)
        {

            List<PointControllerLever> pointControllers = new List<PointControllerLever>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                pointControllers.AddRange(obj.GetComponentsInChildren<PointControllerLever>(true));
            }
            foreach (PointControllerLever pointCon in pointControllers)
            {
                pointCon.getPointInstance().callbackUdons = pointCon.getPointInstance().callbackUdons.AddToArray(pointCon);
            }
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
                {
                    int idx = -1;
                    if (routeLocker.Locker_GTST == null || routeLocker.Locker_GTST.Length == 0)
                    {
                        routeLocker.Locker_GTST = new AbstractLockerConsolidater[routeLocker.GetTargetPoints().Length];
                    }
                    foreach (AbstractPointSetter points in routeLocker.GetTargetPoints())
                    {
                        idx++;
                        foreach(PointControllerLever pointController in pointControllers)
                        {
                            if(pointController.getPointInstance() == points)
                            {
                                routeLocker.Locker_GTST[idx] = pointController;
                                break;
                            }
                        }
                    }
                }
                foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    List<AbstractPanelController> controllers = new List<AbstractPanelController>();
                    foreach (InterlockStateLocker StateLocker in interlocking.GetInterlockStateLinker())
                    {
                        if (StateLocker.getLocker() is AbstractPanelController)
                        {
                            controllers.Add((AbstractPanelController)StateLocker.getLocker());
                        }
                    }
                    foreach (AbstractLockerConsolidater locker in interlocking.GetRouteLocker().Locker_GTST)
                    {
                        if (locker is AbstractPanelController)
                        {
                            controllers.Add((AbstractPanelController)locker);
                        }
                    }
                    if (interlocking.GetTargetSignalLocker() is AbstractPanelController)
                    {
                        controllers.Add((AbstractPanelController)interlocking.GetTargetSignalLocker());
                    }
                    foreach (AbstractPanelController controller in controllers)
                    {
                        controller.interlocks = controller.interlocks.AddToArray(interlocking);
                    }
                }
                foreach (MultiLeverController multiLeverController in obj.GetComponentsInChildren<MultiLeverController>(true))
                {
                    foreach(AbstractPanelController panelController in multiLeverController.controlledLevers)
                    {
                        if (panelController.callbackBehaviours.Contains(UdonSharpEditorUtility.GetBackingUdonBehaviour(panelController))) break;
                        panelController.callbackBehaviours = panelController.callbackBehaviours.AddItem(UdonSharpEditorUtility.GetBackingUdonBehaviour(multiLeverController)).ToArray();
                    }
                }
            }
        }
    }
}
