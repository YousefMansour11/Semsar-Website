using FluentValidation;
using Application.DTOs;
using Domain.Enums;

namespace Application.Validators
{
    public class UpdatePropertyValidator : AbstractValidator<UpdatePropertyDto>
    {
        public UpdatePropertyValidator()
        {
            RuleFor(x => x.TitleEn).MaximumLength(200).WithMessage("Title must not exceed 200 characters");
            RuleFor(x => x.TitleAr).MaximumLength(200).WithMessage("Arabic title must not exceed 200 characters");
            RuleFor(x => x.DescriptionEn).MaximumLength(8000).WithMessage("Description must not exceed 8000 characters");
            RuleFor(x => x.DescriptionAr).MaximumLength(8000).WithMessage("Arabic description must not exceed 8000 characters");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero");
            RuleFor(x => x.Location).MaximumLength(200).WithMessage("Location must not exceed 200 characters");
            RuleFor(x => x.Size).GreaterThan(0).WithMessage("Size must be greater than zero");
            RuleFor(x => x.PropertyType).IsInEnum().WithMessage("Invalid property type");
            RuleFor(x => x.ListingType).IsInEnum().WithMessage("Invalid listing type");
            RuleFor(x => x.Size).GreaterThan(0).WithMessage("Size must be greater than zero");
            RuleFor(x => x.Bedrooms).InclusiveBetween(0, 20).When(x => x.Bedrooms.HasValue).WithMessage("Bedrooms must be between 0 and 20");
            RuleFor(x => x.Bathrooms).InclusiveBetween(0, 20).When(x => x.Bathrooms.HasValue).WithMessage("Bathrooms must be between 0 and 20");
            RuleFor(x => x.Floor).GreaterThanOrEqualTo(0).When(x => x.Floor.HasValue).WithMessage("Floor must be 0 or greater");
            When(x => x.Floor.HasValue && x.TotalFloors.HasValue, () =>
            {
                RuleFor(x => x.TotalFloors).GreaterThanOrEqualTo(x => x.Floor!.Value)
                    .WithMessage("Total floors must be greater than or equal to floor number");
            });
            RuleFor(x => x.SeoTitle).MaximumLength(200).WithMessage("SEO title must not exceed 200 characters");
            RuleFor(x => x.SeoDescription).MaximumLength(500).WithMessage("SEO description must not exceed 500 characters");
            RuleFor(x => x.SeoKeywords).MaximumLength(500).WithMessage("SEO keywords must not exceed 500 characters");
        }
    }
}
