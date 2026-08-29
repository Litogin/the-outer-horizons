using Content.Server._OuterHorizons.SolarFlare.Components;
using Content.Server.Power.Components;

namespace Content.Server._OuterHorizons.SolarFlare;

public sealed class MagneticFieldGeneratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MagneticFieldGeneratorComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var generator, out var apcPower))
        {
            if (!apcPower.Powered)
            {
                if (generator.Filed is not null)
                {
                    QueueDel(generator.Filed);
                    generator.Filed = null;
                }
                continue;
            }

            if (generator.Filed is null)
            {
                generator.Filed = Spawn(generator.ProtoSpawnId, Transform(uid).Coordinates);
            }
        }
    }
}
