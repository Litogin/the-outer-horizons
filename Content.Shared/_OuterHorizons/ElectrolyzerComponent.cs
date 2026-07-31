using Content.Shared.Atmos;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

[RegisterComponent]
public sealed partial class ElectrolyzerComponent : Component
{
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Выходная труба для кислорода.
    /// </summary>
    [DataField]
    public string OutletOxygen = "outletOxygen";

    /// <summary>
    /// Выходная труба для водорода.
    /// </summary>
    [DataField]
    public string OutletHydrogen = "outletHydrogen";

    /// <summary>
    /// Имя solution, откуда берётся вода (заполняется сторонней системой водопроводов).
    /// </summary>
    [DataField]
    public string SolutionName = "tank";

    /// <summary>
    /// Сколько единиц воды (u) расходуем за секунду.
    /// </summary>
    [DataField]
    public float WaterConsumptionRate = 5f;

    /// <summary>
    /// Сколько молей газа получается из 1 единицы воды.
    /// </summary>
    [DataField]
    public float MolesPerWaterUnit = 2f;
}
