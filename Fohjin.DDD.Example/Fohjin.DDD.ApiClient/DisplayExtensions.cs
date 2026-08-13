namespace Fohjin.DDD.ApiClient;

// The generated DTOs (openapi.json -> obj/openapiClient.cs, NSwag's CodeGenerator="NSwagCSharp"
// target in Fohjin.DDD.ApiClient.csproj) are plain data classes with no ToString() override.
// Fohjin.DDD.BankApplication's WinForms views bind them straight to ListBox/ComboBox controls
// without setting DisplayMember (docs/09-client-uis.md), so what the user sees
// depends entirely on ToString() - these partial-class overrides preserve the exact display
// text the Fohjin.DDD.Reporting.Dtos types produced before WinForms was retargeted to call
// Fohjin.DDD.WebApi over HTTP (NSwag generates every DTO here
// as `public partial class`, specifically so hand-written members like these can be added
// without touching the generated file). This also fixes a pre-existing bug in the original
// LedgerReport.ToString() (Fohjin.DDD.Abstractions/Reporting/Dtos/LedgerReport.cs): a plain
// string literal, not interpolated, so every ledger row displayed the literal text
// "{Action} - {Amount:C}" instead of the real values.

public partial class ClientReport
{
    public override string? ToString() => Name;
}

public partial class AccountReport
{
    public override string ToString() => $"{AccountNumber} - ({AccountName})";
}

public partial class ClosedAccountReport
{
    public override string ToString() => $"{AccountNumber} - ({AccountName})";
}

public partial class LedgerReport
{
    public override string ToString() => $"{Action} - {Amount:C}";
}

public partial class BankCardReport
{
    // No card number/expiry/type in the read model to show (docs/02-bank-cards.md) - this is
    // everything that actually exists: which account it's linked to, and its status.
    public override string ToString() => $"{Status} - account {AccountId}";
}
