using FluentValidation;
using Application.DTOs;
using Domain.Enums;

namespace Application.Validators
{
    public class CreatePropertyValidator : AbstractValidator<CreatePropertyDto>
    {
        public CreatePropertyValidator()
        {
            RuleFor(x => x.TitleEn).NotEmpty().WithMessage("Title is required").MaximumLength(200).WithMessage("Title must not exceed 200 characters");
            RuleFor(x => x.TitleAr).MaximumLength(200).WithMessage("Arabic title must not exceed 200 characters");
            RuleFor(x => x.DescriptionEn).MaximumLength(8000).WithMessage("Description must not exceed 8000 characters");
            RuleFor(x => x.DescriptionAr).MaximumLength(8000).WithMessage("Arabic description must not exceed 8000 characters");
            When(x => x.ListingType != PropertyListingType.Rental, () =>
            {
                RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero for Resale listings");
                RuleFor(x => x.RentPerMonth).Null().WithMessage("RentPerMonth should not be set for non-rental listings");
            });

            When(x => x.ListingType == PropertyListingType.Rental, () =>
            {
                RuleFor(x => x.RentPerMonth).GreaterThan(0).When(x => x.Price <= 0)
                    .WithMessage("RentPerMonth must be greater than zero for rental listings when Price is not set");
                RuleFor(x => x.Price).Null().When(x => x.RentPerMonth > 0)
                    .WithMessage("Price should not be set for rental listings; use RentPerMonth instead");
            });
            RuleFor(x => x.Location).NotEmpty().WithMessage("Location is required").MaximumLength(200).WithMessage("Location must not exceed 200 characters");
            RuleFor(x => x.Size).GreaterThan(0).WithMessage("Size must be greater than zero");
            RuleFor(x => x.Bedrooms).InclusiveBetween(0, 20).When(x => x.Bedrooms.HasValue).WithMessage("Bedrooms must be between 0 and 20");
            RuleFor(x => x.Bathrooms).InclusiveBetween(0, 20).When(x => x.Bathrooms.HasValue).WithMessage("Bathrooms must be between 0 and 20");
            RuleFor(x => x.Floor).GreaterThanOrEqualTo(0).When(x => x.Floor.HasValue).WithMessage("Floor must be 0 or greater");
            When(x => x.Floor.HasValue && x.TotalFloors.HasValue, () =>
            {
                RuleFor(x => x.TotalFloors).GreaterThanOrEqualTo(x => x.Floor!.Value)
                    .WithMessage("Total floors must be greater than or equal to floor number");
            });
            RuleFor(x => x.View).IsInEnum().When(x => x.View.HasValue).WithMessage("Invalid view type");
            RuleFor(x => x.PropertyType).IsInEnum().WithMessage("Invalid property type");
            RuleFor(x => x.ListingType).IsInEnum().WithMessage("Invalid listing type");
            RuleFor(x => x.Features).Must(f => f == null || f.Count <= 50).WithMessage("Features must not exceed 50 items");
            RuleFor(x => x.FeaturesAr).Must(f => f == null || f.Count <= 50).WithMessage("Arabic features must not exceed 50 items");

            When(x => x.Contact != null, () =>
            {
                RuleFor(x => x.Contact!.Name).NotEmpty().WithMessage("Contact name is required");
                RuleFor(x => x.Contact!.Phone).NotEmpty().WithMessage("Contact phone is required");
                RuleFor(x => x.Contact!.Type)
                    .Must(t => t == ContactType.Owner || t == ContactType.Broker)
                    .WithMessage("Contact type must be Owner or Broker");
            });

            When(x => x.Installments != null && x.Installments.Any(i => i.IsEnabled), () =>
            {
                RuleForEach(x => x.Installments!.Where(i => i.IsEnabled)).ChildRules(inst =>
                {
                    inst.RuleFor(i => i.DownPaymentPercent).InclusiveBetween(0, 100).WithMessage("Down payment must be between 0 and 100");
                    inst.RuleFor(i => i.Years).GreaterThan(0).WithMessage("Years must be greater than zero");
                });
            });
        }
    }
}
