using System;
using System.Reflection;
using NUnit.Framework;
using RBUR_SignalIntegrator;
using UdonSharp;
using UnityEngine;

public class AbstractLockerConsolidater_Test
{
    private GameObject _testRoot;
    private TestLockerConsolidater _consolidater;
    private Interlocking _locker1;
    private Interlocking _locker2;
    private EventStackHolder _eventStackHolder;

    [SetUp]
    public void Setup()
    {
        _testRoot = new GameObject("AbstractLockerConsolidater_Test");

        _consolidater = CreateChild("Consolidater")
            .AddComponent<TestLockerConsolidater>();

        _eventStackHolder = CreateChild("EventStackHolder")
            .AddComponent<EventStackHolder>();

        _consolidater.eventStackHolder = _eventStackHolder;
        SetLockData(new UdonSharpBehaviour[0], new bool[0], new int[0]);

        _locker1 = CreateInterlocking("Locker1");
        _locker2 = CreateInterlocking("Locker2");
    }

    [TearDown]
    public void Teardown()
    {
        if (_testRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(_testRoot);
        }
    }

    private GameObject CreateChild(string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(_testRoot.transform);
        return child;
    }

    private Interlocking CreateInterlocking(string name)
    {
        GameObject interlockingObject = CreateChild(name);
        Interlocking interlocking = interlockingObject.AddComponent<Interlocking>();

        RouteLocker routeLocker = interlockingObject.AddComponent<RouteLocker>();
        routeLocker.Lockers = new AbstractLockerConsolidater[0];
        routeLocker.setParentInterlock(interlocking);

        GameObject fromLockerObject = CreateChild(name + "_FromLocker");
        TestLockerConsolidater fromLocker = fromLockerObject.AddComponent<TestLockerConsolidater>();
        fromLocker.SetLockData(new UdonSharpBehaviour[0], new bool[0], new int[0]);

        SetRequiredField(interlocking, "From_Locker", fromLocker);
        SetRequiredField(interlocking, "toLocker_States", new LockerAndMeetPosition[0]);
        interlocking.affectedInterlockings = new Interlocking[0];
        interlocking.affectedLockers = new AbstractLockerConsolidater[0];

        return interlocking;
    }

    private void SetLockData(
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

        _consolidater.SetLockData(lockers, lockStates, lockIndices);
    }

    private static void SetRequiredField(object target, string fieldName, object value)
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
        SetLockData(
            new UdonSharpBehaviour[] { _locker1, _locker2 },
            new[] { false, false },
            new[] { 0, 0 });

