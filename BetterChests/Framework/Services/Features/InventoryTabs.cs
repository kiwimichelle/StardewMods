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
    // Fixed base ID for inventory tab gamepad navigation. Must stay constant across menu
    // rebuilds (remote chest access, scene changes, etc.) — a coordinate-derived ID was
    // tried previously and caused gamepad focus to desync after the menu was rebuilt,
    // since bounds.Y/X change between rebuilds but the gamepad focus system can still
    // hold a reference to the old myID.
    private const int BaseTabId = 974_001;

    private readonly IExpressionHandler expressionHandler;
    private readonly IIconRegistry iconRegistry;
    private readonly MenuHandler menuHandler;
    private readonly ITranslationHelper translationHelper;
    private readonly PerScreen<List<InventoryTab>> tabs = new(() => []);

    // Border components that had their leftNeighborID set by this feature on the previous
    // menu build. Tracked so they can be reset before the next build — otherwise, if the
    // grid is rebuilt with fewer/different border components, stale links could remain on
    // components we no longer manage.
    private readonly PerScreen<List<ClickableComponent>> linkedBorderComponents = new(() => []);

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

        // Reset neighbor links left by the previous menu build before doing anything else.
        // This must run even if the early-return conditions below are hit, so a menu that
        // no longer qualifies for tabs doesn't keep stale leftNeighborID references.
        foreach (var border in this.linkedBorderComponents.Value)
        {
            border.leftNeighborID = -1;
        }

        this.linkedBorderComponents.Value.Clear();

        if (this.menuHandler.CurrentMenu is not ItemGrabMenu menu
            || top.InventoryMenu is null
            || container is not { InventoryTabs: FeatureOption.Enabled, SearchItems: FeatureOption.Enabled })
        {
            return;
        }

        // 🌟 完美推算坐标 90，彻底拉开高级留白
        var x = top.InventoryMenu.inventory[0].bounds.X - 100;
        var y = top.InventoryMenu.inventory[0].bounds.Y;

        var createdTabs = new List<InventoryTab>();

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
            createdTabs.Add(tabIcon);

            y += Game1.tileSize;
        }

        this.tabs.Value.AddRange(createdTabs);
        this.SetupGamepadNavigation(top.InventoryMenu, createdTabs);
    }

    /// <summary>
    /// Binds gamepad neighbor IDs between the inventory tabs and the left border of the inventory grid.
    /// Uses a fixed ID base (stable across menu rebuilds) and pairs each tab with exactly one border
    /// component, setting both sides of the link together so the relationship is always symmetric.
    /// </summary>
    /// <param name="inventoryMenu">The inventory menu whose left border the tabs are attached to.</param>
    /// <param name="createdTabs">The tabs that were just created, top to bottom.</param>
    private void SetupGamepadNavigation(InventoryMenu inventoryMenu, IReadOnlyList<InventoryTab> createdTabs)
    {
        if (createdTabs.Count == 0)
        {
            return;
        }

        // Assign a fixed, stable myID and vertical (up/down) links between the tabs themselves.
        for (var i = 0; i < createdTabs.Count; i++)
        {
            createdTabs[i].myID = InventoryTabs.BaseTabId + i;
        }

        for (var i = 0; i < createdTabs.Count; i++)
        {
            createdTabs[i].upNeighborID = i > 0 ? createdTabs[i - 1].myID : -1;
            createdTabs[i].downNeighborID = i < createdTabs.Count - 1 ? createdTabs[i + 1].myID : -1;
        }

        var borderComponents = inventoryMenu.GetBorder(InventoryMenu.BorderSide.Left).ToList();
        if (borderComponents.Count == 0)
        {
            return;
        }

        Log.Trace(
            "[GamepadNav] tabs={0} borders={1} — left border state BEFORE binding:",
            createdTabs.Count,
            borderComponents.Count);

        foreach (var b in borderComponents.OrderBy(c => c.bounds.Center.Y))
        {
            Log.Trace(
                "[GamepadNav]   myID={0} Y={1} left={2} right={3} up={4} down={5}",
                b.myID,
                b.bounds.Y,
                b.leftNeighborID,
                b.rightNeighborID,
                b.upNeighborID,
                b.downNeighborID);
        }

        // Sort both lists top-to-bottom so they can be paired by position.
        var sortedTabs = createdTabs.OrderBy(t => t.bounds.Center.Y).ToList();
        var sortedBorders = borderComponents.OrderBy(c => c.bounds.Center.Y).ToList();

        if (sortedTabs.Count <= sortedBorders.Count)
        {
            // One-to-one (or more borders than tabs): pair each tab with exactly one
            // border component, in order. This guarantees a strictly symmetric link —
            // no two tabs ever write to the same border component's leftNeighborID.
            for (var i = 0; i < sortedTabs.Count; i++)
            {
                var tab = sortedTabs[i];
                var border = sortedBorders[i];
                tab.rightNeighborID = border.myID;
                border.leftNeighborID = tab.myID;
                this.linkedBorderComponents.Value.Add(border);
            }
        }
        else
        {
            // More tabs than border components: split the tabs into contiguous groups,
            // one group per border component, distributing the remainder across the
            // earlier groups so every border component gets at least one tab.
            // (A naive Ceiling(tabs/borders) group size can leave the last border with
            // zero tabs when tabs is an exact multiple of borders minus a small remainder
            // — e.g. 8 tabs / 5 borders gave groups of 2,2,2,2,0, leaving the last border
            // unbound. This distributes 8 into 2,2,2,1,1 instead.)
            var baseSize = sortedTabs.Count / sortedBorders.Count;
            var remainder = sortedTabs.Count % sortedBorders.Count;

            var groupStart = 0;
            for (var borderIndex = 0; borderIndex < sortedBorders.Count; borderIndex++)
            {
                var border = sortedBorders[borderIndex];
                var thisGroupSize = baseSize + (borderIndex < remainder ? 1 : 0);
                var groupEnd = groupStart + thisGroupSize;

                if (groupStart >= groupEnd || groupStart >= sortedTabs.Count)
                {
                    groupStart = groupEnd;
                    continue;
                }

                InventoryTab? representativeTab = null;
                var bestDistance = int.MaxValue;
                for (var tabIndex = groupStart; tabIndex < groupEnd && tabIndex < sortedTabs.Count; tabIndex++)
                {
                    var tab = sortedTabs[tabIndex];
                    tab.rightNeighborID = border.myID;

                    var distance = Math.Abs(tab.bounds.Center.Y - border.bounds.Center.Y);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        representativeTab = tab;
                    }
                }

                if (representativeTab is not null)
                {
                    border.leftNeighborID = representativeTab.myID;
                    this.linkedBorderComponents.Value.Add(border);
                }

                groupStart = groupEnd;
            }
        }

        Log.Trace("[GamepadNav] left border state AFTER binding:");
        foreach (var b in sortedBorders)
        {
            Log.Trace(
                "[GamepadNav]   myID={0} Y={1} left={2} right={3} up={4} down={5} <- tab(s): {6}",
                b.myID,
                b.bounds.Y,
                b.leftNeighborID,
                b.rightNeighborID,
                b.upNeighborID,
                b.downNeighborID,
                string.Join(
                    ", ",
                    sortedTabs.Where(t => t.rightNeighborID == b.myID).Select(t => t.Data.Label)));
        }
    }
}
