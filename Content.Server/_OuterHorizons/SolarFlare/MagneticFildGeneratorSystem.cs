using Content.Server._OuterHorizons.SolarFlare.Components;
using Content.Shared.Power;

namespace Content.Server._OuterHorizons.SolarFlare;

public sealed class MagneticFieldGeneratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagneticFieldGeneratorComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public void OnPowerChanged(EntityUid uid, MagneticFieldGeneratorComponent comp, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            if (comp.Filed is not null)
            {
                QueueDel(comp.Filed);
                comp.Filed = null;
            }
        }
        else
        {
            if (comp.Filed is null)
            {
                comp.Filed = Spawn(comp.ProtoSpawnId, Transform(uid).Coordinates);
            }
        }
    }
}
