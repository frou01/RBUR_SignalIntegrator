using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class AbstractInterlockState : UdonSharpBehaviour
    {
        public virtual bool Check()
        {
            return true;
        }
        public virtual void Lock()
        {

        }
    }
}