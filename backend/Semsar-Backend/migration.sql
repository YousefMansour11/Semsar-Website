IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [BookingRequests] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [PropertyCode] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Message] nvarchar(500) NULL,
        [PreferredDate] datetime2 NULL,
        [Source] nvarchar(50) NOT NULL DEFAULT N'direct',
        [Medium] nvarchar(50) NULL,
        [Campaign] nvarchar(100) NULL,
        [Term] nvarchar(100) NULL,
        [Content] nvarchar(100) NULL,
        [LandingPage] nvarchar(500) NULL,
        [FirstVisitAt] datetime2 NULL,
        [CurrentPage] nvarchar(500) NULL,
        [Referrer] nvarchar(500) NULL,
        [UserAgent] nvarchar(500) NULL,
        [PageViews] int NOT NULL DEFAULT 0,
        [SessionDuration] int NULL,
        [LastReferrer] nvarchar(500) NULL,
        [VisitHistory] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_BookingRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Contacts] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Type] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Contacts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Features] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(200) NULL,
        [NameAr] nvarchar(200) NULL,
        CONSTRAINT [PK_Features] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [LandRequests] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Location] nvarchar(200) NULL,
        [MinPrice] decimal(18,2) NULL,
        [MaxPrice] decimal(18,2) NULL,
        [MinArea] decimal(18,2) NULL,
        [MaxArea] decimal(18,2) NULL,
        [Notes] nvarchar(500) NULL,
        [Source] nvarchar(50) NOT NULL DEFAULT N'direct',
        [Medium] nvarchar(50) NULL,
        [Campaign] nvarchar(100) NULL,
        [Term] nvarchar(100) NULL,
        [Content] nvarchar(100) NULL,
        [LandingPage] nvarchar(500) NULL,
        [FirstVisitAt] datetime2 NULL,
        [CurrentPage] nvarchar(500) NULL,
        [Referrer] nvarchar(500) NULL,
        [UserAgent] nvarchar(500) NULL,
        [PageViews] int NOT NULL DEFAULT 0,
        [SessionDuration] int NULL,
        [LastReferrer] nvarchar(500) NULL,
        [VisitHistory] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LandRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Locations] (
        [Id] int NOT NULL IDENTITY,
        [NameEn] nvarchar(200) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [Slug] nvarchar(300) NOT NULL,
        [ParentId] int NULL,
        [Level] tinyint NOT NULL,
        [Path] nvarchar(500) NOT NULL,
        [Depth] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Locations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Locations_Locations_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [OrphanedUploads] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OrphanedUploads] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Projects] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [Slug] nvarchar(450) NOT NULL,
        [SlugIsAuto] bit NOT NULL,
        [SlugLanguage] nvarchar(5) NOT NULL,
        [SeoTitle] nvarchar(200) NULL,
        [SeoDescription] nvarchar(300) NULL,
        [SeoTitleAr] nvarchar(200) NULL,
        [SeoDescriptionAr] nvarchar(300) NULL,
        [SeoKeywords] nvarchar(500) NULL,
        [SeoKeywordsAr] nvarchar(500) NULL,
        [CanonicalUrl] nvarchar(1000) NOT NULL,
        [MetaGeneratedAt] datetime2 NOT NULL,
        [MetaVersion] int NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [DescriptionEn] nvarchar(max) NULL,
        [DescriptionAr] nvarchar(max) NULL,
        [Location] nvarchar(200) NOT NULL,
        [LocationAr] nvarchar(max) NULL,
        [Developer] nvarchar(200) NOT NULL,
        [Image] nvarchar(500) NULL,
        [Highlights] nvarchar(max) NOT NULL,
        [HighlightsAr] nvarchar(max) NULL,
        [UnitCount] int NOT NULL,
        [ExpectedDeliveryDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Settings] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(max) NOT NULL,
        [Value] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Settings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [Email] nvarchar(max) NULL,
        [FailedLoginAttempts] int NOT NULL,
        [LockoutEnd] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Properties] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [TitleEn] nvarchar(200) NOT NULL,
        [TitleAr] nvarchar(200) NOT NULL,
        [DescriptionEn] nvarchar(max) NULL,
        [DescriptionAr] nvarchar(max) NULL,
        [Price] decimal(18,2) NOT NULL,
        [RentPerMonth] decimal(18,2) NULL,
        [Currency] nvarchar(10) NOT NULL,
        [PropertyType] int NOT NULL,
        [ListingType] int NOT NULL,
        [Location] nvarchar(200) NOT NULL,
        [LocationAr] nvarchar(max) NULL,
        [LocationId] int NULL,
        [Code] nvarchar(50) NOT NULL,
        [Slug] nvarchar(450) NOT NULL,
        [SlugIsAuto] bit NOT NULL,
        [SlugLanguage] nvarchar(5) NOT NULL,
        [SeoTitle] nvarchar(200) NULL,
        [SeoDescription] nvarchar(300) NULL,
        [SeoTitleAr] nvarchar(200) NULL,
        [SeoDescriptionAr] nvarchar(300) NULL,
        [SeoKeywords] nvarchar(500) NULL,
        [SeoKeywordsAr] nvarchar(500) NULL,
        [CanonicalUrl] nvarchar(1000) NOT NULL,
        [MetaGeneratedAt] datetime2 NOT NULL,
        [MetaVersion] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [Size] float NOT NULL,
        [Bedrooms] int NOT NULL,
        [Bathrooms] int NOT NULL,
        [Floor] int NULL,
        [TotalFloors] int NULL,
        [IsFurnished] bit NOT NULL,
        [View] nvarchar(50) NOT NULL,
        [Features] nvarchar(max) NOT NULL,
        [FeaturesAr] nvarchar(max) NOT NULL,
        [ContactId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [ViewCount] int NOT NULL,
        [ContactClickCount] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Properties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Properties_Contacts_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [Contacts] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Properties_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [ProjectDetails] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [CashDiscountPercentage] decimal(18,2) NOT NULL,
        [DownPaymentPercentage] decimal(18,2) NOT NULL,
        [MinInstallmentYears] int NOT NULL,
        [MaxInstallmentYears] int NOT NULL,
        [PaymentNotes] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ProjectDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectDetails_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [ProjectImages] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [Url] nvarchar(1000) NOT NULL,
        [FileName] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [PublicId] nvarchar(500) NULL,
        CONSTRAINT [PK_ProjectImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectImages_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Units] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [TitleEn] nvarchar(200) NOT NULL,
        [TitleAr] nvarchar(200) NOT NULL,
        [DescriptionEn] nvarchar(max) NULL,
        [DescriptionAr] nvarchar(max) NULL,
        [Price] decimal(18,2) NOT NULL,
        [RentPerMonth] decimal(18,2) NULL,
        [Currency] nvarchar(10) NOT NULL,
        [PropertyType] int NOT NULL,
        [ListingType] int NOT NULL,
        [Location] nvarchar(200) NOT NULL,
        [LocationAr] nvarchar(max) NULL,
        [LocationId] int NULL,
        [Code] nvarchar(50) NOT NULL,
        [Slug] nvarchar(450) NOT NULL,
        [SlugIsAuto] bit NOT NULL,
        [SlugLanguage] nvarchar(5) NOT NULL,
        [SeoTitle] nvarchar(200) NULL,
        [SeoDescription] nvarchar(300) NULL,
        [SeoTitleAr] nvarchar(200) NULL,
        [SeoDescriptionAr] nvarchar(300) NULL,
        [SeoKeywords] nvarchar(500) NULL,
        [SeoKeywordsAr] nvarchar(500) NULL,
        [CanonicalUrl] nvarchar(1000) NOT NULL,
        [MetaGeneratedAt] datetime2 NOT NULL,
        [MetaVersion] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [Size] float NOT NULL,
        [Bedrooms] int NOT NULL,
        [Bathrooms] int NOT NULL,
        [Floor] int NULL,
        [IsFurnished] bit NOT NULL,
        [View] nvarchar(50) NOT NULL,
        [UnitNumber] nvarchar(50) NULL,
        [BuildingNumber] nvarchar(50) NULL,
        [DeliveryDate] datetime2 NULL,
        [FinishingType] nvarchar(50) NULL,
        [HasBalcony] bit NOT NULL,
        [HasParking] bit NOT NULL,
        [Features] nvarchar(max) NOT NULL,
        [FeaturesAr] nvarchar(max) NOT NULL,
        [ContactId] int NULL,
        [ProjectId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Units] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Units_Contacts_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [Contacts] ([Id]),
        CONSTRAINT [FK_Units_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Units_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [Token] nvarchar(500) NOT NULL,
        [UserId] int NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByToken] nvarchar(max) NULL,
        [ReasonRevoked] nvarchar(max) NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [Leads] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [PropertyId] int NULL,
        [Name] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Message] nvarchar(500) NULL,
        [Source] nvarchar(50) NOT NULL DEFAULT N'direct',
        [Medium] nvarchar(50) NULL,
        [Campaign] nvarchar(100) NULL,
        [Term] nvarchar(100) NULL,
        [Content] nvarchar(100) NULL,
        [LandingPage] nvarchar(500) NULL,
        [FirstVisitAt] datetime2 NULL,
        [CurrentPage] nvarchar(500) NULL,
        [IsPaid] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Referrer] nvarchar(500) NULL,
        [UserAgent] nvarchar(500) NULL,
        [PageViews] int NOT NULL DEFAULT 0,
        [SessionDuration] int NULL,
        [LastReferrer] nvarchar(500) NULL,
        [VisitHistory] nvarchar(max) NULL,
        [BookingRequestId] int NULL,
        [LandRequestId] int NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Status] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Leads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Leads_BookingRequests_BookingRequestId] FOREIGN KEY ([BookingRequestId]) REFERENCES [BookingRequests] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Leads_LandRequests_LandRequestId] FOREIGN KEY ([LandRequestId]) REFERENCES [LandRequests] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Leads_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [PropertyFeatures] (
        [PropertyId] int NOT NULL,
        [FeatureId] int NOT NULL,
        CONSTRAINT [PK_PropertyFeatures] PRIMARY KEY ([PropertyId], [FeatureId]),
        CONSTRAINT [FK_PropertyFeatures_Features_FeatureId] FOREIGN KEY ([FeatureId]) REFERENCES [Features] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PropertyFeatures_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [PropertyImages] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Url] nvarchar(1000) NOT NULL,
        [FileName] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [PublicId] nvarchar(500) NULL,
        CONSTRAINT [PK_PropertyImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyImages_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [PropertyInstallmentPlans] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [DownPaymentPercent] int NOT NULL,
        [Years] int NOT NULL,
        [MonthlyAmount] decimal(18,2) NULL,
        [IsEnabled] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PropertyInstallmentPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyInstallmentPlans_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [RentalDetails] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Period] int NOT NULL,
        [Furnished] bit NOT NULL,
        [SecurityDeposit] decimal(18,2) NULL,
        [MaintenanceFee] decimal(18,2) NULL,
        [Notes] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RentalDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RentalDetails_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [CodeReservations] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [Prefix] nvarchar(50) NOT NULL,
        [Code] nvarchar(200) NOT NULL,
        [EntityId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PropertyId] int NULL,
        [ProjectId] int NULL,
        [UnitId] int NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CodeReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CodeReservations_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CodeReservations_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CodeReservations_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [SlugReservations] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [Slug] nvarchar(450) NOT NULL,
        [EntityId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PropertyId] int NULL,
        [ProjectId] int NULL,
        [UnitId] int NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SlugReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SlugReservations_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SlugReservations_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SlugReservations_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [UnitFeatures] (
        [UnitId] int NOT NULL,
        [FeatureId] int NOT NULL,
        CONSTRAINT [PK_UnitFeatures] PRIMARY KEY ([UnitId], [FeatureId]),
        CONSTRAINT [FK_UnitFeatures_Features_FeatureId] FOREIGN KEY ([FeatureId]) REFERENCES [Features] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UnitFeatures_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [UnitImage] (
        [Id] int NOT NULL IDENTITY,
        [UnitId] int NOT NULL,
        [Url] nvarchar(1000) NOT NULL,
        [FileName] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [PublicId] nvarchar(500) NULL,
        CONSTRAINT [PK_UnitImage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitImage_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE TABLE [UnitInstallmentPlans] (
        [Id] int NOT NULL IDENTITY,
        [UnitId] int NOT NULL,
        [DownPaymentPercent] int NOT NULL,
        [Years] int NOT NULL,
        [MonthlyAmount] decimal(18,2) NULL,
        [IsEnabled] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UnitInstallmentPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitInstallmentPlans_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BookingRequests_CreatedAt] ON [BookingRequests] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BookingRequests_PropertyCode] ON [BookingRequests] ([PropertyCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BookingRequests_PublicId] ON [BookingRequests] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BookingRequests_PublicKey] ON [BookingRequests] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CodeReservations_EntityType_Code] ON [CodeReservations] ([EntityType], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CodeReservations_ProjectId] ON [CodeReservations] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CodeReservations_PropertyId] ON [CodeReservations] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CodeReservations_UnitId] ON [CodeReservations] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contacts_PublicId] ON [Contacts] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contacts_PublicKey] ON [Contacts] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Features_Key] ON [Features] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LandRequests_PublicId] ON [LandRequests] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LandRequests_PublicKey] ON [LandRequests] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Leads_BookingRequestId] ON [Leads] ([BookingRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Leads_CreatedAt] ON [Leads] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Leads_LandRequestId] ON [Leads] ([LandRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Leads_PropertyId] ON [Leads] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Leads_PublicId] ON [Leads] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Leads_PublicKey] ON [Leads] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Leads_Source] ON [Leads] ([Source]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Location_Level] ON [Locations] ([Level]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Location_NameAr_ParentId] ON [Locations] ([NameAr], [ParentId]) WHERE [ParentId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Location_NameEn_ParentId] ON [Locations] ([NameEn], [ParentId]) WHERE [ParentId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Location_ParentId] ON [Locations] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Location_Path] ON [Locations] ([Path]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Locations_Slug] ON [Locations] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectDetails_ProjectId] ON [ProjectDetails] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProjectImages_ProjectId] ON [ProjectImages] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProjectImages_PublicId] ON [ProjectImages] ([PublicId]) WHERE [PublicId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProjectImages_SortOrder] ON [ProjectImages] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Projects_IsDeleted] ON [Projects] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Projects_PublicId] ON [Projects] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Projects_PublicKey] ON [Projects] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Projects_Slug] ON [Projects] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_Bedrooms] ON [Properties] ([Bedrooms]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Properties_Code] ON [Properties] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_ContactId] ON [Properties] ([ContactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_CreatedAt] ON [Properties] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Properties_IsDeleted] ON [Properties] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_IsFeatured] ON [Properties] ([IsFeatured]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Properties_ListingType_PropertyType_Price_Location_IsFeatured] ON [Properties] ([ListingType], [PropertyType], [Price], [Location], [IsFeatured]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_Location] ON [Properties] ([Location]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_LocationId] ON [Properties] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_Price] ON [Properties] ([Price]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Properties_PropertyType] ON [Properties] ([PropertyType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Properties_PublicId] ON [Properties] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Properties_PublicKey] ON [Properties] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Properties_Slug] ON [Properties] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyFeatures_FeatureId] ON [PropertyFeatures] ([FeatureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyImages_PropertyId] ON [PropertyImages] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyImages_SortOrder] ON [PropertyImages] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyInstallmentPlans_PropertyId] ON [PropertyInstallmentPlans] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RentalDetails_PropertyId] ON [RentalDetails] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SlugReservations_EntityType_Slug] ON [SlugReservations] ([EntityType], [Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SlugReservations_ProjectId] ON [SlugReservations] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SlugReservations_PropertyId] ON [SlugReservations] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SlugReservations_UnitId] ON [SlugReservations] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UnitFeatures_FeatureId] ON [UnitFeatures] ([FeatureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UnitImage_SortOrder] ON [UnitImage] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UnitImage_UnitId] ON [UnitImage] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UnitInstallmentPlans_UnitId] ON [UnitInstallmentPlans] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Units_Bedrooms] ON [Units] ([Bedrooms]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Units_Code] ON [Units] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Units_ContactId] ON [Units] ([ContactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Units_CreatedAt] ON [Units] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Units_IsDeleted] ON [Units] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Units_LocationId] ON [Units] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Units_Price] ON [Units] ([Price]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Units_ProjectId_Code] ON [Units] ([ProjectId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Units_PropertyType] ON [Units] ([PropertyType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Units_PublicId] ON [Units] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Units_PublicKey] ON [Units] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Units_Slug] ON [Units] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_PublicId] ON [Users] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_PublicKey] ON [Users] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524080315_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524080315_InitialCreate', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE TABLE [ProjectVideos] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [Url] nvarchar(1000) NOT NULL,
        [PublicId] nvarchar(500) NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [Duration] float NULL,
        [Width] int NULL,
        [Height] int NULL,
        [Title] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ProjectVideos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectVideos_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE TABLE [PropertyVideos] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Url] nvarchar(1000) NOT NULL,
        [PublicId] nvarchar(500) NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [Duration] float NULL,
        [Width] int NULL,
        [Height] int NULL,
        [Title] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PropertyVideos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyVideos_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE TABLE [UnitVideos] (
        [Id] int NOT NULL IDENTITY,
        [UnitId] int NOT NULL,
        [Url] nvarchar(1000) NOT NULL,
        [PublicId] nvarchar(500) NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [Duration] float NULL,
        [Width] int NULL,
        [Height] int NULL,
        [Title] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UnitVideos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitVideos_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE INDEX [IX_ProjectVideos_ProjectId] ON [ProjectVideos] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE INDEX [IX_ProjectVideos_SortOrder] ON [ProjectVideos] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE INDEX [IX_PropertyVideos_PropertyId] ON [PropertyVideos] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE INDEX [IX_PropertyVideos_SortOrder] ON [PropertyVideos] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE INDEX [IX_UnitVideos_SortOrder] ON [UnitVideos] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    CREATE INDEX [IX_UnitVideos_UnitId] ON [UnitVideos] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524103847_AddVideoEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524103847_AddVideoEntities', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    ALTER TABLE [Projects] ADD [NearbyPlaces] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    ALTER TABLE [Projects] ADD [StartingPrice] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_UnitVideos_IsDeleted] ON [UnitVideos] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_UnitInstallmentPlans_IsDeleted] ON [UnitInstallmentPlans] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_UnitImage_IsDeleted] ON [UnitImage] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Settings_IsDeleted] ON [Settings] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_PropertyVideos_IsDeleted] ON [PropertyVideos] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_PropertyInstallmentPlans_IsDeleted] ON [PropertyInstallmentPlans] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_PropertyImages_IsDeleted] ON [PropertyImages] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_ProjectVideos_IsDeleted] ON [ProjectVideos] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_ProjectImages_IsDeleted] ON [ProjectImages] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Leads_IsDeleted] ON [Leads] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    CREATE INDEX [IX_Leads_Phone] ON [Leads] ([Phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_LandRequests_IsDeleted] ON [LandRequests] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    CREATE INDEX [IX_LandRequests_Phone] ON [LandRequests] ([Phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Contacts_IsDeleted] ON [Contacts] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_BookingRequests_IsDeleted] ON [BookingRequests] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    CREATE INDEX [IX_BookingRequests_Phone] ON [BookingRequests] ([Phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525080000_AddProjectStartingPriceAndNearbyPlaces'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260525080000_AddProjectStartingPriceAndNearbyPlaces', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_AddProjectPropertyTypesCoordinatesTotalArea'
)
BEGIN
    ALTER TABLE [Projects] ADD [Latitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_AddProjectPropertyTypesCoordinatesTotalArea'
)
BEGIN
    ALTER TABLE [Projects] ADD [Longitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_AddProjectPropertyTypesCoordinatesTotalArea'
)
BEGIN
    ALTER TABLE [Projects] ADD [PropertyTypes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_AddProjectPropertyTypesCoordinatesTotalArea'
)
BEGIN
    ALTER TABLE [Projects] ADD [TotalArea] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_AddProjectPropertyTypesCoordinatesTotalArea'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260525090000_AddProjectPropertyTypesCoordinatesTotalArea', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525100000_AddProjectOwnershipType'
)
BEGIN
    ALTER TABLE [Projects] ADD [OwnershipType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525100000_AddProjectOwnershipType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260525100000_AddProjectOwnershipType', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Properties] DROP CONSTRAINT [FK_Properties_Locations_LocationId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Units] DROP CONSTRAINT [FK_Units_Locations_LocationId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Units_Bedrooms] ON [Units];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Units_CreatedAt] ON [Units];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Units_IsDeleted] ON [Units];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Units_Price] ON [Units];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Units_ProjectId_Code] ON [Units];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Units_PropertyType] ON [Units];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Units_Slug] ON [Units];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Properties_Bedrooms] ON [Properties];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Properties_CreatedAt] ON [Properties];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Properties_IsDeleted] ON [Properties];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Properties_IsFeatured] ON [Properties];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Properties_ListingType_PropertyType_Price_Location_IsFeatured] ON [Properties];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DROP INDEX [IX_Properties_Price] ON [Properties];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Units] ADD [MaxArea] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Units] ADD [MaxPrice] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Units] ADD [MinArea] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Units] ADD [MinPrice] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    UPDATE Units SET MinPrice = Price, MaxPrice = NULL, MinArea = Size, MaxArea = NULL WHERE Price IS NOT NULL
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Units]') AND [c].[name] = N'Price');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Units] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Units] DROP COLUMN [Price];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Units]') AND [c].[name] = N'Size');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Units] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Units] DROP COLUMN [Size];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [UnitInstallmentPlans] ADD [DiscountPercent] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [UnitInstallmentPlans] ADD [PaymentType] nvarchar(20) NOT NULL DEFAULT N'Installment';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [PropertyInstallmentPlans] ADD [DiscountPercent] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [PropertyInstallmentPlans] ADD [PaymentType] nvarchar(20) NOT NULL DEFAULT N'Installment';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    CREATE INDEX [IX_Units_ProjectId] ON [Units] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Properties] ADD CONSTRAINT [FK_Properties_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    ALTER TABLE [Units] ADD CONSTRAINT [FK_Units_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525110000_AddPaymentTypeAndUnitMinMax'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260525110000_AddPaymentTypeAndUnitMinMax', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525120000_AddProjectNearbyPlacesAr'
)
BEGIN
    ALTER TABLE [Projects] ADD [NearbyPlacesAr] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525120000_AddProjectNearbyPlacesAr'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260525120000_AddProjectNearbyPlacesAr', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526000000_AddUnitVariants'
)
BEGIN
    CREATE TABLE [UnitVariants] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [PublicKey] nvarchar(450) NOT NULL,
        [UnitId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Size] float NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [RentPerMonth] decimal(18,2) NULL,
        [Bedrooms] int NOT NULL,
        [Bathrooms] int NOT NULL,
        [Floor] int NULL,
        [IsFurnished] bit NOT NULL,
        [View] nvarchar(50) NOT NULL,
        [UnitNumber] nvarchar(50) NULL,
        [BuildingNumber] nvarchar(50) NULL,
        [DeliveryDate] datetime2 NULL,
        [FinishingType] nvarchar(50) NULL,
        [HasBalcony] bit NOT NULL,
        [HasParking] bit NOT NULL,
        [FloorPlanUrl] nvarchar(500) NULL,
        [AvailabilityStatus] nvarchar(50) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UnitVariants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitVariants_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526000000_AddUnitVariants'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_UnitVariants_IsDeleted] ON [UnitVariants] ([IsDeleted]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526000000_AddUnitVariants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UnitVariants_PublicId] ON [UnitVariants] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526000000_AddUnitVariants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UnitVariants_PublicKey] ON [UnitVariants] ([PublicKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526000000_AddUnitVariants'
)
BEGIN
    CREATE INDEX [IX_UnitVariants_UnitId] ON [UnitVariants] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526000000_AddUnitVariants'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260526000000_AddUnitVariants', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [DeliveryText] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [FavoriteCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [InquiryCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [IsFeatured] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [IsRecommended] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [ViewCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [AvailabilityStatus] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [ConstructionStatus] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [DeliveryText] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [FavoriteCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [HighlightsAr] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [InquiryCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [IsRecommended] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [NearbyPlaces] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [NearbyPlacesAr] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [OwnershipType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [ViewCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Units] ADD [VirtualTourUrl] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [AvailabilityStatus] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [ConstructionStatus] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [DeliveryText] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [FavoriteCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [HighlightsAr] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [InquiryCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [IsRecommended] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [NearbyPlaces] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [NearbyPlacesAr] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [OwnershipType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Properties] ADD [VirtualTourUrl] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [AvailabilityStatus] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [ConstructionStatus] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [DeliveryText] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [FavoriteCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [InquiryCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [IsRecommended] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [ViewCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    ALTER TABLE [Projects] ADD [VirtualTourUrl] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527000000_AddPropertyNewFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260527000000_AddPropertyNewFields', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000000_AddDeliveryTextAr'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [DeliveryTextAr] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000000_AddDeliveryTextAr'
)
BEGIN
    ALTER TABLE [Units] ADD [DeliveryTextAr] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000000_AddDeliveryTextAr'
)
BEGIN
    ALTER TABLE [Properties] ADD [DeliveryTextAr] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000000_AddDeliveryTextAr'
)
BEGIN
    ALTER TABLE [Projects] ADD [DeliveryTextAr] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000000_AddDeliveryTextAr'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260528000000_AddDeliveryTextAr', N'10.0.5');
END;

COMMIT;
GO

