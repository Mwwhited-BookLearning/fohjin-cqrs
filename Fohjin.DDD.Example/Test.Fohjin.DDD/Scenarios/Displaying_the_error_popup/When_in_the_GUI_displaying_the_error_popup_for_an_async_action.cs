using System;
using System.Threading.Tasks;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Fohjin.DDD.Scenarios.Displaying_the_error_popup;

// Phase 7 (docs/11-migration-plan.md): CatchPossibleExceptionAsync is the async counterpart to
// CatchPossibleException, added because HTTP calls are real async I/O rather than the
// fire-and-forget in-process bus.Publish()/CommitAsync() pairs presenters used to wrap - see
// PopupPresenter.cs's own comment.
[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_displaying_the_error_popup_for_an_async_action : PresenterTestFixture<PopupPresenter>
{
    protected override void When()
    {
        Presenter.CatchPossibleExceptionAsync(() => throw new Exception("Message")).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void Then_the_name_of_the_exception_is_loaded_in_the_view()
    {
        On<IPopupView>().VerifyThat.ValueIsSetFor(x => x.Exception = "Exception");
    }

    [TestMethod]
    public void Then_the_message_of_the_exception_is_loaded_in_the_view()
    {
        On<IPopupView>().VerifyThat.ValueIsSetFor(x => x.Message = "Message");
    }

    [TestMethod]
    public void Then_display_is_called()
    {
        On<IPopupView>().VerifyThat.Method(x => x.ShowDialog()).WasCalled();
    }
}
