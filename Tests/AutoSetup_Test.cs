using frou01.RigidBodyTrain;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace frou01.RBUR.SignalIntegrator.Tests
{
    public class AutoSetup_Test : MonoBehaviour
    {
        private GameObject _testRoot;
        private Interlocking _locker1;
        private PointController _pointController;
        private AbstractPointSetter _pointSetter;
        private EventStackHolder _eventStackHolder;
        List<PointController> pointControllers = new List<PointController>();

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("AbstractLockerConsolidater_Test");

            pointControllers.Add(_pointController = AbstractLockerConsolidater_Test.CreateChild(_testRoot,"PointController")
                .AddComponent<PointController>());

            _pointSetter = AbstractLockerConsolidater_Test.CreateChild(_testRoot, "Point")
                .AddComponent<PointLever_Setter>();

            _pointController.GetComponent<PointLeverControlToRouteIndexHolder>().control_To_RouteIndexMaps.Add(new PointLeverControlToRouteIndexHolder.ControlToRouteIndexMap(_pointSetter, new int[] { 0, 1 }));

            _eventStackHolder = AbstractLockerConsolidater_Test.CreateChild(_testRoot,"EventStackHolder")
                .AddComponent<EventStackHolder>();


            _locker1 = AbstractLockerConsolidater_Test.CreateInterlocking(_testRoot,"Locker1");
        }

        [TearDown]
        public void Teardown()
        {
            if (_testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_testRoot);
            }
        }

        [Test]
        public void TestApplyToControlMap()
        {
            MultiLeverReferRouteLocker referRouteLocker = AbstractLockerConsolidater_Test.CreateChild(_testRoot, "MultiLeverReferRouteLocker")
                .AddComponent<MultiLeverReferRouteLocker>();
            referRouteLocker.targetMulConSwitch = 1;

            MultiLeverController multiLeverController = referRouteLocker.gameObject.AddComponent<MultiLeverController>();
            MultiLeverSwitchToControlMapsHolder switchMapHolder = multiLeverController.GetComponent<MultiLeverSwitchToControlMapsHolder>();

            SignalController signal = AbstractLockerConsolidater_Test.CreateChild(referRouteLocker.gameObject, "SignalController").AddComponent<SignalController>();

            switchMapHolder.switchPositionNum = 2;
            switchMapHolder.Switch_To_ControlMaps.Add(new MultiLeverSwitchToControlMapsHolder.ToControllerMap(signal, new int[] { 0, 1 }));

            AbstractLockerConsolidater_Test.SetRequiredField(_locker1, "From_Locker", signal);

            RouteLocker routeLocker = _locker1.gameObject.AddComponent<RouteLocker>();
            routeLocker.Lockers = new AbstractLockerConsolidator[0];
            routeLocker.setParentInterlock(_locker1);
            routeLocker.TargetPoints = new AbstractPointSetter[] { _pointSetter };
            routeLocker.TargetRoute = new int[] { 1 };
            AbstractLockerConsolidater_Test.SetRequiredField(_locker1, "interlock_Route", routeLocker);
            //TODO: routeLockerにPointと進路(1)を設定

            referRouteLocker.ApplyToControlMap();
            //TODO: MultiLever_SwitchToControlMapsHolderに_pointControllerと制御位置(1)が設定されていることを確認
            Assert.AreEqual(multiLeverController.ControlledLevers[1], _pointController);
            Assert.AreEqual(multiLeverController.SwitchToControllerMap[1][0], -1);
            Assert.AreEqual(multiLeverController.SwitchToControllerMap[1][1], 1);
        }
    }
}
