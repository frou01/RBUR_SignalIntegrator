using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class RouteSelector_EndButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] RouteSelector_StartLever[] StartLevers;
        public void SetStarts(RouteSelector_StartLever[] StartLevers)
        {
            this.StartLevers = StartLevers;
        }
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