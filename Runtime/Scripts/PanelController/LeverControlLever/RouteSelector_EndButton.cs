using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
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