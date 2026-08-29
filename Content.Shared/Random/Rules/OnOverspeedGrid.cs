//
// Created by Outer Horizons project
//

using Robust.Shared.Physics.Components;

namespace Content.Shared.Random.Rules;

public sealed partial class OnOverspeedGridRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent(uid, out TransformComponent? xform) ||
            xform.GridUid == null)
        {
            return Inverted;
        }

        if (!entManager.TryGetComponent(xform.GridUid.Value, out PhysicsComponent? physics))
        {
            return Inverted;
        }

        if (physics.LinearVelocity.LengthSquared() <= 25f)
        {
            return Inverted;
        }

        return !Inverted;
    }
}
