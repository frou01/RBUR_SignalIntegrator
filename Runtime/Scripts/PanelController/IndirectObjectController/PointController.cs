using frou01.RigidBodyTrain;
using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Serialization.OdinSerializer;

namespace frou01.RBUR.SignalIntegrator
{
    [RequireComponent(typeof(PointLever_ControlToRouteIndexHolder),typeof(PointLever_ControlToParamaterHolder))]
    public class PointController : AbstractIndirectController
    {
        [HideInInspector][SerializeField] protected AbstractPointSetter[] pointInstances;
        [HideInInspector][SerializeField] Animator[] PointSideAnimators;
        [HideInInspector][OdinSerialize][SerializeField] protected int[][] ControlToRouteIndexMap;//2nd index:switch, value:control. -1 is mid(not lever local control)
        [HideInInspector][OdinSerialize][SerializeField] protected float[][] ControlToParamaterMap;//2nd index:route, value:animationParamater.

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void setPointInstances(AbstractPointSetter[] points)
        {
            this.pointInstances = points;
        }
        public AbstractPointSetter[] getPointInstances()
        {
            return GetComponent<PointLever_ControlToRouteIndexHolder>().get_assignedPointArray();
        }
        public void SetControlToRouteIndexMap(int[][] controlToRouteIndexMap)
        {
            this.ControlToRouteIndexMap = controlToRouteIndexMap;
        }
        public int[][] GetControlToRouteIndexMap()
        {
            return GetComponent<PointLever_ControlToRouteIndexHolder>().get_Control_to_RouteIndex_Map();
        }
        public void SetAttachedPointAnimator(Animator[] animators)
        {
            this.PointSideAnimators = animators;
        }
        public Animator[] GetAttachedPointAnimator()
        {
            return GetComponent<PointLever_ControlToParamaterHolder>().get_Attached_Animators();
        }
        public void SetControlToParamaterMap(float[][] controlToParameterMap)
        {
            this.ControlToParamaterMap = controlToParameterMap;
        }
        public float[][] GetRouteToParamaterMap()
        {
            return GetComponent<PointLever_ControlToParamaterHolder>().get_Control_to_Paramater_Map();
        }
#endif
        [SerializeField] string currentRouteParamater = "PointRoute";
        [SerializeField] string progressParamater = "PointMovePos";
        int prevControllingPosition = -1;
        protected int prevPointRouteIndex = int.MinValue;
        [SerializeField] protected float changeTimeLength = 1;
        [SerializeField] protected int CurrentPointRouteMapIndex;
        [SerializeField] protected float changeProgress;
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

        bool isDisabledForSinglePlayer = false;
        protected override void Update()
        {
            base.Update();
            if (prevControllingPosition != controllingPosition)
            {
                foreach (AbstractPointSetter pointSetter in pointInstances)
                {
                    pointSetter.set_route_To(-1);
                }
                prevControllingPosition = controllingPosition;
            }
            if (CurrentPointRouteMapIndex != controllingPosition)
            {
                int prevChanging = Mathf.RoundToInt(changeProgress / changeTimeLength);
                int changing;
                if (controllingPosition * changeTimeLength > changeProgress)
                {
                    prevChanging = Mathf.FloorToInt(changeProgress / changeTimeLength);
                    changeProgress += Time.deltaTime;
                    changing = Mathf.FloorToInt(changeProgress / changeTimeLength);
                }
                else if (controllingPosition * changeTimeLength < changeProgress)
                {
                    prevChanging = Mathf.CeilToInt(changeProgress / changeTimeLength);
                    changeProgress -= Time.deltaTime;
                    changing = Mathf.CeilToInt(changeProgress / changeTimeLength);
                }
                else
                {
                    prevChanging = int.MinValue;
                    changing = Mathf.CeilToInt(changeProgress / changeTimeLength);
                }
                if (prevChanging != changing && changing == controllingPosition)
                {
                    changeProgress = changing * changeTimeLength;
                    int pointIdx = 0;
                    foreach (AbstractPointSetter pointSetter in pointInstances)
                    {
                        pointSetter.set_route_To(ControlToRouteIndexMap[pointIdx][changing]);
                        pointIdx++;
                    }
                }
                int animatorIdx = 0;
                foreach (Animator animator in PointSideAnimators)
                {
                    animator.enabled = true;
                    int mapSection = Mathf.FloorToInt(changeProgress / changeTimeLength);
                    float mapStart = ControlToParamaterMap[animatorIdx][mapSection];
                    float mapEnd = ControlToParamaterMap[animatorIdx][mapSection + mapSection == ControlToParamaterMap[animatorIdx].Length ? 0 : 1];
                    animator.SetFloat(progressParamater, Mathf.Lerp(mapStart, mapEnd,(changeProgress / changeTimeLength) - mapSection));
                    animatorIdx++;
                }
            }
            else
            {
                if (!IsNeedNetworkUpdate() || !isControllerOwner() || (isDisabledForSinglePlayer = VRCPlayerApi.GetPlayerCount() == 1))
                {
                    this.enabled = false;
                }
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (isDisabledForSinglePlayer)
            {
                this.enabled = true;
                isDisabledForSinglePlayer = false;
            }
        }

        public void PointUpdate()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this,nameof(PointUpdate));
            this.enabled = true;
            int controlRouteCorresponding = -2;

