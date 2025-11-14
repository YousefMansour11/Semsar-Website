namespace Application.Interfaces
{
    public interface IPublicIdService
    {
        string GeneratePropertyId();
        string GenerateProjectId();
        string GenerateUnitId();
        string GenerateContactId();
        string GenerateUserId();
        string GenerateLeadId();
        string GenerateBookingRequestId();
        string GenerateLandRequestId();

        string GenerateId(string prefix);
        Guid GenerateGuid();

        bool TryParseEntityType(string publicKey, out string prefix);
        string GetPrefix(string publicKey);
    }
}
