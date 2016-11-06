using RimeCommon.Mounting;
using System;

namespace Loader2013_2.Frostbite
{
    /// <summary>
    /// Superbundle manager for Frostbite 2013.2 superbundles
    /// </summary>
    class SuperbundleManager : BaseSuperbundleManager
    {
        /// <summary>
        /// Constructor that takes a parent plugin
        /// </summary>
        /// <param name="p_Plugin">Loader2013_2Plugin</param>
        public SuperbundleManager(Loader2013_2Plugin p_Plugin) : base(p_Plugin)
        {

        }

        /// <summary>
        /// This function should mount all of the superbundles that have been discovered
        /// by the layout manager
        /// </summary>
        public override void MountSuperbundles()
        {
            throw new NotImplementedException();
        }
    }
}
