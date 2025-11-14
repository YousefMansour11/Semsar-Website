using System.Collections.Generic;

namespace Application.DTOs
{
    public class PropertyAdminDto : PropertyBaseDto
    {
        public string Code { get; set; } = null!;
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public ContactDto? Contact { get; set; }
        // Full SEO fields are inherited from base and can be used as nullable for admin
        // Slug metadata
        public bool SlugIsAuto { get; set; }
        public string? SlugLanguage { get; set; }
        public List<ImageInfoDto> AdminImages { get; set; } = new();
    }

    public class ImageInfoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = null!;
        public string? PublicId { get; set; }
        public string? PublicKey { get; set; }
    }

    public class ContactDto
    {
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public Domain.Enums.ContactType Type { get; set; }
    }
}
