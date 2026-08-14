namespace VFZ.CxO.MigrationTool.Application.Transformers;

public sealed record XgsponResourceIds(string OntIntentId, string L2UserIntentId, string SubscriberId)
{
    public static XgsponResourceIds Generate() =>
        new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
}
