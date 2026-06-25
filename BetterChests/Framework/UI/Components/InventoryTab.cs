namespace StardewMods.BetterChests.Framework.UI.Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.BetterChests.Framework.Models;
using StardewMods.Common.Services.Integrations.FauxCore;
using StardewMods.Common.UI.Components;
using StardewValley.Menus;

/// <summary>A component with an icon that expands into a label when hovered.</summary>
internal sealed class InventoryTab : BaseComponent
{
    private readonly ClickableTextureComponent icon;
    private readonly Vector2 origin;
    private readonly int overrideWidth;
    private readonly int textWidth;

    /// <summary>Initializes a new instance of the <see cref="InventoryTab" /> class.</summary>
    /// <param name="parent">The parent menu.</param>
    /// <param name="x">The x-coordinate of the tab component.</param>
    /// <param name="y">The y-coordinate of the tab component.</param>
    /// <param name="icon">The tab icon.</param>
    /// <param name="tabData">The inventory tab data.</param>
    /// <param name="label">The localized label text.</param>
    /// <param name="overrideWidth">Indicates if the component should have a default width.</param>
    public InventoryTab(ICustomMenu? parent, int x, int y, IIcon icon, TabData tabData, string label, int overrideWidth = -1)
        : base(parent, x, y, Game1.tileSize, Game1.tileSize, label)
    {
        var textBounds = Game1.smallFont.MeasureString(label).ToPoint();
        this.Data = tabData;
        this.overrideWidth = overrideWidth;
        this.textWidth = textBounds.X;

        // 🌟 核心修复 1：将 origin 设为左侧展开后的最大边界锚点，防止判定区两头抖动
        int maxWidth = this.textWidth + Game1.tileSize + IClickableMenu.borderWidth;
        this.origin = new Vector2(x - maxWidth, y);

        // 默认未展开时，判定区直接对齐左侧的图标
        if (overrideWidth == -1)
        {
            this.bounds.X = x - Game1.tileSize;
            this.bounds.Width = Game1.tileSize;
        }
        else
        {
            this.bounds.Width = overrideWidth;
            this.bounds.X = x - overrideWidth;
        }

        this.icon = icon.Component(IconStyle.Transparent, this.bounds.X, y);
    }

    /// <summary>Gets or sets a value indicating whether the tab is currently active.</summary>
    public bool Active { get; set; }

    /// <summary>Gets the tab data.</summary>
    public TabData Data { get; }

    /// <inheritdoc />
    public override void Draw(SpriteBatch spriteBatch, Point cursor, Point offset)
    {
        cursor -= offset;
        this.Update(cursor);

        // 🌟 核心修复：增加手柄吸附判定，确保手柄选中时也能正确高亮
        var hover = this.bounds.Contains(cursor)
            || (Game1.options.SnappyMenus && Game1.activeClickableMenu?.currentlySnappedComponent == this);

        var color = this.Active
            ? Color.White
            : hover
                ? Color.LightGray
                : Color.Gray;

        // Top-Center
        spriteBatch.Draw(
            Game1.mouseCursors,
            new Rectangle(
                this.bounds.X + offset.X + 20,
                this.bounds.Y + offset.Y,
                this.bounds.Width - 40,
                this.bounds.Height),
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
                this.bounds.X + offset.X + 20,
                this.bounds.Y + this.bounds.Height + offset.Y - 20,
                this.bounds.Width - 40,
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
            new Vector2(this.bounds.X + offset.X, this.bounds.Y + offset.Y),
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
            new Vector2(this.bounds.X + offset.X, this.bounds.Y + this.bounds.Height + offset.Y - 20),
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
            new Vector2(this.bounds.Right + offset.X - 20, this.bounds.Y + offset.Y),
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
            new Vector2(this.bounds.Right + offset.X - 20, this.bounds.Y + this.bounds.Height + offset.Y - 20),
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

        if (this.bounds.Width != this.overrideWidth
            && this.bounds.Width != this.textWidth + Game1.tileSize + IClickableMenu.borderWidth)
        {
            return;
        }

        spriteBatch.DrawString(
            Game1.smallFont,
            this.name,
            new Vector2(
                this.bounds.X + Game1.tileSize + offset.X,
                this.bounds.Y + (IClickableMenu.borderWidth / 2f) + offset.Y),
            this.Active ? Game1.textColor : Game1.unselectedOptionColor);
    }

    /// <inheritdoc />
    public override void Update(Point cursor)
    {
        if (this.overrideWidth != -1)
        {
            return;
        }

        // 检测悬浮（包括手柄吸附）
        bool isHovered = this.bounds.Contains(cursor)
            || (Game1.options.SnappyMenus && Game1.activeClickableMenu?.currentlySnappedComponent == this);

        // 最大总宽度
        int maxWidth = this.textWidth + Game1.tileSize + IClickableMenu.borderWidth;

        // 🌟 核心修复 2：平滑增减宽度
        int targetWidth = isHovered ? maxWidth : Game1.tileSize;
        if (this.bounds.Width != targetWidth)
        {
            if (this.bounds.Width < targetWidth)
            {
                this.bounds.Width = Math.Min(this.bounds.Width + 16, targetWidth);
            }
            else
            {
                this.bounds.Width = Math.Max(this.bounds.Width - 16, targetWidth);
            }

            // 🌟 核心修复 3：X 坐标的正确收缩逻辑
            // 未展开时：X 应该在原点左边一个 tileSize 处 (this.icon 的位置)
            // 展开时：X 应该向左延伸到最大宽度处
            int originalX = (int)this.origin.X + maxWidth; // 恢复传入的原始 X 坐标
            this.bounds.X = originalX - this.bounds.Width;
            this.icon.bounds.X = this.bounds.X; // 图标始终保持在整个标签的最左侧
        }
    }
}
