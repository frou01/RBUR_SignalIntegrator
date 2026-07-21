
using frou01.util;
using RBUR_SignalIntegrator;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RBUR_SignalIntegrator_Editor
{
    public class Interlock_BuildProcess : IProcessSceneWithReport
    {
        //public class Interlock_BuildPreProcess : IProcessSceneWithReport
        //{
        //    public int callbackOrder => 0;

        //    public void OnProcessScene(Scene scene, BuildReport report)
        //    {

        //    }

        //}
        public int callbackOrder => -1;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink Start Process");
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (RouteChecker routeChecker in obj.GetComponentsInChildren<RouteChecker>(true))
                {
                    routeChecker.SetupCallback();
                }

                foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    interlocking.SetupLocker();
                }
                foreach (InterlockStateLocker stateLinker in obj.GetComponentsInChildren<InterlockStateLocker>(true))
                {
                    stateLinker.SetupLocker();
                }
                foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
                {
                    routeLocker.SetupLocker();
                }
                //TODO stateLinker,routeLockerからInterlockingへの参照を張る
                foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    interlocking.GetRouteLocker().setParentInterlock(interlocking);
                    foreach (InterlockStateLocker stateLinker in interlocking.GetInterlockStateLinker())
                    {
                        stateLinker.setParentInterlock(interlocking);
                    }
                }
            }
            Debug.Log("InterlockLink Process End");

        }

    }
}