            int pointIdx = 0;
            foreach (PointLever_Setter aPoint in pointInstances)
            {
                int aPointRoute = aPoint.get_current_To_Index();
                if (Logging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] aPoint = " + aPoint.name, this);
                if (Logging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] aPointRoute = " + aPointRoute, this);

                int aControlRouteCorresponding = 0;
                foreach (int anRoute in ControlToRouteIndexMap[pointIdx])
                {
                    if(anRoute == aPointRoute)
                    {
                        if (Logging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] anRoute = " + anRoute, this);
                        if (controlRouteCorresponding == -2) controlRouteCorresponding = aControlRouteCorresponding;
                        else if (controlRouteCorresponding != aControlRouteCorresponding)
                        {
                            controlRouteCorresponding = -1;
                        }
                        break;
                    }
                    aControlRouteCorresponding++;
                }
                if (aControlRouteCorresponding == ControlToRouteIndexMap[pointIdx].Length) controlRouteCorresponding = -1;//Not Found
                pointIdx++;
            }
            CurrentPointRouteMapIndex = controlRouteCorresponding;
            foreach (Animator animator in PointSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(currentRouteParamater, (float)CurrentPointRouteMapIndex / (float)(ControlToRouteIndexMap[0].Length-1));
            }
            if (Logging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] prevPointRouteIndex = " + prevPointRouteIndex, this);
            if (Logging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] PointRouteIndex = " + CurrentPointRouteMapIndex, this);
            if (prevPointRouteIndex != CurrentPointRouteMapIndex)
            {
                prevPointRouteIndex = CurrentPointRouteMapIndex;
                UpdateInterlocks();
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(PointUpdate));
        }
        protected override void SyncController()
        {
            base.SyncController();
        }
        protected override void PostSync()
        {
            //Dont sleep after sync. check on Update() if the controller is in the middle of point change.
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
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), animator.name, guiStyle);
            }

            ControlToRouteIndexMap = GetComponent<PointLever_ControlToRouteIndexHolder>().get_Control_to_RouteIndex_Map();
            int pointIdx = 0;
            foreach (AbstractPointSetter pointSetter in pointInstances)
            {
                if (!pointSetter) continue;
                Gizmos.color = new Color(0.2f, 0.2f, 1f, 1f);
                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(pointSetter.transform), 0.02f, 0.02f);
                guiStyle.normal.textColor = Gizmos.color;
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(pointSetter.transform), 0.6f), pointIdx + " : " + pointSetter.name, guiStyle);

                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                pointSetter.DrawGizmo_From();
                Vector4 colorVecStart = new Vector4(0.2f, 0.2f, 1f, 1f);
                Vector4 colorVecEnd = new Vector4(1f, 0f, 0f, 1f);
                float routeNum = ControlToRouteIndexMap[pointIdx].Length - 1;
                float idx = 0;
                Rail_Script[] routes = pointSetter.getRoutes();
                foreach (int routeIndex in ControlToRouteIndexMap[pointIdx])
                {
                    Vector4 currentCol = Vector4.Lerp(colorVecStart, colorVecEnd, idx / routeNum);
                    Gizmos.color = new Color(currentCol.x, currentCol.y, currentCol.z, currentCol.w);
                    guiStyle.normal.textColor = Gizmos.color;
                    pointSetter.DrawGizmo_To(routes[routeIndex]);

                    Vector3 gizmoStart;
                    Vector3 gizmoEnd;
                    pointSetter.Gizmo_LineTarget(routes[routeIndex], out gizmoStart, out gizmoEnd);
                    Handles.Label(gizmoEnd, pointSetter.name + " : " + idx, guiStyle);
                    idx++;
                }
                pointIdx++;
            }
        }

#endif
    }
}