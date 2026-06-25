namespace StardewMods.BetterChests.Framework.UI.Menus;

using Force.DeepCloner;
using Microsoft.Xna.Framework;
using StardewMods.BetterChests.Framework.Enums;
using StardewMods.Common.Services.Integrations.BetterChests;
using StardewMods.Common.Services.Integrations.FauxCore;
using StardewValley.Menus;

/// <summary>A menu for assigning categories to a container.</summary>
internal sealed class CategorizeMenu : SearchMenu
{
    private readonly IStorageContainer container;
    private readonly ClickableTextureComponent copyButton;
    private readonly IIcon noStackIcon;
    private readonly ClickableTextureComponent okButton;
    private readonly ClickableTextureComponent pasteButton;
    private readonly ClickableTextureComponent saveButton;
    private readonly ClickableTextureComponent stackToggle;

    private IExpression? savedExpression;

    /// <summary>Initializes a new instance of the <see cref="CategorizeMenu" /> class.</summary>
    /// <param name="container">The container to categorize.</param>
    /// <param name="expressionHandler">Dependency used for parsing expressions.</param>
    /// <param name="iconRegistry">Dependency used for registering and retrieving icons.</param>
    public CategorizeMenu(IStorageContainer container, IExpressionHandler expressionHandler, IIconRegistry iconRegistry)
        : base(expressionHandler, iconRegistry, container.CategorizeChestSearchTerm)
    {
        this.container = container;
        this.savedExpression =
            expressionHandler.TryParseExpression(container.CategorizeChestSearchTerm, out var expression)
                ? expression
                : null;

        this.saveButton = iconRegistry
            .Icon(InternalIcon.Save)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + Game1.tileSize + 16,
                hoverText: I18n.Ui_Save_Name());

        this.noStackIcon = iconRegistry.Icon(InternalIcon.NoStack);
        this.stackToggle = this.noStackIcon.Component(
            IconStyle.Button,
            this.xPositionOnScreen + this.width + 4,
            this.yPositionOnScreen + ((Game1.tileSize + 16) * 2),
            hoverText: I18n.Button_IncludeExistingStacks_Name());

        if (this.container.CategorizeChestIncludeStacks is FeatureOption.Enabled)
        {
            this.stackToggle.texture = Game1.mouseCursors;
            this.stackToggle.sourceRect = new Rectangle(103, 469, 16, 16);
        }

        this.copyButton = iconRegistry
            .Icon(InternalIcon.Copy)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + ((Game1.tileSize + 16) * 3),
                hoverText: I18n.Ui_Copy_Tooltip());

        this.pasteButton = iconRegistry
            .Icon(InternalIcon.Paste)
            .Component(
                IconStyle.Button,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + ((Game1.tileSize + 16) * 4),
                hoverText: I18n.Ui_Paste_Tooltip());

        this.okButton = iconRegistry
            .Icon(VanillaIcon.Ok)
            .Component(
                IconStyle.Transparent,
                this.xPositionOnScreen + this.width + 4,
                this.yPositionOnScreen + this.height - Game1.tileSize - (IClickableMenu.borderWidth / 2));

        this.allClickableComponents.Add(this.saveButton);
        this.allClickableComponents.Add(this.stackToggle);
        this.allClickableComponents.Add(this.copyButton);
        this.allClickableComponents.Add(this.pasteButton);
        this.allClickableComponents.Add(this.okButton);
    }

    /// <inheritdoc />
    public override bool TryLeftClick(Point cursor)
    {
        if (this.saveButton.bounds.Contains(cursor) && this.readyToClose())
        {
            Game1.playSound("drumkit6");
            this.savedExpression = this.Expression.DeepClone();
            this.container.CategorizeChestSearchTerm = this.SearchText;
            this.container.CategorizeChestIncludeStacks = this.stackToggle.texture.Equals(Game1.mouseCursors)
                ? FeatureOption.Enabled
                : FeatureOption.Disabled;

            return true;
        }

        if (this.stackToggle.bounds.Contains(cursor))
        {
            Game1.playSound("drumkit6");
            if (this.stackToggle.texture.Equals(Game1.mouseCursors))
            {
                this.stackToggle.texture = this.noStackIcon.Texture(IconStyle.Button);
                this.stackToggle.sourceRect = new Rectangle(0, 0, 16, 16);
                return true;
            }

            this.stackToggle.texture = Game1.mouseCursors;
            this.stackToggle.sourceRect = new Rectangle(103, 469, 16, 16);
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

        if (this.okButton.bounds.Contains(cursor))
        {
            Game1.playSound("bigDeSelect");
            this.exitThisMenuNoSound();
            this.container.ShowMenu();
            return true;
        }

        return base.TryLeftClick(cursor);
    }

    /// <inheritdoc />
    public override void populateClickableComponentList()
    {
        // 先让基类（SearchMenu）填充它自己的基础组件
        base.populateClickableComponentList();

        // 🌟 核心修复：为右侧悬浮的功能按钮链分配独立且不与原生冲突的高位手柄 ID
        this.saveButton.myID = 99601;
        this.stackToggle.myID = 99602;
        this.copyButton.myID = 99603;
        this.pasteButton.myID = 99604;
        this.okButton.myID = 99605;

        // 串联垂直方向的双向手柄导航链
        this.saveButton.downNeighborID = this.stackToggle.myID;

        this.stackToggle.upNeighborID = this.saveButton.myID;
        this.stackToggle.downNeighborID = this.copyButton.myID;

        this.copyButton.upNeighborID = this.stackToggle.myID;
        this.copyButton.downNeighborID = this.pasteButton.myID;

        this.pasteButton.upNeighborID = this.copyButton.myID;
        this.pasteButton.downNeighborID = this.okButton.myID;

        this.okButton.upNeighborID = this.pasteButton.myID;

        // 开放横向破局口：允许手柄通过自动搜索（或强制指定）跳入和跳出右侧侧边栏
        // 设置 SNAP_TO_DEFAULT 允许 1.6 的智能导航引擎根据物理相对位置自动帮玩家横向对齐左侧主菜单
        this.saveButton.leftNeighborID = ClickableComponent.SNAP_TO_DEFAULT;
        this.stackToggle.leftNeighborID = ClickableComponent.SNAP_TO_DEFAULT;
        this.copyButton.leftNeighborID = ClickableComponent.SNAP_TO_DEFAULT;
        this.pasteButton.leftNeighborID = ClickableComponent.SNAP_TO_DEFAULT;
        this.okButton.leftNeighborID = ClickableComponent.SNAP_TO_DEFAULT;
    }

    /// <inheritdoc />
    protected override List<Item> GetItems()
    {
        var items = base.GetItems();
        return items;
    }

    /// <inheritdoc />
    protected override bool HighlightMethod(Item item) =>
        this.savedExpression is null || this.savedExpression.Equals(item);
}
