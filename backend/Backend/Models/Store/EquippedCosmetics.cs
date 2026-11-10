namespace Backend.Models.Store;

public readonly record struct EquippedCosmetics(string Body, string Turret)
{
    public static EquippedCosmetics Default => new("v3-body", "v3-turret");
}
