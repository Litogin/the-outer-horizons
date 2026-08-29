using Robust.Shared.GameStates;

namespace Content.Shared._OuterHorizons.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class MapBoundsComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Radius { get; set; } = 5000f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float BaseImpulseVelocity { get; set; } = 1f;
}
