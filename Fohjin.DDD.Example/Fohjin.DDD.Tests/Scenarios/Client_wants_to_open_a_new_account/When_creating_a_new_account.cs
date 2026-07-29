using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.Events.Account;
using Fohjin.DDD.EventStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.Tests.Scenarios.Client_wants_to_open_a_new_account;

[TestClass]
[TestCategory("unit")]
public class When_creating_a_new_account : AggregateRootTestFixture<ActiveAccount>
{
    protected override IEnumerable<IDomainEvent> Given()
    {
        return new List<IDomainEvent>();
    }

    protected override void When()
    {
        AggregateRoot = ActiveAccount.CreateNew(Guid.NewGuid(), "New Account", "Account Number");
    }

    [TestMethod]
    public void Then_an_account_created_event_will_be_published()
    {
        PublishedEvents?.Last().WillBeOfType<AccountOpenedEvent>();
    }

    [TestMethod]
    public void Then_the_published_event_will_contain_the_new_name_and_number_of_the_account()
    {
        PublishedEvents?.Last<AccountOpenedEvent>()!.AccountName.WillBe("New Account");
        PublishedEvents?.Last<AccountOpenedEvent>()!.AccountNumber.WillBe("Account Number");
    }

    [TestMethod]
    public void Then_the_published_event_will_have_the_same_aggregate_id()
    {
        PublishedEvents?.Last<AccountOpenedEvent>()!.AccountId.WillBe(AggregateRoot?.Id);
    }

    protected override void Finally()
    {
    }
}