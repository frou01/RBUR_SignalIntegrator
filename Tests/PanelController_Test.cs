using System;
using System.Reflection;
using NUnit.Framework;
using RBUR_SignalIntegrator;
using UdonSharp;
using UnityEngine;
using VRC.Udon;

/// <summary>
/// Unit tests for AbstractPanelController
/// Tests cover: TrySetControlTarget(), AddNewTargeter(), EvaluateControlTarget(), 
/// EvaluateUserDirectControl(), isLocalOverride(), UpdateLocking() override, 
/// applyPositionToController() override, and related functionality
/// 
/// Design:
/// - Uses MockLocker instead of VRC SDK dependencies
/// - EventStackHolder can be null (gracefully handled)
/// - Tests focus on control target evaluation and position management
/// </summary>
public class PanelController_Test
{
    private GameObject _testRoot;
    private TestPanelController _panelController;
    private MockLocker _locker1;
    private MockLocker _locker2;
    private EventStackHolder _eventStackHolder;

    [SetUp]
    public void Setup()
    {
        _testRoot = new GameObject("PanelController_Test");

        _panelController = CreateChild("PanelController")
            .AddComponent<TestPanelController>();

        _eventStackHolder = CreateChild("EventStackHolder")
            .AddComponent<EventStackHolder>();

        _panelController.eventStackHolder = _eventStackHolder;

        SetLockData(new UdonSharpBehaviour[0], new bool[0], new int[0]);
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        _locker1 = new MockLocker("Locker1");
        _locker2 = new MockLocker("Locker2");

        _panelController.ReferingInterlocks = new Interlocking[0];
        _panelController.LockstateCallbackBehaviours = new UdonBehaviour[0];
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

        _panelController.SetLockData(lockers, lockStates, lockIndices);
    }

    private void SetTargeterData(
        UdonSharpBehaviour[] targeters,
        int[] controlTargets)
    {
        Assert.That(targeters, Is.Not.Null);
        Assert.That(controlTargets, Is.Not.Null);
        Assert.That(controlTargets.Length, Is.EqualTo(targeters.Length),
            "ターゲッター配列と制御ターゲット配列の要素数が一致していません。");

        _panelController.SetTargeterData(targeters, controlTargets);
    }

    // ========== TrySetControlTarget() Tests ==========

    [Test]
    public void TrySetControlTarget_WithNewTargeter_AddsTargeterAndSetsControl()
    {
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        _panelController.TrySetControlTarget(_locker1, 10, false);

        Assert.AreEqual(1, _panelController.GetTargeterInstanceCount());
        Assert.AreEqual(10, _panelController.GetControlTarget(0));
    }

