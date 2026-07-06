
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class Interlocking : UdonSharpBehaviour
    {
        //TODO Local鎖錠
        //TODO 同期デッドロック防止：両否決

        [SerializeField] RouteChecker interlock_Route;
        [SerializeField] AbstractInterlockState[] interlockStates;


    }

}