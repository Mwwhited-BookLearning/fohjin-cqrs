using Fohjin.DDD.Bootstrap;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Fohjin.DDD.Reporting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fohjin.DDD.Tests.TestUtilities;

namespace Fohjin.DDD.Tests.Reporting.Infrastructure;

[TestClass]
[TestCategory("unit")]
public class RepositoryTest
{
    public TestContext TestContext { get; set; } = null!;

    private SqlServerReportingRepository? _repository;

    [TestInitialize]
    public async Task SetUp()
    {
        var connectionString = TestSqlServer.ConnectionStringFor(TestContext.GetDatabaseNameForTest("Reporting"));

        await new ReportingDatabaseBootStrapper().ReCreateDatabaseSchema(connectionString);

        var dbContextOptions = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _repository = new SqlServerReportingRepository(new PooledDbContextFactory<ReportingDbContext>(dbContextOptions));
    }

    [TestMethod]
    public async Task Will_be_able_to_save_and_retrieve_a_client_dto()
    {
        var clientDto = new ClientReport(Guid.NewGuid(), "Mark Nijhof");
        await _repository!.SaveAsync(clientDto);
        var sut = await _repository.Query<ClientReport>().FirstOrDefaultAsync(x => x.Name == "Mark Nijhof");

        Assert.AreEqual(clientDto.Id, sut?.Id);
        Assert.AreEqual(clientDto.Name, sut?.Name);
    }

    [TestMethod]
    public async Task Will_be_able_to_save_and_retrieve_a_client_details_dto()
    {
        var clientDetailsDto = new ClientDetailsReport(Guid.NewGuid(), "Mark Nijhof", "Street", "123", "5006", "Bergen", "123456789");
        await _repository!.SaveAsync(clientDetailsDto);
        var sut = await _repository.Query<ClientDetailsReport>().FirstOrDefaultAsync(x => x.ClientName == "Mark Nijhof");

        Assert.AreEqual(clientDetailsDto.Id, sut?.Id);
        Assert.AreEqual(clientDetailsDto.ClientName, sut?.ClientName);
        Assert.AreEqual(clientDetailsDto.Street, sut?.Street);
        Assert.AreEqual(clientDetailsDto.StreetNumber, sut?.StreetNumber);
        Assert.AreEqual(clientDetailsDto.PostalCode, sut?.PostalCode);
        Assert.AreEqual(clientDetailsDto.City, sut?.City);
        Assert.AreEqual(clientDetailsDto.PhoneNumber, sut?.PhoneNumber);
    }

    [TestMethod]
    public async Task Will_be_able_to_save_and_retrieve_an_account_dto()
    {
        var clientDetailsId = Guid.NewGuid();
        await _repository!.SaveAsync(new ClientDetailsReport(clientDetailsId, "Mark Nijhof", "Street", "123", "5006", "Bergen", "123456789"));

        var accountDto = new AccountReport(Guid.NewGuid(), clientDetailsId, "Account Name", "1234567890");
        await _repository.SaveAsync(accountDto);
        var sut = await _repository.Query<AccountReport>().FirstOrDefaultAsync(x => x.AccountName == "Account Name");

        Assert.AreEqual(accountDto.Id, sut?.Id);
        Assert.AreEqual(accountDto.ClientDetailsReportId, sut?.ClientDetailsReportId);
        Assert.AreEqual(accountDto.AccountName, sut?.AccountName);
        Assert.AreEqual(accountDto.AccountNumber, sut?.AccountNumber);
    }

    [TestMethod]
    public async Task Will_be_able_to_save_and_retrieve_an_account_details_dto()
    {
        var accountDetailsDto = new AccountDetailsReport(Guid.NewGuid(), Guid.NewGuid(), "Account Name", 10.5M, "1234567890");
        await _repository!.SaveAsync(accountDetailsDto);
        var sut = await _repository.Query<AccountDetailsReport>().FirstOrDefaultAsync(x => x.AccountName == "Account Name");

        Assert.AreEqual(accountDetailsDto.Id, sut?.Id);
        Assert.AreEqual(accountDetailsDto.ClientReportId, sut?.ClientReportId);
        Assert.AreEqual(accountDetailsDto.AccountName, sut?.AccountName);
        Assert.AreEqual(accountDetailsDto.Balance, sut?.Balance);
        Assert.AreEqual(accountDetailsDto.AccountNumber, sut?.AccountNumber);
    }

    [TestMethod]
    public async Task Will_be_able_to_save_and_retrieve_a_ledger_dto()
    {
        var accountDetailsId = Guid.NewGuid();
        await _repository!.SaveAsync(new AccountDetailsReport(accountDetailsId, Guid.NewGuid(), "Account Name", 0M, "1234567890"));

        var ledgerDto = new LedgerReport(Guid.NewGuid(), accountDetailsId, "Action", 12.3M);
        await _repository.SaveAsync(ledgerDto);
        var sut = await _repository.Query<LedgerReport>().FirstOrDefaultAsync(x => x.Action == "Action" && x.Amount == 12.3M);

        Assert.AreEqual(ledgerDto.Id, sut?.Id);
        Assert.AreEqual(ledgerDto.AccountDetailsReportId, sut?.AccountDetailsReportId);
        Assert.AreEqual(ledgerDto.Amount, sut?.Amount);
        Assert.AreEqual(ledgerDto.Action, sut?.Action);
    }

