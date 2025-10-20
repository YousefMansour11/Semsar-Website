namespace Domain.Entities
{
    public class UnitFeature
    {
        public int UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        public int FeatureId { get; set; }
        public Feature Feature { get; set; } = null!;
    }
}
