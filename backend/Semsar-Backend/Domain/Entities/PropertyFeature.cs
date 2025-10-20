namespace Domain.Entities
{
    public class PropertyFeature
    {
        public int PropertyId { get; set; }
        public Property Property { get; set; } = null!;

        public int FeatureId { get; set; }
        public Feature Feature { get; set; } = null!;
    }
}
