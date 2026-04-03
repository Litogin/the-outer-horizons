using Robust.Shared.Containers;

namespace Content.Shared._OuterHorizons.ContainerSprite;

public sealed class InworldSpriteSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InworldSpriteComponent, EntGotInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<InworldSpriteComponent, EntGotRemovedFromContainerMessage>(OnContainerRemoved);
    }

    private void OnContainerInserted(Entity<InworldSpriteComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        _appearance.SetData(ent.Owner, InworldSpriteState.State, true);
    }
    private void OnContainerRemoved(Entity<InworldSpriteComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        _appearance.SetData(ent.Owner, InworldSpriteState.State, false);
    }
}
