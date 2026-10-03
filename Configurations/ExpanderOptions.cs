namespace ItemCountExpander.Configurations
{
    /// <summary>
    /// Compiled-in settings. Deliberately not file-configurable: 255 is the only
    /// meaningful limit because item counts are byte-width everywhere (memory,
    /// network sync and save files).
    /// </summary>
    public static class ExpanderOptions
    {
        /// <summary>Guard constant written by vanilla; searched for in target bodies.</summary>
        public const int VanillaLimit = 200;

        /// <summary>Replacement value. Hard ceiling: byte.MaxValue.</summary>
        public const int Limit = byte.MaxValue;

        /// <summary>
        /// Expected "getItemCount() &gt;= 200" sites per target method in all known
        /// game versions. A different count means the game code changed shape and
        /// this module must be re-verified.
        /// </summary>
        public const int ExpectedReplacementsPerTarget = 1;
    }
}
