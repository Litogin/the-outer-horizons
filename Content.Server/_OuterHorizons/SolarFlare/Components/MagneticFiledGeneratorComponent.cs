namespace Content.Server._OuterHorizons.SolarFlare.Components;

[RegisterComponent]
public sealed partial class MagneticFiledGeneratorComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Entity<MagneticFiledComponent> Filed = default!;
}
