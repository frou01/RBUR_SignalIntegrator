using RBUR_SignalIntegrator;
using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RBUR_SignalIntegrator_Editor
{
    public class Interlock_PreBuildProcess : IProcessSceneWithReport
    {
        //public class Interlock_BuildPreProcess : IProcessSceneWithReport
        //{
        //    public int callbackOrder => 0;

        //    public void OnProcessScene(Scene scene, BuildReport report)
        //    {

        //    }

        //}
        public int callbackOrder => -200;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("Interlock Start PreProcess");
            EventStackHolder eventStackHolder = null;
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                eventStackHolder = obj.GetComponentInChildren<EventStackHolder>(true);
                if (eventStackHolder) break;
            }
            //if (!eventStackHolder)
            //{
            //GameObject newGo = new GameObject("EventStackHolder");
            //eventStackHolder = newGo.AddUdonSharpComponent<EventStackHolder>();
            //}
            if (eventStackHolder)
            {
                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    foreach (SignalEvaluator con in obj.GetComponentsInChildren<SignalEvaluator>())
                    {
                        con.eventStackHolder = eventStackHolder;
                    }
                    foreach (MultiLeverController con in obj.GetComponentsInChildren<MultiLeverController>())
                    {
                        con.eventStackHolder = eventStackHolder;
                    }
                    foreach (RouteLocker con in obj.GetComponentsInChildren<RouteLocker>())
                    {
                        con.eventStackHolder = eventStackHolder;
                    }
                    foreach (Interlock_ToLockerAndMeetPosition con in obj.GetComponentsInChildren<Interlock_ToLockerAndMeetPosition>())
                    {
                        con.eventStackHolder = eventStackHolder;
                    }
                    foreach (Interlocking con in obj.GetComponentsInChildren<Interlocking>())
                    {
                        con.eventStackHolder = eventStackHolder;
                    }
                    foreach (AbstractLockerConsolidater con in obj.GetComponentsInChildren<AbstractLockerConsolidater>())
                    {
                        con.eventStackHolder = eventStackHolder;
                    }
                }
            }


            Debug.Log("Interlock End PreProcess");

        }

    }
}
