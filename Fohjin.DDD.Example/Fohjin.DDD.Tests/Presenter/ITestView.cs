using Fohjin.DDD.BankApplication.Views;

namespace Fohjin.DDD.Tests.Presenter;

public interface ITestView : IView
{
    event Action OnTest;
}