using Content.Server._OuterHorizons.SolarFlare.Components;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;

namespace Content.Shared.SolarFlare;

public sealed class SunSystem : EntitySystem
{

    [Dependency] private MapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolarFlareComponent, ComponentInit>(OnSolarFlare);
        SubscribeLocalEvent<SolarFlareComponent, ComponentRemove>(OnRemove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolarFlareComponent, RadiationSourceComponent>();
        while (query.MoveNext(out var uid, out var solarFlare, out var radiationSource))
            OnUpdateRad(uid, solarFlare, radiationSource, frameTime);
    }

    private void OnSolarFlare(EntityUid uid, SolarFlareComponent comp, ComponentInit arg)
    {
        var mapIds = _mapSystem.GetAllMapIds();
        var isMap = false;
        foreach (var mapId in mapIds)
        {
            if (uid == _mapSystem.GetMap(mapId))
            {
                isMap = true;
                break;
            }
        }

        if (!isMap)
        {
            RemComp<SolarFlareComponent>(uid);
        }

        var radSourceComp = AddComp<RadiationSourceComponent>(uid);
        radSourceComp.IgnoreDistation = true;
        radSourceComp.Slope = 0f;
    }

    private void OnRemove(EntityUid uid, SolarFlareComponent comp, ComponentRemove args)
    {
        RemComp<RadiationSourceComponent>(uid);
    }

    private void OnUpdateRad(EntityUid uid, SolarFlareComponent solarFlare, RadiationSourceComponent radiation, float frameTime)
    {
        if (MathF.Abs(radiation.Intensity - solarFlare.SolarFlareOnRadiation) < 0.001f)
        {
            radiation.Intensity = solarFlare.SolarFlareOnRadiation;
        }

        float step = solarFlare.Speed * frameTime;

        if (radiation.Intensity < solarFlare.SolarFlareOnRadiation)
        {
            radiation.Intensity = MathF.Min(radiation.Intensity + step, solarFlare.SolarFlareOnRadiation);
        }
        else
        {
            radiation.Intensity = MathF.Max(radiation.Intensity - step, solarFlare.SolarFlareOnRadiation);
        }
    }
}
