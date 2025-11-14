using Domain.Enums;

namespace Application.DTOs
{
    public class CreateContactInfoDto
    {
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public ContactType Type { get; set; }
    }
}
