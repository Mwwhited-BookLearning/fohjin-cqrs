namespace Fohjin.DDD.EventHandlers
{
    public class UnsupportedTransferTypeException : Exception
    {
        public UnsupportedTransferTypeException(string transferType)
            : base(string.Format("Transfer type '{0}' is not implemented", transferType))
        {
        }
    }
}
