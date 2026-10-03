using HarmonyLib;
using ItemCountExpander.Models;
using ItemCountExpander.Monitors;
using ItemCountExpander.Services;
using SDG.Framework.Modules;

namespace ItemCountExpander
{
    /// <summary>
    /// Module entry point. Must stay a public non-abstract class with a public
    /// parameterless constructor: ModuleHook instantiates IModuleNexus types via
    /// Activator.CreateInstance and silently skips abstract (incl. static) ones.
    /// </summary>
    public class Module : IModuleNexus
    {
        private readonly Harmony harmony = new Harmony(nameof(ItemCountExpander));

        public void initialize()
        {
            TargetResolutionResult resolution = TargetResolver.Resolve(PatchTargetDefinitions.InventoryLimits);
            if (!resolution.AllResolved)
            {
                PatchIntegrityMonitor.ReportUnresolved(resolution);
                return;
            }

            PatchOutcome[] outcomes = PatchApplier.Apply(harmony, resolution.Resolved);
            PatchIntegrityMonitor.ReportOutcomes(outcomes);
        }

        public void shutdown()
        {
            harmony.UnpatchAll(harmony.Id);
        }
    }
}
