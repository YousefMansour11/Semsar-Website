using FluentValidation;
using Application.DTOs;

namespace Application.Validators
{
    public class BookingRequestValidator : AbstractValidator<BookingSubmitDto>
    {
        public BookingRequestValidator()
        {
            RuleFor(x => x.PropertyId).NotEmpty().When(x => x.UnitId == null || x.UnitId == 0)
                .WithMessage("PropertyId or UnitId is required");
            RuleFor(x => x.UnitId).NotEmpty().When(x => x.PropertyId == null || x.PropertyId == 0)
                .WithMessage("PropertyId or UnitId is required");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters")
                .MustBeSafeText();
            RuleFor(x => x.Phone).NotEmpty().MaximumLength(20)
                .WithMessage("Phone must not exceed 20 characters")
                .MustNotContainHtml();
            RuleFor(x => x.Message).MaximumLength(500)
                .WithMessage("Message must not exceed 500 characters")
                .MustBeSafeText();
            RuleFor(x => x.PreferredDate)
                .Must(date => !date.HasValue || date.Value > DateTime.UtcNow.AddDays(-1))
                .WithMessage("Preferred date must be in the future");
            RuleFor(x => x.Honeypot).MustBeHoneypot();
            RuleFor(x => x.SubmittedAt)
                .Must(BeValidSubmissionTime)
                .WithMessage("Invalid submission. Please try again.");
        }

        private static bool BeValidSubmissionTime(DateTime? submittedAt)
        {
            if (submittedAt == null) return false;
            var elapsed = DateTime.UtcNow - submittedAt.Value.ToUniversalTime();
            return elapsed.TotalSeconds >= 3 && elapsed.TotalHours <= 1;
        }
    }
}
