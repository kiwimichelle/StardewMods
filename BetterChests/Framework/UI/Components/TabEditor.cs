namespace StardewMods.BetterChests.Framework.UI.Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.BetterChests.Framework.Models;
using StardewMods.Common.Helpers;
using StardewMods.Common.Models.Events;
using StardewMods.Common.Services.Integrations.FauxCore;
using StardewMods.Common.UI.Components;
using StardewValley.Menus;

/// <summary>A component for configuring a tab.</summary>
internal sealed class TabEditor : BaseComponent
{
    private readonly ClickableTextureComponent downArrow;
    private readonly ClickableTextureComponent upArrow;
    private ClickableTextureComponent icon;
    private EventHandler<IClicked>? moveDown;
    private EventHandler<IClicked>? moveUp;

    /// <summary>Initializes a new instance of the <see cref="TabEditor" /> class.</summary>
    /// <param name="iconRegistry">Dependency used for registering and retrieving icons.</param>
    /// <param name="parent">The parent menu.</param>
    /// <param name="x">The x-coordinate of the tab component.</param>
    /// <param name="y">The y-coordinate of the tab component.</param>
    /// <param name="width">The width of the tab component.</param>
    /// <param name="icon">The tab icon.</param>
    /// <param name="tabData">The inventory tab data.</param>
    public TabEditor(
        IIconRegistry iconRegistry,
        ICustomMenu parent,
        int x,
        int y,
        int width,
        IIcon icon,
        TabData tabData)
        : base(parent, x, y, width, Game1.tileSize, tabData.Label)
    {
        this.Data = tabData;
        this.icon = icon.Component(IconStyle.Transparent, this.bounds.X + Game1.tileSize, y);
        this.icon.bounds.Width = this.bounds.Width - (Game1.tileSize * 2);

        // ====== 手柄导航网格 ID 分配 ======
        // 规则：TabEditor 使用 TabIdBase 开始的独立 ID 段，步长 3（每个 tab 占 3 个 ID）。
        // ID 按 tabData 的引用哈希 + 偏移分配，与外部所有已知 ID 段完全隔离：
        //   - 原版 ItemGrabMenu 组件：约 0–1000
        //   - MenuManager 箭头：88561, 88562
        //   - MenuManager 图标：88571, 88572
        //   - TabEditor 使用：从 500_000 开始，每个实例步长 3，永不碰撞
        //
        // 旧方案 (y * 1000) + x 用屏幕坐标计算 ID，x 为负数且随分辨率变化，
        // 在某些分辨率下会算出 88571/88572，触发 ConfigureChest 的手柄误判。
        var tabIdBase = 500_000 + (Math.Abs(tabData.GetHashCode()) % 100_000) * 3;
        this.myID = tabIdBase + 2;

        // 为两个箭头注入专属 ID
        this.upArrow = iconRegistry
            .Icon(VanillaIcon.ArrowUp)
            .Component(IconStyle.Transparent, x + 8, y + 8, hoverText: I18n.Ui_MoveUp_Tooltip());

        this.downArrow = iconRegistry
            .Icon(VanillaIcon.ArrowDown)
            .Component(IconStyle.Transparent, x + width - Game1.tileSize + 8, y + 8, hoverText: I18n.Ui_MoveDown_Tooltip());

        this.upArrow.myID = tabIdBase;
        this.downArrow.myID = tabIdBase + 1;

        // 双向网格缝合
        this.upArrow.rightNeighborID = this.myID;     // 向上箭头右边是主体
        this.leftNeighborID = this.upArrow.myID;       // 主体左边是向上箭头
        this.rightNeighborID = this.downArrow.myID;   // 主体右边是向下箭头
        this.downArrow.leftNeighborID = this.myID;     // 向下箭头左边是主体
    }

    /// <summary>Event triggered when the move up button is clicked.</summary>
    public event EventHandler<IClicked> MoveDown
    {
        add => this.moveDown += value;
        remove => this.moveDown -= value;
    }

    /// <summary>Event triggered when the move down button is clicked.</summary>
    public event EventHandler<IClicked> MoveUp
    {
        add => this.moveUp += value;
        remove => this.moveUp -= value;
    }

    /// <summary>Gets or sets a value indicating whether the tab is currently active.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Gets the tab data.</summary>
    public TabData Data { get; }

    /// <summary>Gets or sets the index.</summary>
    public int Index { get; set; }

    /// <summary>Gets the down arrow component.</summary>
    internal ClickableTextureComponent DownArrow => this.downArrow;

    /// <summary>Gets the up arrow component.</summary>
    internal ClickableTextureComponent UpArrow => this.upArrow;