    [Test]
    public void TrySetControlTarget_WithExistingTargeter_UpdatesControl()
    {
        // Create a real UdonSharpBehaviour mock wrapper for existing targeters
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 5 });

        _panelController.TrySetControlTarget(realLocker1, 15, false);

        Assert.AreEqual(1, _panelController.GetTargeterInstanceCount());
        Assert.AreEqual(15, _panelController.GetControlTarget(0));
    }

    [Test]
    public void TrySetControlTarget_WithUserDirectControl_SetsUserDirectIndexAndAppliesPosition()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 0 });
        _panelController.SetCurrentPosition(0);

        _panelController.TrySetControlTarget(realLocker1, 10, true);

        Assert.AreEqual(10, _panelController.GetControlTarget(0));
        Assert.AreEqual(0, _panelController.GetUserDirectIndex());
        Assert.AreEqual(10, _panelController.LastAppliedPosition);
    }

    [Test]
    public void TrySetControlTarget_MultipleTargetersSwitchUserDirect()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { -1, 0 });
        _panelController.SetCurrentPosition(5);

        // Act: Set locker2's control target to 20 with UserDirect=true
        _panelController.TrySetControlTarget(realLocker2, 20, true);

        // Assert
        // UserDirectIndex should be set to index of locker2 (which is 1)
        Assert.AreEqual(1, _panelController.GetUserDirectIndex());
        // Control target for locker2 should be 20
        Assert.AreEqual(20, _panelController.GetControlTarget(1));
        // LastAppliedPosition should be 20 (from EvaluateControlTarget() → trySetPosition())
        Assert.AreEqual(20, _panelController.LastAppliedPosition);
    }

    [Test]
    public void TrySetControlTarget_LeavesEventStackBalanced()
    {
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        _panelController.TrySetControlTarget(_locker1, 10, false);

        Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
    }

    // ========== AddNewTargeter() Tests ==========

    [Test]
    public void AddNewTargeter_WithNewInstance_AddsTargeterAndInitializesControl()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        bool result = _panelController.AddNewTargeter(realLocker1);

        Assert.IsTrue(result);
        Assert.AreEqual(1, _panelController.GetTargeterInstanceCount());
        Assert.AreEqual(1, _panelController.GetControlTargetArrayLength());
        Assert.AreEqual(0, _panelController.GetControlTarget(0));
    }

    [Test]
    public void AddNewTargeter_WithDuplicateInstance_ReturnsFalse()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 10 });

        bool result = _panelController.AddNewTargeter(realLocker1);

        Assert.IsFalse(result);
        Assert.AreEqual(1, _panelController.GetTargeterInstanceCount());
    }

    [Test]
    public void AddNewTargeter_PreservesExistingDataAndInitializesNewElements()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 5 });

        bool result = _panelController.AddNewTargeter(realLocker2);

        Assert.IsTrue(result);
        Assert.AreEqual(2, _panelController.GetTargeterInstanceCount());
        Assert.AreEqual(2, _panelController.GetControlTargetArrayLength());
        Assert.AreEqual(5, _panelController.GetControlTarget(0));
        Assert.AreEqual(0, _panelController.GetControlTarget(1));
    }

    // ========== EvaluateControlTarget() Tests ==========

    [Test]
    public void EvaluateControlTarget_WithNoTargets_ReturnsMinusOne()
    {
        SetTargeterData(new UdonSharpBehaviour[0], new int[0]);

        int result = _panelController.EvaluateControlTarget();

        Assert.AreEqual(-1, result);
    }

    [Test]
    public void EvaluateControlTarget_WithAllTargetsMinusOne_ReturnsMinusOne()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { -1, -1 });

        int result = _panelController.EvaluateControlTarget();

        Assert.AreEqual(-1, result);
    }

    [Test]
    public void EvaluateControlTarget_ReturnsFirstNonMinusOneTarget()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { -1, 10 });

        int result = _panelController.EvaluateControlTarget();

        Assert.AreEqual(10, result);
    }

    [Test]
    public void EvaluateControlTarget_WithMultipleValidTargets_ReturnsFirst()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { 5, 10 });

        int result = _panelController.EvaluateControlTarget();

        Assert.AreEqual(5, result);
    }

    // ========== EvaluateUserDirectControl() Tests ==========

    [Test]
    public void EvaluateUserDirectControl_WithNoUserDirect_ReturnsMinusOne()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { 5, 10 });
        _panelController.SetUserDirectIndex(-1);

        int result = _panelController.EvaluateUserDirectControl();

        Assert.AreEqual(-1, result);
    }

    [Test]
    public void EvaluateUserDirectControl_WithValidUserDirectIndex_ReturnsCorrectTarget()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        var realLocker2 = CreateChild("RealLocker2").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1, realLocker2 },
            new[] { 5, 15 });
        _panelController.SetUserDirectIndex(1);

        int result = _panelController.EvaluateUserDirectControl();

        Assert.AreEqual(15, result);
    }

    [Test]
    public void EvaluateUserDirectControl_WithOutOfRangeIndex_ReturnsMinusOne()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 5 });
        _panelController.SetUserDirectIndex(5);

        int result = _panelController.EvaluateUserDirectControl();

        Assert.AreEqual(-1, result);
    }

    // ========== isLocalOverride() Tests ==========

    [Test]
    public void IsLocalOverride_WithUserDirectControl_ReturnsTrue()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 10 });
        _panelController.SetUserDirectIndex(0);

        bool result = _panelController.isLocalOverride();

        Assert.IsTrue(result);
    }

    [Test]
    public void IsLocalOverride_WithoutUserDirectControl_ReturnsFalse()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { 10 });
        _panelController.SetUserDirectIndex(-1);

        bool result = _panelController.isLocalOverride();

        Assert.IsFalse(result);
    }

    [Test]
    public void IsLocalOverride_WithUserDirectControlMinusOne_ReturnsFalse()
    {
        var realLocker1 = CreateChild("RealLocker1").AddComponent<TestPanelController>();
        SetTargeterData(
            new UdonSharpBehaviour[] { realLocker1 },
            new[] { -1 });
        _panelController.SetUserDirectIndex(0);

        bool result = _panelController.isLocalOverride();

        Assert.IsFalse(result);
    }

    // ========== applyPositionToController() Override Tests ==========

    [Test]
    public void ApplyPositionToController_WithDifferentPosition_UpdatesControllingPosition()
    {
        _panelController.SetCurrentPosition(5);

        _panelController.ApplyPositionPublic(10);

        Assert.AreEqual(10, _panelController.GetCurrentPosition());
    }

    [Test]
    public void ApplyPositionToController_WithSamePosition_DoesNotUpdate()
    {
        _panelController.SetCurrentPosition(5);

        _panelController.ApplyPositionPublic(5);

        Assert.AreEqual(5, _panelController.GetCurrentPosition());
    }

    [Test]
    public void ApplyPositionToController_WithMinusOne_DoesNotUpdate()
    {
        _panelController.SetCurrentPosition(5);

        _panelController.ApplyPositionPublic(-1);

        Assert.AreEqual(5, _panelController.GetCurrentPosition());
    }

    [Test]
    public void ApplyPositionToController_LeavesEventStackBalanced()
    {
        _panelController.ApplyPositionPublic(10);

        Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
    }

    // ========== OnDeserialization() Tests ==========

    [Test]
    public void OnDeserialization_AppliesCurrentPosition()
    {
        _panelController.SetCurrentPosition(10);
        _panelController.ReferingInterlocks = new Interlocking[0];

        _panelController.OnDeserializationPublic();

        Assert.AreEqual(10, _panelController.LastAppliedPosition);
    }

    [Test]
    public void OnDeserialization_LeavesEventStackBalanced()
    {
        _panelController.ReferingInterlocks = new Interlocking[0];

        _panelController.OnDeserializationPublic();

        Assert.AreEqual(string.Empty, _eventStackHolder.GetCurrentStackString());
    }
}

/// <summary>
/// Concrete test implementation of AbstractPanelController for testing.
/// Provides access to protected members and test-specific functionality.
/// Uses Reflection to bypass VRC SDK requirements.
/// </summary>
public class TestPanelController : AbstractPanelController
{
    public int LastAppliedPosition { get; private set; }

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
        LastAppliedPosition = posIndex;
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(AbstractPanelController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractPanelController).Name + "." + fieldName + " が見つかりません。");
        }

        field.SetValue(this, value);
    }

    private T GetField<T>(string fieldName) where T : class
    {
        FieldInfo field = typeof(AbstractPanelController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractPanelController).Name + "." + fieldName + " が見つかりません。");
        }

        T value = field.GetValue(this) as T;
        if (value == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractPanelController).Name + "." + fieldName + " が null です。");
        }

        return value;
    }

    private object GetFieldValue(string fieldName)
    {
        FieldInfo field = typeof(AbstractPanelController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException(
                typeof(AbstractPanelController).Name + "." + fieldName + " が見つかりません。");
        }

        return field.GetValue(this);
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
