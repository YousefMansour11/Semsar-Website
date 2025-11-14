using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class ContactUpdateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [Phone]
        public string Phone { get; set; } = null!;

        [Required]
        public ContactType Type { get; set; }
    }
}