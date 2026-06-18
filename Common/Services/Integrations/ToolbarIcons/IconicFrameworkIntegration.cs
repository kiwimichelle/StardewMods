#if IS_FAUXCORE

namespace StardewMods.FauxCore.Common.Services.Integrations.IconicFramework;
#else

namespace StardewMods.Common.Services.Integrations.IconicFramework;
#endif

/// <inheritdoc />
internal sealed class IconicFrameworkIntegration : ModIntegration<IIconicFrameworkApi>
{
    /// <summary>Initializes a new instance of the <see cref="IconicFrameworkIntegration" /> class.</summary>
    /// <param name="modRegistry">Dependency used for fetching metadata about loaded mods.</param>
    public IconicFrameworkIntegration(IModRegistry modRegistry)
        : base(modRegistry)
    {
        // Nothing
    }

    /// <inheritdoc />
    public override string UniqueId => "furyx639.ToolbarIcons";

    /// <inheritdoc />
    public override ISemanticVersion Version { get; } = new SemanticVersion(3, 2, 0);
}
