using Content.Shared.Station.Components;

namespace Content.Shared.Random.Rules;

public sealed partial class OnStationGridRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent(uid, out TransformComponent? xform) ||
            xform.GridUid == null)
        {
            return Inverted;
        }

        if (!entManager.HasComponent<StationMemberComponent>(xform.GridUid.Value))
        {
            return Inverted;
        }

        return !Inverted;
    }
}
