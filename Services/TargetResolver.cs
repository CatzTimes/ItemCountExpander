using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ItemCountExpander.Models;

namespace ItemCountExpander.Services
{
    /// <summary>
    /// Resolves target definitions against the loaded game assembly. Explicit
    /// parameter signatures mean future game overloads cannot throw
    /// AmbiguousMatchException during module load.
    /// </summary>
    public static class TargetResolver
    {
        public static TargetResolutionResult Resolve(PatchTargetDefinition[] definitions)
        {
            List<ResolvedTarget> resolved = new List<ResolvedTarget>(definitions.Length);
            List<string> unresolved = new List<string>();

            foreach (PatchTargetDefinition definition in definitions)
            {
                MethodBase method = AccessTools.Method(definition.DeclaringType, definition.MethodName, definition.ParameterTypes);
                if (method == null)
                {
                    unresolved.Add(definition.DisplayName);
                }
                else
                {
                    resolved.Add(new ResolvedTarget(definition, method));
                }
            }

            return new TargetResolutionResult(resolved.ToArray(), unresolved.ToArray());
        }
    }
}
