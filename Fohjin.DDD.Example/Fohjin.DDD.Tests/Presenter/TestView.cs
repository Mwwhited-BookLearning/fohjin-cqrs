using Fohjin.DDD.BankApplication.Views;

namespace Fohjin.DDD.Tests.Presenter;

public class TestView : ITestView
{
    public event Action OnTest = null!;

    public void Test()
    {
        OnTest();
    }

    // IView Interface plumbing
    public void Dispose()
    {
    }

    public DialogResults ShowDialog()
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
    }
}