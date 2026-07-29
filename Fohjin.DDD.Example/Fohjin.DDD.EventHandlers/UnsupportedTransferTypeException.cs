namespace Fohjin.DDD.EventHandlers;

public class UnsupportedTransferTypeException(string transferType) : Exception(string.Format("Transfer type '{0}' is not implemented", transferType))
{
}
