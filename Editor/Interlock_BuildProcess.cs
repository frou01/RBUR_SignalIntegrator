
using frou01.util;
using RBUR_SignalIntegrator;
using UdonSharpEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;
using VRC.Udon;

public class Interlock_BuildProcess : IProcessSceneWithReport
{
    //public class Interlock_BuildPreProcess : IProcessSceneWithReport
    //{
    //    public int callbackOrder => 0;

    //    public void OnProcessScene(Scene scene, BuildReport report)
    //    {
            
    //    }

    //}
    public int callbackOrder => 0;

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
            foreach (InterlockStateLinker stateLinker in obj.GetComponentsInChildren<InterlockStateLinker>(true))
            {
                stateLinker.SetupLocker();
            }
            foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
            {
                routeLocker.SetupLocker();
            }
            //TODO stateLinker,routeLockerからInterlockingへの参照を張る
        }
        Debug.Log("InterlockLink Process End");

    }

}
