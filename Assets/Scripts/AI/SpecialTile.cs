public interface ISpecialTile
{
    bool CanPass();
    void OnApproach();// Called one step before the entity enters
    void OnClear();// Called after the entity has moved past

    bool IsDoor();
}