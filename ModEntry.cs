using System;
using System.Globalization;

namespace proffit_tool
{
    /// <summary>
    /// Entry point of the mod. The loader finds the one class implementing IMod, creates it,
    /// applies the [HarmonyPatch] classes of this assembly, then calls Init and OnEnable.
    /// All of that happens in the main menu: no save is loaded yet, so do not touch game
    /// state here. Subscribe to ModHook events instead (see ModdingGuide.md, "Hook Points").
    /// </summary>
    public class ModEntry : IMod
    {
        private const string BoughtPriceTag = "BOUGHT_PRICE_TAG";

        public static ModEntry Instance { get; private set; }
        public ModManifest Manifest { get; private set; }
        public ModLog Log { get; private set; }

        public void Init(ModManifest manifest)
        {
            Instance = this;
            Manifest = manifest;
            Log = new ModLog(manifest);
            Log.Log($"{manifest.Name} v{manifest.ModVersion} loaded.");
        }

        public void OnEnable()
        {
            ModHook.OnShutterOpenedEarly += OnShutterOpened;
            ModHook.OnCreateTooltipLate += LogSellingItemProfit;
            Log.Log("Enabled.");
        }

        public void OnDisable()
        {
            ModHook.OnShutterOpenedEarly -= OnShutterOpened;
            ModHook.OnCreateTooltipLate -= LogSellingItemProfit;
            Log.Log("Disabled.");
        }

        public void OnShutterOpened()
        {
            Log.Log("Quick Builder integrated now Worked.");
        }

        private void LogSellingItemProfit(RichTextBuilder tooltip, GameItem item)
        {
            if (item == null || !item.IsInsideContainerPlacedForSelling())
            {
                return;
            }

            if (!item.IsTag(BoughtPriceTag))
            {
                Log.Warning($"Cannot calculate profit for {item.name}: no recorded acquisition price.");
                return;
            }

            long acquisitionPrice = item.GetTagReadonly(BoughtPriceTag).GetInt();
            long salePrice = item.GetNegociatedValue();
            long profit = salePrice - acquisitionPrice;

            if (salePrice == 0)
            {
                Log.Log($"Sale value for {item.name} is zero; profit is {FormatMoney(profit)} and margin is unavailable.");
                return;
            }

            double marginPercent = (double)profit / salePrice * 100;
            Log.Log(
                $"Sale margin for {item.name}: profit {FormatMoney(profit)} " +
                $"({marginPercent.ToString("F1", CultureInfo.InvariantCulture)}%); " +
                $"sale {FormatMoney(salePrice)}, acquisition {FormatMoney(acquisitionPrice)}.");
        }

        private static string FormatMoney(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