        Assert.IsFalse(_consolidater.isLocked());
    }

    [Test]
    public void IsLocked_WithAnyLock_ReturnsTrue()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1, _locker2 },
            new[] { false, true },
            new[] { 0, 5 });

        Assert.IsTrue(_consolidater.isLocked());
    }

    [Test]
    public void IsLocked_WithEmptyArrays_ReturnsFalse()
    {
        SetLockData(
            new UdonSharpBehaviour[0],
            new bool[0],
            new int[0]);

        Assert.IsFalse(_consolidater.isLocked());
    }

    [Test]
    public void AddNewLocker_WithNewInstance_AddsLockerAndInitializesArrays()
    {
        SetLockData(
            new UdonSharpBehaviour[0],
            new bool[0],
            new int[0]);

        bool result = _consolidater.AddNewLocker(_locker1);

        Assert.IsTrue(result);
        Assert.AreEqual(1, _consolidater.GetLockerInstanceCount());
        Assert.AreEqual(1, _consolidater.GetLockStateCount());
        Assert.AreEqual(1, _consolidater.GetLockIndexCount());
        Assert.IsFalse(_consolidater.GetLockState(0));
        Assert.AreEqual(0, _consolidater.GetLockIndex(0));
    }

    [Test]
    public void AddNewLocker_WithDuplicateInstance_ReturnsFalse()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { false },
            new[] { 0 });

        bool result = _consolidater.AddNewLocker(_locker1);

        Assert.IsFalse(result);
        Assert.AreEqual(1, _consolidater.GetLockerInstanceCount());
    }

    [Test]
    public void AddNewLocker_PreservesExistingDataAndInitializesNewElements()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });

        bool result = _consolidater.AddNewLocker(_locker2);

        Assert.IsTrue(result);
        Assert.AreEqual(2, _consolidater.GetLockerInstanceCount());
        Assert.AreEqual(2, _consolidater.GetLockStateCount());
        Assert.AreEqual(2, _consolidater.GetLockIndexCount());

        Assert.IsTrue(_consolidater.GetLockState(0));
        Assert.AreEqual(5, _consolidater.GetLockIndex(0));
        Assert.IsFalse(_consolidater.GetLockState(1));
        Assert.AreEqual(0, _consolidater.GetLockIndex(1));
    }

    [Test]
    public void TryUpdateLocking_WithNewLocker_AddsAndLocks()
    {
        SetLockData(
            new UdonSharpBehaviour[0],
            new bool[0],
            new int[0]);

        bool result = _consolidater.tryUpdateLocking(_locker1, true, 5);

        Assert.IsTrue(result);
        Assert.AreEqual(1, _consolidater.GetLockerInstanceCount());
        Assert.IsTrue(_consolidater.isLocked());
        Assert.AreEqual(5, _consolidater.LastAppliedPosition);
    }

    [Test]
    public void TryUpdateLocking_UnlockingExistingLocker_ReleasesLock()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });

        bool result = _consolidater.tryUpdateLocking(_locker1, false, 5);

        Assert.IsTrue(result);
        Assert.IsFalse(_consolidater.isLocked());
    }

    [Test]
    public void TryUpdateLocking_WithSameLockPosition_Succeeds()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });

        bool result = _consolidater.tryUpdateLocking(_locker2, true, 5);

        Assert.IsTrue(result);
        Assert.AreEqual(2, _consolidater.GetLockerInstanceCount());
        Assert.IsTrue(_consolidater.isLocked());
    }

    [Test]
    public void TryUpdateLocking_SameLockerChangesPosition_Succeeds()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });

        bool result = _consolidater.tryUpdateLocking(_locker1, true, 10);

        Assert.IsTrue(result);
        Assert.AreEqual(10, _consolidater.GetLockIndex(0));
        Assert.AreEqual(10, _consolidater.LastAppliedPosition);
    }

    [Test]
    public void TryUpdateLocking_ConflictingPositions_ReturnsFalse_ThenUnlockingOneResolvesConflict()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1, _locker2 },
            new[] { false, false },
            new[] { 0, 0 });

        bool firstLockResult = _consolidater.tryUpdateLocking(_locker1, true, 5);
        bool conflictingLockResult = _consolidater.tryUpdateLocking(_locker2, true, 10);

        Assert.IsTrue(firstLockResult);
        Assert.IsFalse(conflictingLockResult);

        bool unlockResult = _consolidater.tryUpdateLocking(_locker2, false, 10);

        Assert.IsTrue(unlockResult);
        Assert.IsTrue(_consolidater.isLocked());
        Assert.IsFalse(_consolidater.GetLockState(1));
        Assert.IsTrue(_consolidater.GetLockState(0));
        Assert.AreEqual(5, _consolidater.LastAppliedPosition);
    }

    [Test]
    public void TryUpdateLocking_LeavesEventStackBalanced()
    {
        SetLockData(
            new UdonSharpBehaviour[0],
            new bool[0],
            new int[0]);

        bool result = _consolidater.tryUpdateLocking(_locker1, true, 5);

        Assert.IsTrue(result);
        Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
    }

    [Test]
    public void CheckFaultAndGetLockIndex_WithNoLocks_ReturnsNoFaultAndMinusOne()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1, _locker2 },
            new[] { false, false },
            new[] { 0, 0 });

        _consolidater.EvaluateLockFault(out bool lockFault, out int lockIndex);

        Assert.IsFalse(lockFault);
        Assert.AreEqual(-1, lockIndex);
    }

    [Test]
    public void CheckFaultAndGetLockIndex_WithLocksAtSamePosition_ReturnsNoFault()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1, _locker2 },
            new[] { true, true },
            new[] { 5, 5 });

        _consolidater.EvaluateLockFault(out bool lockFault, out int lockIndex);

        Assert.IsFalse(lockFault);
        Assert.AreEqual(5, lockIndex);
    }

    [Test]
    public void CheckFaultAndGetLockIndex_WithLocksAtDifferentPositions_ReturnsFault()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1, _locker2 },
            new[] { true, true },
            new[] { 5, 10 });

        _consolidater.EvaluateLockFault(out bool lockFault, out int lockIndex);

        Assert.IsTrue(lockFault);
        Assert.AreEqual(5, lockIndex);
    }

    [Test]
    public void ResetUpdatedDetectorFlag_ClearsFlag()
    {
        _consolidater.SetUpdatedDetectorFlag(true);

        _consolidater.ResetFlagPosChangeOnLockDetector();

        Assert.IsFalse(_consolidater.GetFlagPosChangeOnLockDetector());
    }

    [Test]
    public void RelayFailSafe_DispatchesToRegisteredInterlocking()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { false },
            new[] { 0 });

        _consolidater.RelayFailSafe();

        Assert.IsTrue(_locker1.FailSafeCalled);
    }

    [Test]
    public void SetToFailSafePosition_ClearsLocksAndAppliesFailSafePosition()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1, _locker2 },
            new[] { true, true },
            new[] { 5, 5 });
        _consolidater.SetFailSafeIndex(0);
        _consolidater.SetCurrentPosition(5);

        _consolidater.SetToFailSafePosition();

        Assert.IsFalse(_consolidater.isLocked());
        Assert.IsFalse(_consolidater.GetLockState(0));
        Assert.IsFalse(_consolidater.GetLockState(1));
        Assert.AreEqual(0, _consolidater.GetLockIndex(0));
        Assert.AreEqual(0, _consolidater.GetLockIndex(1));
        Assert.AreEqual(0, _consolidater.LastAppliedPosition);
        Assert.IsTrue(_consolidater.GetFlagPosChangeOnLockDetector());
    }
}

public class TestLockerConsolidater : AbstractLockerConsolidater
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

    protected override void applyLockToController(bool state)
    {
    }

    protected override void applyPositionToController(int posIndex)
    {
        PositionWasApplied = true;
        LastAppliedPosition = posIndex;
    }

    public override bool isControllerOwner()
    {
        return true;
    }

    public override void setControllerOwner()
    {
    }

    protected override void SyncController()
    {
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(AbstractLockerConsolidater).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractLockerConsolidater).Name + "." + fieldName + " が見つかりません。");
        }

        field.SetValue(this, value);
    }

    private T GetField<T>(string fieldName) where T : class
    {
        FieldInfo field = typeof(AbstractLockerConsolidater).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractLockerConsolidater).Name + "." + fieldName + " が見つかりません。");
        }

        T value = field.GetValue(this) as T;
        if (value == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractLockerConsolidater).Name + "." + fieldName + " が null です。");
        }

        return value;
    }
}