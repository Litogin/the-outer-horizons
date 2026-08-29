using Robust.Shared.Prototypes;

namespace Content.Server._OuterHorizons.SolarFlare.Components;

[RegisterComponent]
public sealed partial class MagneticFieldGeneratorComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Filed = null;

    [DataField("spawn", required: true)]
    public string ProtoSpawnId = null!;
}
