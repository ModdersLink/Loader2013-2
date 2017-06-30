using RimeLib.Frostbite.Db;
using RimeLib.IO;
using RimeLib.Frostbite.Bundles;

namespace Loader2013_2.Frostbite.Bundles
{
    /// <summary>
    /// Implementation for a Frostbite 2013.2 (Battlefield 4, DAI) SuperbundleLayout
    /// </summary>
    public class SuperbundleLayout : RimeLib.Frostbite.Bundles.SuperbundleLayout
    {
        /// <summary>
        /// Constructor for handling Frostbite 2013.2 specific superbundles
        /// </summary>
        /// <param name="p_Reader"></param>
        /// <param name="p_Entry"></param>
        /// <param name="p_Authoritative"></param>
        public SuperbundleLayout(RimeReader p_Reader, SuperbundleEntry p_Entry, bool p_Authoritative)
            : base(p_Reader, p_Entry, p_Authoritative)
        {
        }

        /// <summary>
        /// Parses the superbundle layout for Frostbite 2013.2 (Battlefield 4) superbundles
        /// Reads out the bundle, chunk entries
        /// </summary>
        /// <param name="p_Object">DbObject of the superbundle's table of contents (.toc)</param>
        protected override void ParseLayout(DbObject p_Object)
        {
            // Read out all of the bundles
            var s_Bundles = p_Object["bundles"].Value as DbObject;

            if (s_Bundles != null)
            {
                for (var i = 0; i < s_Bundles.Count; ++i)
                {
                    var s_BundleEntry = new BundleEntry(s_Bundles[i].Value as DbObject, this);
                    BundleEntries.TryAdd(s_BundleEntry.ID.ToLowerInvariant(), s_BundleEntry);
                }
            }

            // Read out all of the chunks
            var s_Chunks = p_Object["chunks"].Value as DbObject;

            if (s_Chunks != null)
            {
                for (var i = 0; i < s_Chunks.Count; ++i)
                {
                    var s_ChunkEntry = new ChunkEntry(s_Chunks[i].Value as DbObject, this);
                    ChunkEntries.TryAdd(s_ChunkEntry.ID, s_ChunkEntry);
                }
            }
        }
    }
}
