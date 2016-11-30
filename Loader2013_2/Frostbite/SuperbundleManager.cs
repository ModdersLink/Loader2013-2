using RimeCommon.Mounting;
using RimeCommon.VFS;
using RimeCommon.VFS.Backends;
using RimeLib.Frostbite.Bundles;
using RimeLib.Frostbite.Ebx;
using RimeLib.IO;
using RimeLib.IO.Conversion;
using System;
using System.IO;

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
            // On new superbundle manager creation we want to clear out all previous partitions. As a new superbundle manager only gets created when a new game is loaded
            PartitionRegistry.RemoveAllPartitions();

            // Create and mount the superbundle filesystem
            FileSystem.Mount(new SuperbundleBackend(), "/sb");
        }

        /// <summary>
        /// This function should mount all of the superbundles that have been discovered
        /// by the layout manager.
        /// </summary>
        public override void MountSuperbundles()
        {
            // Iterate through each superbundle entry and actually read inside of the superbundle, without loading the data
            foreach (var l_Entry in BasePlugin.LayoutManager.Superbundles)
            {
                RimeReader s_BaseReader;

                // TODO: Maybe except here?
                if (!FileSystem.OpenFileRead(l_Entry.BasePath + ".toc", out s_BaseReader, Endianness.LittleEndian))
                    continue;

                var s_LayoutMagic = s_BaseReader.ReadUInt32();

                // Check if the file is obfuscated.
                switch (s_LayoutMagic)
                {
                    case Loader2013_2Plugin.c_Obfuscated_0:
                    case Loader2013_2Plugin.c_Obfuscated_1:
                        s_BaseReader.EnableDeobfuscation();
                        break;

                    case Loader2013_2Plugin.c_Signed:
                        s_BaseReader.Seek(0x228, SeekOrigin.Current);
                        break;

                    default:
                        s_BaseReader.Dispose();
                        throw new Exception("There is an unknown superbundle type.");
                        //continue;
                }

                // This will throw a null reference exception, you MUST implement a per-game SuperbundleLayout
                // A SuperbundleLayout is equal to reading out a superbundle's toc, the "bundles" and "chunks" that are found in a toc.
                // But this does not READ the data, just the layout of it
                SuperbundleLayout s_BaseLayout = null; //new SuperbundleLayout(s_BaseReader, s_Entry, false);
                s_BaseReader.Dispose();

                SuperbundleLayout s_AuthoritativeLayout = null;

                // TODO: Parse Authoritative data
                // If this Superbundle also has an authoritative alternative, then
                // find it and parse its layout.

                var s_Superbundle = CollectSuperbundleData(s_BaseLayout, s_AuthoritativeLayout);

                MountSuperbundle(s_Superbundle);
            }
        }

        protected SuperbundleBase CollectSuperbundleData(SuperbundleLayout p_BaseLayout, SuperbundleLayout p_AuthoritativeLayout)
        {
            // TODO: You will have to implement your own version of a Superbundle per-game/engine
            SuperbundleBase s_Superbundle = null;

            // If we are only parsing a base game, with no authoritative (patch) data
            if (p_AuthoritativeLayout == null)
            {
                // TODO: Implement Superbundle inherited from SuperbundleBase
                // This will throw a null reference exception
                s_Superbundle = null;
                // Create an instance of a Superbundle, passing p_BaseLayout for the layout.
                // Within the superbundle create new Dictionary's for BundleEntries, and ChunkEntries.

                return s_Superbundle;
            }


            // TODO: Parse authoritative superbundle layout
            return s_Superbundle;
        }

        /// <summary>
        /// Mounts the loaded superbundle to the virtual file system
        /// </summary>
        /// <param name="p_Superbundle"></param>
        protected void MountSuperbundle(SuperbundleBase p_Superbundle)
        {
            // Get the superbundle backend
            var s_SuperbundleBackend = FileSystem.GetBackend("/sb");
            if (s_SuperbundleBackend == null)
                return;

            // Mount this superbundle to the backend for further use
            s_SuperbundleBackend.Mount(p_Superbundle);

            // Get the bundle backend
            var s_BundleBackend = FileSystem.GetBackend("bundles");
            if (s_BundleBackend == null)
                return;

            // Tell the bundle backend to mount the bundles within the superbundle
            foreach (var l_Bundle in p_Superbundle.Bundles)
                s_BundleBackend.Mount(l_Bundle);

            // Maybe you would want to inform your user that the mounting process has finished
            // Maybe not, I'm not the boss of you

            // If you are using Rime, you want to use MessageManager.SendMessage with a new SuperbundleMountedMessage containing the mounted superbundle object
        }
    }
}
