BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529000000_AddUnitVariantNameAr'
)
BEGIN
    ALTER TABLE [UnitVariants] ADD [NameAr] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529000000_AddUnitVariantNameAr'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260529000000_AddUnitVariantNameAr', N'10.0.5');
END;

COMMIT;
GO

