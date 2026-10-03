using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ItemCountExpander.Configurations;
using SDG.Unturned;

namespace ItemCountExpander.Services
{
    /// <summary>
    /// Rewrites the vanilla per-page item-count guard "getItemCount() &gt;= 200"
    /// into "getItemCount() &gt;= Limit". A constant is only rewritten when it sits
    /// in the exact guard shape — a count call immediately before, a conditional
    /// branch immediately after — so unrelated 200 constants inside these methods
    /// can never be hit by accident. The guard appears as the last operand of a
    /// compound condition in some game versions (e.g. ReceiveDragItem in
    /// decompiled builds), so matching is done on instruction triplets, never on
    /// statement shapes.
    /// </summary>
    public static class LimitTranspiler
    {
        private static readonly MethodInfo[] CountSources = BuildCountSources();

        public static readonly HarmonyMethod TranspilerHook =
            new HarmonyMethod(AccessTools.Method(typeof(LimitTranspiler), nameof(Transpile)));

        /// <summary>Replacements made since the last ResetForNextTarget call.</summary>
        public static int Replacements { get; private set; }

        public static void ResetForNextTarget()
        {
            Replacements = 0;
        }

        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                if (!IsVanillaGuardConstant(code[i]))
                {
                    continue;
                }

                if (i == 0 || !IsCountCall(code[i - 1]))
                {
                    continue;
                }

                if (i + 1 >= code.Count || !IsComparisonBranch(code[i + 1]))
                {
                    continue;
                }

                code[i].operand = ExpanderOptions.Limit;
                Replacements++;
            }

            return code;
        }

        private static MethodInfo[] BuildCountSources()
        {
            MethodInfo[] candidates =
            {
                AccessTools.Method(typeof(Items), "getItemCount"),
                AccessTools.Method(typeof(PlayerInventory), "getItemCount", new[] { typeof(byte) }),
            };

            List<MethodInfo> known = new List<MethodInfo>(candidates.Length);
            foreach (MethodInfo candidate in candidates)
            {
                if (candidate != null)
                {
                    known.Add(candidate);
                }
            }

            return known.ToArray();
        }

        private static bool IsVanillaGuardConstant(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Ldc_I4
                && instruction.operand is int value
                && value == ExpanderOptions.VanillaLimit;
        }

        private static bool IsCountCall(CodeInstruction instruction)
        {
            if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
            {
                return false;
            }

            MethodInfo callee = instruction.operand as MethodInfo;
            if (callee == null)
            {
                return false;
            }

            for (int i = 0; i < CountSources.Length; i++)
            {
                if (IsSameMethod(callee, CountSources[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameMethod(MethodInfo left, MethodInfo right)
        {
            if (left.DeclaringType != right.DeclaringType || left.Name != right.Name)
            {
                return false;
            }

            ParameterInfo[] leftParameters = left.GetParameters();
            ParameterInfo[] rightParameters = right.GetParameters();
            if (leftParameters.Length != rightParameters.Length)
            {
                return false;
            }

            for (int i = 0; i < leftParameters.Length; i++)
            {
                if (leftParameters[i].ParameterType != rightParameters[i].ParameterType)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsComparisonBranch(CodeInstruction instruction)
        {
            string name = instruction.opcode.Name;
            return name != null
                && (name.StartsWith("blt", StringComparison.Ordinal)
                    || name.StartsWith("bge", StringComparison.Ordinal)
                    || name.StartsWith("beq", StringComparison.Ordinal)
                    || name.StartsWith("bne.un", StringComparison.Ordinal)
                    || name.StartsWith("brtrue", StringComparison.Ordinal));
        }
    }
}
