using RimeCommon.Mounting;
using System;
using System.Collections.Generic;
using RimeLib.Frostbite.Bundles;
using RimeCommon.VFS.Backends;
using RimeCommon.VFS;
using System.Collections.Concurrent;
using RimeLib.IO;
using RimeCommon.Messaging;
using RimeCommon.Logging;
using RimeLib.IO.Conversion;
using RimeCommon.Messages.Mounting;
using RimeCommon.Logging.Messages;
using System.IO;
using RimeLib.Frostbite.Db;

namespace Loader2013_2.Frostbite.Bundles
{
    /// <summary>
    /// Bundle manager for Frostbite 2013.2 engine bundles
    /// </summary>
    public class BundleManager : BaseBundleManager
    {
        /// <summary>
        /// This is an internal structure for tracking purposes
        /// </summary>
        protected struct InternalBundleEntry
        {
            public BundleEntry BundleEntry { get; set; }
            public SuperbundleBase SuperbundleBase { get; set; }
            public bool Mounted { get; set; }
        }

        private readonly ConcurrentDictionary<string, InternalBundleEntry> m_Bundles;

        /// <summary>
        /// Constructor that takes a parent plugin
        /// </summary>
        /// <param name="p_Plugin">Loader2013_2Plugin</param>
        public BundleManager(Loader2013_2Plugin p_Plugin) : base(p_Plugin)
        {
            m_Bundles = new ConcurrentDictionary<string, InternalBundleEntry>();

            // Mount bundles, resources, and ebx to the VFS
            FileSystem.Mount(new BundleBackend(), "/bundles");
            FileSystem.Mount(new ResourceBackend(), "/res");
            FileSystem.Mount(new EbxBackend(), "/ebx");
        }

        /// <summary>
        /// Discovers the bundles within a superbundle
        /// </summary>
        /// <param name="p_Superbundle"></param>
        public override void DiscoverBundles(SuperbundleBase p_Superbundle)
        {
            foreach (var s_Bundle in p_Superbundle.BundleEntries)
            {
                var s_Entry = new InternalBundleEntry
                {
                    BundleEntry = s_Bundle.Value,
                    SuperbundleBase = p_Superbundle,
                    Mounted = false
                };

                m_Bundles.AddOrUpdate(s_Bundle.Key, s_Entry, (k, v) => s_Entry);
            }
        }

        /// <summary>
        /// Unmounts this bundle from the vfs
        /// </summary>
        /// <param name="p_Name">Name of the bundle to unmount, this takes the full bundle path</param>
        /// <returns>True on success, false otherwise</returns>
        public override bool DismountBundle(string p_Name)
        {
            // TODO: Dismount bundle from VFS
            InternalBundleEntry s_Entry;
            m_Bundles.TryRemove(p_Name, out s_Entry);

            return true;
        }

        /// <summary>
        /// This unmounts all bundles that have currently been mounted
        /// </summary>
        public override void DismountBundles()
        {
            // TODO: Dismount bundles from VFS
            m_Bundles.Clear();
        }

        /// <summary>
        /// This returns a string list of all bundles that have been loaded already
        /// </summary>
        /// <returns>List<string> of the bundle full names</returns>
        public override List<string> GetDiscoveredBundles()
        {
            return new List<string>(m_Bundles.Keys);
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
            var s_Endianness = p_Superbundle.BaseLayout.Cas ? Endianness.LittleEndian : Endianness.BigEndian;

            RimeReader s_BaseReader;
            if (!FileSystem.OpenFileRead(p_Superbundle.BasePath + ".sb", out s_BaseReader, s_Endianness))
                return;

            RimeReader s_PatchReader = null;

            if (p_Superbundle.PatchPath != null &&
                !FileSystem.OpenFileRead(p_Superbundle.PatchPath + ".sb", out s_PatchReader, s_Endianness))
            {
                s_BaseReader.Dispose();
                return;
            }

            foreach (var s_Bundle in p_Superbundle.BundleEntries)
            {
                MessageManager.SendMessageAsync(new LogWriteMessage
                {
                    LogLevel = LogsLevel.Info,
                    Message = $"Mounting superbundle '{p_Superbundle.Name}' bundle [{s_Bundle.Key}]..."
                });

                BundleBase s_MountedBundle;

                if (p_Superbundle.AuthoritativeLayout != null && s_Bundle.Value.BaseSuperbundle.Cas)
                    s_MountedBundle = MountPatchedCASBundle(p_Superbundle, s_Bundle.Value, s_BaseReader, s_PatchReader);
                else if (s_Bundle.Value.DeltaEntry != null && s_Bundle.Value.DeltaEntry.Delta)
                    s_MountedBundle = MountPatchedDeltaBundle(p_Superbundle, s_Bundle.Value, s_BaseReader, s_PatchReader);
                else if (s_Bundle.Value.DeltaEntry != null)
                    s_MountedBundle = MountPatchedBundle(p_Superbundle, s_Bundle.Value, s_BaseReader, s_PatchReader);
                else if (s_Bundle.Value.BaseSuperbundle.Cas)
                    s_MountedBundle = MountCASBundle(p_Superbundle, s_Bundle.Value, s_BaseReader, s_PatchReader);
                else
                    s_MountedBundle = MountBundle(p_Superbundle, s_Bundle.Value, s_BaseReader, s_PatchReader);

                // VFS
                if (s_MountedBundle == null)
                    return;

                MountBundleToVFS(s_MountedBundle);
            }

            s_BaseReader.Dispose();

            s_PatchReader?.Dispose();

            MessageManager.SendMessage(new SuperbundleBundlesMountedMessage { SuperbundleBase = p_Superbundle });

            MessageManager.SendMessageAsync(new LogWriteMessage
            {
                LogLevel = LogsLevel.Info,
                Message = $"Finished mounting superbundle '{p_Superbundle.Name}' bundles!"
            });
        }