    /// <inheritdoc />
    public override void Draw(SpriteBatch spriteBatch, Point cursor, Point offset)
    {
        cursor -= offset;
        var hover = this.bounds.Contains(cursor);
        var color = this.Active
            ? Color.White
            : hover
                ? Color.LightGray
                : Color.Gray;

        // Top-Center
        spriteBatch.Draw(
            Game1.mouseCursors,
            new Rectangle(
                this.icon.bounds.X + offset.X + 20,
                this.icon.bounds.Y + offset.Y,
                this.icon.bounds.Width - 40,
                this.icon.bounds.Height),
            new Rectangle(21, 368, 6, 16),
            color,
            0,
            Vector2.Zero,
            SpriteEffects.None,
            0.5f);

        // Bottom-Center
        spriteBatch.Draw(
            Game1.mouseCursors,
            new Rectangle(
                this.icon.bounds.X + offset.X + 20,
                this.icon.bounds.Y + this.icon.bounds.Height + offset.Y - 20,
                this.icon.bounds.Width - 40,
                20),
            new Rectangle(21, 368, 6, 5),
            color,
            0,
            Vector2.Zero,
            SpriteEffects.FlipVertically,
            0.5f);

        // Top-Left
        spriteBatch.Draw(
            Game1.mouseCursors,
            new Vector2(this.icon.bounds.X + offset.X, this.icon.bounds.Y + offset.Y),
            new Rectangle(16, 368, 5, 15),
            color,
            0,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            0.5f);

        // Bottom-Left
        spriteBatch.Draw(
            Game1.mouseCursors,
            new Vector2(this.icon.bounds.X + offset.X, this.icon.bounds.Y + this.icon.bounds.Height + offset.Y - 20),
            new Rectangle(16, 368, 5, 5),
            color,
            0,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.FlipVertically,
            0.5f);

        // Top-Right
        spriteBatch.Draw(
            Game1.mouseCursors,
            new Vector2(this.icon.bounds.Right + offset.X - 20, this.icon.bounds.Y + offset.Y),
            new Rectangle(16, 368, 5, 15),
            color,
            0,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.FlipHorizontally,
            0.5f);

        // Bottom-Right
        spriteBatch.Draw(
            Game1.mouseCursors,
            new Vector2(
                this.icon.bounds.Right + offset.X - 20,
                this.icon.bounds.Y + this.icon.bounds.Height + offset.Y - 20),
            new Rectangle(16, 368, 5, 5),
            color,
            0,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically,
            0.5f);

        if (this.Active && hover)
        {
            this.icon.tryHover(cursor.X, cursor.Y);
        }

        this.icon.draw(spriteBatch, color, 1f, 0, offset.X, offset.Y);

        spriteBatch.DrawString(
            Game1.smallFont,
            this.name,
            new Vector2(
                this.icon.bounds.X + Game1.tileSize + offset.X,
                this.icon.bounds.Y + (IClickableMenu.borderWidth / 2f) + offset.Y),
            this.Active ? Game1.textColor : Game1.unselectedOptionColor);

        // 在 TabEditor.cs 的 Draw 方法中修改箭头的绘制与悬停
        if (!this.Active)
        {
            return;
        }

        // 🌟 修复：绘制时必须传入 offset，保持渲染与 cursor -= offset 后的碰撞区一致
        this.upArrow.tryHover(cursor.X, cursor.Y);
        this.downArrow.tryHover(cursor.X, cursor.Y);

        this.upArrow.draw(spriteBatch, color, 1f, 0, offset.X, offset.Y);
        this.downArrow.draw(spriteBatch, color, 1f, 0, offset.X, offset.Y);

        if (this.upArrow.bounds.Contains(cursor))
        {
            this.SetHoverText(this.upArrow.hoverText);
            return;
        }

        if (this.downArrow.bounds.Contains(cursor))
        {
            this.SetHoverText(this.downArrow.hoverText);
            return;
        }

        this.SetHoverText(null);
    }

    /// <inheritdoc />
    public override ICustomComponent MoveTo(Point location)
    {
        base.MoveTo(location);

        // 同步更新 X 坐标
        this.icon.bounds.X = location.X + Game1.tileSize;
        this.icon.bounds.Y = location.Y;

        this.upArrow.bounds.X = location.X + 8;
        this.upArrow.bounds.Y = location.Y + 8;

        this.downArrow.bounds.X = location.X + this.bounds.Width - Game1.tileSize + 8;
        this.downArrow.bounds.Y = location.Y + 8;
        return this;
    }

    /// <inheritdoc />
    public override bool TryLeftClick(Point cursor)
    {
        // 只要光标（无论是鼠标移动过去的，还是手柄吸附过去重合的）在范围内就触发
        if (this.Active && this.downArrow.bounds.Contains(cursor))
        {
            this.moveDown.InvokeAll(this, new ClickedEventArgs(SButton.MouseLeft, cursor));
            return true;
        }

        if (this.Active && this.upArrow.bounds.Contains(cursor))
        {
            this.moveUp.InvokeAll(this, new ClickedEventArgs(SButton.MouseLeft, cursor));
            return true;
        }

        return base.TryLeftClick(cursor);
    }

    /// <inheritdoc />
    public override bool TryRightClick(Point cursor)
    {
        if (this.Active && this.downArrow.bounds.Contains(cursor))
        {
            this.moveDown.InvokeAll(this, new ClickedEventArgs(SButton.MouseRight, cursor));
            return true;
        }

        if (this.Active && this.upArrow.bounds.Contains(cursor))
        {
            this.moveUp.InvokeAll(this, new ClickedEventArgs(SButton.MouseRight, cursor));
            return true;
        }

        return base.TryRightClick(cursor);
    }

    /// <summary>Updates the tab icon.</summary>
    /// <param name="icon">The icon to update to.</param>
    public void UpdateIcon(IIcon icon)
    {
        this.icon = icon.Component(IconStyle.Transparent, this.icon.bounds.X, this.icon.bounds.Y);
        this.icon.bounds.Width = this.bounds.Width - (Game1.tileSize * 2);
    }
}
