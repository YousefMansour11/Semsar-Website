using Application.Common;
using Application.Interfaces;

namespace Application.Services
{
    public class PublicIdService : IPublicIdService
    {
        private const char Separator = '_';

        public string GeneratePropertyId() => GenerateId(EntityType.Property);
        public string GenerateProjectId() => GenerateId(EntityType.Project);
        public string GenerateUnitId() => GenerateId(EntityType.Unit);
        public string GenerateContactId() => GenerateId(EntityType.Contact);
        public string GenerateUserId() => GenerateId(EntityType.User);
        public string GenerateLeadId() => GenerateId(EntityType.Lead);
        public string GenerateBookingRequestId() => GenerateId(EntityType.BookingRequest);
        public string GenerateLandRequestId() => GenerateId(EntityType.LandRequest);

        public string GenerateId(string prefix)
        {
            var guid = GenerateGuid();
            var base32 = EncodeToBase32(guid);
            return $"{prefix}{Separator}{base32}";
        }

        public Guid GenerateGuid()
        {
            return Guid.CreateVersion7();
        }

        public bool TryParseEntityType(string publicKey, out string prefix)
        {
            prefix = string.Empty;
            if (string.IsNullOrWhiteSpace(publicKey)) return false;

            var separatorIndex = publicKey.IndexOf(Separator);
            if (separatorIndex <= 0 || separatorIndex >= publicKey.Length - 1) return false;

            prefix = publicKey.Substring(0, separatorIndex);
            return true;
        }

        public string GetPrefix(string publicKey)
        {
            var separatorIndex = publicKey.IndexOf(Separator);
            if (separatorIndex <= 0) return string.Empty;
            return publicKey.Substring(0, separatorIndex);
        }

        private static string EncodeToBase32(Guid guid)
        {
            var bytes = guid.ToByteArray();
            const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
            var result = new char[26];
            var buffer = new byte[16];
            Buffer.BlockCopy(bytes, 0, buffer, 0, 16);

            for (var i = 25; i >= 0; i--)
            {
                var remainder = 0u;
                for (var j = 0; j < 16; j++)
                {
                    remainder = (remainder << 8) | buffer[j];
                    buffer[j] = (byte)(remainder >> 5);
                    remainder &= 0x1F;
                }
                result[i] = alphabet[(int)remainder];
            }

            return new string(result);
        }
    }
}
