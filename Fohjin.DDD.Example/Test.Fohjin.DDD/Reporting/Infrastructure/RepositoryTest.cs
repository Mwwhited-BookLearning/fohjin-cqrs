using Fohjin.DDD.Bootstrap;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Fohjin.DDD.Reporting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Fohjin.DDD.TestUtilities;

namespace Test.Fohjin.DDD.Reporting.Infrastructure;

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
        var sut = (await _repository.GetByExampleAsync<ClientReport>(new { Name = "Mark Nijhof" })).FirstOrDefault();

        Assert.AreEqual(clientDto.Id, sut?.Id);
        Assert.AreEqual(clientDto.Name, sut?.Name);
    }

    [TestMethod]
    public async Task Will_be_able_to_save_and_retrieve_a_client_details_dto()
    {
        var clientDetailsDto = new ClientDetailsReport(Guid.NewGuid(), "Mark Nijhof", "Street", "123", "5006", "Bergen", "123456789");
        await _repository!.SaveAsync(clientDetailsDto);
        var sut = (await _repository.GetByExampleAsync<ClientDetailsReport>(new { ClientName = "Mark Nijhof" })).FirstOrDefault();

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
        var accountDto = new AccountReport(Guid.NewGuid(), Guid.NewGuid(), "Account Name", "1234567890");
        await _repository!.SaveAsync(accountDto);
        var sut = (await _repository.GetByExampleAsync<AccountReport>(new { AccountName = "Account Name" })).FirstOrDefault();

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
        var sut = (await _repository.GetByExampleAsync<AccountDetailsReport>(new { AccountName = "Account Name" })).FirstOrDefault();

        Assert.AreEqual(accountDetailsDto.Id, sut?.Id);
        Assert.AreEqual(accountDetailsDto.ClientReportId, sut?.ClientReportId);
        Assert.AreEqual(accountDetailsDto.AccountName, sut?.AccountName);
        Assert.AreEqual(accountDetailsDto.Balance, sut?.Balance);
        Assert.AreEqual(accountDetailsDto.AccountNumber, sut?.AccountNumber);
    }

    [TestMethod]
    public async Task Will_be_able_to_save_and_retrieve_a_ledger_dto()
    {
        var ledgerDto = new LedgerReport(Guid.NewGuid(), Guid.NewGuid(), "Action", 12.3M);
        await _repository!.SaveAsync(ledgerDto);
        var sut = (await _repository.GetByExampleAsync<LedgerReport>(new { Action = "Action", Amount = 12.3M })).FirstOrDefault();

        Assert.AreEqual(ledgerDto.Id, sut?.Id);
        Assert.AreEqual(ledgerDto.AccountDetailsReportId, sut?.AccountDetailsReportId);
        Assert.AreEqual(ledgerDto.Amount, sut?.Amount);
        Assert.AreEqual(ledgerDto.Action, sut?.Action);
    }

    [TestMethod]
    public async Task When_calling_GetByExample_it_will_return_a_list_with_dtos_matching_the_example()
    {
        await _repository!.SaveAsync(new ClientReport(Guid.NewGuid(), "Mark Nijhof"));
        await _repository.SaveAsync(new ClientReport(Guid.NewGuid(), "Mark Nijhof"));
        var sut = await _repository.GetByExampleAsync<ClientReport>(new { Name = "Mark Nijhof" });

        Assert.AreEqual(2, sut?.Count());
    }

    [TestMethod]
    public async Task When_calling_GetByExample_it_will_return_a_list_with_dtos_matching_the_example_inclusing_child_objects()
    {
        var AccountId = Guid.NewGuid();
        await _repository!.SaveAsync(new AccountDetailsReport(AccountId, Guid.NewGuid(), "Account Name", 10.5M, "1234567890"));

        await _repository.SaveAsync(new LedgerReport(Guid.NewGuid(), AccountId, "Action 1", 12.3M));
        await _repository.SaveAsync(new LedgerReport(Guid.NewGuid(), AccountId, "Action 2", 24.6M));
        await _repository.SaveAsync(new LedgerReport(Guid.NewGuid(), Guid.NewGuid(), "Action 3", 96.3M));

        var sut = (await _repository.GetByExampleAsync<AccountDetailsReport>(new { AccountName = "Account Name" })).FirstOrDefault();

        Assert.AreEqual(2, sut?.Ledgers.Count());
        Assert.AreEqual("Action 1", sut?.Ledgers.First().Action);
        Assert.AreEqual(12.3M, sut?.Ledgers.First().Amount);
        Assert.AreEqual("Action 2", sut?.Ledgers.Last().Action);
        Assert.AreEqual(24.6M, sut?.Ledgers.Last().Amount);
    }

    [TestMethod]
    public async Task Will_be_able_to_update_an_already_saved_dto()
    {
        Guid guid = Guid.NewGuid();
        await _repository!.SaveAsync(new ClientReport(guid, "Mark Nijhof"));

        await _repository.UpdateAsync<ClientReport>(new { Name = "Mark Albert Nijhof" }, new { Id = guid });

        var sut = await _repository.GetByExampleAsync<ClientReport>(new { Id = guid });

        Assert.AreEqual(1, sut?.Count());
        Assert.AreEqual("Mark Albert Nijhof", sut?.First().Name);
    }

    [TestMethod]
    public async Task Will_be_able_to_delete_an_already_saved_dto()
    {
        Guid guid = Guid.NewGuid();
        await _repository!.SaveAsync(new ClientReport(guid, "Mark Nijhof"));

        await _repository.DeleteAsync<ClientReport>(new { Id = guid });

        var sut = await _repository.GetByExampleAsync<ClientReport>(new { Id = guid });

        Assert.AreEqual(0, sut?.Count());
    }
}
