using frou01.util;
using RBUR_SignalIntegrator;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RBUR_SignalIntegrator_Editor
{
    public class Interlock_GACLink_BuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink Start Process");
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                //TODO GACからInterlockへイベントを配送するためPickUpEventLinker/SyncEventLinkerを自動設定する

                foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    interlocking.SetupLocker();
                }
            }
            Debug.Log("InterlockLink Process End");

        }
    }
}