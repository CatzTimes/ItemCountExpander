using ItemCountExpander.Configurations;
using ItemCountExpander.Models;
using SDG.Unturned;

namespace ItemCountExpander.Monitors
{
    /// <summary>
    /// Turns patch results into log evidence. ModuleHook swallows entry-point
    /// exceptions and still marks the module as Initialized, so without these
    /// lines a game update that breaks this module would fail silently and the
    /// vanilla 200 limit would quietly come back.
    /// </summary>
    public static class PatchIntegrityMonitor
    {
        public static void ReportUnresolved(TargetResolutionResult resolution)
        {
            UnturnedLog.error(
                "ItemCountExpander: target method(s) not found: "
                + string.Join(", ", resolution.UnresolvedDisplayNames)
                + ". Patching skipped entirely (all-or-nothing); vanilla limit "
                + ExpanderOptions.VanillaLimit + " stays. A game update likely broke "
                + "compatibility (game version " + Provider.APP_VERSION + ").");
        }

        public static void ReportOutcomes(PatchOutcome[] outcomes)
        {
            bool mismatch = false;

            foreach (PatchOutcome outcome in outcomes)
            {
                if (outcome.Replacements != ExpanderOptions.ExpectedReplacementsPerTarget)
                {
                    mismatch = true;
                    UnturnedLog.error(
                        "ItemCountExpander: " + outcome.DisplayName + " — replaced "
                        + outcome.Replacements + " guard site(s), expected "
                        + ExpanderOptions.ExpectedReplacementsPerTarget
                        + ". Game code shape changed; verify compatibility (game version "
                        + Provider.APP_VERSION + ").");
                }
            }

            if (mismatch)
            {
                return;
            }

            int total = 0;
            foreach (PatchOutcome outcome in outcomes)
            {
                total += outcome.Replacements;
            }

            UnturnedLog.info(
                "ItemCountExpander: patched " + outcomes.Length + " method(s), replaced "
                + total + " count guard site(s); per-page item limit is now "
                + ExpanderOptions.Limit + " (game version " + Provider.APP_VERSION + ").");
        }
    }
}
