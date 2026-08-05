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

    /// <summary>
    /// Выходная труба для кислорода.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> LiquidToConsume = "Water";

    /// <summary>
    /// Выходная труба.
    /// </summary>
    [DataField]
    public string Outlet = "outlet";

    /// <summary>
    /// Имя solution, откуда берётся жидкость (заполняется сторонней системой водопроводов).
    /// </summary>
    [DataField]
    public string SolutionName = "tank";

    /// <summary>
    /// Сколько единиц жидкости (u) расходуем за секунду.
    /// </summary>
    [DataField]
    public float LiquidConsumptionRate = 5f;
}
