using System;
using System.Reflection;
using NUnit.Framework;
using RBUR_SignalIntegrator;
using UdonSharp;
using UnityEngine;
using VRC.Udon;

/// <summary>
/// Unit tests for AbstractIndirectController
/// Tests cover: TrySetControlTarget(), AddNewTargeter(), EvaluateControlTarget(), 
/// EvaluateUserDirectControl(), isLocalOverride(), UpdateLocking() override, 
/// applyPositionToController() override, and related functionality
/// 
/// Design:
/// - Uses MockLocker instead of VRC SDK dependencies
/// - EventStackHolder can be null (gracefully handled)
/// - Tests focus on control target evaluation and position management
/// </summary>
public class IndirectController_Test
{
    private GameObject _testRoot;
    private TestIndirectController _indirectController;
    private MockLocker _locker1;
    private MockLocker _locker2;
    private EventStackHolder _eventStackHolder;

    [SetUp]
    public void Setup()
    {
        _testRoot = new GameObject("IndirectController_Test");

        _indirectController = CreateChild("IndirectController")
            .AddComponent<TestIndirectController>();

        _eventStackHolder = CreateChild("EventStackHolder")
            .AddComponent<EventStackHolder>();

        _indirectController.eventStackHolder = _eventStackHolder;

        SetLockData(new UdonSharpBehaviour[0], new bool[0], new int[0]);
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        _locker1 = new MockLocker("Locker1");
        _locker2 = new MockLocker("Locker2");

        _indirectController.ReferingInterlocks = new Interlocking[0];
        _indirectController.LockstateCallbackBehaviours = new UdonBehaviour[0];

        // Start() を明示的に呼んで初期化を完了させる
        _indirectController.StartPublic();
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

        _indirectController.SetLockData(lockers, lockStates, lockIndices);
    }

    private void SetTargeterData(
        UdonSharpBehaviour[] targeters,
        int[] controlTargets)
    {
        Assert.That(targeters, Is.Not.Null);
        Assert.That(controlTargets, Is.Not.Null);
        Assert.That(controlTargets.Length, Is.EqualTo(targeters.Length),
            "ターゲッター配列と制御ターゲット配列の要素数が一致していません。");

        _indirectController.SetTargeterData(targeters, controlTargets);
    }

    // ========== TrySetControlTarget() Tests ==========

    [Test]
    public void TrySetControlTarget_WithNewTargeter_AddsTargeterAndSetsControl()
    {
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        _indirectController.TrySetControlTarget(_locker1, 10, false);

        Assert.AreEqual(1, _indirectController.GetTargeterInstanceCount());
        Assert.AreEqual(10, _indirectController.GetControlTarget(0));
    }

