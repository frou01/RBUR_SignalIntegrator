using frou01.RigidBodyTrain;
using frou01.RBUR.SignalIntegrator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static frou01.RBUR.SignalIntegrator.Editor.BuildProcessUtility;

namespace frou01.RBUR.SignalIntegrator.Editor
{
    /// <summary>
    /// 制御盤(てこ類)の設定反映と、Interlocking との相互参照の構築
    /// </summary>
    public class Interlock_Pannel_BuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => -101;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            List<PointController> pointControllers = FindComponents<PointController>(scene);
            List<Interlocking> interlocks = FindComponents<Interlocking>(scene);

            ApplyRouteLockerToMultiLever(scene, pointControllers, interlocks);
            ApplyPointControllerMaps(pointControllers);
            ApplyMultiLeverControlMaps(scene);
            ResolveRouteLockerPointControllers(scene, pointControllers);
            //以降は RouteLocker.Lockers(PointController分)が解決済みであることが前提
            BuildIndirectControllerReferences(scene, interlocks);
            BuildAffectedInterlockings(interlocks);
            ExpandIndirectControllerReferences(scene);
            BuildMultiLeverReferences(scene);
            SetupRouteSelectors(scene);
        }

        /// <summary>
        /// 進路てこ : 対応する Interlocking の進路条件を、MultiLever のてこ設定に変換する
        /// </summary>
        private static void ApplyRouteLockerToMultiLever(Scene scene, List<PointController> pointControllers, List<Interlocking> interlocks)
        {
            foreach (MultiLever_ReferRouteLocker mulConSetter in FindComponents<MultiLever_ReferRouteLocker>(scene))
            {
                MultiLeverController mulCon = mulConSetter.GetComponent<MultiLeverController>();
                foreach (SignalController signalControllerLever in mulCon.ControlledLevers.Where(val => val is SignalController))
                {
                    Interlocking correspondInterlock = interlocks.FirstOrDefault(val => val.GetFromLocker() == signalControllerLever);
                    if (correspondInterlock)
                    {
                        mulConSetter.ApplyToControlMap(mulCon, pointControllers, correspondInterlock.GetRouteLocker());
                    }
                    else
                    {
                        Debug.LogError("Interlock not found", mulCon);
                    }
                }
            }
        }

        /// <summary>
        /// 転てつてこ : Holder の設定を PointController に反映し、分岐器からのコールバック先に登録する
        /// </summary>
        private static void ApplyPointControllerMaps(List<PointController> pointControllers)
        {
            foreach (PointController pointCon in pointControllers)
            {
                pointCon.GetComponent<PointLever_ControlToRouteIndexHolder>().ApplyToUdonBehaviour();
                pointCon.GetComponent<PointLever_ControlToParamaterHolder>().ApplyToUdonBehaviour();
                foreach (AbstractPointSetter point in pointCon.getPointInstances())
                {
                    point.callbackUdons = point.callbackUdons.AddUnique(pointCon);
                }
            }
        }

        private static void ApplyMultiLeverControlMaps(Scene scene)
        {
            foreach (MultiLeverController mulCon in FindComponents<MultiLeverController>(scene))
            {
                mulCon.GetComponent<MultiLever_SwitchToControlMapsHolder>().apply_mulCon_to_Con_Map();
            }
        }

        /// <summary>
        /// RouteLocker の各分岐器を制御する PointController を Lockers に登録し、進路番号を制御位置に変換する
        /// </summary>
        private static void ResolveRouteLockerPointControllers(Scene scene, List<PointController> pointControllers)
        {
            foreach (RouteLocker routeLocker in FindComponents<RouteLocker>(scene))
            {
                int lockerIdx = -1;
                routeLocker.ControlTargetIndex = (int[])routeLocker.TargetRoute.Clone();
                foreach (AbstractPointSetter point in routeLocker.TargetPoints)
                {
                    lockerIdx++;
                    foreach (PointController pointController in pointControllers)
                    {
                        int pointIdx = Array.IndexOf(pointController.getPointInstances(), point);

                        if (pointIdx >= 0)
                        {
                            routeLocker.ControlTargetIndex[lockerIdx] = Array.IndexOf(pointController.GetControlToRouteIndexMap()[pointIdx], routeLocker.TargetRoute[lockerIdx]);
                            routeLocker.Lockers[lockerIdx] = pointController;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 各 Interlocking が直接使う IndirectController に、Interlocking への逆参照を張る
        /// </summary>
        private static void BuildIndirectControllerReferences(Scene scene, List<Interlocking> interlocks)
        {
            foreach (AbstractIndirectController controller in FindComponents<AbstractIndirectController>(scene))
            {
                controller.ReferingInterlocks = new Interlocking[0];
            }
            foreach (Interlocking currentInterlock in interlocks)
            {
                foreach (AbstractIndirectController controller in GetIndirectControllers(currentInterlock))
                {
                    controller.ReferingInterlocks = controller.ReferingInterlocks.AddUnique(currentInterlock);
                }
            }
        }

        /// <summary>
        /// 同じ IndirectController を使う Interlocking 同士を、互いの affectedInterlockings に登録する(自身を含む)
        /// </summary>
        private static void BuildAffectedInterlockings(List<Interlocking> interlocks)
        {
            foreach (Interlocking currentInterlock in interlocks)
            {
                currentInterlock.affectedInterlockings = GetIndirectControllers(currentInterlock)
                    .SelectMany(controller => controller.ReferingInterlocks)
                    .Distinct()
                    .ToArray();
            }
        }

        /// <summary>
        /// IndirectController の逆参照を、関係する Interlocking 全体に広げる
        /// </summary>
        private static void ExpandIndirectControllerReferences(Scene scene)
        {
            foreach (AbstractIndirectController controller in FindComponents<AbstractIndirectController>(scene))
            {
                controller.ReferingInterlocks = controller.ReferingInterlocks
                    .SelectMany(interlock => interlock.affectedInterlockings)
                    .Distinct()
                    .ToArray();
            }
        }

        private static void BuildMultiLeverReferences(Scene scene)
        {
            foreach (MultiLeverController multiLeverController in FindComponents<MultiLeverController>(scene))
            {
                multiLeverController.ReferingInterlocks = multiLeverController.ControlledLevers
                    .SelectMany(panelController => panelController.ReferingInterlocks)
                    .Distinct()
                    .ToArray();
            }
        }

        private static void SetupRouteSelectors(Scene scene)
        {
            foreach (RouteSelector_EndButton end in FindComponents<RouteSelector_EndButton>(scene))
            {
                end.StartLevers = new RouteSelector_StartLever[0];
            }
            foreach (RouteSelector_StartLever startLever in FindComponents<RouteSelector_StartLever>(scene))
            {
                startLever.AssignMaps();
                foreach (RouteSelector_EndButton end in startLever.getRouteEnds().SelectMany(val => val))
                {
                    end.StartLevers = end.StartLevers.AddUnique(startLever);
                }
            }
        }

        /// <summary>
        /// Interlocking が使う IndirectController (toLocker_States / RouteLocker / From_Locker のうち該当するもの)
        /// </summary>
        private static List<AbstractIndirectController> GetIndirectControllers(Interlocking interlock)
        {
            List<AbstractLockerConsolidater> lockers = new List<AbstractLockerConsolidater>();
            lockers.AddRange(interlock.GetInterlockStateLinker().Select(stateLocker => stateLocker.getLocker()));
            if (interlock.GetRouteLocker()) lockers.AddRange(interlock.GetRouteLocker().Lockers);
            lockers.Add(interlock.GetFromLocker());
            return lockers.OfType<AbstractIndirectController>().ToList();
        }
    }
}
