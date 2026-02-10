using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Components;

[RegisterComponent]
public sealed partial class PipeAppearanceComponent : Component
{
    [DataField]
    public SpriteSpecifier.Rsi[] Sprite = [new(new("_OuterHorizons/Structures/Misc/Atmospherics/pipe.rsi"), "pipeConnector"), // OH14-Changes start, pipes retexture
        new(new("_OuterHorizons/Structures/Misc/Atmospherics/pipe_alt1.rsi"), "pipeConnector"),
        new(new("_OuterHorizons/Structures/Misc/Atmospherics/pipe_alt2.rsi"), "pipeConnector")]; // OH14-Changes end
}
