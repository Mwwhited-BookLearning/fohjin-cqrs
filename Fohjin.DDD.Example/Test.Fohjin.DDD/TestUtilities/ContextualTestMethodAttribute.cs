using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;

namespace Test.Fohjin.DDD.TestUtilities;

public class ContextualTestMethodAttribute : TestMethodAttribute
{
    public const string CurrentTestMethod = nameof(CurrentTestMethod);
    public const string CurrentTestInstance = nameof(CurrentTestInstance);

    private readonly static AsyncLocal<ITestMethod?> _current = new();
    private readonly static AsyncLocal<object?> _instance = new();

    public static ITestMethod? Current => _current.Value;
    public static object? Instance
    {
        get => _instance.Value;
        set => _instance.Value = value;
    }

    public ContextualTestMethodAttribute(
        string? displayName = null,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = -1) : base(callerFilePath, callerLineNumber)
    {
        this.DisplayName = displayName;
    }

    public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
    {
        _current.Value = testMethod;
        var ret = await base.ExecuteAsync(testMethod);
        _current.Value = null;
        return ret;
    }
}