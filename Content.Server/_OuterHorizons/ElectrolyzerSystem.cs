using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems;

[UsedImplicitly]
public sealed partial class ElectrolyzerSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectrolyzerComponent, AtmosDeviceUpdateEvent>(OnUpdated);
    }

    private void OnUpdated(EntityUid uid, ElectrolyzerComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        if (!comp.Enabled)
            return;

        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver) && !apcPowerReceiver.Powered)
            return;

        // Получаем выходные трубы (кислород и водород)
        if (!_nodeContainer.TryGetNodes(uid, comp.OutletOxygen, comp.OutletHydrogen,
                out PipeNode? outletOxygen, out PipeNode? outletHydrogen))
            return;

        if (outletOxygen?.GetType() != typeof(PipeNode)
         || outletHydrogen?.GetType() != typeof(PipeNode))
            return;

        // Не работаем, если оба выхода забиты
        if (outletOxygen.Air.Pressure >= Atmospherics.MaxOutputPressure
            && outletHydrogen.Air.Pressure >= Atmospherics.MaxOutputPressure)
            return;

        // Получаем solution с водой
        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionName, out var solution, out _))
            return;

        // Сколько воды доступно
        var waterAvailable = solution.Value.Comp.Solution.GetTotalPrototypeQuantity("Water");
        if (waterAvailable <= 0)
            return;

        // Считаем, сколько воды потратить за этот тик
        var waterToConsume = FixedPoint2.Min(
            waterAvailable,
            FixedPoint2.New(comp.WaterConsumptionRate * args.dt)
        );

        if (waterToConsume <= 0)
            return;

        // Удаляем воду из solution
        //_solutionContainer.RemoveReagent(uid, "Water", waterToConsume, comp.SolutionName);
        _solutionContainer.RemoveReagent(solution.Value, "Water", waterToConsume);

        // Переводим единицы воды в моли газа
        var totalMoles = (float)waterToConsume * comp.MolesPerWaterUnit;

        // Электролиз: 2H₂O → 2H₂ + O₂
        // По молям: ⅓ O₂, ⅔ H₂
        var oxygenMoles = totalMoles * (1f / 3f);
        var hydrogenMoles = totalMoles * (2f / 3f);

        var temperature = solution.Value.Comp.Solution.Temperature;

        // Выдаём кислород
        if (oxygenMoles > 0 && outletOxygen.Air.Pressure < Atmospherics.MaxOutputPressure)
        {
            var limitOxygen = AtmosphereSystem.MolesToMaxPressure(
                new GasMixture { Temperature = temperature },
                outletOxygen.Air,
                Atmospherics.MaxOutputPressure);

            var actualOxygen = Math.Min(oxygenMoles, Math.Max(limitOxygen, 0));
            if (actualOxygen > 0)
            {
                var oxygenMix = new GasMixture { Temperature = temperature };
                oxygenMix.SetMoles(Gas.Oxygen, actualOxygen);
                _atmosphereSystem.Merge(outletOxygen.Air, oxygenMix);
            }
        }

        // Выдаём водород (Tritium как плейсхолдер H₂)
        if (hydrogenMoles > 0 && outletHydrogen.Air.Pressure < Atmospherics.MaxOutputPressure)
        {
            var limitHydrogen = AtmosphereSystem.MolesToMaxPressure(
                new GasMixture { Temperature = temperature },
                outletHydrogen.Air,
                Atmospherics.MaxOutputPressure);

            var actualHydrogen = Math.Min(hydrogenMoles, Math.Max(limitHydrogen, 0));
            if (actualHydrogen > 0)
            {
                var hydrogenMix = new GasMixture { Temperature = temperature };
                hydrogenMix.SetMoles(Gas.Tritium, actualHydrogen);
                _atmosphereSystem.Merge(outletHydrogen.Air, hydrogenMix);
            }
        }
    }
}
