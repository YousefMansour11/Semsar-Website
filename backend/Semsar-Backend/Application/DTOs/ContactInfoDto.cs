using Domain.Enums;

namespace Application.DTOs
{
    public class ContactInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public ContactType Type { get; set; }
    }
}