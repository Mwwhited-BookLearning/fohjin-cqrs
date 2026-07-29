using Fohjin.DDD.ApiClient;

namespace Fohjin.DDD.ApiClient.Tests;

// Phase 6 exit criteria: the Account Details edit/transaction commands (name, deposit,
// withdrawal, transfer, close) all reach the domain through the generated client and show up
// in AccountDetailsReport.
[TestClass]
[TestCategory("integration")]
public class AccountDetailsAndTransactionCommandsTest : WebApiIntegrationTestFixture
{
    private FohjinApiClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _client = new FohjinApiClient(HttpClient) { BaseUrl = HttpClient.BaseAddress!.ToString() };
    }

    [TestMethod]
    public async Task Generated_client_can_deposit_withdraw_rename_and_close_an_account()
    {
        var accountId = await CreateClientWithAccountAsync("Account Test Client", "Checking");

        var details = await _client.GetAccountDetailsByIdAsync(accountId);
        Assert.AreEqual("Checking", details.AccountName);
        Assert.AreEqual(0d, details.Balance);

        await _client.DepositCashAsync(accountId, new DepositCashRequest { Amount = 100d });
        details = await PollUntilAsync(() => _client.GetAccountDetailsByIdAsync(accountId), d => d.Balance == 100d);
        Assert.IsTrue(details.Ledgers.Any(l => l.Action == "Deposit" && l.Amount == 100d));

        await _client.WithdrawalCashAsync(accountId, new WithdrawalCashRequest { Amount = 40d });
        details = await PollUntilAsync(() => _client.GetAccountDetailsByIdAsync(accountId), d => d.Balance == 60d);
        Assert.IsTrue(details.Ledgers.Any(l => l.Action == "Withdrawal" && l.Amount == 40d));

        await _client.ChangeAccountNameAsync(accountId, new ChangeAccountNameRequest { AccountName = "Savings" });
        details = await PollUntilAsync(() => _client.GetAccountDetailsByIdAsync(accountId), d => d.AccountName == "Savings");

        await _client.CloseAccountAsync(accountId);
        // AccountClosedEventHandler deletes the live AccountDetailsReport row and
        // ClosedAccountCreatedEventHandler saves a ClosedAccountDetailsReport with the same id
        // in its place - GetAccountDetailsById falls back to it, so this still returns 200
        // (not 404) once the close has been processed, preserving the final balance.
        details = await PollUntilAsync(() => _client.GetAccountDetailsByIdAsync(accountId), d => d.Balance == 60d);
        Assert.AreEqual("Savings", details.AccountName);
    }

    [TestMethod]
    public async Task Sending_a_money_transfer_debits_the_source_account()
    {
        var accountId = await CreateClientWithAccountAsync("Transfer Source Client", "Source Account");
        var targetAccountId = await CreateClientWithAccountAsync("Transfer Target Client", "Target Account");
        var targetDetails = await _client.GetAccountDetailsByIdAsync(targetAccountId);

        await _client.DepositCashAsync(accountId, new DepositCashRequest { Amount = 100d });
        await PollUntilAsync(() => _client.GetAccountDetailsByIdAsync(accountId), d => d.Balance == 100d);

        await _client.SendMoneyTransferAsync(accountId, new SendMoneyTransferRequest { Amount = 30d, AccountNumber = targetDetails.AccountNumber });

        // MoneyTransferSendEventHandler debits the source account as soon as the command is
        // handled - crediting the target is a separate, intentionally non-deterministic path
        // (Fohjin.DDD.Services/MoneyTransferService.cs randomly picks internal/external/failed
        // routing with a 5s delay to simulate an external bank), so this test only asserts the
        // deterministic, immediate half of the flow.
        var details = await PollUntilAsync(() => _client.GetAccountDetailsByIdAsync(accountId), d => d.Balance == 70d);
        Assert.IsTrue(details.Ledgers.Any(l => l.Action != null && l.Action.StartsWith("Transfer to") && l.Amount == 30d));
    }

    [TestMethod]
    public async Task GetAccountDetailsById_returns_404_for_unknown_id()
    {
        var exception = await Assert.ThrowsExactlyAsync<ApiException>(() => _client.GetAccountDetailsByIdAsync(Guid.NewGuid()));
        Assert.AreEqual(404, exception.StatusCode);
    }

    private async Task<Guid> CreateClientWithAccountAsync(string clientName, string accountName)
    {
        await _client.CreateClientAsync(new CreateClientRequest
        {
            ClientName = clientName,
            Street = "Welhavens gate",
            StreetNumber = "49b",
            PostalCode = "5006",
            City = "Bergen",
            PhoneNumber = "95009937",
        });

        var client = await PollUntilFoundAsync(() => _client.GetClientsAsync(), c => c.Name == clientName);
        Assert.IsNotNull(client, $"the created client '{clientName}' did not appear in GetClientsAsync within the retry window");

        await _client.OpenNewAccountForClientAsync(client!.Id, new OpenNewAccountForClientRequest { AccountName = accountName });

        var account = await PollUntilFoundAsync(() => _client.GetAccountsAsync(), a => a.AccountName == accountName);
        Assert.IsNotNull(account, $"the opened account '{accountName}' did not appear in GetAccountsAsync within the retry window");

        return account!.Id;
    }

    // DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md), so reads made
    // right after a command/create returns need to poll briefly rather than assume the read
    // model has caught up already - same pattern as CreateAndReadClientThroughGeneratedApiClientTest.
    private static async Task<T?> PollUntilFoundAsync<T>(Func<Task<ICollection<T>>> fetch, Func<T, bool> predicate) where T : class
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var match = (await fetch()).FirstOrDefault(predicate);
            if (match is not null)
                return match;

            await Task.Delay(100);
        }

        return null;
    }

    private static async Task<T> PollUntilAsync<T>(Func<Task<T>> fetch, Func<T, bool> predicate)
    {
        T result = await fetch();
        for (var attempt = 0; attempt < 30 && !predicate(result); attempt++)
        {
            await Task.Delay(100);
            result = await fetch();
        }

        Assert.IsTrue(predicate(result), "condition was not met within the retry window");
        return result;
    }
}
