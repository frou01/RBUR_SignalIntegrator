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

            List<PointController> pointControllers = new List<PointController>();
            List<MultiLever_ReferRouteLocker> multiLever_ReferRouteLocker = new List<MultiLever_ReferRouteLocker>();
            List<Interlocking> interlocks = new List<Interlocking>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                pointControllers.AddRange(obj.GetComponentsInChildren<PointController>(true));
                multiLever_ReferRouteLocker.AddRange(obj.GetComponentsInChildren<MultiLever_ReferRouteLocker>(true));
                interlocks.AddRange(obj.GetComponentsInChildren<Interlocking>(true));
            }
            foreach (MultiLever_ReferRouteLocker mulconSetter in multiLever_ReferRouteLocker)
            {
                MultiLeverController mulCon = mulconSetter.GetComponent<MultiLeverController>();
                foreach(SignalController signalControllerLever in mulCon.controlledLevers.Where(val => val is SignalController))
                {
                    Interlocking correspondInterlock = interlocks.FirstOrDefault(val => val.GetFromLocker() == signalControllerLever);
                    if (correspondInterlock)
                    {
                        mulconSetter.ApplyToControlMap(mulCon, pointControllers, correspondInterlock.GetRouteLocker());
                    }
                    else
                    {
                        Debug.LogError("Interlock not found", mulCon);
                    }
                }
            }

            foreach (PointController pointCon in pointControllers)
            {
                pointCon.SetControlToRouteIndexMap(pointCon.GetComponent<PointLever_ControlToRouteIndexHolder>().get_Control_to_RouteIndex_Map());
                pointCon.SetControlToParamaterMap(pointCon.GetComponent<PointLever_ControlToParamaterHolder>().get_Control_to_Paramater_Map());
                foreach (AbstractPointSetter point in pointCon.getPointInstances())
                {
                    point.callbackUdons = point.callbackUdons.AddToArray(pointCon);
                }
            }
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (MultiLeverController MulCon in obj.GetComponentsInChildren<MultiLeverController>(true))
                {
                    MulCon.GetComponent<MultiLever_SwitchToControlMapsHolder>().apply_mulCon_to_Con_Map();
                }
            }
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
                {
                    int LockerIdx = -1;
                    routeLocker.ControlTargetIndex = (int[])routeLocker.getTargetRoute().Clone();
                    foreach (AbstractPointSetter point in routeLocker.GetTargetPoints())
                    {
                        LockerIdx++;
                        foreach (PointController pointController in pointControllers)
                        {
                            int pointIdx = Array.IndexOf(pointController.getPointInstances(), point);

                            if (pointIdx >= 0)
                            {
                                routeLocker.ControlTargetIndex[LockerIdx] = Array.IndexOf(pointController.GetControlToRouteIndexMap()[pointIdx], routeLocker.getTargetRoute()[LockerIdx]);
                                routeLocker.Lockers[LockerIdx] = pointController;
                                break;
                            }
                        }
                    }
                }
            }

            foreach (Interlocking currentInterlock in interlocks)
            {
                //Add Interlocking Reverse reference to Controller
                List<AbstractIndirectController> controllers = new List<AbstractIndirectController>();
                foreach (LockerAndMeetPosition StateLocker in currentInterlock.GetInterlockStateLinker())
                {
                    if (StateLocker.getLocker() is AbstractIndirectController)
                    {
                        controllers.Add((AbstractIndirectController)StateLocker.getLocker());
                    }
                }
                if (currentInterlock.GetRouteLocker()) foreach (AbstractLockerConsolidater locker in currentInterlock.GetRouteLocker().Lockers)
                    {
                        if (locker is AbstractIndirectController)
                        {
                            controllers.Add((AbstractIndirectController)locker);
                        }
                    }
                if (currentInterlock.GetFromLocker() is AbstractIndirectController)
                {
                    controllers.Add((AbstractIndirectController)currentInterlock.GetFromLocker());
                }
                foreach (AbstractIndirectController controller in controllers)
                {
                    controller.ReferingInterlocks = controller.ReferingInterlocks.AddToArray(currentInterlock);
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (Interlocking currentInterlock in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    //Add Affected Interlocks to Interlocking 
                    List<Interlocking> ReverseReferencedInterlocks = new List<Interlocking>();
                    foreach (LockerAndMeetPosition StateLocker in currentInterlock.GetInterlockStateLinker())
                    {
                        if (StateLocker.getLocker() is AbstractIndirectController)
                        {
                            ReverseReferencedInterlocks.AddRange(((AbstractIndirectController)StateLocker.getLocker()).ReferingInterlocks);
                        }
                    }
                    if (currentInterlock.GetRouteLocker()) foreach (AbstractLockerConsolidater locker in currentInterlock.GetRouteLocker().Lockers)
                    {
                        if (locker is AbstractIndirectController)
                        {
                            ReverseReferencedInterlocks.AddRange(((AbstractIndirectController)locker).ReferingInterlocks);
                        }
                    }
                    if (currentInterlock.GetFromLocker() is AbstractIndirectController)
                    {
                        ReverseReferencedInterlocks.AddRange(((AbstractIndirectController)currentInterlock.GetFromLocker()).ReferingInterlocks);
                    }
                    ReverseReferencedInterlocks = ReverseReferencedInterlocks.Distinct().ToList();

                    currentInterlock.affectedInterlockings = currentInterlock.affectedInterlockings.AddRangeToArray(ReverseReferencedInterlocks.ToArray());
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (AbstractIndirectController controller in obj.GetComponentsInChildren<AbstractIndirectController>(true))
                {
                    List<Interlocking> ReferingInterlocks = new List<Interlocking>();
                    foreach(Interlocking interlock in controller.ReferingInterlocks)
                    {
                        ReferingInterlocks.AddRange(interlock.affectedInterlockings);
                    }
                    ReferingInterlocks = ReferingInterlocks.Distinct().ToList();
                    controller.ReferingInterlocks = ReferingInterlocks.ToArray();
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (MultiLeverController multiLeverController in obj.GetComponentsInChildren<MultiLeverController>(true))
                {
                    foreach (AbstractIndirectController panelController in multiLeverController.controlledLevers)
                    {
                        multiLeverController.ReferingInterlocks = multiLeverController.ReferingInterlocks.AddRangeToArray(panelController.ReferingInterlocks);
                    }
                    multiLeverController.ReferingInterlocks = multiLeverController.ReferingInterlocks.Distinct().ToArray();
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (RouteSelector_StartLever routeSlector_Start in obj.GetComponentsInChildren<RouteSelector_StartLever>(true))
                {
                    routeSlector_Start.AssignMaps();
                    foreach (RouteSelector_EndButton end in routeSlector_Start.getRouteEnds().SelectMany(val => val).ToArray())
                    {
                        end.StartLevers = end.StartLevers.AddItem(routeSlector_Start).ToArray();
                    }
                }
            }

        }
    }
}
