using frou01.RigidBodyTrain;
using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Serialization.OdinSerializer;

namespace RBUR_SignalIntegrator
{
    [RequireComponent(typeof(PointLever_ControlToRouteIndexHolder),typeof(PointLever_RouteIndexToParamaterHolder))]
    public class PointControllerLever : AbstractPanelController
    {
        [SerializeField] protected AbstractPointSetter[] pointInstances;
        [HideInInspector][OdinSerialize][SerializeField] protected int[][] ControlToRouteIndexMap;//2nd index:switch, value:control. -1 is mid(not lever local control)
        [HideInInspector][OdinSerialize][SerializeField] protected float[][] RouteToParamaterMap;//2nd index:route, value:animationParamater.
        public void SetControlToRouteIndexMap(int[][] ControlToRouteIndexMap)
        {
            this.ControlToRouteIndexMap = ControlToRouteIndexMap;
        }
        public int[][] GetControlToRouteIndexMap()
        {
            return this.ControlToRouteIndexMap;
        }
        public void SetRouteToParamaterMap(float[][] RouteToParamaterMap)
        {
            this.RouteToParamaterMap = RouteToParamaterMap;
        }
        public float[][] GetRouteToParamaterMap()
        {
            return this.RouteToParamaterMap;
        }
        public AbstractPointSetter[] getPointInstances()
        {
            return pointInstances;
        }
        [SerializeField] Animator[] PointSideAnimators;
        public Animator[] attachedPointAnimator()
        {
            return PointSideAnimators;
        }
        [SerializeField] string currentRouteParamater = "PointRoute";
        [SerializeField] string progressParamater = "PointMovePos";
        int pointCon_prevControllingPosition = -1;
        protected int PrevPointRouteIndex = int.MinValue;
        [SerializeField] protected int PointRouteIndex;
        [SerializeField] protected float changeProgress;
        [SerializeField] protected float changeTimeLength = 1;
        [SerializeField] bool NetworkUpdate;
        protected override void Start()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(Start));
            base.Start();
            PointUpdate();
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(Start));
        }
        public override int GetCurrentPosition()
        {
            return controllingPosition;
        }
        protected override void applyPositionToController(int posIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(applyPositionToController));
            base.applyPositionToController(posIndex);
            this.enabled = true;
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(applyPositionToController));
        }

        private void Update()
        {
            if (pointCon_prevControllingPosition != controllingPosition)
            {
                foreach (AbstractPointSetter pointSetter in pointInstances)
                {
                    pointSetter.set_route_To(-1);
                }
                pointCon_prevControllingPosition = controllingPosition;
            }
            if (PointRouteIndex != controllingPosition)
            {
                int prevChanging = Mathf.RoundToInt(changeProgress / changeTimeLength);
                int Changing;
                if (controllingPosition * changeTimeLength > changeProgress)
                {
                    prevChanging = Mathf.FloorToInt(changeProgress / changeTimeLength);
                    changeProgress += Time.deltaTime;
                    Changing = Mathf.FloorToInt(changeProgress / changeTimeLength);
                }
                else if (controllingPosition * changeTimeLength < changeProgress)
                {
                    prevChanging = Mathf.CeilToInt(changeProgress / changeTimeLength);
                    changeProgress -= Time.deltaTime;
                    Changing = Mathf.CeilToInt(changeProgress / changeTimeLength);
                }
                else
                {
                    prevChanging = int.MinValue;
                    Changing = Mathf.CeilToInt(changeProgress / changeTimeLength);
                }
                if (prevChanging != Changing && Changing == controllingPosition)
                {
                    changeProgress = Changing * changeTimeLength;
                    int pointIdx = 0;
                    foreach (AbstractPointSetter pointSetter in pointInstances)
                    {
                        pointSetter.set_route_To(ControlToRouteIndexMap[pointIdx][Changing]);
                        pointIdx++;
                    }
                }
                int animatorindex = 0;
                foreach (Animator animator in PointSideAnimators)
                {
                    animator.enabled = true;
                    int mapSection = Mathf.FloorToInt(changeProgress / changeTimeLength);
                    float mapstart = RouteToParamaterMap[animatorindex][mapSection];
                    float mapEnd = RouteToParamaterMap[animatorindex][mapSection + mapSection == RouteToParamaterMap[animatorindex].Length ? 0 : 1];
                    animator.SetFloat(progressParamater, Mathf.Lerp(mapstart, mapEnd,(changeProgress / changeTimeLength) - mapSection));
                    animatorindex++;
                }
            }
            else
            {
                if (NetworkUpdate && !isControllerOwner())
                {
                    NetworkUpdate = false;
                }
                if (!NetworkUpdate)
                {
                    this.enabled = false;
                }
            }
        }

        public void PointUpdate()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this,nameof(PointUpdate));
            this.enabled = true;
            int Control_RouteCorresponding = -1;

            int pointIdx = 0;
            foreach (PointLever_Setter aPoint in pointInstances)
            {
                int aPointRoute = aPoint.get_current_To_Index();

                int aControl_RouteCorresponding = 0;
                foreach (int anRoute in ControlToRouteIndexMap[pointIdx])
                {
                    if(anRoute == aPointRoute)
                    {
                        if (Control_RouteCorresponding == -1) Control_RouteCorresponding = aControl_RouteCorresponding;
                        else if (Control_RouteCorresponding != aControl_RouteCorresponding)
                        {
                            Control_RouteCorresponding = -1;
                            break;
                        }
                    }
                    aControl_RouteCorresponding++;
                }
                pointIdx++;
            }
            PointRouteIndex = Control_RouteCorresponding;
            foreach (Animator animator in PointSideAnimators)
            {
                animator.enabled = true;
                animator.SetFloat(currentRouteParamater, (float)PointRouteIndex / (float)(ControlToRouteIndexMap[0].Length-1));
            }
            if(PrevPointRouteIndex != PointRouteIndex)
            {
                PrevPointRouteIndex = PointRouteIndex;
                foreach (Interlocking interlock in ReferingInterlocks)
                {
                    interlock.UpdateInterlock();
                }
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(PointUpdate));
        }
        public override void SyncController()
        {
            this.enabled = true;
            NetworkUpdate = true;
            base.SyncController();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            base.OnPostSerialization(result);
            if (result.success)
            {
                NetworkUpdate = false;
            }
        }
        public override void SetToFailSafePosition()//for exception
        {
            setControllerOwner();
            int idx = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                SettedLockStates[idx] = false;
                SettedLockIndex[idx] = failSafeIndex;

                idx++;
            }
            setSwitchPosition(failSafeIndex);
            trySetPosition(switchToControllerMap[switchPosition]);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 1f, 0.1f);
            foreach (AbstractPointSetter pointSetter in pointInstances)
            {
                if (pointSetter) GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(pointSetter.transform), 0.02f, 0.02f);
            }
        }
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in PointSideAnimators)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;
                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), this.gameObject.name + ".PointAnimator", guiStyle);
            }

            ControlToRouteIndexMap = GetComponent<PointLever_ControlToRouteIndexHolder>().get_Control_to_RouteIndex_Map();
            int pointIdx = 0;
            foreach (AbstractPointSetter pointSetter in pointInstances)
            {
                if (!pointSetter) continue;
                Gizmos.color = new Color(0.2f, 0.2f, 1f, 1f);
                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(pointSetter.transform), 0.02f, 0.02f);
                guiStyle.normal.textColor = Gizmos.color;
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(pointSetter.transform), 0.6f), this.gameObject.name + ".PointInstance " + pointIdx, guiStyle);

                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                pointSetter.DrawGizmo_From();
                Vector4 colorVec_Start = new Vector4(0.2f, 0.2f, 1f, 1f);
                Vector4 colorVec_End = new Vector4(1f, 0f, 0f, 1f);
                float routeNum = ControlToRouteIndexMap[pointIdx].Length - 1;
                float idx = 0;
                Rail_Script[] routes = pointSetter.getRoutes();
                foreach (int routeIndex in ControlToRouteIndexMap[pointIdx])
                {
                    Vector4 currentCol = Vector4.Lerp(colorVec_Start, colorVec_End, idx / routeNum);
                    Gizmos.color = new Color(currentCol.x, currentCol.y, currentCol.z, currentCol.w);
                    guiStyle.normal.textColor = Gizmos.color;
                    pointSetter.DrawGizmo_To(routes[routeIndex]);

                    Vector3 gizmoStart;
                    Vector3 gizmoEnd;
                    pointSetter.Gizmo_LineTarget(routes[routeIndex], out gizmoStart, out gizmoEnd);
                    Handles.Label(gizmoEnd, this.gameObject.name + ".ControlIndex " + idx, guiStyle);
                    idx++;
                }
                pointIdx++;
            }
        }
#endif
    }
}