    [Test]
    public void TrySetControlTarget_WithExistingTargeter_UpdatesControl()
    {
        // Create a real UdonSharpBehaviour mock wrapper for existing targeters
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 5 });

        _indirectController.TrySetControlTarget(realLocker1, 15, false);

        Assert.AreEqual(1, _indirectController.GetTargeterInstanceCount());
        Assert.AreEqual(15, _indirectController.GetControlTarget(0));
    }

    [Test]
    public void TrySetControlTarget_WithUserDirectControl_SetsUserDirectIndexAndAppliesPosition()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 0 });
        _indirectController.SetCurrentPosition(0);

        _indirectController.TrySetControlTarget(realLocker1, 10, true);

        Assert.AreEqual(10, _indirectController.GetControlTarget(0));
        Assert.AreEqual(0, _indirectController.GetUserDirectIndex());
        Assert.AreEqual(10, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void TrySetControlTarget_MultipleTargetersSwitchUserDirect()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { -1, 0 });
        _indirectController.SetCurrentPosition(5);

        // Act: Set locker2's control target to 20 with UserDirect=true
        _indirectController.TrySetControlTarget(realLocker2, 20, true);

        // Assert
        // UserDirectIndex should be set to index of locker2 (which is 1)
        Assert.AreEqual(1, _indirectController.GetUserDirectIndex());
        // Control target for locker2 should be 20
        Assert.AreEqual(20, _indirectController.GetControlTarget(1));
        // LastAppliedPosition should be 20 (from EvaluateControlTarget() → trySetPosition())
        Assert.AreEqual(20, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void TrySetControlTarget_LeavesEventStackBalanced()
    {
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        _indirectController.TrySetControlTarget(_locker1, 10, false);

        Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
    }

    // ========== AddNewTargeter() Tests ==========

    [Test]
    public void AddNewTargeter_WithNewInstance_AddsTargeterAndInitializesControl()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        bool result = _indirectController.AddNewTargeter(realLocker1);

        Assert.IsTrue(result);
        Assert.AreEqual(1, _indirectController.GetTargeterInstanceCount());
        Assert.AreEqual(1, _indirectController.GetControlTargetArrayLength());
        Assert.AreEqual(-1, _indirectController.GetControlTarget(0));
    }

    [Test]
    public void AddNewTargeter_WithDuplicateInstance_ReturnsFalse()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 10 });

        bool result = _indirectController.AddNewTargeter(realLocker1);

        Assert.IsFalse(result);
        Assert.AreEqual(1, _indirectController.GetTargeterInstanceCount());
    }

    [Test]
    public void AddNewTargeter_PreservesExistingDataAndInitializesNewElements()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 5 });

        bool result = _indirectController.AddNewTargeter(realLocker2);

        Assert.IsTrue(result);
        Assert.AreEqual(2, _indirectController.GetTargeterInstanceCount());
        Assert.AreEqual(2, _indirectController.GetControlTargetArrayLength());
        Assert.AreEqual(5, _indirectController.GetControlTarget(0));
        Assert.AreEqual(-1, _indirectController.GetControlTarget(1));
    }

    // ========== EvaluateControlTarget() Tests ==========

    [Test]
    public void EvaluateControlTarget_WithNoTargets_ReturnsMinusOne()
    {
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        int result = _indirectController.EvaluateControlTarget();

        Assert.AreEqual(-1, result);
    }

    [Test]
    public void EvaluateControlTarget_WithAllTargetsMinusOne_ReturnsMinusOne()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { -1, -1 });

        int result = _indirectController.EvaluateControlTarget();

        Assert.AreEqual(-1, result);
    }

    [Test]
    public void EvaluateControlTarget_ReturnsFirstNonMinusOneTarget()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { -1, 10 });

        int result = _indirectController.EvaluateControlTarget();

        Assert.AreEqual(10, result);
    }

    [Test]
    public void EvaluateControlTarget_WithMultipleValidTargets_ReturnsFirst()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { 5, 10 });

        int result = _indirectController.EvaluateControlTarget();

        Assert.AreEqual(5, result);
    }

    // ========== EvaluateUserDirectControl() Tests ==========

    [Test]
    public void EvaluateUserDirectControl_WithNoUserDirect_ReturnsMinusOne()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { 5, 10 });
        _indirectController.SetUserDirectIndex(-1);

        int result = _indirectController.EvaluateUserDirectControl();

        Assert.AreEqual(-1, result);
    }

    [Test]
    public void EvaluateUserDirectControl_WithValidUserDirectIndex_ReturnsCorrectTarget()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { 5, 15 });
        _indirectController.SetUserDirectIndex(1);

        int result = _indirectController.EvaluateUserDirectControl();

        Assert.AreEqual(15, result);
    }

    [Test]
    public void EvaluateUserDirectControl_WithOutOfRangeIndex_ReturnsMinusOne()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 5 });
        _indirectController.SetUserDirectIndex(5);

        int result = _indirectController.EvaluateUserDirectControl();

        Assert.AreEqual(-1, result);
    }

    // ========== applyPositionToController() Override Tests ==========

    [Test]
    public void ApplyPositionToController_WithDifferentPosition_UpdatesControllingPosition()
    {
        _indirectController.SetCurrentPosition(5);

        _indirectController.ApplyPositionPublic(10);

        Assert.AreEqual(10, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void ApplyPositionToController_WithSamePosition_DoesNotUpdate()
    {
        _indirectController.SetCurrentPosition(5);

        _indirectController.ApplyPositionPublic(5);

        Assert.AreEqual(5, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void ApplyPositionToController_WithMinusOne_DoesNotUpdate()
    {
        _indirectController.SetCurrentPosition(5);

        _indirectController.ApplyPositionPublic(-1);

        Assert.AreEqual(5, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void ApplyPositionToController_LeavesEventStackBalanced()
    {
        _indirectController.ApplyPositionPublic(10);

        Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
    }

    // ========== OnDeserialization() Tests ==========

    [Test]
    public void OnDeserialization_AppliesCurrentPosition()
    {
        _indirectController.SetCurrentPosition(10);
        _indirectController.ReferingInterlocks = new Interlocking[0];

        _indirectController.OnDeserializationPublic();

        Assert.AreEqual(10, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void OnDeserialization_LeavesEventStackBalanced()
    {
        _indirectController.ReferingInterlocks = new Interlocking[0];

        _indirectController.OnDeserializationPublic();

        Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
    }

    // ========== TrySetControlTarget() Tests - 戻り値検証追加 ==========

    [Test]
    public void TrySetControlTarget_WithNewTargeter_ReturnsTrue()
    {
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        bool result = _indirectController.TrySetControlTarget(_locker1, 10, false);

        Assert.IsTrue(result);
        Assert.AreEqual(1, _indirectController.GetTargeterInstanceCount());
        Assert.AreEqual(10, _indirectController.GetControlTarget(0));
    }

    [Test]
    public void TrySetControlTarget_NotLockedAppliesPosition_ReturnsTrue()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { -1 });
        SetLockData(
            new UdonSharpBehaviour[0],
            new bool[0],
            new int[0]);
        _indirectController.SetCurrentPosition(0);

        bool result = _indirectController.TrySetControlTarget(realLocker1, 10, false);

        Assert.IsTrue(result);
        Assert.AreEqual(10, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void TrySetControlTarget_LockedDoesNotApplyPosition()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { -1 });
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });
        _indirectController.SetCurrentPosition(5);

        _indirectController.TrySetControlTarget(realLocker1, 10, false);

        Assert.AreEqual(5, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void TrySetControlTarget_CacheClearedOnNewControl()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 10 });

        int firstEval = _indirectController.EvaluateControlTarget();
        _indirectController.TrySetControlTarget(realLocker1, 20, false);
        int secondEval = _indirectController.EvaluateControlTarget();

        Assert.AreEqual(10, firstEval);
        Assert.AreEqual(20, secondEval);
    }


    // ========== UpdateLocking() Override Tests ==========

    [Test]
    public void UpdateLocking_WhenLockStateChangesToTrue_DoesNotApplyPosition()
    {
        SetLockData(new UdonSharpBehaviour[0], new bool[0], new int[0]);
        SetTargeterData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { 10 });
        _indirectController.SetCurrentPosition(5);

        _indirectController.tryUpdateLocking(_locker1, true, 5);

        // ロック時は位置を適用しない
        Assert.AreNotEqual(10, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void UpdateLocking_WhenLockStateChangesToFalse_AppliesEvaluatedControlTarget()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 15 });
        _indirectController.SetCurrentPosition(5);

        _indirectController.tryUpdateLocking(_locker1, false, 5);

        Assert.IsFalse(_indirectController.isLocked());
        Assert.AreEqual(15, _indirectController.GetCurrentPosition());
    }

    [Test]
    public void UpdateLocking_WhenLockStateChangesAndControlPositionSame_DoesNotCallSetUpdatedFlag()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });
        SetTargeterData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { 5 });
        _indirectController.SetCurrentPosition(5);

        _indirectController.tryUpdateLocking(_locker1, false, 5);

        // 位置が同じなので UpdatedDetectorFlag は立たない
        Assert.IsFalse(_indirectController.GetFlagPosChangeOnLockDetector());
    }

    [Test]
    public void UpdateLocking_WhenLockStateChangesAndNoActiveController_DoesNotCallSetUpdatedFlag()
    {
        SetLockData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { true },
            new[] { 5 });
        SetTargeterData(
            new UdonSharpBehaviour[] { _locker1 },
            new[] { -1 });
        _indirectController.SetCurrentPosition(0);

        _indirectController.tryUpdateLocking(_locker1, false, 5);

        // 位置が同じなので UpdatedDetectorFlag は立たない
        Assert.IsFalse(_indirectController.GetFlagPosChangeOnLockDetector());
    }

    // ========== EvaluateControlTarget() Caching Tests ==========

    [Test]
    public void EvaluateControlTarget_CacheHit_ReturnsCachedValue()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestIndirectController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestIndirectController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { 5, 10 });

        int firstCall = _indirectController.EvaluateControlTarget();
        int secondCall = _indirectController.EvaluateControlTarget();

        Assert.AreEqual(5, firstCall);
        Assert.AreEqual(5, secondCall);
    }

    // ========== GetNetworkUpdate() Tests ==========

    [Test]
    public void GetNetworkUpdate_InitiallyFalse()
    {
        bool result = _indirectController.GetNetworkUpdatePublic();

        Assert.IsFalse(result);
    }

    // ========== OnDeserialization Tests ==========

    [Test]
    public void OnDeserialization_UpdatesSendControllingPosition()
    {
        _indirectController.SetCurrentPosition(10);
        _indirectController.ReferingInterlocks = new Interlocking[0];

        _indirectController.OnDeserializationPublic();

        Assert.AreEqual(10, _indirectController.GetCurrentPosition());
        Assert.AreEqual(10, _indirectController.GetSendControllingPositionPublic());
    }
}

