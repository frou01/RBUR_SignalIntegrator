using frou01.RigidBodyTrain;
using HarmonyLib;
using RBUR_SignalIntegrator;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                pointCon.getPointInstance().callbackUdons.AddItem(pointCon);
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
                    List<SignalControllerLever> controllers = new List<SignalControllerLever>();
                    foreach (InterlockStateLocker StateLocker in interlocking.GetInterlockStateLinker())
                    {
                        if (StateLocker.getLocker() is SignalControllerLever)
                        {
                            controllers.Add((SignalControllerLever)StateLocker.getLocker());
                        }
                    }
                    foreach (AbstractLockerConsolidater locker in interlocking.GetRouteLocker().Locker_GTST)
                    {
                        if (locker is SignalControllerLever)
                        {
                            controllers.Add((SignalControllerLever)locker);
                        }
                    }
                    if (interlocking.GetTargetSignalLocker() is SignalControllerLever)
                    {
                        controllers.Add((SignalControllerLever)interlocking.GetTargetSignalLocker());
                    }
                    foreach (SignalControllerLever controller in controllers)
                    {
                        controller.interlocks.AddItem(interlocking);
                    }
                }
            }
        }
    }
}
