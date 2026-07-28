namespace Fohjin.DDD.Common;

public interface ISystemTimer
{
    void Trigger(Func<Task> value, int @in);
}
