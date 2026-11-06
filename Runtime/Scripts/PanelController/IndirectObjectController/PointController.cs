using frou01.RigidBodyTrain;
using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Serialization.OdinSerializer;

namespace frou01.RBUR.SignalIntegrator
{
    [RequireComponent(typeof(PointLeverControlToRouteIndexHolder),typeof(PointLeverControlToParameterHolder))]
    public class PointController : AbstractIndirectController
    {
        [HideInInspector][SerializeField] protected AbstractPointSetter[] pointInstances;
        [FormerlySerializedAs("PointSideAnimators")][HideInInspector][SerializeField] Animator[] pointSideAnimators;
        [FormerlySerializedAs("ControlToRouteIndexMap")][HideInInspector][OdinSerialize][SerializeField] protected int[][] controlToRouteIndexMap;//2nd index:switch, value:control. -1 is mid(not lever local control)
        [FormerlySerializedAs("ControlToParamaterMap")][HideInInspector][OdinSerialize][SerializeField] protected float[][] controlToParameterMap;//2nd index:route, value:animationParamater.

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void SetPointInstances(AbstractPointSetter[] points)
        {
            this.pointInstances = points;
        }
        public AbstractPointSetter[] GetPointInstances()
        {
            return GetComponent<PointLeverControlToRouteIndexHolder>().GetAssignedPoints();
        }
        public void SetControlToRouteIndexMap(int[][] controlToRouteIndexMap)
        {
            this.controlToRouteIndexMap = controlToRouteIndexMap;
        }
        public int[][] GetControlToRouteIndexMap()
        {
            return GetComponent<PointLeverControlToRouteIndexHolder>().GetControlToRouteIndexMap();
        }
        public void SetAttachedPointAnimator(Animator[] animators)
        {
            this.pointSideAnimators = animators;
        }
        public Animator[] GetAttachedPointAnimator()
        {
            return GetComponent<PointLeverControlToParameterHolder>().GetAttachedAnimators();
        }
        public void SetControlToParameterMap(float[][] controlToParameterMap)
        {
            this.controlToParameterMap = controlToParameterMap;
        }
        public float[][] GetControlToParameterMap()
        {
            return GetComponent<PointLeverControlToParameterHolder>().GetControlToParameterMap();
        }
#endif
        [FormerlySerializedAs("currentRouteParamater")][SerializeField] string currentRouteParamName = "PointRoute";
        [FormerlySerializedAs("progressParamater")][SerializeField] string progressParamName = "PointMovePos";
        int prevControllingPosition = -1;
        protected int prevPointRouteIndex = int.MinValue;
        [SerializeField] protected float changeTimeLength = 1;
        [FormerlySerializedAs("CurrentPointRouteMapIndex")][SerializeField] protected int currentPointRouteMapIndex;
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
        protected override void ApplyPositionToController(int posIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(ApplyPositionToController));
            base.ApplyPositionToController(posIndex);
            this.enabled = true;
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(ApplyPositionToController));
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
            if (currentPointRouteMapIndex != controllingPosition)
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
                        pointSetter.set_route_To(controlToRouteIndexMap[pointIdx][changing]);
                        pointIdx++;
                    }
                }
                int animatorIdx = 0;
                foreach (Animator animator in pointSideAnimators)
                {
                    animator.enabled = true;
                    int mapSection = Mathf.FloorToInt(changeProgress / changeTimeLength);
                    float mapStart = controlToParameterMap[animatorIdx][mapSection];
                    float mapEnd = controlToParameterMap[animatorIdx][mapSection + mapSection == controlToParameterMap[animatorIdx].Length ? 0 : 1];
                    animator.SetFloat(progressParamName, Mathf.Lerp(mapStart, mapEnd,(changeProgress / changeTimeLength) - mapSection));
                    animatorIdx++;
                }
            }
            else
            {
                if (!IsNeedNetworkUpdate() || !IsControllerOwner() || (isDisabledForSinglePlayer = VRCPlayerApi.GetPlayerCount() == 1))
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
                if (isLogging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] aPoint = " + aPoint.name, this);
                if (isLogging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] aPointRoute = " + aPointRoute, this);

                int aControlRouteCorresponding = 0;
                foreach (int anRoute in controlToRouteIndexMap[pointIdx])
                {
                    if(anRoute == aPointRoute)
                    {
                        if (isLogging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] anRoute = " + anRoute, this);
                        if (controlRouteCorresponding == -2) controlRouteCorresponding = aControlRouteCorresponding;
                        else if (controlRouteCorresponding != aControlRouteCorresponding)
                        {
                            controlRouteCorresponding = -1;
                        }
                        break;
                    }
                    aControlRouteCorresponding++;
                }
                if (aControlRouteCorresponding == controlToRouteIndexMap[pointIdx].Length) controlRouteCorresponding = -1;//Not Found
                pointIdx++;
            }
            currentPointRouteMapIndex = controlRouteCorresponding;
            foreach (Animator animator in pointSideAnimators)
            {
                AnimatorSleeper_UdonBehaviour sleeper = animator.GetComponentInChildren<AnimatorSleeper_UdonBehaviour>(); if (sleeper)
                {
                    sleeper.ResetCount();
                }
                animator.enabled = true;
                animator.SetFloat(currentRouteParamName, (float)currentPointRouteMapIndex / (float)(controlToRouteIndexMap[0].Length-1));
            }
            if (isLogging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] prevPointRouteIndex = " + prevPointRouteIndex, this);
            if (isLogging) Debug.Log("Debug [on " + this.name + "," + nameof(PointUpdate) + "] PointRouteIndex = " + currentPointRouteMapIndex, this);
            if (prevPointRouteIndex != currentPointRouteMapIndex)
            {
                prevPointRouteIndex = currentPointRouteMapIndex;
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
            foreach (Animator animator in pointSideAnimators)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;
                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), animator.name, guiStyle);
            }

            controlToRouteIndexMap = GetComponent<PointLeverControlToRouteIndexHolder>().GetControlToRouteIndexMap();
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
                float routeNum = controlToRouteIndexMap[pointIdx].Length - 1;
                float idx = 0;
                Rail_Script[] routes = pointSetter.getRoutes();
                foreach (int routeIndex in controlToRouteIndexMap[pointIdx])
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