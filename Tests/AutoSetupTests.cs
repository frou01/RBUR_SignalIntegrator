using frou01.RigidBodyTrain;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace frou01.RBUR.SignalIntegrator.Tests
{
    public class AutoSetupTests : MonoBehaviour
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
            _testRoot = new GameObject("AbstractLockerConsolidatorTests");

            pointControllers.Add(_pointController = AbstractLockerConsolidatorTests.CreateChild(_testRoot,"PointController")
                .AddComponent<PointController>());

            _pointSetter = AbstractLockerConsolidatorTests.CreateChild(_testRoot, "Point")
                .AddComponent<PointLever_Setter>();

            _pointController.GetComponent<PointLeverControlToRouteIndexHolder>().control_To_RouteIndexMaps.Add(new PointLeverControlToRouteIndexHolder.ControlToRouteIndexMap(_pointSetter, new int[] { 0, 1 }));

            _eventStackHolder = AbstractLockerConsolidatorTests.CreateChild(_testRoot,"EventStackHolder")
                .AddComponent<EventStackHolder>();


            _locker1 = AbstractLockerConsolidatorTests.CreateInterlocking(_testRoot,"Locker1");
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
            MultiLeverReferRouteLocker referRouteLocker = AbstractLockerConsolidatorTests.CreateChild(_testRoot, "MultiLeverReferRouteLocker")
                .AddComponent<MultiLeverReferRouteLocker>();
            referRouteLocker.targetMulConSwitch = 1;

            MultiLeverController multiLeverController = referRouteLocker.gameObject.AddComponent<MultiLeverController>();
            MultiLeverSwitchToControlMapsHolder switchMapHolder = multiLeverController.GetComponent<MultiLeverSwitchToControlMapsHolder>();

            SignalController signal = AbstractLockerConsolidatorTests.CreateChild(referRouteLocker.gameObject, "SignalController").AddComponent<SignalController>();

            switchMapHolder.switchPositionNum = 2;
            switchMapHolder.Switch_To_ControlMaps.Add(new MultiLeverSwitchToControlMapsHolder.ToControllerMap(signal, new int[] { 0, 1 }));

            AbstractLockerConsolidatorTests.SetRequiredField(_locker1, "fromLocker", signal);

            RouteLocker routeLocker = _locker1.gameObject.AddComponent<RouteLocker>();
            routeLocker.Lockers = new AbstractLockerConsolidator[0];
            routeLocker.SetParentInterlock(_locker1);
            routeLocker.TargetPoints = new AbstractPointSetter[] { _pointSetter };
            routeLocker.TargetRoutes = new int[] { 1 };
            AbstractLockerConsolidatorTests.SetRequiredField(_locker1, "routeLocker", routeLocker);
            //TODO: routeLockerにPointと進路(1)を設定

            referRouteLocker.ApplyToControlMap();
            //TODO: MultiLever_SwitchToControlMapsHolderに_pointControllerと制御位置(1)が設定されていることを確認
            Assert.AreEqual(multiLeverController.ControlledLevers[1], _pointController);
            Assert.AreEqual(multiLeverController.SwitchToControllerMap[1][0], -1);
            Assert.AreEqual(multiLeverController.SwitchToControllerMap[1][1], 1);
        }
    }
}
