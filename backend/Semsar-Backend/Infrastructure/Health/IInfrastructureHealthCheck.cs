namespace Infrastructure.Health
{
    public interface IInfrastructureHealthCheck
    {
        void ValidateScopedDependencies();
    }
}
