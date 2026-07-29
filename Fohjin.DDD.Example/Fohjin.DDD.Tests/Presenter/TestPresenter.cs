using Fohjin.DDD.BankApplication.Presenters;

namespace Fohjin.DDD.Tests.Presenter;

public class TestPresenter(ITestView view) : Presenter<ITestView>(view)
{
    public bool TestValue { get; set; } = false;

    public void Test()
    {
        TestValue = true;
    }
}