        /// <summary>
        /// Mounts a content addressable storage bundle
        /// </summary>
        /// <param name="p_Superbundle">Superbundle to get bundles from</param>
        /// <param name="p_BundleEntry">The bundle entry information</param>
        /// <param name="p_BaseReader">The reader of the base file</param>
        /// <param name="p_PatchReader">The reader of the patch file</param>
        /// <returns>Loaded BundleBase object</returns>
        private BundleBase MountCASBundle(SuperbundleBase p_Superbundle, BundleEntry p_BundleEntry, RimeReader p_BaseReader, RimeReader p_PatchReader)
        {
            p_BaseReader.Seek(p_BundleEntry.Offset, SeekOrigin.Begin);

            var s_Object = new DbObject(p_BaseReader, p_BundleEntry.Size);
            var s_Bundle = new Bundle(s_Object[0].Value as DbObject, p_BundleEntry);

            p_Superbundle.Bundles.TryAdd(p_BundleEntry.ID, s_Bundle);

            return s_Bundle;
        }

        private BundleBase MountPatchedCASBundle(SuperbundleBase p_Superbundle, BundleEntry p_BundleEntry, RimeReader p_BaseReader, RimeReader p_PatchReader)
        {
            throw new NotImplementedException("Patch files for Frostbite 2013.2 not supported.");
        }

        /// <summary>
        /// Helper function to mount a base bundle
        /// </summary>
        /// <param name="p_Superbundle">Superbundle to get bundles from</param>
        /// <param name="p_BundleEntry">The bundle entry information</param>
        /// <param name="p_BaseReader">The reader of the base file</param>
        /// <param name="p_PatchReader">The reader of the patch file</param>
        /// <returns>Loaded BundleBase object</returns>
        private BundleBase MountBundle(SuperbundleBase p_Superbundle, BundleEntry p_BundleEntry, RimeReader p_BaseReader, RimeReader p_PatchReader)
        {
            p_BaseReader.Seek(p_BundleEntry.Offset, SeekOrigin.Begin);

            var s_Manifest = new BundleManifest(p_BaseReader, p_BundleEntry);
            var s_Bundle = s_Manifest.RealManifest.Bundle;

            p_Superbundle.Bundles.TryAdd(p_BundleEntry.ID, s_Bundle);

            return s_Bundle;
        }

        /// <summary>
        /// Helper function to mount a patched bundle
        /// </summary>
        /// <param name="p_Superbundle">Superbundle to get bundles from</param>
        /// <param name="p_BundleEntry">The bundle entry information</param>
        /// <param name="p_BaseReader">The reader of the base file</param>
        /// <param name="p_PatchReader">The reader of the patch file</param>
        /// <returns>BundleBase object</returns>
        private BundleBase MountPatchedBundle(SuperbundleBase p_Superbundle, BundleEntry p_BundleEntry, RimeReader p_BaseReader, RimeReader p_PatchReader)
        {
            throw new NotImplementedException("Patch files for Frostbite 2013.2 not supported.");
        }

        /// <summary>
        /// Helper function to mount patched bundles
        /// </summary>
        /// <param name="p_Superbundle">Superbundle to get bundles from</param>
        /// <param name="p_BundleEntry">The bundle entry information</param>
        /// <param name="p_BaseReader">The reader of the base file</param>
        /// <param name="p_PatchReader">The reader of the patch file</param>
        /// <returns>BundleBase object</returns>
        private BundleBase MountPatchedDeltaBundle(SuperbundleBase p_Superbundle, BundleEntry p_BundleEntry, RimeReader p_BaseReader, RimeReader p_PatchReader)
        {
            throw new NotImplementedException("Patch files for Frostbite 2013.2 not supported.");
        }

        /// <summary>
        /// This will parse a loaded bundle and mount everything to the VFS
        /// </summary>
        /// <param name="p_Bundle">Bundle to be mounted</param>
        private void MountBundleToVFS(BundleBase p_Bundle)
        {
            FileSystem.Mount("/bundles", p_Bundle);

            foreach (var s_Chunk in p_Bundle.ChunkEntries)
                FileSystem.Mount("/chunks", s_Chunk);

            foreach (var s_Resource in p_Bundle.ResourceEntries)
                FileSystem.Mount("/res", s_Resource);

            foreach (var s_Ebx in p_Bundle.PartitionEntries)
                FileSystem.Mount("/ebx", s_Ebx);
        }
    }
}
