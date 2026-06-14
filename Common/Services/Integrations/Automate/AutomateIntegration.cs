#if IS_FAUXCORE

namespace StardewMods.FauxCore.Common.Services.Integrations.Automate;
#else

namespace StardewMods.Common.Services.Integrations.Automate;
#endif

/// <inheritdoc />
internal sealed class AutomateIntegration(IModRegistry modRegistry) : ModIntegration<IAutomateApi>(modRegistry)
{
    /// <inheritdoc />
    public override string UniqueId => "Pathoschild.Automate";
}
