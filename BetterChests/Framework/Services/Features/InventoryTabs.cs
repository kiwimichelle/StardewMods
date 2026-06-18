namespace StardewMods.BetterChests.Framework.Services.Features;

using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewMods.BetterChests.Framework.Interfaces;
using StardewMods.BetterChests.Framework.Models.Events;
using StardewMods.BetterChests.Framework.UI.Components;
using StardewMods.Common.Interfaces;
using StardewMods.Common.Services;
using StardewMods.Common.Services.Integrations.BetterChests;
using StardewMods.Common.Services.Integrations.FauxCore;
using StardewValley.Menus;

/// <summary>Adds inventory tabs to the side of the <see cref="ItemGrabMenu" />.</summary>
internal sealed class InventoryTabs : BaseFeature<InventoryTabs>
{
    private readonly IExpressionHandler expressionHandler;
    private readonly IIconRegistry iconRegistry;
    private readonly MenuHandler menuHandler;
    private readonly ITranslationHelper translationHelper;
    private readonly PerScreen<List<InventoryTab>> tabs = new(() => []);

    /// <summary>Initializes a new instance of the <see cref="InventoryTabs" /> class.</summary>
    /// <param name="eventManager">Dependency used for managing events.</param>
    /// <param name="expressionHandler">Dependency used for parsing expressions.</param>
    /// <param name="iconRegistry">Dependency used for registering and retrieving icons.</param>
    /// <param name="menuHandler">Dependency used for managing the current menu.</param>
    /// <param name="modConfig">Dependency used for accessing config data.</param>
    /// <param name="translationHelper">Dependency used for managing translations.</param>
    public InventoryTabs(
        IEventManager eventManager,
        IExpressionHandler expressionHandler,
        IIconRegistry iconRegistry,
        MenuHandler menuHandler,
        IModConfig modConfig,
        ITranslationHelper translationHelper)
        : base(eventManager, modConfig)
    {
        this.expressionHandler = expressionHandler;
        this.iconRegistry = iconRegistry;
        this.menuHandler = menuHandler;
        this.translationHelper = translationHelper;
    }

    /// <inheritdoc />
    public override bool ShouldBeActive => this.Config.DefaultOptions.InventoryTabs != FeatureOption.Disabled;

    /// <inheritdoc />
    protected override void Activate() =>
        this.Events.Subscribe<InventoryMenuChangedEventArgs>(this.OnInventoryMenuChanged);

    /// <inheritdoc />
    protected override void Deactivate() =>
        this.Events.Unsubscribe<InventoryMenuChangedEventArgs>(this.OnInventoryMenuChanged);

    private void OnClicked(object? sender, IClicked e)
    {
        if (sender is not InventoryTab tab)
        {
            return;
        }

        Log.Trace("{0}: Switching tab to {1}.", this.Id, tab.Data.Label);
        Game1.playSound("drumkit6");
        _ = this.expressionHandler.TryParseExpression(tab.Data.SearchTerm, out var expression);
        this.Events.Publish(new SearchChangedEventArgs(tab.Data.SearchTerm, expression));
    }

    private void OnInventoryMenuChanged(InventoryMenuChangedEventArgs e)
    {
        var container = this.menuHandler.Top.Container;
        var top = this.menuHandler.Top;
        this.tabs.Value.Clear();

        if (this.menuHandler.CurrentMenu is not ItemGrabMenu menu
            || top.InventoryMenu is null
            || container is not { InventoryTabs: FeatureOption.Enabled, SearchItems: FeatureOption.Enabled })
        {
            return;
        }

        // 🌟 完美推算坐标 90，彻底拉开高级留白
        var x = top.InventoryMenu.inventory[0].bounds.X - 100;
        var y = top.InventoryMenu.inventory[0].bounds.Y;

        foreach (var tabData in this.Config.InventoryTabList)
        {
            if (!this.iconRegistry.TryGetIcon(tabData.Icon, out var icon))
            {
                continue;
            }

            // 🌟 核心修复：根据内置的 Icon 路径，运行时动态匹配并调用 I18n 获取最新翻译
            string label = tabData.Icon switch
            {
                "furyx639.BetterChests/Clothing" => I18n.Tabs_Clothing_Name(),
                "furyx639.BetterChests/Cooking" => I18n.Tabs_Cooking_Name(),
                "furyx639.BetterChests/Crops" => I18n.Tabs_Crops_Name(),
                "furyx639.BetterChests/Equipment" => I18n.Tabs_Equipment_Name(),
                "furyx639.BetterChests/Fishing" => I18n.Tabs_Fishing_Name(),
                "furyx639.BetterChests/Materials" => I18n.Tabs_Materials_Name(),
                "furyx639.BetterChests/Miscellaneous" => I18n.Tabs_Misc_Name(),
                "furyx639.BetterChests/Seeds" => I18n.Tabs_Seeds_Name(),
                _ => tabData.Label, // 如果是玩家自行在 config 中加的自定义标签，则回退使用配置文件的原始文本
            };

            // 将动态获取到的本地化字符串 label 传入新构造函数
            var tabIcon = new InventoryTab(null, x, y, icon, tabData, label);
            tabIcon.Clicked += this.OnClicked;
            e.AddComponent(tabIcon);

            y += Game1.tileSize;
        }
    }
}
