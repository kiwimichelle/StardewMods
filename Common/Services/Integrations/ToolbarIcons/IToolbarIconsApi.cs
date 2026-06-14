#if IS_FAUXCORE

namespace StardewMods.FauxCore.Common.Services.Integrations.ToolbarIcons;

using Microsoft.Xna.Framework;
using StardewMods.FauxCore.Common.Services.Integrations.FauxCore;
using StardewValley.Menus;

#else

namespace StardewMods.Common.Services.Integrations.ToolbarIcons;

using Microsoft.Xna.Framework;
using StardewMods.Common.Services.Integrations.FauxCore;
using StardewValley.Menus;

#endif

/// <summary>Public api to add icons above or below the toolbar.</summary>
public interface IToolbarIconsApi
{
    /// <summary>Event triggered when any toolbar icon is pressed.</summary>
    [Obsolete("Use Subscribe(Action<IIconPressedEventArgs>) and Unsubscribe(Action<IIconPressedEventArgs>) instead.")]
    public event EventHandler<string> ToolbarIconPressed;

    /// <summary>Adds an icon next to the <see cref="Toolbar" />.</summary>
    /// <param name="id">A unique identifier for the icon.</param>
    /// <param name="texturePath">The path to the texture icon.</param>
    /// <param name="sourceRect">The source rectangle of the icon.</param>
    /// <param name="hoverText">Text to appear when hovering over the icon.</param>
    [Obsolete("Use AddToolbarIcon(string, string, Rectangle?, Func<string>?, Func<string>?) instead.")]
    public void AddToolbarIcon(string id, string texturePath, Rectangle? sourceRect, string? hoverText);

    /// <summary>Adds an icon next to the <see cref="Toolbar" />.</summary>
    /// <param name="id">A unique identifier for the icon.</param>
    /// <param name="texturePath">The path to the texture icon.</param>
    /// <param name="sourceRect">The source rectangle of the icon.</param>
    /// <param name="getTitle">Text to appear as the title in the Radial Menu.</param>
    /// <param name="getDescription">Text to appear when hovering over the icon.</param>
    public void AddToolbarIcon(
        string id,
        string texturePath,
        Rectangle? sourceRect,
        Func<string>? getTitle,
        Func<string>? getDescription);

    /// <summary>Removes an icon.</summary>
    /// <param name="id">A unique identifier for the icon.</param>
    [Obsolete("This no longer has any effect.")]
    public void RemoveToolbarIcon(string id);

    /// <summary>Subscribes to an event handler.</summary>
    /// <param name="handler">The event handler to subscribe.</param>
    void Subscribe(Action<IIconPressedEventArgs> handler);

    /// <summary>Unsubscribes an event handler from an event.</summary>
    /// <param name="handler">The event handler to unsubscribe.</param>
    void Unsubscribe(Action<IIconPressedEventArgs> handler);
}