/// <summary>
/// Concrete test implementation of AbstractIndirectController for testing.
/// Provides access to protected members and test-specific functionality.
/// Uses Reflection to bypass VRC SDK requirements.
/// </summary>
public class TestIndirectController : AbstractIndirectController
{
    public void StartPublic()
    {
        base.Start();
    }

    public void SetLockData(
        UdonSharpBehaviour[] lockers,
        bool[] lockStates,
        int[] lockIndices)
    {
        SetField("lockerInstances", lockers);
        SetField("SettedLockStates", lockStates);
        SetField("SettedLockIndex", lockIndices);
    }

    public void SetTargeterData(
        UdonSharpBehaviour[] targeters,
        int[] controlTargets)
    {
        SetField("TargeterInstances", targeters);
        SetField("ControlTargetsArray", controlTargets);
    }

    public void SetCurrentPosition(int position)
    {
        SetField("controllingPosition", position);
    }

    public void SetUserDirectIndex(int index)
    {
        SetField("UserDirectIndex", index);
    }

    public int GetTargeterInstanceCount()
    {
        return GetField<UdonSharpBehaviour[]>("TargeterInstances").Length;
    }

    public int GetControlTargetArrayLength()
    {
        return GetField<int[]>("ControlTargetsArray").Length;
    }

