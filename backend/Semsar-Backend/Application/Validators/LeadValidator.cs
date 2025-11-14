using FluentValidation;
using Application.DTOs;

namespace Application.Validators
{
    public class LeadValidator : AbstractValidator<LeadCreateDto>
    {
        public LeadValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters")
                .MustBeSafeText();
            RuleFor(x => x.Phone).NotEmpty().MaximumLength(20)
                .WithMessage("Phone must not exceed 20 characters")
                .MustNotContainHtml();
            RuleFor(x => x.Message).MaximumLength(1000)
                .WithMessage("Message must not exceed 1000 characters")
                .MustBeSafeText();
            RuleFor(x => x.PropertyId).GreaterThan(0).When(x => x.PropertyId.HasValue)
                .WithMessage("Valid property ID is required");
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
