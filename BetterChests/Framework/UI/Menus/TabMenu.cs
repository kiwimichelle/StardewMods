namespace StardewMods.BetterChests.Framework.UI.Menus;

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewMods.BetterChests.Framework.Enums;
using StardewMods.BetterChests.Framework.Interfaces;
using StardewMods.BetterChests.Framework.Models;
using StardewMods.BetterChests.Framework.Services;
using StardewMods.BetterChests.Framework.UI.Components;
using StardewMods.Common.Services.Integrations.FauxCore;
using StardewMods.Common.UI.Menus;
using StardewValley.Menus;

/// <summary>Menu for customizing tabs.</summary>
internal sealed class TabMenu : SearchMenu
{
    private readonly ClickableTextureComponent addButton;
    private readonly IModConfig config;
    private readonly ClickableTextureComponent copyButton;
    private readonly ClickableTextureComponent editButton;
    private readonly IIconRegistry iconRegistry;
    private readonly ClickableTextureComponent okButton;
    private readonly ClickableTextureComponent pasteButton;
    private readonly ClickableTextureComponent removeButton;
    private readonly ClickableTextureComponent saveButton;
    private readonly List<TabEditor> tabs = new();

    private TabEditor? activeTab;

    /// <summary>Initializes a new instance of the <see cref="TabMenu" /> class.</summary>
    /// <param name="configManager">Dependency used for managing config data.</param>
    /// <param name="expressionHandler">Dependency used for parsing expressions.</param>
    /// <param name="iconRegistry">Dependency used for registering and retrieving icons.</param>
    public TabMenu(ConfigManager configManager, IExpressionHandler expressionHandler, IIconRegistry iconRegistry)
        : base(expressionHandler, iconRegistry, string.Empty)
    {
        this.iconRegistry = iconRegistry;
        this.config = configManager.GetNew();

        this.saveButton = iconRegistry
            .Icon(InternalIcon.Save)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + Game1.tileSize + 16,
                hoverText: I18n.Ui_Save_Name());

