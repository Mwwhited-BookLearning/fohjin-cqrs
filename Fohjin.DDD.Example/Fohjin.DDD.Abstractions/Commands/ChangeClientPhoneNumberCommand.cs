namespace Fohjin.DDD.Commands;

public record ChangeClientPhoneNumberCommand(Guid Id, string? PhoneNumber) : CommandBase(Id);
