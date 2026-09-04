using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class RouteSelector_EndButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public RouteSelector_StartLever[] StartLevers;
        public void OnPerform()
        {
            //Route End Button Pushed
            foreach (RouteSelector_StartLever startLever in StartLevers)
            {
                startLever.SelectRoute(this);
            }
        }
    }
}