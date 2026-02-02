using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Property> Properties => Set<Property>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Feature> Features => Set<Feature>();
        public DbSet<PropertyFeature> PropertyFeatures => Set<PropertyFeature>();
        public DbSet<UnitFeature> UnitFeatures => Set<UnitFeature>();
        public DbSet<Domain.Entities.User> Users => Set<Domain.Entities.User>();
        public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
        public DbSet<PropertyVideo> PropertyVideos => Set<PropertyVideo>();
        public DbSet<ProjectVideo> ProjectVideos => Set<ProjectVideo>();
        public DbSet<UnitVideo> UnitVideos => Set<UnitVideo>();
        public DbSet<Domain.Entities.OrphanedUpload> OrphanedUploads => Set<Domain.Entities.OrphanedUpload>();
        public DbSet<LandRequest> LandRequests => Set<LandRequest>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectDetails> ProjectDetails => Set<ProjectDetails>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<RentalDetails> RentalDetails => Set<RentalDetails>();
        public DbSet<ContactInfo> Contacts => Set<ContactInfo>();
        public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
        public DbSet<PropertyInstallmentPlan> PropertyInstallmentPlans => Set<PropertyInstallmentPlan>();
        public DbSet<UnitInstallmentPlan> UnitInstallmentPlans => Set<UnitInstallmentPlan>();
        public DbSet<UnitVariant> UnitVariants => Set<UnitVariant>();
        public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<Domain.Entities.SlugReservation> SlugReservations => Set<Domain.Entities.SlugReservation>();
        public DbSet<Domain.Entities.CodeReservation> CodeReservations => Set<Domain.Entities.CodeReservation>();
        public DbSet<Domain.Entities.RefreshToken> RefreshTokens => Set<Domain.Entities.RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft-delete global query filters for ISoftDelete entities
            var softDeleteTypes = new[]
            {
                typeof(Property), typeof(Unit), typeof(Project),
                typeof(Lead), typeof(BookingRequest), typeof(Setting),
                typeof(ContactInfo), typeof(PropertyImage), typeof(UnitImage),
                typeof(PropertyVideo), typeof(ProjectVideo), typeof(UnitVideo),
                typeof(UnitInstallmentPlan), typeof(PropertyInstallmentPlan),
                typeof(LandRequest), typeof(ProjectImage),
                typeof(UnitVariant)
            };

            foreach (var type in softDeleteTypes)
            {
                var parameter = Expression.Parameter(type, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
                modelBuilder.Entity(type).HasQueryFilter(filter);
            }

            // Property
            modelBuilder.Entity<Property>(b =>
            {
                b.Property(x => x.Price).HasPrecision(18, 2);
                b.Property(x => x.RentPerMonth).HasPrecision(18, 2);
                b.Property(x => x.DescriptionEn).HasColumnType("nvarchar(max)").IsRequired(false);
                b.Property(x => x.DescriptionAr).HasColumnType("nvarchar(max)").IsRequired(false);
                b.Property(x => x.Slug).HasMaxLength(450).IsRequired();
                b.HasIndex(x => x.Slug).IsUnique();
                b.HasIndex(x => x.Location);
                b.HasIndex(x => x.LocationId);
                b.Property(x => x.CanonicalUrl).HasMaxLength(1000).IsRequired();
                b.HasIndex(x => x.PropertyType);
                b.Property(x => x.View).HasConversion<string>().HasMaxLength(50);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.Property(x => x.Code).HasMaxLength(50).IsRequired();
                b.HasIndex(x => x.Code).IsUnique();
            });

            // Location (hierarchical)
            modelBuilder.Entity<Location>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Slug).HasMaxLength(300).IsRequired();
                b.HasIndex(x => x.Slug).IsUnique();
                b.Property(x => x.Path).HasMaxLength(500).IsRequired();
                b.HasIndex(x => x.Path).HasDatabaseName("IX_Location_Path");
                b.Property(x => x.Level).HasConversion<byte>().IsRequired();
                b.HasIndex(x => x.Level).HasDatabaseName("IX_Location_Level");
                b.HasIndex(x => x.ParentId).HasDatabaseName("IX_Location_ParentId");
                b.HasIndex(x => new { x.NameEn, x.ParentId }).IsUnique().HasDatabaseName("IX_Location_NameEn_ParentId");
                b.HasIndex(x => new { x.NameAr, x.ParentId }).IsUnique().HasDatabaseName("IX_Location_NameAr_ParentId");
                b.Property(x => x.Depth).HasDefaultValue(0);
                b.Property(x => x.IsActive).HasDefaultValue(true);
                b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                b.HasOne(x => x.Parent)
                    .WithMany(x => x.Children)
                    .HasForeignKey(x => x.ParentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            // Feature (catalog)
            modelBuilder.Entity<Feature>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Key).HasMaxLength(100).IsRequired();
                b.HasIndex(x => x.Key).IsUnique();
            });

            // PropertyFeature (join)
            modelBuilder.Entity<PropertyFeature>(b =>
            {
                b.HasKey(x => new { x.PropertyId, x.FeatureId });
                b.HasOne(x => x.Property)
                    .WithMany(p => p.PropertyFeatures)
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Feature)
                    .WithMany(f => f.PropertyFeatures)
                    .HasForeignKey(x => x.FeatureId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => x.FeatureId);
            });

            // UnitFeature (join)
            modelBuilder.Entity<UnitFeature>(b =>
            {
                b.HasKey(x => new { x.UnitId, x.FeatureId });
                b.HasOne(x => x.Unit)
                    .WithMany(u => u.UnitFeatures)
                    .HasForeignKey(x => x.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Feature)
                    .WithMany(f => f.UnitFeatures)
                    .HasForeignKey(x => x.FeatureId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => x.FeatureId);
            });

            // Unit
            modelBuilder.Entity<Unit>(b =>
            {
                b.Property(x => x.MinPrice).HasPrecision(18, 2);
                b.Property(x => x.MaxPrice).HasPrecision(18, 2);
                b.Property(x => x.View).HasConversion<string>().HasMaxLength(50);
                b.Property(x => x.FinishingType).HasConversion<string>().HasMaxLength(50);
                b.Property(x => x.DescriptionEn).HasColumnType("nvarchar(max)").IsRequired(false);
                b.Property(x => x.DescriptionAr).HasColumnType("nvarchar(max)").IsRequired(false);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasIndex(x => x.Code).IsUnique();
            });

            // PropertyInstallmentPlan (one-to-many with Property)
            modelBuilder.Entity<PropertyInstallmentPlan>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.PropertyId);
                b.HasOne(x => x.Property)
                    .WithMany(p => p.Installments)
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
                b.Property(x => x.PaymentType).HasConversion<string>().HasMaxLength(20).IsRequired();
                b.Property(x => x.DiscountPercent).HasDefaultValue(null);
                b.Property(x => x.MonthlyAmount).HasPrecision(18, 2);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // UnitInstallmentPlan (one-to-many with Unit)
            modelBuilder.Entity<UnitInstallmentPlan>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.UnitId);
                b.HasOne(x => x.Unit)
                    .WithMany(u => u.Installments)
                    .HasForeignKey(x => x.UnitId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
                b.Property(x => x.PaymentType).HasConversion<string>().HasMaxLength(20).IsRequired();
                b.Property(x => x.DiscountPercent).HasDefaultValue(null);
                b.Property(x => x.MonthlyAmount).HasPrecision(18, 2);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // UnitVariant (one-to-many with Unit)
            modelBuilder.Entity<UnitVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.UnitId);
                b.HasIndex(x => x.PublicId).IsUnique();
                b.HasOne(x => x.Unit)
                    .WithMany(u => u.Variants)
                    .HasForeignKey(x => x.UnitId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
                b.Property(x => x.Price).HasPrecision(18, 2);
                b.Property(x => x.RentPerMonth).HasPrecision(18, 2);
                b.Property(x => x.View).HasConversion<string>().HasMaxLength(50);
                b.Property(x => x.FinishingType).HasConversion<string>().HasMaxLength(50);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Property images
            modelBuilder.Entity<PropertyImage>()
                .HasOne(pi => pi.Property)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.PropertyId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            modelBuilder.Entity<PropertyImage>().Property(pi => pi.PublicId).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<PropertyImage>(b =>
            {
                b.HasIndex(pi => pi.PropertyId);
                b.HasIndex(pi => pi.SortOrder);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Project images
            modelBuilder.Entity<ProjectImage>()
                .HasOne(pi => pi.Project)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            modelBuilder.Entity<ProjectImage>().Property(pi => pi.PublicId).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<ProjectImage>(b =>
            {
                b.HasIndex(pi => pi.ProjectId);
                b.HasIndex(pi => pi.SortOrder);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Unit images
            modelBuilder.Entity<UnitImage>()
                .HasOne(ui => ui.Unit)
                .WithMany(u => u.Images)
                .HasForeignKey(ui => ui.UnitId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            modelBuilder.Entity<UnitImage>().Property(ui => ui.PublicId).HasMaxLength(500).IsRequired(false);

            // Property videos
            modelBuilder.Entity<PropertyVideo>()
                .HasOne(pv => pv.Property)
                .WithMany(p => p.Videos)
                .HasForeignKey(pv => pv.PropertyId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            modelBuilder.Entity<PropertyVideo>(b =>
            {
                b.HasIndex(pv => pv.PropertyId);
                b.HasIndex(pv => pv.SortOrder);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Project videos
            modelBuilder.Entity<ProjectVideo>()
                .HasOne(pv => pv.Project)
                .WithMany(p => p.Videos)
                .HasForeignKey(pv => pv.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            modelBuilder.Entity<ProjectVideo>(b =>
            {
                b.HasIndex(pv => pv.ProjectId);
                b.HasIndex(pv => pv.SortOrder);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Unit videos
            modelBuilder.Entity<UnitVideo>()
                .HasOne(uv => uv.Unit)
                .WithMany(u => u.Videos)
                .HasForeignKey(uv => uv.UnitId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            modelBuilder.Entity<UnitVideo>(b =>
            {
                b.HasIndex(uv => uv.UnitId);
                b.HasIndex(uv => uv.SortOrder);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Rental details
            modelBuilder.Entity<RentalDetails>()
                .HasOne(r => r.Property)
                .WithOne(p => p.RentalDetails)
                .HasForeignKey<RentalDetails>(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<RentalDetails>(b =>
            {
                b.Property(x => x.MaintenanceFee).HasPrecision(18, 2);
                b.Property(x => x.SecurityDeposit).HasPrecision(18, 2);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Unique indexes on PublicId for distributed identification
            modelBuilder.Entity<Property>().HasIndex(p => p.PublicId).IsUnique();
            modelBuilder.Entity<ContactInfo>().HasIndex(c => c.PublicId).IsUnique();
            modelBuilder.Entity<Unit>().HasIndex(u => u.PublicId).IsUnique();
            modelBuilder.Entity<Project>().HasIndex(p => p.PublicId).IsUnique();
            modelBuilder.Entity<Domain.Entities.User>().HasIndex(u => u.PublicId).IsUnique();
            modelBuilder.Entity<Lead>().HasIndex(l => l.PublicId).IsUnique();
            modelBuilder.Entity<BookingRequest>().HasIndex(b => b.PublicId).IsUnique();
            modelBuilder.Entity<LandRequest>().HasIndex(l => l.PublicId).IsUnique();
            modelBuilder.Entity<ProjectImage>().HasIndex(i => i.PublicId).IsUnique();

            // Unique indexes on PublicKey for public-key-based lookups
            modelBuilder.Entity<Property>().HasIndex(p => p.PublicKey).IsUnique();
            modelBuilder.Entity<Unit>().HasIndex(u => u.PublicKey).IsUnique();
            modelBuilder.Entity<Project>().HasIndex(p => p.PublicKey).IsUnique();
            modelBuilder.Entity<ContactInfo>().HasIndex(c => c.PublicKey).IsUnique();
            modelBuilder.Entity<Domain.Entities.User>().HasIndex(u => u.PublicKey).IsUnique();
            modelBuilder.Entity<Lead>().HasIndex(l => l.PublicKey).IsUnique();
            modelBuilder.Entity<BookingRequest>().HasIndex(b => b.PublicKey).IsUnique();
            modelBuilder.Entity<LandRequest>().HasIndex(l => l.PublicKey).IsUnique();

            // ContactInfo
            modelBuilder.Entity<ContactInfo>(b =>
            {
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Property-Contact relationship
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Contact)
                .WithMany(c => c.Properties)
                .HasForeignKey(p => p.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            // Project
            modelBuilder.Entity<ProjectDetails>()
                .HasOne(pd => pd.Project)
                .WithOne(p => p.Details)
                .HasForeignKey<ProjectDetails>(pd => pd.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<Project>(b =>
            {
                b.Property(x => x.Slug).HasMaxLength(450).IsRequired();
                b.HasIndex(x => x.Slug).IsUnique();
                b.Property(x => x.CanonicalUrl).HasMaxLength(1000).IsRequired();
                b.Property(x => x.DescriptionEn).HasColumnType("nvarchar(max)").IsRequired(false);
                b.Property(x => x.DescriptionAr).HasColumnType("nvarchar(max)").IsRequired(false);
                b.Property(x => x.StartingPrice).HasPrecision(18, 2);
                b.Property(x => x.TotalArea).HasPrecision(18, 2);
                b.Property(x => x.RowVersion).IsRowVersion();
                // JSON arrays for bilingual highlights (EF Core 8+ auto-maps List<string> to JSON)
                // Filtered index for soft-delete queries
                b.HasIndex(x => x.IsDeleted).HasFilter("[IsDeleted] = 0");
            });

            modelBuilder.Entity<ProjectDetails>(b =>
            {
                b.Property(x => x.DownPaymentPercentage).HasPrecision(18, 2);
                b.Property(x => x.CashDiscountPercentage).HasPrecision(18, 2);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // LandRequest
            modelBuilder.Entity<LandRequest>(b =>
            {
                b.HasIndex(l => l.IsDeleted).HasFilter("[IsDeleted] = 0");
                b.HasIndex(l => l.Phone);
                b.Property(x => x.MinArea).HasPrecision(18, 2);
                b.Property(x => x.MaxArea).HasPrecision(18, 2);
                b.Property(x => x.MinPrice).HasPrecision(18, 2);
                b.Property(x => x.MaxPrice).HasPrecision(18, 2);
                b.Property(lr => lr.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                b.Property(lr => lr.Source).HasMaxLength(50).HasDefaultValue("direct");
                b.Property(lr => lr.Medium).HasMaxLength(50).IsRequired(false);
                b.Property(lr => lr.Campaign).HasMaxLength(100).IsRequired(false);
                b.Property(lr => lr.Term).HasMaxLength(100).IsRequired(false);
                b.Property(lr => lr.Content).HasMaxLength(100).IsRequired(false);
                b.Property(lr => lr.LandingPage).HasMaxLength(500).IsRequired(false);
                b.Property(lr => lr.FirstVisitAt).IsRequired(false);
                b.Property(lr => lr.CurrentPage).HasMaxLength(500).IsRequired(false);
                b.Property(lr => lr.Referrer).HasMaxLength(500).IsRequired(false);
                b.Property(lr => lr.UserAgent).HasMaxLength(500).IsRequired(false);
                b.Property(lr => lr.PageViews).HasDefaultValue(0);
                b.Property(lr => lr.SessionDuration).IsRequired(false);
                b.Property(lr => lr.LastReferrer).HasMaxLength(500).IsRequired(false);
                b.Property(lr => lr.VisitHistory).HasMaxLength(8000).IsRequired(false);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<Lead>(b =>
            {
                b.HasIndex(l => l.IsDeleted).HasFilter("[IsDeleted] = 0");
                b.HasIndex(l => l.Phone);
                b.HasIndex(l => l.PropertyId);
                b.Property(l => l.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                b.Property(l => l.Source).HasMaxLength(50).HasDefaultValue("direct");
                b.Property(l => l.Medium).HasMaxLength(50).IsRequired(false);
                b.Property(l => l.Campaign).HasMaxLength(100).IsRequired(false);
                b.Property(l => l.Term).HasMaxLength(100).IsRequired(false);
                b.Property(l => l.Content).HasMaxLength(100).IsRequired(false);
                b.Property(l => l.LandingPage).HasMaxLength(500).IsRequired(false);
                b.Property(l => l.FirstVisitAt).IsRequired(false);
                b.Property(l => l.CurrentPage).HasMaxLength(500).IsRequired(false);
                b.Property(l => l.IsPaid).HasDefaultValue(false);
                b.Property(l => l.Referrer).HasMaxLength(500).IsRequired(false);
                b.Property(l => l.UserAgent).HasMaxLength(500).IsRequired(false);
                b.Property(l => l.PageViews).HasDefaultValue(0);
                b.Property(l => l.SessionDuration).IsRequired(false);
                b.Property(l => l.LastReferrer).HasMaxLength(500).IsRequired(false);
                b.Property(l => l.VisitHistory).HasMaxLength(8000).IsRequired(false);
                b.HasOne(l => l.Property)
                    .WithMany()
                    .HasForeignKey(l => l.PropertyId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                b.HasOne(l => l.BookingRequest)
                    .WithMany()
                    .HasForeignKey(l => l.BookingRequestId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                b.HasOne(l => l.LandRequest)
                    .WithMany()
                    .HasForeignKey(l => l.LandRequestId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                b.HasIndex(l => l.CreatedAt);
                b.HasIndex(l => l.Source);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Slug reservation: use nullable FK columns for polymorphic relationship
            modelBuilder.Entity<Domain.Entities.SlugReservation>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => new { x.EntityType, x.Slug }).IsUnique();
                b.Property(x => x.Slug).HasMaxLength(450).IsRequired();
                b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
                // Nullable FK columns - only one will be populated based on EntityType
                b.HasOne(x => x.Property)
                    .WithMany()
                    .HasForeignKey("PropertyId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                b.HasOne(x => x.Project)
                    .WithMany()
                    .HasForeignKey("ProjectId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                b.HasOne(x => x.Unit)
                    .WithMany()
                    .HasForeignKey("UnitId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Code reservation: same pattern as slug reservation
            modelBuilder.Entity<Domain.Entities.CodeReservation>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => new { x.EntityType, x.Code }).IsUnique();
                b.Property(x => x.Code).HasMaxLength(200).IsRequired();
                b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
                b.Property(x => x.Prefix).HasMaxLength(50);
                b.HasOne(x => x.Property)
                    .WithMany()
                    .HasForeignKey("PropertyId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                b.HasOne(x => x.Project)
                    .WithMany()
                    .HasForeignKey("ProjectId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                b.HasOne(x => x.Unit)
                    .WithMany()
                    .HasForeignKey("UnitId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<Domain.Entities.RefreshToken>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.Token).IsUnique();
                b.HasIndex(x => x.UserId);
                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
                b.Property(x => x.Token).HasMaxLength(500).IsRequired();
                b.Property(x => x.IpAddress).HasMaxLength(45);
                b.Property(x => x.UserAgent).HasMaxLength(500);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<BookingRequest>(b =>
            {
                b.HasIndex(br => br.IsDeleted).HasFilter("[IsDeleted] = 0");
                b.HasIndex(br => br.Phone);
                b.HasIndex(br => br.PropertyCode);
                b.HasIndex(br => br.CreatedAt);
                b.Property(br => br.Source).HasMaxLength(50).HasDefaultValue("direct");
                b.Property(br => br.Medium).HasMaxLength(50).IsRequired(false);
                b.Property(br => br.Campaign).HasMaxLength(100).IsRequired(false);
                b.Property(br => br.Term).HasMaxLength(100).IsRequired(false);
                b.Property(br => br.Content).HasMaxLength(100).IsRequired(false);
                b.Property(br => br.LandingPage).HasMaxLength(500).IsRequired(false);
                b.Property(br => br.FirstVisitAt).IsRequired(false);
                b.Property(br => br.CurrentPage).HasMaxLength(500).IsRequired(false);
                b.Property(br => br.Referrer).HasMaxLength(500).IsRequired(false);
                b.Property(br => br.UserAgent).HasMaxLength(500).IsRequired(false);
                b.Property(br => br.PageViews).HasDefaultValue(0);
                b.Property(br => br.SessionDuration).IsRequired(false);
                b.Property(br => br.LastReferrer).HasMaxLength(500).IsRequired(false);
                b.Property(br => br.VisitHistory).HasMaxLength(8000).IsRequired(false);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<Setting>(b =>
            {
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<Domain.Entities.OrphanedUpload>(b =>
            {
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            // Filtered indexes for remaining soft-delete entities
            modelBuilder.Entity<ContactInfo>().HasIndex(c => c.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<Setting>().HasIndex(s => s.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<PropertyImage>().HasIndex(p => p.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<UnitImage>().HasIndex(u => u.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<PropertyVideo>().HasIndex(p => p.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<ProjectVideo>().HasIndex(p => p.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<UnitVideo>().HasIndex(u => u.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<PropertyInstallmentPlan>().HasIndex(p => p.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<UnitInstallmentPlan>().HasIndex(u => u.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<UnitVariant>().HasIndex(u => u.IsDeleted).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<UnitVariant>().HasIndex(u => u.PublicKey).IsUnique();
            modelBuilder.Entity<ProjectImage>().HasIndex(p => p.IsDeleted).HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<UnitImage>(b =>
            {
                b.HasIndex(ui => ui.UnitId);
                b.HasIndex(ui => ui.SortOrder);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<User>(b =>
            {
                b.HasIndex(u => u.Username).IsUnique();
                b.HasIndex(u => u.IsActive);
                b.Property(x => x.RowVersion).IsRowVersion();
            });
        }
    }
}
