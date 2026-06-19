namespace StardewMods.BetterChests.Framework.Services.Features;

using System.Globalization;
using StardewModdingAPI.Events;
using StardewMods.BetterChests.Framework.Enums;
using StardewMods.BetterChests.Framework.Services.Factory;
using StardewMods.BetterChests.Framework.UI.Menus;
using StardewMods.Common.Helpers;
using StardewMods.Common.Interfaces;
using StardewMods.Common.Models;
using StardewMods.Common.Services.Integrations.FauxCore;
using StardewMods.Common.Services.Integrations.IconicFramework;
using StardewMods.Common.UI.Menus;

/// <summary>Feature used for debugging purposes.</summary>
internal sealed class DebugMode : BaseFeature<DebugMode>
{
    private readonly ConfigManager configManager;
    private readonly ContainerFactory containerFactory;
    private readonly ContainerHandler containerHandler;
    private readonly IExpressionHandler expressionHandler;
    private readonly IIconRegistry iconRegistry;
    private readonly IconicFrameworkIntegration iconicFrameworkIntegration;
    private string? registeredIconId;

    /// <summary>Initializes a new instance of the <see cref="DebugMode" /> class.</summary>
    /// <param name="commandHelper">Dependency used for handling console commands.</param>
    /// <param name="configManager">Dependency used for accessing config data.</param>
    /// <param name="containerFactory">Dependency used for accessing containers.</param>
    /// <param name="containerHandler">Dependency used for handling operations by containers.</param>
    /// <param name="eventManager">Dependency used for managing events.</param>
    /// <param name="expressionHandler">Dependency used for parsing expressions.</param>
    /// <param name="iconRegistry">Dependency used for registering and retrieving icons.</param>
    /// <param name="iconicFrameworkIntegration">Dependency for Toolbar Icons integration.</param>
    public DebugMode(
        ICommandHelper commandHelper,
        ConfigManager configManager,
        ContainerFactory containerFactory,
        ContainerHandler containerHandler,
        IEventManager eventManager,
        IExpressionHandler expressionHandler,
        IIconRegistry iconRegistry,
        IconicFrameworkIntegration iconicFrameworkIntegration)
        : base(eventManager, configManager)
    {
        // Init
        this.configManager = configManager;
        this.containerFactory = containerFactory;
        this.containerHandler = containerHandler;
        this.expressionHandler = expressionHandler;
        this.iconRegistry = iconRegistry;
        this.iconicFrameworkIntegration = iconicFrameworkIntegration;

        // Commands
        commandHelper.Add("bc_config", I18n.Command_PlayerConfig(), this.Command);
        commandHelper.Add("bc_reset", I18n.Command_ResetAll(), this.Command);
        commandHelper.Add("bc_menu", I18n.Command_Menu(), this.Command);
    }

    /// <inheritdoc />
#if DEBUG
    public override bool ShouldBeActive => true;
#else
    public override bool ShouldBeActive => this.Config.DebugMode;
#endif

    /// <summary>Executes a command based on the provided command string and arguments.</summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The arguments for the command.</param>
    public void Command(string command, string[] args)
    {
        switch (command)
        {
            case "bc_config":
                this.Configure(args);
                return;

            case "bc_reset":
                this.ResetAll();
                return;

            case "bc_menu":
                this.ShowMenu(args);
                return;
        }
    }

    /// <inheritdoc />
    protected override void Activate()
    {
        if (!this.iconicFrameworkIntegration.IsLoaded || !this.iconRegistry.TryGetIcon(InternalIcon.Debug, out var icon))
        {
            return;
        }

        this.registeredIconId = icon.UniqueId;
        this.iconicFrameworkIntegration.Api.Subscribe(this.OnIconPressed);
        this.iconicFrameworkIntegration.Api.AddToolbarIcon(
            this.registeredIconId,
            icon.Path,
            icon.Area,
            () => I18n.Button_Debug_Name(),
            () => I18n.Button_Debug_Name());
    }

    /// <inheritdoc />
    protected override void Deactivate()
    {
        if (!this.iconicFrameworkIntegration.IsLoaded)
        {
            return;
        }

        this.iconicFrameworkIntegration.Api.Unsubscribe(this.OnIconPressed);
        this.registeredIconId = null;
    }

    private void OnIconPressed(IIconPressedEventArgs e)
    {
        if (this.registeredIconId is null
            || e.Id != this.registeredIconId
            || e.Button != SButton.MouseLeft
            || !Context.IsPlayerFree)
        {
            return;
        }

        if (Game1.activeClickableMenu?.readyToClose() == false)
        {
            return;
        }

        Game1.activeClickableMenu?.exitThisMenu();
        Game1.activeClickableMenu = new DebugMenu(this);
    }

    private void ResetAll()
    {
        var defaultOptions = new DefaultStorageOptions();
        foreach (var container in this.containerFactory.GetAll())
        {
            defaultOptions.CopyTo(container);
        }
    }

    private void Configure(IReadOnlyList<string> args)
    {
        if (args.Count != 1)
        {
            return;
        }

        switch (args[0])
        {
            case "backpack":
                if (!this.containerFactory.TryGetOne(Game1.player, out var container))
                {
                    return;
                }

                this.containerHandler.Configure(container);
                this.Events.Subscribe<MenuChangedEventArgs>(this.OnAfterContainerConfig); // 加这一行
                return;

            default:
                return;
        }
    }

    private void OnAfterContainerConfig(MenuChangedEventArgs e)
    {
        if (e.OldMenu?.GetType().Name != "SpecificModConfigMenu"
            || e.NewMenu?.GetType().Name == "SpecificModConfigMenu")
        {
            return;
        }

        this.Events.Unsubscribe<MenuChangedEventArgs>(this.OnAfterContainerConfig);
        this.configManager.SetupMainConfig();
    }

    private void ShowMenu(IReadOnlyList<string> args)
    {
        if (args.Count != 1 || !Context.IsPlayerFree)
        {
            return;
        }

        if (Game1.activeClickableMenu?.readyToClose() == false)
        {
            return;
        }

        Game1.activeClickableMenu?.exitThisMenu();
        switch (args[0].Trim().ToLower(CultureInfo.InvariantCulture))
        {
            case "config":
                Game1.activeClickableMenu = new ConfigMenu();
                return;

            case "icons":
                Game1.activeClickableMenu = new IconPicker(this.iconRegistry);
                return;

            case "layout":
                Game1.activeClickableMenu = new LayoutMenu();
                return;

            case "search":
                Game1.activeClickableMenu = new SearchMenu(
                    this.expressionHandler,
                    this.iconRegistry,
                    "({category}~\"fish\" !{tags}~\"ocean\" [{quality}~iridium {quality}~gold])");

                return;

            case "tab":
                Game1.activeClickableMenu = new TabMenu(this.configManager, this.expressionHandler, this.iconRegistry);

                return;
        }
    }
}
