using RimeCommon.Mounting;
using System;
using System.Collections.Generic;
using RimeLib.Frostbite.Bundles;
using RimeCommon.VFS.Backends;
using RimeCommon.VFS;

namespace Loader2013_2.Frostbite
{
    /// <summary>
    /// Bundle manager for Frostbite 2013.2 engine bundles
    /// </summary>
    class BundleManager : BaseBundleManager
    {
        /// <summary>
        /// Constructor that takes a parent plugin
        /// </summary>
        /// <param name="p_Plugin">Loader2013_2Plugin</param>
        public BundleManager(Loader2013_2Plugin p_Plugin) : base(p_Plugin)
        {
            // TODO: Create a collection of bundle's
            // VFS
            FileSystem.Mount(new BundleBackend(), "/bundles");
        }

        /// <summary>
        /// Discovers the bundles within a superbundle
        /// </summary>
        /// <param name="p_Superbundle"></param>
        public override void DiscoverBundles(SuperbundleBase p_Superbundle)
        {
            foreach (var l_Bundle in p_Superbundle.BundleEntries)
            {
                // TODO: Add each bundle entry to a collection
            }
        }

        /// <summary>
        /// Unmounts this bundle from the vfs
        /// </summary>
        /// <param name="p_Name">Name of the bundle to unmount, this takes the full bundle path</param>
        /// <returns>True on success, false otherwise</returns>
        public override bool DismountBundle(string p_Name)
        {
            // TODO: Remove the bundle from the collection
            throw new NotImplementedException();
        }

        /// <summary>
        /// This unmounts all bundles that have currently been mounted
        /// </summary>
        public override void DismountBundles()
        {
            // TODO: Remove all bundles from the collection
            throw new NotImplementedException();
        }

        /// <summary>
        /// This returns a string list of all bundles that have been loaded already
        /// </summary>
        /// <returns>List<string> of the bundle full names</returns>
        public override List<string> GetDiscoveredBundles()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Mounts a bundle based on the full bundle name
        /// </summary>
        /// <param name="p_Name">Name of the bundle</param>
        /// <returns>True on success, false otherwise</returns>
        public override bool MountBundle(string p_Name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This mounts all bundles that have not been mounted already
        /// </summary>
        /// <param name="p_Superbundle">Superbundle to load all bundles that are contained within</param>
        public override void MountBundles(SuperbundleBase p_Superbundle)
        {
            throw new NotImplementedException();
        }
    }
}
