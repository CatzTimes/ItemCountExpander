using System.Reflection;

namespace ItemCountExpander.Models
{
    /// <summary>A patch target definition resolved to a concrete game method.</summary>
    public sealed class ResolvedTarget
    {
        public ResolvedTarget(PatchTargetDefinition definition, MethodBase method)
        {
            Definition = definition;
            Method = method;
        }

        public PatchTargetDefinition Definition { get; }

        public MethodBase Method { get; }
    }

    /// <summary>Aggregate resolution result; AllResolved drives the all-or-nothing gate.</summary>
    public sealed class TargetResolutionResult
    {
        public TargetResolutionResult(ResolvedTarget[] resolved, string[] unresolvedDisplayNames)
        {
            Resolved = resolved;
            UnresolvedDisplayNames = unresolvedDisplayNames;
        }

        public ResolvedTarget[] Resolved { get; }

        public string[] UnresolvedDisplayNames { get; }

        public bool AllResolved
        {
            get { return UnresolvedDisplayNames.Length == 0; }
        }
    }

    /// <summary>Transpiler result for one patched method.</summary>
    public sealed class PatchOutcome
    {
        public PatchOutcome(string displayName, int replacements)
        {
            DisplayName = displayName;
            Replacements = replacements;
        }

        public string DisplayName { get; }

        /// <summary>How many guard constants the transpiler rewrote in this method.</summary>
        public int Replacements { get; }
    }
}
