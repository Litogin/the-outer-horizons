using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._OuterHorizons.Electrolyzer;

[RegisterComponent]
public sealed partial class ElectrolyzerComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField("releasedGases")]
    public Dictionary<Gas, FixedPoint2> ReleasedGases = new() { { Gas.Oxygen, 1/3f }, { Gas.Tritium, 2/3f } };

    [DataField]
    public ProtoId<ReagentPrototype> LiquidToConsume = "Water";

    [DataField]
    public string Outlet = "outlet";

    [DataField]
    public string SolutionName = "tank";

    [DataField]
    public float LiquidConsumptionRate = 5f;
}
