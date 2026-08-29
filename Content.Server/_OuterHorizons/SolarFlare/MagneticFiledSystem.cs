using Content.Server._OuterHorizons.SolarFlare.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._OuterHorizons;

public sealed class MagneticFiledSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagneticFiledComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<MagneticFiledComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(Entity<MagneticFiledComponent> entity, ref StartCollideEvent args)
    {
        if (!HasComp<ProtectSolarRadiationComponent>(args.OtherEntity))
            AddComp<ProtectSolarRadiationComponent>(args.OtherEntity);
    }

    private void OnEndCollide(Entity<MagneticFiledComponent> entity, ref EndCollideEvent args)
    {
        RemComp<ProtectSolarRadiationComponent>(args.OtherEntity);
    }
}
