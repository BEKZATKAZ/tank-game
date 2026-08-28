namespace Tanks.Shared.Entities
{
    public interface IEntity
    {
        EntityId Id { get; }
        string DisplayName { get; }
    }
}
