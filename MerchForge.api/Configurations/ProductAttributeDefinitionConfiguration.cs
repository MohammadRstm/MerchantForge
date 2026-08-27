using System.Text.Json;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class ProductAttributeDefinitionConfiguration
    : IEntityTypeConfiguration<ProductAttributeDefinition>
{
    private static ProductAttributeDefinition Seed(
        string id,
        Guid domainId,
        string key,
        string label,
        ProductAttributeValueType valueType,
        int displayOrder,
        bool isRequired = false,
        string[]? allowedValues = null) => new()
        {
            Id = Guid.Parse(id),
            BusinessDomainId = domainId,
            Key = key,
            Label = label,
            ValueType = valueType,
            IsRequired = isRequired,
            AllowedValues = allowedValues is null
                ? null
                : JsonSerializer.SerializeToDocument(allowedValues),
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
            UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
        };

    // Closed sets where a free-form value would be meaningless to filter or display
    // consistently across a catalog.
    private static readonly string[] GarmentSizes = ["XS", "S", "M", "L", "XL", "2XL"];

    private static readonly string[] GroceryDietaryTags =
        ["Vegan", "Vegetarian", "Gluten-Free", "Dairy-Free", "Nut-Free", "Sugar-Free"];

    public void Configure(EntityTypeBuilder<ProductAttributeDefinition> builder)
    {
        builder.ToTable("product_attribute_definitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(100);

        // Stored as its name, not its ordinal: this value is snapshotted into
        // Business.MetadataShape, so an ordinal would silently change meaning if the
        // enum were ever reordered.
        builder.Property(x => x.ValueType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.IsRequired)
            .IsRequired();

        // Real json column, so a malformed allowed-value set is rejected by the
        // database rather than only when validation next runs.
        builder.Property(x => x.AllowedValues)
            .HasColumnType("json");

        builder.Property(x => x.DisplayOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Restrict: retiring a domain must not delete field definitions that
        // businesses have already snapshotted into their metadata shape.
        builder.HasOne(x => x.BusinessDomain)
            .WithMany(x => x.ProductAttributeDefinitions)
            .HasForeignKey(x => x.BusinessDomainId)
            .OnDelete(DeleteBehavior.Restrict);

        // One definition per key per domain. "color" may exist under both Fashion
        // and Electronics as separate rows, exactly like category slugs.
        builder.HasIndex(x => new { x.BusinessDomainId, x.Key })
            .IsUnique();

        var fashion = BusinessDomainConfiguration.FashionId;
        var restaurant = BusinessDomainConfiguration.RestaurantId;
        var electronics = BusinessDomainConfiguration.ElectronicsId;
        var grocery = BusinessDomainConfiguration.GroceryId;

        builder.HasData(
            // ---- Fashion ----
            Seed("a1000000-0000-4000-8000-000000000001", fashion, "colors", "Colors", ProductAttributeValueType.ColorList, 1, isRequired: true),
            Seed("a1000000-0000-4000-8000-000000000002", fashion, "sizes", "Sizes", ProductAttributeValueType.TextList, 2, isRequired: true, allowedValues: GarmentSizes),
            Seed("a1000000-0000-4000-8000-000000000003", fashion, "material", "Material", ProductAttributeValueType.Text, 3),
            Seed("a1000000-0000-4000-8000-000000000004", fashion, "fit", "Fit", ProductAttributeValueType.Text, 4),
            Seed("a1000000-0000-4000-8000-000000000005", fashion, "pattern", "Pattern", ProductAttributeValueType.Text, 5),
            Seed("a1000000-0000-4000-8000-000000000006", fashion, "gender", "Gender", ProductAttributeValueType.Text, 6),
            Seed("a1000000-0000-4000-8000-000000000007", fashion, "season", "Season", ProductAttributeValueType.Text, 7),
            Seed("a1000000-0000-4000-8000-000000000008", fashion, "careInstructions", "Care instructions", ProductAttributeValueType.Text, 8),
            Seed("a1000000-0000-4000-8000-000000000009", fashion, "countryOfOrigin", "Country of origin", ProductAttributeValueType.Text, 9),
            Seed("a1000000-0000-4000-8000-00000000000a", fashion, "handmade", "Handmade", ProductAttributeValueType.Boolean, 10),
            Seed("a1000000-0000-4000-8000-00000000000b", fashion, "brand", "Brand", ProductAttributeValueType.Text, 11),

            // ---- Restaurant ----
            Seed("a2000000-0000-4000-8000-000000000001", restaurant, "ingredients", "Ingredients", ProductAttributeValueType.TextList, 1),
            Seed("a2000000-0000-4000-8000-000000000002", restaurant, "allergens", "Allergens", ProductAttributeValueType.TextList, 2),
            Seed("a2000000-0000-4000-8000-000000000003", restaurant, "spicy", "Spicy", ProductAttributeValueType.Boolean, 3),
            Seed("a2000000-0000-4000-8000-000000000004", restaurant, "vegetarian", "Vegetarian", ProductAttributeValueType.Boolean, 4),
            Seed("a2000000-0000-4000-8000-000000000005", restaurant, "vegan", "Vegan", ProductAttributeValueType.Boolean, 5),
            Seed("a2000000-0000-4000-8000-000000000006", restaurant, "glutenFree", "Gluten free", ProductAttributeValueType.Boolean, 6),
            Seed("a2000000-0000-4000-8000-000000000007", restaurant, "calories", "Calories", ProductAttributeValueType.Number, 7),
            Seed("a2000000-0000-4000-8000-000000000008", restaurant, "portionSize", "Portion size", ProductAttributeValueType.Text, 8),
            Seed("a2000000-0000-4000-8000-000000000009", restaurant, "preparationMinutes", "Preparation time (minutes)", ProductAttributeValueType.Number, 9),
            Seed("a2000000-0000-4000-8000-00000000000a", restaurant, "servingTemperature", "Serving temperature", ProductAttributeValueType.Text, 10),

            // ---- Electronics ----
            Seed("a3000000-0000-4000-8000-000000000001", electronics, "brand", "Brand", ProductAttributeValueType.Text, 1),
            Seed("a3000000-0000-4000-8000-000000000002", electronics, "model", "Model", ProductAttributeValueType.Text, 2),
            Seed("a3000000-0000-4000-8000-000000000003", electronics, "storage", "Storage", ProductAttributeValueType.Text, 3),
            Seed("a3000000-0000-4000-8000-000000000004", electronics, "ram", "RAM", ProductAttributeValueType.Text, 4),
            Seed("a3000000-0000-4000-8000-000000000005", electronics, "screenSize", "Screen size", ProductAttributeValueType.Text, 5),
            Seed("a3000000-0000-4000-8000-000000000006", electronics, "batteryCapacity", "Battery capacity", ProductAttributeValueType.Text, 6),
            Seed("a3000000-0000-4000-8000-000000000007", electronics, "colors", "Colors", ProductAttributeValueType.ColorList, 7),
            Seed("a3000000-0000-4000-8000-000000000008", electronics, "connectivity", "Connectivity", ProductAttributeValueType.TextList, 8),
            Seed("a3000000-0000-4000-8000-000000000009", electronics, "operatingSystem", "Operating system", ProductAttributeValueType.Text, 9),
            Seed("a3000000-0000-4000-8000-00000000000a", electronics, "warrantyMonths", "Warranty (months)", ProductAttributeValueType.Number, 10),

            // ---- Grocery ----
            Seed("a4000000-0000-4000-8000-000000000001", grocery, "unit", "Unit", ProductAttributeValueType.Text, 1, isRequired: true),
            Seed("a4000000-0000-4000-8000-000000000002", grocery, "organic", "Organic", ProductAttributeValueType.Boolean, 2),
            Seed("a4000000-0000-4000-8000-000000000003", grocery, "origin", "Origin", ProductAttributeValueType.Text, 3),
            Seed("a4000000-0000-4000-8000-000000000004", grocery, "brand", "Brand", ProductAttributeValueType.Text, 4),
            Seed("a4000000-0000-4000-8000-000000000005", grocery, "dietaryTags", "Dietary tags", ProductAttributeValueType.TextList, 5, allowedValues: GroceryDietaryTags),
            Seed("a4000000-0000-4000-8000-000000000006", grocery, "storageInstructions", "Storage instructions", ProductAttributeValueType.Text, 6),
            Seed("a4000000-0000-4000-8000-000000000007", grocery, "shelfLifeDays", "Shelf life (days)", ProductAttributeValueType.Number, 7),
            Seed("a4000000-0000-4000-8000-000000000008", grocery, "packSize", "Pack size", ProductAttributeValueType.Text, 8),
            Seed("a4000000-0000-4000-8000-000000000009", grocery, "ingredients", "Ingredients", ProductAttributeValueType.Text, 9),
            Seed("a4000000-0000-4000-8000-00000000000a", grocery, "isFrozen", "Frozen", ProductAttributeValueType.Boolean, 10));
    }
}
