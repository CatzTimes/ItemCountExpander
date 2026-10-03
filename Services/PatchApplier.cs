using System.Collections.Generic;
using HarmonyLib;
using ItemCountExpander.Models;

namespace ItemCountExpander.Services
{
    /// <summary>
    /// Applies the resolved patches. Harmony executes the transpiler during
    /// Patch(), so per-target replacement counts are available immediately.
    /// </summary>
    public static class PatchApplier
    {
        public static PatchOutcome[] Apply(Harmony harmony, ResolvedTarget[] targets)
        {
            List<PatchOutcome> outcomes = new List<PatchOutcome>(targets.Length);

            foreach (ResolvedTarget target in targets)
            {
                LimitTranspiler.ResetForNextTarget();
                harmony.Patch(target.Method, transpiler: LimitTranspiler.TranspilerHook);
                outcomes.Add(new PatchOutcome(target.Definition.DisplayName, LimitTranspiler.Replacements));
            }

            return outcomes.ToArray();
        }
    }
}
