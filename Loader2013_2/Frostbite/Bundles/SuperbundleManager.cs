using RimeCommon.Logging;
using RimeCommon.Logging.Messages;
using RimeCommon.Messages.Mounting;
using RimeCommon.Messaging;
using RimeCommon.Mounting;
using RimeCommon.VFS;
using RimeCommon.VFS.Backends;
using RimeLib.Frostbite.Bundles;
using RimeLib.Frostbite.Core;
using RimeLib.Frostbite.Ebx;
using RimeLib.IO;
using RimeLib.IO.Conversion;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Loader2013_2.Frostbite.Bundles
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
            // Create and mount the chunk filesystem
            FileSystem.Mount(new ChunkBackend(), "/chunks");
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
#if DEBUG
                        throw new Exception("There is an unknown superbundle type.");
#else
                        continue;
#endif
                }

                // Open the base layout's superbundle
                var s_BaseLayout = new SuperbundleLayout(s_BaseReader, l_Entry, false);
                s_BaseReader.Close();


                SuperbundleLayout s_AuthoritativeLayout = null;

                // If this Superbundle also has an authoritative alternative, then
                // find it and parse its layout.
                if (l_Entry.AuthoritativePackage != null)
                {
                    // Create a new reader
                    RimeReader s_AuthoritativeReader;

                    // Open the patched file
                    if (FileSystem.OpenFileRead(l_Entry.PatchPath + ".toc", out s_AuthoritativeReader, Endianness.LittleEndian))
                    {
                        // Read out the magic of the file
                        var s_AuthoritativeLayoutMagic = s_AuthoritativeReader.ReadUInt32();
                        switch (s_AuthoritativeLayoutMagic)
                        {
                            case Loader2013_2Plugin.c_Obfuscated_0:
                            case Loader2013_2Plugin.c_Obfuscated_1:
                                s_AuthoritativeReader.EnableDeobfuscation();
                                break;

                            case Loader2013_2Plugin.c_Signed:
                                s_AuthoritativeReader.Seek(0x228, SeekOrigin.Current);
                                break;
                            default:
                                s_AuthoritativeReader.Close();
                                s_AuthoritativeReader = null;
                                break;
                        }

                        if (s_AuthoritativeReader != null)
                        {
                            s_AuthoritativeLayout = new SuperbundleLayout(s_AuthoritativeReader, l_Entry, true);
                            s_AuthoritativeReader.Close();
                        }
                    }
                }

                var s_Superbundle = CollectSuperbundleData(s_BaseLayout, s_AuthoritativeLayout);

                MountSuperbundle(s_Superbundle);
            }

            MessageManager.SendMessageAsync(new LogWriteMessage
            {
                LogLevel = RimeCommon.Logging.LogsLevel.Info,
                Message = "Finished mounting fb2013.2 superbundles!"
            });
        }

        /// <summary>
        /// This will iterate through each of the superbundles and get the information that is needed for Rime to operate
        /// </summary>
        /// <param name="p_BaseLayout">The base layout</param>
        /// <param name="p_AuthoritativeLayout">The authoritative layout</param>
        /// <returns></returns>
        protected SuperbundleBase CollectSuperbundleData(SuperbundleLayout p_BaseLayout, SuperbundleLayout p_AuthoritativeLayout)
        {
            SuperbundleBase s_Superbundle = null;

            // If we are only parsing a base game, with no authoritative (patch) data
            if (p_AuthoritativeLayout == null)
            {
                // Create an instance of a Superbundle, passing p_BaseLayout for the layout.
                // Within the superbundle create new Dictionary's for BundleEntries, and ChunkEntries.
                s_Superbundle = new Superbundle(p_BaseLayout)
                {
                    BundleEntries = new System.Collections.Concurrent.ConcurrentDictionary<string, BundleEntry>(),
                    ChunkEntries = new System.Collections.Concurrent.ConcurrentDictionary<RimeLib.Frostbite.Core.GUID, ChunkEntry>()
                };

                return s_Superbundle;
            }


            // This Superbundle has an authoritative version.
            // Collect chunks and bundles in order to be able to 
            // properly delegate authoritative reading.
            s_Superbundle = new Superbundle(p_BaseLayout, p_AuthoritativeLayout)
            {
                BundleEntries = CollectSuperbundleBundles(p_BaseLayout, p_AuthoritativeLayout),
                ChunkEntries = CollectSuperbundleChunks(p_BaseLayout, p_AuthoritativeLayout)
            };

            return s_Superbundle;
        }

        /// <summary>
        /// Iterates through the superbundle for all of the bundles
        /// </summary>
        /// <param name="p_BaseLayout">Base layout</param>
        /// <param name="p_AuthoritativeLayout">Authoritative layout</param>
        /// <returns>Dictionary with (name, bundle object)</returns>
        protected ConcurrentDictionary<string, BundleEntry> CollectSuperbundleBundles(SuperbundleLayout p_BaseLayout, SuperbundleLayout p_AuthoritativeLayout)
        {
            var s_BundleEntries = new ConcurrentDictionary<string, BundleEntry>(p_BaseLayout.BundleEntries);

            if (p_BaseLayout.Cas)
                return new ConcurrentDictionary<string, BundleEntry>(p_AuthoritativeLayout.BundleEntries);

            foreach (var s_Entry in p_AuthoritativeLayout.BundleEntries)
            {
                if (s_Entry.Value.Base && !s_Entry.Value.Delta)
                    continue;

                if (s_BundleEntries.ContainsKey(s_Entry.Key))
                    s_BundleEntries[s_Entry.Key].DeltaEntry = s_Entry.Value;
                else
                    s_BundleEntries.TryAdd(s_Entry.Key, s_Entry.Value);
            }

            return s_BundleEntries;
        }

        /// <summary>
        /// Iterates through the superbundle for all chunk entries
        /// </summary>
        /// <param name="p_BaseLayout">Base layout</param>
        /// <param name="p_AuthoritativeLayout">Authoritative layout</param>
        /// <returns></returns>
        protected ConcurrentDictionary<GUID, ChunkEntry> CollectSuperbundleChunks(SuperbundleLayout p_BaseLayout, SuperbundleLayout p_AuthoritativeLayout)
        {
            var s_ChunkEntries = new ConcurrentDictionary<GUID, ChunkEntry>(p_BaseLayout.ChunkEntries);

            foreach (var s_Entry in p_AuthoritativeLayout.ChunkEntries)
                s_ChunkEntries.AddOrUpdate(s_Entry.Key, s_Entry.Value, (p_K, p_V) => s_Entry.Value);

            return s_ChunkEntries;
        }

        /// <summary>
        /// Mounts the loaded superbundle to the virtual file system
        /// </summary>
        /// <param name="p_Superbundle"></param>
        protected void MountSuperbundle(SuperbundleBase p_Superbundle)
        {
            MessageManager.SendMessageAsync(new LogWriteMessage
            {
                LogLevel = LogsLevel.Info,
                Message = $"Mounting superbundle '{p_Superbundle.Name}'..."
            });

            // Mount superbundle to the VFS
            FileSystem.Mount("/sb", p_Superbundle);

            // Mount all of the chunks to the chunk VFS
            foreach (var l_Chunk in p_Superbundle.ChunkEntries)
                FileSystem.Mount("/chunks", l_Chunk.Value);

            MessageManager.SendMessage(new SuperbundleMountedMessage
            {
                Superbundle = p_Superbundle
            });
        }
    }
}
