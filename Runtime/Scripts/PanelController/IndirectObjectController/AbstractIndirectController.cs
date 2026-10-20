using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using frou01.util;
using VRC.Udon.Common;
using UnityEngine.Serialization;
using UnityEngine.UIElements;





#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
#endif

namespace RBUR_SignalIntegrator
{

    /// <summary>
    /// virtualization controller for machine<Point,Signal,Sign>
    /// Consolidate the control from multiple sources and apply it to the machine.
    /// </summary>
    public class AbstractIndirectController : AbstractLockerConsolidater
    {
        public bool Logging = false;
        [SerializeField][HideInInspector] public Interlocking[] ReferingInterlocks;
        int PannelCon_prevControllingPosition = -1;

        [HideInInspector] public int currentUserControl;
        [SerializeField][UdonSynced] private protected int controllingPosition;//machine controlling position

        [FormerlySerializedAs("callbackBehaviours")]
        [Tooltip("Auto assign by buildProcess")][SerializeField] public UdonBehaviour[] LockstateCallbackBehaviours;

        private protected bool UpdateInterlockBySwitching = true;//Point controller is not need to update interlock when switching, it will be update by the point machine.
        private protected bool suspendSync = false;


        protected override void Start()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(Start));
            this.enabled = false;
            base.Start();
            OnDeserialization();
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(Start));
        }

        /// <summary>
        /// Tries to update the locking state for a given instance.
        /// if the lock state changes, it will also attempt to set the position based on the evaluated control target and notify any registered callback behaviours about the lock state update.
        /// </summary>
        /// <param name="triedFromInstance">The instance attempting to update the lock.</param>
        /// <param name="lockState">The desired lock state.</param>
        /// <param name="lockPositionSelector">The position selector for the lock.</param>
        /// <param name="triedInstanceIndex">The index of the instance attempting the update.</param>
        /// <returns>True if the update was successful, false otherwise(lock position conflict).</returns>
        protected override bool UpdateLocking(UdonSharpBehaviour triedFromInstance, bool lockState, int lockPositionSelector, out int triedInstanceIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateLocking));
            bool prevLock = isLocked();
            bool res = base.UpdateLocking(triedFromInstance, lockState, lockPositionSelector, out triedInstanceIndex);
            if(isLocked() != prevLock)
            {
                if (!isLocked())
                {
                    int nextControlTarget = EvaluateControlTarget();
                    if (nextControlTarget != controllingPosition)
                    {
                        trySetPosition(nextControlTarget);
                        if (UpdateInterlockBySwitching)
                            UpdateInterlocks();
                    }
                }
                foreach (UdonBehaviour beh in LockstateCallbackBehaviours)
                {
                    beh.SendCustomEvent("PanelLockstateUpdate");
                }
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLocking));

            return res;
        }
        protected override void applyLockToController(bool state)
        {
            //There is No Other instance for apply lock. bool array has updated.
        }

        /// <summary>
        /// Apply the controlling position to the controller. 
        /// If the position index is different from the current controlling position, it will update the controlling position and synchronize it across the network if the local player is the owner of this object.
        /// Override this method in derived classes to implement specific behavior for applying the position to the related-Objects.
        /// </summary>
        /// <param name="posIndex">The position index to apply to the controller.</param>
        protected override void applyPositionToController(int posIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(applyPositionToController));
            if (posIndex != -1 && posIndex != controllingPosition)
            {
                controllingPosition = posIndex;
                if (!suspendSync && isControllerOwner())
                {
                    if (PannelCon_prevControllingPosition != controllingPosition) SyncController();
                }
            }

            PannelCon_prevControllingPosition = controllingPosition;
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(applyPositionToController));
        }

        //Getters
        public override int GetCurrentPosition()
        {
            return controllingPosition;
        }
        public override bool isControllerOwner()
        {
            return Networking.IsOwner(this.gameObject);
        }

        //Networking control methods
        public override void setControllerOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        protected override void SyncController()
        {
            if (isControllerOwner())
            {
                if (!this.enabled) this.enabled = true;
                this.RequestSerialization();
            }
        }

        //Networking callbacks
        public override void OnDeserialization()
        {
            if (eventStackHolder != null) eventStackHolder.AddStack(this, nameof(OnDeserialization));
            if (Logging) Debug.Log("Debug [on " + this.name + "," + nameof(OnDeserialization) + "] pre : controllingPosition = " + controllingPosition, this);

            applyPositionToController(controllingPosition);//Force Apply Control


            //Post-control Interlock update
            if (UpdateInterlockBySwitching)
            {
                UpdateInterlocks();
            }

            if (Logging) Debug.Log("Debug [on " + this.name + "," + nameof(OnDeserialization) + "] post: controllingPosition = " + controllingPosition, this);
            if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(OnDeserialization));
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success)
            {
                DisableAfterSync();
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(_RetrySync),Random.Range(0,3));
            }
        }
        public void _RetrySync()
        {
            SyncController();
        }
        protected virtual void DisableAfterSync()
        {
            this.enabled = false;
        }


        //Control setter/getter
        [HideInInspector][SerializeField] private protected UdonSharpBehaviour[] TargeterInstances;
        [HideInInspector][SerializeField] private protected int[] ControlTargetsArray;
        [HideInInspector][SerializeField] private protected int UserDirectIndex = -1;

        /// <summary>
        /// Set the control target from the targeter instance. If the targeter instance is not registered, it will be added to the list of targeter instances.
        /// </summary>
        /// <param name="TargeterInstance">The targeter instance to set the control target for.</param>
        /// <param name="ControlTarget">Control target is the target value to be set for the specified targeter instance. -1 means no control.</param>
        /// <param name="UserDirect">Indicates whether the control is being set directly by the user. Panel controller store the user direct instance index.</param>
        public virtual void TrySetControlTarget(UdonSharpBehaviour TargeterInstance, int ControlTarget,bool UserDirect)
        {
            if (eventStackHolder != null) eventStackHolder.AddStack(this, nameof(TrySetControlTarget));
            int targeterIDX;
            getTargeterIndex(TargeterInstance, out targeterIDX);
            if (TargeterInstances.Length == targeterIDX)
            {
                AddNewTargeter(TargeterInstance);
            }
            ControlTargetsArray[targeterIDX] = ControlTarget;
            if (UserDirect)
            {
                UserDirectIndex = targeterIDX;

                //If the user direct control is set, immediately update Interlock and apply the control target.
                //Pre-control Interlock update
                suspendSync = true;
                UpdateInterlocks();

                trySetPosition(EvaluateControlTarget());

                //Post-control Interlock update
                if (UpdateInterlockBySwitching)
                {
                    UpdateInterlocks();
                }
                suspendSync = false;
                SyncController();
            }
            else
            {
                trySetPosition(EvaluateControlTarget());
            }
            if (eventStackHolder != null) eventStackHolder.RemoveStack(this, nameof(TrySetControlTarget));
        }
        
        /// <summary>
        /// Adds a new targeter instance to the list of targeter instances if it is not already present. Returns true if the instance was added, false if it was already present.
        /// </summary>
        /// <param name="triedFrom">The targeter instance to add.</param>
        /// <returns>True if the instance was added, false if it was already present.</returns>
        public virtual bool AddNewTargeter(UdonSharpBehaviour triedFrom)
        {
            foreach (UdonSharpBehaviour targeter in TargeterInstances)
            {
                if (triedFrom == targeter) return false;
            }

            UdonSharpBehaviour[] newTargeterInstances = new UdonSharpBehaviour[TargeterInstances.Length + 1];
            TargeterInstances.CopyTo(newTargeterInstances, 0);
            newTargeterInstances[TargeterInstances.Length] = triedFrom;
            TargeterInstances = newTargeterInstances;

            int[] newTargetsArray = new int[ControlTargetsArray.Length + 1];
            ControlTargetsArray.CopyTo(newTargetsArray, 0);
            newTargetsArray[ControlTargetsArray.Length] = -1;
            ControlTargetsArray = newTargetsArray;

            return true;
        }

        /// <summary>
        /// Gets the index of the specified targeter instance.
        /// </summary>
        /// <param name="triedFrom">The targeter instance to find the index for.</param>
        /// <param name="triedIndex">The index of the targeter instance.</param>
        protected void getTargeterIndex(UdonSharpBehaviour triedFrom, out int triedIndex)
        {
            triedIndex = 0;
            foreach (UdonSharpBehaviour targeter in TargeterInstances)
            {
                triedIndex++;
                if (triedFrom == targeter)
                {
                    triedIndex--;
                    break;
                }
            }
        }

        /// <summary>
        /// Evaluates the control target based on the current control targets array.
        /// </summary>
        /// <returns>The evaluated control target.</returns>
        public virtual int EvaluateControlTarget()
        {
            int controlTarget = -1;
            foreach (int anTarget in ControlTargetsArray)
            {
                if(anTarget != -1)
                {
                    controlTarget = anTarget;
                    break;
                }
            }
            
            return controlTarget;
        }

        /// <summary>
        /// Evaluates the user direct control based on the current control targets array.
        /// </summary>
        /// <returns>The evaluated user direct control target.</returns>
        public virtual int EvaluateUserDirectControl()
        {
            if (UserDirectIndex >= 0 && UserDirectIndex < ControlTargetsArray.Length)
            {
                return ControlTargetsArray[UserDirectIndex];
            }
            else
            {
                return -1;
            }
        }
        public virtual bool isLocalOverride()
        {
            return EvaluateUserDirectControl() != -1;
        }

        protected virtual void UpdateInterlocks()
        {
            int loopLimit = 10;
            bool needNextUpdate;
            do
            {
                loopLimit--;
                needNextUpdate = false;
                foreach (Interlocking interlock in ReferingInterlocks)
                {
                    needNextUpdate |= interlock.UpdateInterlock(false);
                }
            } while (needNextUpdate && loopLimit > 0);
            if (loopLimit <= 0)
            {
                Debug.LogError(this.name + ": Interlock Update Looping. Interlock Settings is inconsistency." + this.name, this);
                foreach (Interlocking interlock in ReferingInterlocks)
                {
                    Debug.LogError(this.name + ":    " + interlock.name, interlock);
                }
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
        }
        protected virtual void OnDrawGizmosSelected()
        {
        }

#endif
    }
}