using System;
using System.Reflection;
using NUnit.Framework;
using frou01.RBUR.SignalIntegrator;
using UdonSharp;
using UnityEngine;

namespace frou01.RBUR.SignalIntegrator.Tests
{
    public class AbstractLockerConsolidatorTests
    {
        private GameObject _testRoot;
        private FakeLockerConsolidator _consolidator;
        private Interlocking _locker1;
        private Interlocking _locker2;
        private EventStackHolder _eventStackHolder;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("AbstractLockerConsolidatorTests");

            _consolidator = CreateChild(_testRoot, "Consolidator")
                .AddComponent<FakeLockerConsolidator>();

            _eventStackHolder = CreateChild(_testRoot, "EventStackHolder")
                .AddComponent<EventStackHolder>();

            _consolidator.eventStackHolder = _eventStackHolder;
            SetLockData(_consolidator, new UdonSharpBehaviour[0], new bool[0], new int[0]);

            _locker1 = CreateInterlocking(_testRoot, "Locker1");
            _locker2 = CreateInterlocking(_testRoot, "Locker2");
        }

        [TearDown]
        public void Teardown()
        {
            if (_testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_testRoot);
            }
        }

        public static GameObject CreateChild(GameObject root, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child;
        }

        public static Interlocking CreateInterlocking(GameObject root, string name)
        {
            GameObject interlockingObject = CreateChild(root, name);
            Interlocking interlocking = interlockingObject.AddComponent<Interlocking>();

            RouteLocker routeLocker = interlockingObject.AddComponent<RouteLocker>();
            routeLocker.Lockers = new AbstractLockerConsolidator[0];
            routeLocker.SetParentInterlock(interlocking);

            GameObject fromLockerObject = CreateChild(root, name + "_FromLocker");
            FakeLockerConsolidator fromLocker = fromLockerObject.AddComponent<FakeLockerConsolidator>();
            fromLocker.SetLockData(new UdonSharpBehaviour[0], new bool[0], new int[0]);

            SetRequiredField(interlocking, "From_Locker", fromLocker);
            SetRequiredField(interlocking, "toLocker_States", new LockerAndMeetPosition[0]);
            interlocking.affectedInterlockings = new Interlocking[0];
            interlocking.affectedLockers = new AbstractLockerConsolidator[0];

            return interlocking;
        }

        public static void SetLockData(FakeLockerConsolidator consolidator,
            UdonSharpBehaviour[] lockers,
            bool[] lockStates,
            int[] lockIndices)
        {
            Assert.That(lockers, Is.Not.Null);
            Assert.That(lockStates, Is.Not.Null);
            Assert.That(lockIndices, Is.Not.Null);
            Assert.That(lockStates.Length, Is.EqualTo(lockers.Length),
                "ロッカー配列とロック状態配列の要素数が一致していません。");
            Assert.That(lockIndices.Length, Is.EqualTo(lockers.Length),
                "ロッカー配列とロック位置配列の要素数が一致していません。");

            consolidator.SetLockData(lockers, lockStates, lockIndices);
        }

        public static void SetRequiredField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, target.GetType().Name + "." + fieldName + " が見つかりません。");
            field.SetValue(target, value);
        }

        [Test]
        public void IsLocked_WithNoLocks_ReturnsFalse()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1, _locker2 },
                new[] { false, false },
                new[] { 0, 0 });

            Assert.IsFalse(_consolidator.IsLocked());
        }

        [Test]
        public void IsLocked_WithAnyLock_ReturnsTrue()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1, _locker2 },
                new[] { false, true },
                new[] { 0, 5 });

            Assert.IsTrue(_consolidator.IsLocked());
        }

        [Test]
        public void IsLocked_WithEmptyArrays_ReturnsFalse()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[0],
                new bool[0],
                new int[0]);

            Assert.IsFalse(_consolidator.IsLocked());
        }

        [Test]
        public void AddNewLocker_WithNewInstance_AddsLockerAndInitializesArrays()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[0],
                new bool[0],
                new int[0]);

            bool result = _consolidator.AddNewLocker(_locker1);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _consolidator.GetLockerInstanceCount());
            Assert.AreEqual(1, _consolidator.GetLockStateCount());
            Assert.AreEqual(1, _consolidator.GetLockIndexCount());
            Assert.IsFalse(_consolidator.GetLockState(0));
            Assert.AreEqual(0, _consolidator.GetLockIndex(0));
        }

        [Test]
        public void AddNewLocker_WithDuplicateInstance_ReturnsFalse()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1 },
                new[] { false },
                new[] { 0 });

            bool result = _consolidator.AddNewLocker(_locker1);

            Assert.IsFalse(result);
            Assert.AreEqual(1, _consolidator.GetLockerInstanceCount());
        }

        [Test]
        public void AddNewLocker_PreservesExistingDataAndInitializesNewElements()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1 },
                new[] { true },
                new[] { 5 });

            bool result = _consolidator.AddNewLocker(_locker2);

            Assert.IsTrue(result);
            Assert.AreEqual(2, _consolidator.GetLockerInstanceCount());
            Assert.AreEqual(2, _consolidator.GetLockStateCount());
            Assert.AreEqual(2, _consolidator.GetLockIndexCount());

            Assert.IsTrue(_consolidator.GetLockState(0));
            Assert.AreEqual(5, _consolidator.GetLockIndex(0));
            Assert.IsFalse(_consolidator.GetLockState(1));
            Assert.AreEqual(0, _consolidator.GetLockIndex(1));
        }

        [Test]
        public void TryUpdateLocking_WithNewLocker_AddsAndLocks()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[0],
                new bool[0],
                new int[0]);

            bool result = _consolidator.TryUpdateLocking(_locker1, true, 5);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _consolidator.GetLockerInstanceCount());
            Assert.IsTrue(_consolidator.IsLocked());
            Assert.AreEqual(5, _consolidator.LastAppliedPosition);
        }

        [Test]
        public void TryUpdateLocking_UnlockingExistingLocker_ReleasesLock()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1 },
                new[] { true },
                new[] { 5 });

            bool result = _consolidator.TryUpdateLocking(_locker1, false, 5);

            Assert.IsTrue(result);
            Assert.IsFalse(_consolidator.IsLocked());
        }

        [Test]
        public void TryUpdateLocking_WithSameLockPosition_Succeeds()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1 },
                new[] { true },
                new[] { 5 });

            bool result = _consolidator.TryUpdateLocking(_locker2, true, 5);

            Assert.IsTrue(result);
            Assert.AreEqual(2, _consolidator.GetLockerInstanceCount());
            Assert.IsTrue(_consolidator.IsLocked());
        }

        [Test]
        public void TryUpdateLocking_SameLockerChangesPosition_Succeeds()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1 },
                new[] { true },
                new[] { 5 });

            bool result = _consolidator.TryUpdateLocking(_locker1, true, 10);

            Assert.IsTrue(result);
            Assert.AreEqual(10, _consolidator.GetLockIndex(0));
            Assert.AreEqual(10, _consolidator.LastAppliedPosition);
        }

        [Test]
        public void TryUpdateLocking_ConflictingPositions_ReturnsFalse_ThenUnlockingOneResolvesConflict()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1, _locker2 },
                new[] { false, false },
                new[] { 0, 0 });

            bool firstLockResult = _consolidator.TryUpdateLocking(_locker1, true, 5);
            bool conflictingLockResult = _consolidator.TryUpdateLocking(_locker2, true, 10);

            Assert.IsTrue(firstLockResult);
            Assert.IsFalse(conflictingLockResult);

            bool unlockResult = _consolidator.TryUpdateLocking(_locker2, false, 10);

            Assert.IsTrue(unlockResult);
            Assert.IsTrue(_consolidator.IsLocked());
            Assert.IsFalse(_consolidator.GetLockState(1));
            Assert.IsTrue(_consolidator.GetLockState(0));
            Assert.AreEqual(5, _consolidator.LastAppliedPosition);
        }

        [Test]
        public void TryUpdateLocking_LeavesEventStackBalanced()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[0],
                new bool[0],
                new int[0]);

            bool result = _consolidator.TryUpdateLocking(_locker1, true, 5);

            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
        }

        [Test]
        public void CheckFaultAndGetLockIndex_WithNoLocks_ReturnsNoFaultAndMinusOne()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1, _locker2 },
                new[] { false, false },
                new[] { 0, 0 });

            _consolidator.EvaluateLockFault(out bool lockFault, out int lockIndex);

            Assert.IsFalse(lockFault);
            Assert.AreEqual(-1, lockIndex);
        }

        [Test]
        public void CheckFaultAndGetLockIndex_WithLocksAtSamePosition_ReturnsNoFault()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1, _locker2 },
                new[] { true, true },
                new[] { 5, 5 });

            _consolidator.EvaluateLockFault(out bool lockFault, out int lockIndex);

            Assert.IsFalse(lockFault);
            Assert.AreEqual(5, lockIndex);
        }

        [Test]
        public void CheckFaultAndGetLockIndex_WithLocksAtDifferentPositions_ReturnsFault()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1, _locker2 },
                new[] { true, true },
                new[] { 5, 10 });

            _consolidator.EvaluateLockFault(out bool lockFault, out int lockIndex);

            Assert.IsTrue(lockFault);
            Assert.AreEqual(5, lockIndex);
        }

        [Test]
        public void ResetUpdatedDetectorFlag_ClearsFlag()
        {
            _consolidator.SetUpdatedDetectorFlag(true);

            _consolidator.ResetFlagPosChangeOnLockDetector();

            Assert.IsFalse(_consolidator.GetFlagPosChangeOnLockDetector());
        }

        [Test]
        public void RelayFailSafe_DispatchesToRegisteredInterlocking()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1 },
                new[] { false },
                new[] { 0 });

            _consolidator.RelayFailSafe();

            Assert.IsTrue(_locker1.FailSafeCalled);
        }

        [Test]
        public void SetToFailSafePosition_ClearsLocksAndAppliesFailSafePosition()
        {
            SetLockData(_consolidator,
                new UdonSharpBehaviour[] { _locker1, _locker2 },
                new[] { true, true },
                new[] { 5, 5 });
            _consolidator.SetFailSafeIndex(0);
            _consolidator.SetCurrentPosition(5);

            _consolidator.SetToFailSafePosition();

            Assert.IsFalse(_consolidator.IsLocked());
            Assert.IsFalse(_consolidator.GetLockState(0));
            Assert.IsFalse(_consolidator.GetLockState(1));
            Assert.AreEqual(0, _consolidator.GetLockIndex(0));
            Assert.AreEqual(0, _consolidator.GetLockIndex(1));
            Assert.AreEqual(0, _consolidator.LastAppliedPosition);
            Assert.IsTrue(_consolidator.GetFlagPosChangeOnLockDetector());
        }
    }

    public class FakeLockerConsolidator : AbstractLockerConsolidator
    {
        public bool PositionWasApplied { get; private set; }
        public int LastAppliedPosition { get; private set; }
        private int _testCurrentPosition;

        public void SetLockData(
            UdonSharpBehaviour[] lockers,
            bool[] lockStates,
            int[] lockIndices)
        {
            SetField("lockerInstances", lockers);
            SetField("SettedLockStates", lockStates);
            SetField("SettedLockIndex", lockIndices);
        }

        public void SetCurrentPosition(int position)
        {
            _testCurrentPosition = position;
        }

        public void SetFailSafeIndex(int index)
        {
            SetField("failSafeIndex", index);
        }

        public void SetUpdatedDetectorFlag(bool value)
        {
            SetField("PositionUpdatedByLocking", value);
        }

        public int GetLockerInstanceCount()
        {
            return GetField<UdonSharpBehaviour[]>("lockerInstances").Length;
        }

        public int GetLockStateCount()
        {
            return GetField<bool[]>("SettedLockStates").Length;
        }

        public int GetLockIndexCount()
        {
            return GetField<int[]>("SettedLockIndex").Length;
        }

        public bool GetLockState(int index)
        {
            return GetField<bool[]>("SettedLockStates")[index];
        }

        public int GetLockIndex(int index)
        {
            return GetField<int[]>("SettedLockIndex")[index];
        }

        public void EvaluateLockFault(out bool lockFault, out int lockIndex)
        {
            base.CheckFaultAndGetLockIndex(out lockFault, out lockIndex);
        }

        public override int GetCurrentPosition()
        {
            return _testCurrentPosition;
        }

        protected override void ApplyLockToController(bool state)
        {
        }

        protected override void ApplyPositionToController(int posIndex)
        {
            PositionWasApplied = true;
            LastAppliedPosition = posIndex;
        }

        public override bool IsControllerOwner()
        {
            return true;
        }

        public override void SetControllerOwner()
        {
        }

        protected override void SyncController()
        {
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = typeof(AbstractLockerConsolidator).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new InvalidOperationException(
                    typeof(AbstractLockerConsolidator).Name + "." + fieldName + " が見つかりません。");
            }

            field.SetValue(this, value);
        }

        private T GetField<T>(string fieldName) where T : class
        {
            FieldInfo field = typeof(AbstractLockerConsolidator).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new InvalidOperationException(
                    typeof(AbstractLockerConsolidator).Name + "." + fieldName + " が見つかりません。");
            }

            T value = field.GetValue(this) as T;
            if (value == null)
            {
                throw new InvalidOperationException(
                    typeof(AbstractLockerConsolidator).Name + "." + fieldName + " が null です。");
            }

            return value;
        }
    }
}