
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
        Debug.Log("InterlockLink PreProcess");
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            foreach (RouteChecker RouteChecker in obj.GetComponentsInChildren<RouteChecker>(true))
            {
                RouteChecker.SetupCallback();
            }
        }
        Debug.Log("InterlockLink Process");

    }

}