    public int GetControlTarget(int index)
    {
        return GetField<int[]>("ControlTargetsArray")[index];
    }

    public int GetUserDirectIndex()
    {
        return (int)GetFieldValue("UserDirectIndex");
    }

    public void ApplyPositionPublic(int posIndex)
    {
        base.applyPositionToController(posIndex);
    }

    public void OnDeserializationPublic()
    {
        base.OnDeserialization();
    }

    // Override only what's necessary for testing (VRC Networking dependencies)
    public override bool isControllerOwner()
    {
        return true;
    }

    public override void setControllerOwner()
    {
        // Test stub - no-op
    }

    protected override void SyncController()
    {
        // Test stub - no-op
    }

    protected override void applyPositionToController(int posIndex)
    {
        base.applyPositionToController(posIndex);
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(AbstractIndirectController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractIndirectController).Name + "." + fieldName + " が見つかりません。");
        }

        field.SetValue(this, value);
    }

    private T GetField<T>(string fieldName) where T : class
    {
        FieldInfo field = typeof(AbstractIndirectController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractIndirectController).Name + "." + fieldName + " が見つかりません。");
        }

        T value = field.GetValue(this) as T;
        if (value == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractIndirectController).Name + "." + fieldName + " が null です。");
        }

        return value;
    }

    private object GetFieldValue(string fieldName)
    {
        FieldInfo field = typeof(AbstractIndirectController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractIndirectController).Name + "." + fieldName + " が見つかりません。");
        }

        return field.GetValue(this);
    }

    public bool GetNetworkUpdatePublic()
    {
        return GetNetworkUpdate();
    }

    public int GetSendControllingPositionPublic()
    {
        FieldInfo field = typeof(AbstractIndirectController).GetField(
            "sendControllingPosition",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractIndirectController).Name + ".sendControllingPosition が見つかりません。");
        }

        return (int)field.GetValue(this);
    }

    protected override void PostApplyControl()
    {
    }
}

/// <summary>
/// Simple mock locker for testing without VRC SDK dependencies.
/// </summary>
public class MockLocker : UdonSharpBehaviour
{
    public string Name { get; set; }

    public MockLocker(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;
}
