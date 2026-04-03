using Robust.Shared.Serialization;

namespace Content.Shared._OuterHorizons.ContainerSprite;

[RegisterComponent]
public sealed partial class InworldSpriteComponent : Component
{
}

[Serializable, NetSerializable]
public enum InworldSpriteState
{
    State
}
