using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public abstract class AbstractLever_Control_Reciever : UdonSharpBehaviour
    {
        protected virtual void Start()
        {

        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public abstract int GetCurrentControl();
#else
        public virtual int GetCurrentControl()
        {
            return -1;
        }
#endif

    }
}