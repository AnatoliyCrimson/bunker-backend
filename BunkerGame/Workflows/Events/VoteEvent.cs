namespace BunkerGame.Workflows.Events;

public class VoteEvent
{
    public Guid UserId { get; set; }
    public List<Guid> TargetIds { get; set; } = new();
}