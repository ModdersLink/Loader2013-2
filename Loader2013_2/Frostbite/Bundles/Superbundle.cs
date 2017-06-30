using RimeLib.Frostbite.Bundles;

namespace Loader2013_2.Frostbite.Bundles
{
    /// <summary>
    /// Superbundle implementation for Frostbite 2013.2 (Battlefield 4, DAI)
    /// </summary>
    public class Superbundle : SuperbundleBase
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="p_BaseLayout">The superbundle layout</param>
        /// <param name="p_AuthoritativeLayout">The authoritative superbundle layout (patched file)</param>
        public Superbundle(SuperbundleLayout p_BaseLayout, SuperbundleLayout p_AuthoritativeLayout = null)
            : base(p_BaseLayout, p_AuthoritativeLayout)
        {

        }

        /// <summary>
        /// Frees the resources
        /// </summary>
        public override void Dispose()
        {

        }
    }
}
