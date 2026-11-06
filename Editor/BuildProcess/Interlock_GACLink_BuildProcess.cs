using frou01.GrabController;
using frou01.RigidBodyTrain;
using frou01.util;
using frou01.RBUR.SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using VRC.Udon;
using static frou01.RBUR.SignalIntegrator.Editor.BuildProcessUtility;

namespace frou01.RBUR.SignalIntegrator.Editor
{
    /// <summary>
    /// GAC(GrabAnimatorController) で操作するてこと Interlocking の連携
    /// </summary>
    public class Interlock_GACLink_BuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => -100;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink GAC Start Process");
            List<Controller_Base> controllers = FindComponents<Controller_Base>(scene);
            List<GACLockerConsolidater> gacLockers = FindComponents<GACLockerConsolidater>(scene);

            ResolveRouteLockerGACLockers(scene, controllers, gacLockers);
            //以降は RouteLocker.Lockers(GAC分)が解決済みであることが前提
            bool failed = false;
            foreach (Interlocking interlocking in FindComponents<Interlocking>(scene))
            {
                failed |= !LinkEventsToInterlock(interlocking);
            }
            if (failed)
            {
                throw new BuildFailedException("Add PickUpEventLinker to interlocked Controller");
            }
            Debug.Log("InterlockLink GAC Process End");
        }

        /// <summary>
        /// 分岐器のAnimatorを制御している Controller_Base を探し、対応する GACLockerConsolidater を RouteLocker.Lockers に登録する
        /// </summary>
        private static void ResolveRouteLockerGACLockers(Scene scene, List<Controller_Base> controllers, List<GACLockerConsolidater> gacLockers)
        {
            foreach (RouteLocker routeLocker in FindComponents<RouteLocker>(scene))
            {
                int idx = -1;
                foreach (AbstractPointSetter point in routeLocker.TargetPoints)
                {
                    idx++;
                    Animator pointAnimator = point.GetComponent<Animator>();
                    if (!pointAnimator) continue;

                    Controller_Base pointController = controllers.FirstOrDefault(checkingController =>
                        pointAnimator == checkingController.TargetAnimator || checkingController.MultiTargetAnimators.Contains(pointAnimator));
                    if (!pointController) continue;

                    GACLockerConsolidater pointLocker = gacLockers.FirstOrDefault(checkingLocker => pointController == checkingLocker.getGACController());
                    if (pointLocker == null)
                    {
                        Debug.LogError("Correspond GACLockerConsolidater not found", pointController);
                    }
                    else
                    {
                        routeLocker.Lockers[idx] = pointLocker;
                    }
                }
            }
        }

        /// <summary>
        /// Interlocking が使う GAC てこの EventLinker に、Interlocking の UdonBehaviour を登録する
        /// </summary>
        /// <returns>false: PickUpEventLinker が無いてこがあった</returns>
        private static bool LinkEventsToInterlock(Interlocking interlocking)
        {
            bool success = true;
            UdonBehaviour interlockUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(interlocking);
            foreach (GACLockerConsolidater locker in GetGACLockers(interlocking))
            {
                VRCPickup pickup = locker.GetComponentInChildren<VRCPickup>();
                if (!pickup) continue;

                SyncEventLinker syncEventLinker = pickup.GetComponent<SyncEventLinker>();
                PickUpEventLinker pickUpEventLinker = pickup.GetComponent<PickUpEventLinker>();
                if (!syncEventLinker)
                {
                    syncEventLinker = pickup.gameObject.AddUdonSharpComponent<SyncEventLinker>();
                }
                if (!pickUpEventLinker)
                {
                    Debug.LogError("PickUpEventLinker not found", pickup.gameObject);
                    success = false;
                    continue;
                }
                syncEventLinker.targets = syncEventLinker.targets.AddUnique(interlockUdon);
                pickUpEventLinker.targets = pickUpEventLinker.targets.AddUnique(interlockUdon);
            }
            return success;
        }

        /// <summary>
        /// Interlocking が使う Locker のうち GACLockerConsolidater のもの (From_Locker / RouteLocker / toLocker_States)
        /// </summary>
        private static List<GACLockerConsolidater> GetGACLockers(Interlocking interlocking)
        {
            List<AbstractLockerConsolidater> lockers = new List<AbstractLockerConsolidater>();
            lockers.Add(interlocking.GetFromLocker());
            if (interlocking.GetRouteLocker()) lockers.AddRange(interlocking.GetRouteLocker().Lockers);
            lockers.AddRange(interlocking.GetInterlockStateLinker().Select(stateLocker => stateLocker.getLocker()));
            return lockers.OfType<GACLockerConsolidater>().ToList();
        }
    }
}
