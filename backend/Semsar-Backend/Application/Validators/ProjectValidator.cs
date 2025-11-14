using FluentValidation;
using Application.DTOs;

namespace Application.Validators
{
    public class ProjectValidator : AbstractValidator<ProjectDto>
    {
        public ProjectValidator()
        {
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("Project Name (English) is required").MaximumLength(200).WithMessage("Project name must not exceed 200 characters");
            RuleFor(x => x.NameAr).MaximumLength(200).WithMessage("Arabic project name must not exceed 200 characters");
            RuleFor(x => x.DescriptionEn).NotEmpty().WithMessage("Description (English) is required").MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");
            RuleFor(x => x.DescriptionAr).MaximumLength(2000).WithMessage("Arabic description must not exceed 2000 characters");
            RuleFor(x => x.Location).NotEmpty().WithMessage("Location is required").MaximumLength(200).WithMessage("Location must not exceed 200 characters");
            RuleFor(x => x.Developer).NotEmpty().WithMessage("Developer name is required").MaximumLength(200).WithMessage("Developer name must not exceed 200 characters");
            RuleFor(x => x.Highlights).Must(h => h == null || h.Count <= 20).WithMessage("Highlights must not exceed 20 items");
            When(x => x.Highlights != null, () =>
            {
                RuleForEach(x => x.Highlights).Must(h => string.IsNullOrWhiteSpace(h) || h.Length <= 200).WithMessage("Each highlight must not exceed 200 characters");
            });
        }
    }
}