    [TestMethod]
    public async Task When_calling_Query_it_will_return_a_list_with_dtos_matching_the_filter()
    {
        await _repository!.SaveAsync(new ClientReport(Guid.NewGuid(), "Mark Nijhof"));
        await _repository.SaveAsync(new ClientReport(Guid.NewGuid(), "Mark Nijhof"));
        var sut = await _repository.Query<ClientReport>().Where(x => x.Name == "Mark Nijhof").ToListAsync();

        Assert.AreEqual(2, sut?.Count());
    }

    [TestMethod]
    public async Task Will_be_able_to_update_an_already_saved_dto()
    {
        Guid guid = Guid.NewGuid();
        await _repository!.SaveAsync(new ClientReport(guid, "Mark Nijhof"));

        await _repository.UpdateAsync<ClientReport>(new { Name = "Mark Albert Nijhof" }, new { Id = guid });

        var sut = await _repository.GetByIdAsync<ClientReport>(guid);

        Assert.IsNotNull(sut);
        Assert.AreEqual("Mark Albert Nijhof", sut.Name);
    }

    [TestMethod]
    public async Task Will_be_able_to_delete_an_already_saved_dto()
    {
        Guid guid = Guid.NewGuid();
        await _repository!.SaveAsync(new ClientReport(guid, "Mark Nijhof"));

        await _repository.DeleteAsync<ClientReport>(new { Id = guid });

        var sut = await _repository.GetByIdAsync<ClientReport>(guid);

        Assert.IsNull(sut);
    }

    [TestMethod]
    public async Task GetByIdAsync_for_ClientDetailsReport_loads_real_navigations_in_insertion_order_split_by_status()
    {
        var clientDetailsId = Guid.NewGuid();
        await _repository!.SaveAsync(new ClientDetailsReport(clientDetailsId, "Mark Nijhof", "Street", "123", "5006", "Bergen", "123456789"));

        var openAccountId = Guid.NewGuid();
        var closedAccountId = Guid.NewGuid();
        await _repository.SaveAsync(new AccountReport(openAccountId, clientDetailsId, "Checking", "1111", "Open"));
        await _repository.SaveAsync(new AccountReport(closedAccountId, clientDetailsId, "Old Savings", "2222", "Closed"));

        var bankCardId1 = Guid.NewGuid();
        var bankCardId2 = Guid.NewGuid();
        await _repository.SaveAsync(new BankCardReport(bankCardId1, clientDetailsId, openAccountId, "Active"));
        await _repository.SaveAsync(new BankCardReport(bankCardId2, clientDetailsId, openAccountId, "Cancelled"));

        var sut = await _repository.GetByIdAsync<ClientDetailsReport>(clientDetailsId);

        Assert.IsNotNull(sut);
        Assert.AreEqual(1, sut.Accounts.Count());
        Assert.AreEqual(openAccountId, sut.Accounts.Single().Id);
        Assert.AreEqual(1, sut.ClosedAccounts.Count());
        Assert.AreEqual(closedAccountId, sut.ClosedAccounts.Single().Id);
        Assert.AreEqual(2, sut.BankCards.Count());
        Assert.AreEqual(bankCardId1, sut.BankCards.First().Id, "expected insertion order, not an arbitrary order");
        Assert.AreEqual(bankCardId2, sut.BankCards.Last().Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_for_AccountDetailsReport_loads_real_Ledgers_navigation_in_insertion_order()
    {
        var accountDetailsId = Guid.NewGuid();
        await _repository!.SaveAsync(new AccountDetailsReport(accountDetailsId, Guid.NewGuid(), "Checking", 100M, "1111"));

        var otherAccountDetailsId = Guid.NewGuid();
        await _repository.SaveAsync(new AccountDetailsReport(otherAccountDetailsId, Guid.NewGuid(), "Other", 0M, "2222"));

        await _repository.SaveAsync(new LedgerReport(Guid.NewGuid(), accountDetailsId, "Deposit", 50M));
        await _repository.SaveAsync(new LedgerReport(Guid.NewGuid(), accountDetailsId, "Withdrawal", 25M));
        await _repository.SaveAsync(new LedgerReport(Guid.NewGuid(), otherAccountDetailsId, "Deposit", 999M));

        var sut = await _repository.GetByIdAsync<AccountDetailsReport>(accountDetailsId);

        Assert.IsNotNull(sut);
        Assert.AreEqual(2, sut.Ledgers.Count);
        Assert.AreEqual("Deposit", sut.Ledgers.First().Action, "expected insertion order, not an arbitrary order");
        Assert.AreEqual("Withdrawal", sut.Ledgers.Last().Action);
    }
}
