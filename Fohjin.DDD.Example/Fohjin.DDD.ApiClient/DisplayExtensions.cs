namespace Fohjin.DDD.ApiClient;

// The generated DTOs (openapi.json -> obj/openapiClient.cs, NSwag's CodeGenerator="NSwagCSharp"
// target in Fohjin.DDD.ApiClient.csproj) are plain data classes with no ToString() override.
// Fohjin.DDD.BankApplication's WinForms views bind them straight to ListBox/ComboBox controls
// without setting DisplayMember (Phase 7, docs/11-migration-plan.md), so what the user sees
// depends entirely on ToString() - these partial-class overrides preserve the exact display
// text the pre-Phase-7 Fohjin.DDD.Reporting.Dtos types produced (NSwag generates every DTO here
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
