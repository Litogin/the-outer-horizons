using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared._OuterHorizons.Electrolyzer;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server._OuterHorizons.Electrolyzer;

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

        // Получаем выходную трубу
        if (!_nodeContainer.TryGetNode(uid, comp.Outlet,
                out PipeNode? outlet))
            return;

        // Не работаем, если выход забит
        if (outlet.Air.Pressure >= Atmospherics.MaxOutputPressure)
            return;

        // Получаем solution с жидкостью
        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionName, out var solution, out _))
            return;

        // Сколько жидкости доступно
        var waterAvailable = solution.Value.Comp.Solution.GetTotalPrototypeQuantity(comp.LiquidToConsume);
        if (waterAvailable <= 0)
            return;

        // Считаем, сколько жидкости потратить за этот тик
        var waterToConsume = FixedPoint2.Min(
            waterAvailable,
            FixedPoint2.New(comp.LiquidConsumptionRate * args.dt)
        );

        if (waterToConsume <= 0)
            return;

        // Удаляем жидкость из solution
        _solutionContainer.RemoveReagent(solution.Value, comp.LiquidToConsume, waterToConsume);

        var temperature = solution.Value.Comp.Solution.Temperature;

        foreach (var gas in comp.ReleasedGases.Keys)
        {
            // Переводим единицы жидкости в моли газа
            var moles = (float)waterToConsume * comp.ReleasedGases[gas];
            if (moles > 0)
            {
                var limitOxygen = AtmosphereSystem.MolesToMaxPressure(
                    new GasMixture { Temperature = temperature },
                    outlet.Air,
                    Atmospherics.MaxOutputPressure);

                var actualOxygen = Math.Min(moles.Float(), Math.Max(limitOxygen, 0));
                if (actualOxygen > 0)
                {
                    var oxygenMix = new GasMixture { Temperature = temperature };
                    oxygenMix.SetMoles(gas, actualOxygen);
                    _atmosphereSystem.Merge(outlet.Air, oxygenMix);
                }
            }
        }
    }
}
