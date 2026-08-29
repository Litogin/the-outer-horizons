namespace Content.Server._OuterHorizons.SolarFlare.Components;

[RegisterComponent]
public sealed partial class SolarFlareComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float SolarFlareOnRadiation = 400;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Speed = 0.03f;
}
