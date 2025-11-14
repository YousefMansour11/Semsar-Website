namespace Application.Interfaces;

public class EntityGraphNode
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<EntityGraphEdge> Edges { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class EntityGraphEdge
{
    public string Relationship { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
}

public class EntityGraphResult
{
    public List<EntityGraphNode> Nodes { get; set; } = new();
    public string JsonLd { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
}

public interface IEntityGraphService
{
    EntityGraphNode BuildEntityNode(string entityType, string entityId, string name, string? description = null);
    void AddRelationship(EntityGraphNode source, string relationship, EntityGraphNode target, double weight = 1.0);
    EntityGraphResult BuildKnowledgeGraph(string entityType, string entityId);
    bool VerifyGraphIntegrity(string entityType, string entityId);
    List<EntityGraphNode> GetRelatedEntities(string entityType, string entityId, int maxDepth = 2);
}
