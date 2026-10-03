using System;
using SDG.Unturned;

namespace ItemCountExpander.Models
{
    /// <summary>A vanilla method whose per-page item-count guard is widened.</summary>
    public sealed class PatchTargetDefinition
    {
        public PatchTargetDefinition(string displayName, Type declaringType, string methodName, Type[] parameterTypes)
        {
            DisplayName = displayName;
            DeclaringType = declaringType;
            MethodName = methodName;
            ParameterTypes = parameterTypes;
        }

        /// <summary>Human-readable identity used in log output.</summary>
        public string DisplayName { get; }

        public Type DeclaringType { get; }

        public string MethodName { get; }

        /// <summary>
        /// Exact signature: guards against AmbiguousMatchException when the game
        /// gains overloads. MethodName uses nameof where possible so renames fail
        /// the build instead of failing silently at module load.
        /// </summary>
        public Type[] ParameterTypes { get; }
    }

    /// <summary>
    /// The three server-authoritative per-page count guards, verified against
    /// both the official SDK source and decompiled client builds. Every dynamic
    /// item-add path in the game converges to one of these.
    /// </summary>
    public static class PatchTargetDefinitions
    {
        public static readonly PatchTargetDefinition[] InventoryLimits =
        {
            new PatchTargetDefinition(
                "PlayerInventory.ReceiveDragItem",
                typeof(PlayerInventory),
                nameof(PlayerInventory.ReceiveDragItem),
                new[] { typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte) }),
            new PatchTargetDefinition(
                "PlayerInventory.tryAddItem(x, y, page, rot)",
                typeof(PlayerInventory),
                "tryAddItem",
                new[] { typeof(Item), typeof(byte), typeof(byte), typeof(byte), typeof(byte) }),
            new PatchTargetDefinition(
                "Items.tryAddItem(item, isStateUpdatable)",
                typeof(Items),
                "tryAddItem",
                new[] { typeof(Item), typeof(bool) }),
        };
    }
}
