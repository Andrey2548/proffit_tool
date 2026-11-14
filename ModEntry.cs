using System;
using System.Globalization;
using HarmonyLib;

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
        private bool isEnabled;

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
            isEnabled = true;
            ModHook.OnShutterOpenedEarly += OnShutterOpened;
            ModHook.OnCreateTooltipLate += LogSellingItemProfit;
            Log.Log("Enabled.");
        }

        public void OnDisable()
        {
            isEnabled = false;
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
            if (item == null)
            {
                return;
            }

            if (!item.IsInsideContainerPlacedForSelling())
            {
                return;
            }

            LogItemProfit(item);
        }

        internal void LogNegotiationCounterProfit(GameItem item)
        {
            if (isEnabled && item != null)
            {
                LogItemProfit(item);
            }
        }

        private void LogItemProfit(GameItem item)
        {
            bool usedBasePrice = !item.IsTag(BoughtPriceTag);
            long acquisitionPrice = usedBasePrice
                ? item.GetBaseValue()
                : item.GetTagReadonly(BoughtPriceTag).GetInt();
            long salePrice = item.GetNegociatedValue();
            long profit = salePrice - acquisitionPrice;
            string priceSource = usedBasePrice ? "base price fallback" : "purchase price";

            if (salePrice == 0)
            {
                Log.Log(
                    $"Sale value for {item.name} is zero; profit is {FormatMoney(profit)} using " +
                    $"{priceSource} {FormatMoney(acquisitionPrice)}, and margin is unavailable.");
                return;
            }

            double marginPercent = (double)profit / salePrice * 100;
            Log.Log(
                $"Sale margin for {item.name}: profit {FormatMoney(profit)} " +
                $"({marginPercent.ToString("F1", CultureInfo.InvariantCulture)}%); " +
                $"sale {FormatMoney(salePrice)}, {priceSource} {FormatMoney(acquisitionPrice)}.");
        }

        private static string FormatMoney(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }

    [HarmonyPatch(typeof(NegociationUIManager), nameof(NegociationUIManager.UpdateUI), new[] { typeof(GameItem) })]
    internal static class NegotiationCounterPlacementPatch
    {
        private static void Postfix(GameItem __0, GameItem ___currentNegociatedItem)
        {
            if (ReferenceEquals(__0, ___currentNegociatedItem))
            {
                ModEntry.Instance?.LogNegotiationCounterProfit(__0);
            }
        }
    }
}
