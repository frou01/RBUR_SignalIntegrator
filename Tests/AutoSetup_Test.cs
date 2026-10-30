using NUnit.Framework;
using RBUR_SignalIntegrator;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Reflection;
using UdonSharp;
using UnityEngine;

namespace RBUR_SignalIntegrator.Test
{
    public class AutoSetup_Test : MonoBehaviour
    {
        private GameObject _testRoot;
        private Interlocking _locker1;
        private PointController _pointController;
        private EventStackHolder _eventStackHolder;
        List<PointController> pointControllers = new List<PointController>();

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("AbstractLockerConsolidater_Test");

            pointControllers.Add(_pointController = AbstractLockerConsolidater_Test.CreateChild(_testRoot,"Consolidater")
                .AddComponent<PointController>());
            _pointController.

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

    }
}