        this.copyButton = iconRegistry
            .Icon(InternalIcon.Copy)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + ((Game1.tileSize + 16) * 2),
                hoverText: I18n.Ui_Copy_Tooltip());

        this.pasteButton = iconRegistry
            .Icon(InternalIcon.Paste)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + ((Game1.tileSize + 16) * 3),
                hoverText: I18n.Ui_Paste_Tooltip());

        this.editButton = iconRegistry
            .Icon(VanillaIcon.ColorPicker)
            .Component(
                IconStyle.Transparent,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + ((Game1.tileSize + 16) * 4),
                hoverText: I18n.Ui_Edit_Tooltip());

        this.addButton = iconRegistry
            .Icon(VanillaIcon.Plus)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + ((Game1.tileSize + 16) * 5),
                hoverText: I18n.Ui_Add_Tooltip());

        this.removeButton = iconRegistry
            .Icon(VanillaIcon.Trash)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + ((Game1.tileSize + 16) * 6),
                hoverText: I18n.Ui_Remove_Tooltip());

        this.okButton = iconRegistry
            .Icon(VanillaIcon.Ok)
            .Component(
                IconStyle.Transparent,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + this.height - Game1.tileSize - (IClickableMenu.borderWidth / 2));

        for (var i = this.config.InventoryTabList.Count - 1; i >= 0; i--)
        {
            if (!this.iconRegistry.TryGetIcon(this.config.InventoryTabList[i].Icon, out _))
            {
                this.config.InventoryTabList.RemoveAt(i);
            }
        }

        for (var i = 0; i < this.config.InventoryTabList.Count; i++)
        {
            var tabData = this.config.InventoryTabList[i];
            var icon = this.iconRegistry.Icon(tabData.Icon);
            var tabIcon = new TabEditor(
                this.iconRegistry,
                this,
                this.xPositionOnScreen - (Game1.tileSize * 2) - 256,
                this.yPositionOnScreen + (Game1.tileSize * (i + 1)) + 16,
                (Game1.tileSize * 2) + 256,
                icon,
                tabData)
            {
                Active = i == 0,
                Index = i,
            };

            tabIcon.Clicked += this.OnClicked;
            tabIcon.MoveDown += this.OnMoveDown;
            tabIcon.MoveUp += this.OnMoveUp;

            this.tabs.Add(tabIcon);

            if (i == 0)
            {
                this.activeTab = tabIcon;
                this.SetSearchText(tabData.SearchTerm, true);
            }
        }

        this.populateClickableComponentList();
    }

    /// <summary>重写 1.6 菜单的生命周期方法，动态且安全地缝合手柄导航网格.</summary>
    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();

        // 确保右侧功能键全部加入手柄检测队列
        this.allClickableComponents.Add(this.saveButton);
        this.allClickableComponents.Add(this.copyButton);
        this.allClickableComponents.Add(this.pasteButton);
        this.allClickableComponents.Add(this.editButton);
        this.allClickableComponents.Add(this.addButton);
        this.allClickableComponents.Add(this.removeButton);
        this.allClickableComponents.Add(this.okButton);

        foreach (var tab in this.tabs)
        {
            this.allClickableComponents.Add(tab);
            this.allClickableComponents.Add(tab.UpArrow);
            this.allClickableComponents.Add(tab.DownArrow);
        }
    }

    /// <summary>当游戏窗口大小或 UI 缩放改变时，重新计算并对齐所有组件坐标.</summary>
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);

        // 1. 重新同步右侧按钮的绝对坐标
        this.saveButton.bounds.X = this.xPositionOnScreen + this.width + 4;
        this.saveButton.bounds.Y = this.yPositionOnScreen + Game1.tileSize + 16;
        this.copyButton.bounds.X = this.xPositionOnScreen + this.width + 4;
        this.copyButton.bounds.Y = this.yPositionOnScreen + ((Game1.tileSize + 16) * 2);
        this.pasteButton.bounds.X = this.xPositionOnScreen + this.width + 4;
        this.pasteButton.bounds.Y = this.yPositionOnScreen + ((Game1.tileSize + 16) * 3);
        this.editButton.bounds.X = this.xPositionOnScreen + this.width + 4;
        this.editButton.bounds.Y = this.yPositionOnScreen + ((Game1.tileSize + 16) * 4);
        this.addButton.bounds.X = this.xPositionOnScreen + this.width + 4;
        this.addButton.bounds.Y = this.yPositionOnScreen + ((Game1.tileSize + 16) * 5);
        this.removeButton.bounds.X = this.xPositionOnScreen + this.width + 4;
        this.removeButton.bounds.Y = this.yPositionOnScreen + ((Game1.tileSize + 16) * 6);
        this.okButton.bounds.X = this.xPositionOnScreen + this.width + 4;
        this.okButton.bounds.Y = this.yPositionOnScreen + this.height - Game1.tileSize - (IClickableMenu.borderWidth / 2);

        // 2. 重新迭代定位左侧的所有 Tabs
        for (var i = 0; i < this.tabs.Count; i++)
        {
            this.tabs[i].MoveTo(new Point(
                this.xPositionOnScreen - (Game1.tileSize * 2) - 256,
                this.yPositionOnScreen + (Game1.tileSize * (i + 1)) + 16));
        }

        // 3. 重新构建导航网格
        this.populateClickableComponentList();
    }

    /// <inheritdoc />
    public override bool TryLeftClick(Point cursor)
    {
        Game1.addHUDMessage(new HUDMessage($"cursor={cursor} save={this.saveButton.bounds.Contains(cursor)}", HUDMessage.error_type));

        if (this.saveButton.bounds.Contains(cursor) && this.readyToClose())
        {
            Game1.playSound("drumkit6");
            if (this.activeTab is not null)
            {
                this.config.InventoryTabList[this.activeTab.Index].SearchTerm = this.SearchText;
            }

            return true;
        }

        if (this.copyButton.bounds.Contains(cursor))
        {
            Game1.playSound("drumkit6");
            DesktopClipboard.SetText(this.SearchText);
            return true;
        }

        if (this.pasteButton.bounds.Contains(cursor))
        {
            Game1.playSound("drumkit6");
            var searchText = string.Empty;
            DesktopClipboard.GetText(ref searchText);
            this.SetSearchText(searchText, true);
            return true;
        }

        if (this.editButton.bounds.Contains(cursor))
        {
            Game1.playSound("drumkit6");
            if (this.activeTab is not null)
            {
                var iconPicker = new IconPicker(this.iconRegistry);
                iconPicker.IconSelected += this.OnIconSelected;
                this.SetChildMenu(iconPicker);
            }

            return true;
        }

        if (this.addButton.bounds.Contains(cursor))
        {
            Game1.playSound("drumkit6");
            var tabData = new TabData
            {
                Icon = VanillaIcon.Plus.ToStringFast(),
                Label = I18n.Ui_NewTab_Name(),
            };

            var icon = this.iconRegistry.Icon(VanillaIcon.Plus);
            var tabIcon = new TabEditor(
                this.iconRegistry,
                this,
                this.xPositionOnScreen - (Game1.tileSize * 2) - 256,
                this.yPositionOnScreen + (Game1.tileSize * (this.config.InventoryTabList.Count + 1)) + 16,
                (Game1.tileSize * 2) + 256,
                icon,
                tabData)
            {
                Active = false,
                Index = this.config.InventoryTabList.Count,
            };

            tabIcon.Clicked += this.OnClicked;
            tabIcon.MoveDown += this.OnMoveDown;
            tabIcon.MoveUp += this.OnMoveUp;

            this.tabs.Add(tabIcon);
            this.config.InventoryTabList.Add(tabData);

            this.populateClickableComponentList();
            return true;
        }

        if (this.removeButton.bounds.Contains(cursor))
        {
            Game1.playSound("drumkit6");
            if (this.activeTab is not null)
            {
                this.config.InventoryTabList.RemoveAt(this.activeTab.Index);

                var idx = this.tabs.IndexOf(this.activeTab);
                for (var index = idx; index < this.tabs.Count - 1; index++)
                {
                    var current = this.tabs[index];
                    var next = this.tabs[index + 1];
                    (current.Index, next.Index) = (next.Index, current.Index);

                    var currentY = current.bounds.Y;
                    var nextY = next.bounds.Y;

                    current.MoveTo(new Point(current.bounds.X, nextY));
                    next.MoveTo(new Point(next.bounds.X, currentY));

                    (this.tabs[index], this.tabs[index + 1]) = (this.tabs[index + 1], this.tabs[index]);
                }

                this.tabs.RemoveAt(this.tabs.Count - 1);
                this.activeTab = null;

                this.populateClickableComponentList();
            }

            return true;
        }

        if (this.okButton.bounds.Contains(cursor))
        {
            Game1.playSound("bigDeSelect");
            this.exitThisMenuNoSound();
            return true;
        }
        foreach (var tab in this.tabs)
        {
            if (tab.TryLeftClick(cursor))
            {
                return true;
            }
        }

        return base.TryLeftClick(cursor);
    }

    /// <inheritdoc />
    protected override bool HighlightMethod(Item item) => true;

    private void OnClicked(object? sender, IClicked e)
    {
        if (sender is not TabEditor tabEditor)
        {
            return;
        }

        Game1.playSound("drumkit6");
        this.SetSearchText(tabEditor.Data.SearchTerm, true);
        if (this.activeTab is not null)
        {
            this.activeTab.Active = false;
        }

        this.activeTab = tabEditor;
        if (this.activeTab is not null)
        {
            this.activeTab.Active = true;
        }
    }

    private void OnIconSelected(object? sender, IIcon? icon)
    {
        if (this.activeTab is null || string.IsNullOrWhiteSpace(icon?.UniqueId))
        {
            return;
        }

        this.config.InventoryTabList[this.activeTab.Index].Icon = icon.UniqueId;
        this.activeTab.UpdateIcon(icon);
    }

    private void OnMoveDown(object? sender, IClicked e)
    {
        if (sender is not TabEditor tabEditor || tabEditor.Index >= this.config.InventoryTabList.Count - 1)
        {
            return;
        }

        Game1.playSound("drumkit6");

        (this.config.InventoryTabList[tabEditor.Index], this.config.InventoryTabList[tabEditor.Index + 1]) = (
            this.config.InventoryTabList[tabEditor.Index + 1], this.config.InventoryTabList[tabEditor.Index]);

        var index = this.tabs.IndexOf(tabEditor);
        var current = this.tabs[index];
        var next = this.tabs[index + 1];
        (current.Index, next.Index) = (next.Index, current.Index);

        var currentY = current.bounds.Y;
        var nextY = next.bounds.Y;

        current.MoveTo(new Point(current.bounds.X, nextY));
        next.MoveTo(new Point(next.bounds.X, currentY));

        (this.tabs[index], this.tabs[index + 1]) = (this.tabs[index + 1], this.tabs[index]);

        this.populateClickableComponentList();
    }

    private void OnMoveUp(object? sender, IClicked e)
    {
        if (sender is not TabEditor { Index: > 0 } tabEditor)
        {
            return;
        }

        Game1.playSound("drumkit6");
        (this.config.InventoryTabList[tabEditor.Index], this.config.InventoryTabList[tabEditor.Index - 1]) = (
            this.config.InventoryTabList[tabEditor.Index - 1], this.config.InventoryTabList[tabEditor.Index]);

        var index = this.tabs.IndexOf(tabEditor);
        var current = this.tabs[index];
        var previous = this.tabs[index - 1];
        (current.Index, previous.Index) = (previous.Index, current.Index);

        var currentY = current.bounds.Y;
        var previousY = previous.bounds.Y;

        current.MoveTo(new Point(current.bounds.X, previousY));
        previous.MoveTo(new Point(previous.bounds.X, currentY));

        (this.tabs[index], this.tabs[index - 1]) = (this.tabs[index - 1], this.tabs[index]);

        this.populateClickableComponentList();
    }
}
