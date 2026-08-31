using frou01.RigidBodyTrain;
using HarmonyLib;
using RBUR_SignalIntegrator;
using System;
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
                pointCon.SetControlToRouteIndexMap(pointCon.GetComponent<PointLever_ControlToRouteIndexHolder>().get_Control_to_RouteIndex_Map());
                pointCon.SetRouteToParamaterMap(pointCon.GetComponent<PointLever_RouteIndexToParamaterHolder>().get_Control_to_Paramater_Map());
                foreach (AbstractPointSetter point in pointCon.getPointInstances())
                {
                    point.callbackUdons = point.callbackUdons.AddToArray(pointCon);
                }
            }
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (MultiLeverController MulCon in obj.GetComponentsInChildren<MultiLeverController>(true))
                {
                    MulCon.Set_switchToControllerMap(MulCon.GetComponent<MultiLever_SwitchToControlMapsHolder>().get_mulCon_to_Con_Map());
                }
            }
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
                {
                    int LockerIdx = -1;
                    routeLocker.ControlTargetIndex = (int[])routeLocker.getTargetRoute().Clone();
                    if (routeLocker.Locker_GTST == null || routeLocker.Locker_GTST.Length == 0)
                    {
                        routeLocker.Locker_GTST = new AbstractLockerConsolidater[routeLocker.GetTargetPoints().Length];
                    }
                    foreach (AbstractPointSetter point in routeLocker.GetTargetPoints())
                    {
                        LockerIdx++;
                        foreach (PointControllerLever pointController in pointControllers)
                        {
                            int pointIdx = Array.IndexOf(pointController.getPointInstances(), point);

                            if (pointIdx >= 0)
                            {
                                routeLocker.ControlTargetIndex[LockerIdx] = Array.IndexOf(pointController.GetControlToRouteIndexMap()[pointIdx], routeLocker.getTargetRoute()[LockerIdx]);
                                routeLocker.Locker_GTST[LockerIdx] = pointController;
                                break;
                            }
                        }
                    }
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (Interlocking currentInterlock in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    //Add Interlocking Reverse reference to Controller
                    List<AbstractPanelController> controllers = new List<AbstractPanelController>();
                    foreach (Interlock_ToLockerAndMeetPosition StateLocker in currentInterlock.GetInterlockStateLinker())
                    {
                        if (StateLocker.getLocker() is AbstractPanelController)
                        {
                            controllers.Add((AbstractPanelController)StateLocker.getLocker());
                        }
                    }
                    if (currentInterlock.GetRouteLocker()) foreach (AbstractLockerConsolidater locker in currentInterlock.GetRouteLocker().Locker_GTST)
                    {
                        if (locker is AbstractPanelController)
                        {
                            controllers.Add((AbstractPanelController)locker);
                        }
                    }
                    if (currentInterlock.GetFromLocker() is AbstractPanelController)
                    {
                        controllers.Add((AbstractPanelController)currentInterlock.GetFromLocker());
                    }
                    foreach (AbstractPanelController controller in controllers)
                    {
                        controller.ReferingInterlocks = controller.ReferingInterlocks.AddToArray(currentInterlock);
                    }
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (Interlocking currentInterlock in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    //Add Affected Interlocks to Interlocking 
                    List<Interlocking> ReverseReferencedInterlocks = new List<Interlocking>();
                    foreach (Interlock_ToLockerAndMeetPosition StateLocker in currentInterlock.GetInterlockStateLinker())
                    {
                        if (StateLocker.getLocker() is AbstractPanelController)
                        {
                            ReverseReferencedInterlocks.AddRange(((AbstractPanelController)StateLocker.getLocker()).ReferingInterlocks);
                        }
                    }
                    if (currentInterlock.GetRouteLocker()) foreach (AbstractLockerConsolidater locker in currentInterlock.GetRouteLocker().Locker_GTST)
                    {
                        if (locker is AbstractPanelController)
                        {
                            ReverseReferencedInterlocks.AddRange(((AbstractPanelController)locker).ReferingInterlocks);
                        }
                    }
                    if (currentInterlock.GetFromLocker() is AbstractPanelController)
                    {
                        ReverseReferencedInterlocks.AddRange(((AbstractPanelController)currentInterlock.GetFromLocker()).ReferingInterlocks);
                    }
                    ReverseReferencedInterlocks = ReverseReferencedInterlocks.Distinct().ToList();

                    currentInterlock.affectedInterlockings = currentInterlock.affectedInterlockings.AddRangeToArray(ReverseReferencedInterlocks.ToArray());
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (MultiLeverController multiLeverController in obj.GetComponentsInChildren<MultiLeverController>(true))
                {
                    foreach (AbstractPanelController panelController in multiLeverController.controlledLevers)
                    {
                        multiLeverController.ReferingInterlocks = multiLeverController.ReferingInterlocks.AddRangeToArray(panelController.ReferingInterlocks);

                        if (panelController.callbackBehaviours.Contains(UdonSharpEditorUtility.GetBackingUdonBehaviour(panelController))) break;
                        panelController.callbackBehaviours = panelController.callbackBehaviours.AddItem(UdonSharpEditorUtility.GetBackingUdonBehaviour(multiLeverController)).ToArray();
                    }
                    multiLeverController.ReferingInterlocks = multiLeverController.ReferingInterlocks.Distinct().ToArray();
                }
            }
        }
    }
}
