namespace StardewMods.BetterChests.Framework.Services.Features;

using StardewModdingAPI.Events;
using StardewMods.BetterChests.Framework.Interfaces;
using StardewMods.BetterChests.Framework.Services.Factory;
using StardewMods.Common.Interfaces;
using StardewMods.Common.Services;
using StardewMods.Common.Services.Integrations.BetterChests;
using StardewValley.Menus;

/// <summary>Automatically organizes items between chests during sleep.</summary>
internal sealed class AutoOrganize : BaseFeature<AutoOrganize>
{
    private readonly ContainerFactory containerFactory;
    private readonly ContainerHandler containerHandler;

    /// <summary>Initializes a new instance of the <see cref="AutoOrganize" /> class.</summary>
    /// <param name="containerFactory">Dependency used for accessing containers.</param>
    /// <param name="containerHandler">Dependency used for handling operations by containers.</param>
    /// <param name="eventManager">Dependency used for managing events.</param>
    /// <param name="modConfig">Dependency used for accessing config data.</param>
    public AutoOrganize(
        ContainerFactory containerFactory,
        ContainerHandler containerHandler,
        IEventManager eventManager,
        IModConfig modConfig)
        : base(eventManager, modConfig)
    {
        this.containerHandler = containerHandler;
        this.containerFactory = containerFactory;
    }

    /// <inheritdoc />
    public override bool ShouldBeActive => this.Config.DefaultOptions.AutoOrganize != FeatureOption.Disabled;

    /// <inheritdoc />
    protected override void Activate() => this.Events.Subscribe<DayEndingEventArgs>(this.OnDayEnding);

    /// <inheritdoc />
    protected override void Deactivate() => this.Events.Unsubscribe<DayEndingEventArgs>(this.OnDayEnding);

    private void OnDayEnding(DayEndingEventArgs e) => this.OrganizeAll();

    private void OrganizeAll()
    {
        // Sort once by priority descending; avoids two Dictionary allocations and sparse integer loops
        var sorted = this
            .containerFactory.GetAll(container => container.AutoOrganize == FeatureOption.Enabled)
            .OrderByDescending(container => container.StashToChestPriority)
            .ToList();

        if (sorted.Count == 0)
        {
            return;
        }

        // Transfer from lower-priority containers into higher-priority ones
        for (var toIdx = 0; toIdx < sorted.Count; toIdx++)
        {
            var containerTo = sorted[toIdx];
            for (var fromIdx = toIdx + 1; fromIdx < sorted.Count; fromIdx++)
            {
                var containerFrom = sorted[fromIdx];

                // Only transfer when priorities actually differ
                if (containerFrom.StashToChestPriority >= containerTo.StashToChestPriority)
                {
                    continue;
                }

                if (!this.containerHandler.Transfer(containerFrom, containerTo, out var amounts))
                {
                    continue;
                }

                foreach (var (name, amount) in amounts)
                {
                    if (amount > 0)
                    {
                        Log.Info(
                            "{0}: {{ Item: {1}, Quantity: {2}, From: {3}, To: {4} }}",
                            this.Id,
                            name,
                            amount,
                            containerFrom,
                            containerTo);
                    }
                }
            }

            ItemGrabMenu.organizeItemsInList(containerTo.Items);
        }
    }
}
