using Content.Shared.Atmos;
using Content.Shared.FixedPoint;

namespace Content.Shared._OuterHorizons.Electrolyzer;

[RegisterComponent]
public sealed partial class ElectrolyzerComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField("releasedGases")]
    public Dictionary<Gas, FixedPoint2> ReleasedGases = new() { { Gas.Oxygen, 1/4f }, { Gas.Tritium, 1/4f }, { Gas.Plasma, 2/4f } };

    /// <summary>
    /// Выходная труба для кислорода.
    /// </summary>
    [DataField]
    public string LiquidToConsume = "Water";

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
