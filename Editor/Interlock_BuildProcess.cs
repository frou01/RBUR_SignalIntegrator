
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
            foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
            {
                {//signalController is only 1.
                    PickUpEventLinker linker = getPickUpEventLinker_FromSignal(interlocking);
                    linker.targets = null;
                }
                GACLockerConsolidater[] pointLocker = interlocking.pointLocker;
                for (int index = 0; index < pointLocker.Length; index++)
                {
                    PickUpEventLinker linker = getPickUpEventLinker_FromPoint(pointLocker[index]);
                    linker.targets = null;
                }
            }
        }
        Debug.Log("InterlockLink Process");
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
            {
                SetupEventLink(interlocking);
            }
        }
        
    }

    protected void SetupEventLink(Interlocking ProcessingInterLock)
    {
        Debug.Log("Setup " + ProcessingInterLock.gameObject.name);
        //signalController is only 1.
        setUdonToEventLinker(ProcessingInterLock, getPickUpEventLinker_FromSignal(ProcessingInterLock));
        GACLockerConsolidater[] pointLocker = ProcessingInterLock.pointLocker;
        for (int index = 0; index < pointLocker.Length; index++)
        {
            setUdonToEventLinker(ProcessingInterLock, getPickUpEventLinker_FromPoint(pointLocker[index]));
        }
    }

    protected static PickUpEventLinker getPickUpEventLinker_FromSignal(Interlocking ProcessingInterLock)
    {
        if (!ProcessingInterLock.signalController.gameObject.GetComponent<PickUpEventLinker>())
        {
            ProcessingInterLock.signalController.gameObject.AddUdonSharpComponent<PickUpEventLinker>();
            foreach (UdonBehaviour udons in ProcessingInterLock.signalController.gameObject.GetComponents<UdonBehaviour>())
            {
                udons.SyncMethod = Networking.SyncType.Manual;
            }
        }
        return ProcessingInterLock.signalController.gameObject.GetComponent<PickUpEventLinker>();
    }

    protected static PickUpEventLinker getPickUpEventLinker_FromPoint(GACLockerConsolidater ProcessingGACLock)
    {
        if (!ProcessingGACLock.target.gameObject.GetComponent<PickUpEventLinker>())
        {
            ProcessingGACLock.target.gameObject.AddUdonSharpComponent<PickUpEventLinker>();
            foreach (UdonBehaviour udons in ProcessingGACLock.target.gameObject.GetComponents<UdonBehaviour>())
            {
                udons.SyncMethod = Networking.SyncType.Manual;
            }
        }
        return ProcessingGACLock.target.gameObject.GetComponent<PickUpEventLinker>();
    }

    protected void setUdonToEventLinker(Interlocking ProcessingInterLock, PickUpEventLinker linker)
    {
        if (linker.targets == null)
        {
            linker.targets = new UdonBehaviour[0];
        }
        Debug.Log("SetLink " + linker.gameObject.name);

        UdonBehaviour[] newTarget = new UdonBehaviour[linker.targets.Length + 1];
        linker.targets.CopyTo(newTarget, 0);
        newTarget[linker.targets.Length] = ProcessingInterLock.gameObject.GetComponent<UdonBehaviour>();
        linker.targets = newTarget;
    }
}
