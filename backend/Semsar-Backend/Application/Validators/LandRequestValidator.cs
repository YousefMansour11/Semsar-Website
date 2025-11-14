using FluentValidation;
using Application.DTOs;

namespace Application.Validators
{
    public class LandRequestValidator : AbstractValidator<CreateLandRequestDto>
    {
        public LandRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters")
                .MustBeSafeText();
            RuleFor(x => x.Phone).NotEmpty().MaximumLength(20)
                .WithMessage("Phone must not exceed 20 characters")
                .MustNotContainHtml();
            RuleFor(x => x.Location).NotEmpty().MaximumLength(200)
                .WithMessage("Location must not exceed 200 characters")
                .MustBeSafeText();
            RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MinArea).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxArea).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Notes).MaximumLength(1000)
                .WithMessage("Notes must not exceed 1000 characters")
                .MustBeSafeText();
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
