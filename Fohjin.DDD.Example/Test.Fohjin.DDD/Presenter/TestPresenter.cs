using Fohjin.DDD.BankApplication.Presenters;

namespace Test.Fohjin.DDD.Presenter;

public class TestPresenter(ITestView view) : Presenter<ITestView>(view)
{
    public bool TestValue { get; set; } = false;

    public void Test()
    {
        TestValue = true;
    }
}