namespace Fohjin.DDD.Domain.Account;

public class Amount(decimal decimalAmount)
{
    private readonly decimal _decimalAmount = decimalAmount;

    public Amount Substract(Amount amount)
    {
        var newDecimalAmount = _decimalAmount - amount._decimalAmount;
        return new Amount(newDecimalAmount);
    }

    public Amount Add(Amount amount)
    {
        var newDecimalAmount = _decimalAmount + amount._decimalAmount;
        return new Amount(newDecimalAmount);
    }

    public bool IsNegative() => _decimalAmount < 0;
    public static implicit operator decimal(Amount amount) => amount._decimalAmount;
    public static implicit operator Amount(decimal decimalAmount) => new (decimalAmount);
}