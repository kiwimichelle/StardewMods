#if IS_FAUXCORE

namespace StardewMods.FauxCore.Common.Services.Integrations.IconicFramework;
#else

namespace StardewMods.Common.Services.Integrations.IconicFramework;
#endif

#pragma warning disable CA1711

/// <summary>Represents the event arguments for a toolbar icon being pressed.</summary>
public interface IIconPressedEventArgs
{
    /// <summary>Gets the button that was pressed.</summary>
    public SButton Button { get; }

    /// <summary>Gets the id of the icon that was pressed.</summary>
    public string Id { get; }
}
