using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Test.Fohjin.DDD;

[TestClass]
[TestCategory("unit")]
public abstract class BaseTestFixture
{
    protected Exception CaughtException;
    protected virtual void Given() { }
    protected abstract void When();
    protected virtual void Finally() { }

    //[Given]
    public void Setup()
    {
        CaughtException = new ThereWasNoExceptionButOneWasExpectedException();
        Given();

        try
        {
            When();
        }
        catch (Exception exception)
        {
            CaughtException = exception;
        }
        finally
        {
            Finally();
        }
    }
}

[TestClass]
[TestCategory("unit")]
public abstract class BaseTestFixture<TSubjectUnderTest>
{
    public TestContext TestContext { get; set; } = null!;
    public IServiceCollection Services { get; } = new ServiceCollection()
        .AddLogging(opt => opt.AddConsole().SetMinimumLevel(LogLevel.Information));

    public IServiceProvider Provider => field ??= Services.BuildServiceProvider();

    public ILogger<T> Logger<T>() => Provider.GetRequiredService<ILogger<T>>();

    private Dictionary<Type, object> mocks;

    protected Dictionary<Type, object> DoNotMock;
    protected TSubjectUnderTest SubjectUnderTest;
    protected Exception CaughtException;
    protected virtual void SetupDependencies() { }
    protected virtual void Given() { }
    protected abstract Task WhenAsync();
    protected virtual void Finally() { }

    [TestInitialize]
    public async Task Setup()
    {
        mocks = new Dictionary<Type, object>();
        DoNotMock = new Dictionary<Type, object>();
        CaughtException = new ThereWasNoExceptionButOneWasExpectedException();

        BuildMocks();
        SetupDependencies();
        SubjectUnderTest = BuildSubjectUnderTest();

        Given();

        try
        {
            await WhenAsync();
        }
        catch (Exception exception)
        {
            CaughtException = exception;
        }
        finally
        {
            Finally();
        }
    }

    public Mock<TType> OnDependency<TType>() where TType : class
    {
        return (Mock<TType>)mocks?[typeof(TType)];
    }

    private TSubjectUnderTest BuildSubjectUnderTest()
    {
        var constructorInfo = typeof(TSubjectUnderTest).GetConstructors().First();

        List<object> parameters = [];
        foreach (var mock in mocks ?? Enumerable.Empty<KeyValuePair<Type, object>>())
        {
            if (DoNotMock == null )
            {
                continue;
            }

            if (!DoNotMock.TryGetValue(mock.Key, out var theObject))
            {
                theObject = ((Mock)mock.Value).Object;
            }
            parameters.Add(theObject);
        }

        return (TSubjectUnderTest)constructorInfo.Invoke([.. parameters]);
    }

    private void BuildMocks()
    {
        var constructorInfo = typeof(TSubjectUnderTest).GetConstructors().First();

        foreach (var parameter in constructorInfo.GetParameters())
        {
            mocks?.Add(parameter.ParameterType, CreateMock(parameter.ParameterType));
        }
    }

    private static object CreateMock(Type type)
    {
        var constructorInfo = typeof(Mock<>).MakeGenericType(type).GetConstructors().First();
        return constructorInfo.Invoke([]);
    }
}

//public class GivenAttribute : SetUpAttribute { }

//public class ThenAttribute : TestMethodAttribute { }