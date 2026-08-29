namespace Content.Server._OuterHorizons.SolarFlare.Components;

[RegisterComponent]
public sealed partial class SolarFlareComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float SolarFlareOnRadiation = 150;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Speed = 0.01f;
}
