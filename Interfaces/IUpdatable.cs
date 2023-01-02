namespace nng_server.Interfaces;

public interface IUpdatable
{
    public TimeSpan WaitTime { get; }
    public bool UpdateAtStartup { get; }
}
