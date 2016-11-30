using System;
using RimeCommon.Mounting;
using RimeCommon.VFS;
using System.Collections.Generic;
using RimeLib.Frostbite;
using RimeLib.IO;
using RimeLib.IO.Conversion;
using System.IO;
using RimeLib.Frostbite.Db;
using RimeLib.Frostbite.Bundles;

namespace Loader2013_2.Frostbite
{
    /// <summary>
    /// Handles parsing of the layout of a Frostbite 2013.2 engine game
    /// </summary>
    class LayoutManager : BaseLayoutManager
    {
        /// <summary>
        /// The base layout path, this may change depending on engine or game revision
        /// </summary>
        const string c_BaseLayoutPath = "/game/Data/layout.toc";

        /// <summary>
        /// Constructor that takes a parent plugin
        /// </summary>
        /// <param name="p_Plugin">Loader2013_2Plugin</param>
        public LayoutManager(Loader2013_2Plugin p_Plugin) : base(p_Plugin)
        {

        }

        /// <summary>
        /// This function will parse and check the virtual file system for the game files
        /// </summary>
        public override void DiscoverLayout()
        {
            // Clear possibly old data.
            Packages.Clear();
            Superbundles.Clear();
            FileSystems.Clear();
            AuthoritativePackage = null;
            Layout = null;

            // Try to find a Data directory
            if (!FileSystem.DirectoryExists("/game/Data"))
                throw new Exception("Failed to find the Data directory. Content mounting failed.");

            // Discover any packages that might exist.
            if (FileSystem.DirectoryExists("/game/Update"))
                DiscoverPackages();

            // Parse the base Layout file.
            ParseLayout();

            // Discover available superbundles.
            DiscoverSuperbundles();

            // Discover filesystems.
            DiscoverFileSystems();
        }

        /// <summary>
        /// This function should discover the filesystem entries
        /// These are normally stored in a frostbite game as "fs"
        /// </summary>
        protected override void DiscoverFileSystems()
        {
            var s_BaseLayout = Layout[0].Value as DbObject;

            if (s_BaseLayout == null)
                throw new Exception("No Superbundles found in Layout. This probably means data is corrupted or the engine you're trying to load is unsupported.");

            var s_FileSystems = s_BaseLayout["fs"].Value as DbObject;

            // As seen on some fb game I can't remember, fs isn't always required...
            if (s_FileSystems == null)
                return;

            for (var i = 0; i < s_FileSystems.Count; ++i)
            {
                var s_FileSystemEntry = s_FileSystems[i].Value as string;

                if (string.IsNullOrWhiteSpace(s_FileSystemEntry))
                    continue;

                var s_Entry = new FileSystemEntry(s_FileSystemEntry);

                // Figure out in which package this entry is in (if any).
                if (!FileSystem.FileExists(s_Entry.BasePath))
                    throw new Exception("FileSystem '" + s_FileSystemEntry + "' could not be located. Please make sure your game data is not corrupt.");

                // Check whether this entry also exists in the authoritative package.
                if (AuthoritativePackage != null && FileSystem.FileExists("/game" + AuthoritativePackage.Path + "/Data/" + s_FileSystemEntry))
                    s_Entry.AuthoritativePackage = AuthoritativePackage;

                FileSystems.Add(s_Entry);
            }
        }

        /// <summary>
        /// This function discovers all packages that are currently paired with the current game
        /// aka DLC
        /// </summary>
        protected override void DiscoverPackages()
        {
            List<FSLeaf> s_Leaves;

            // Query the VFS to see if we have a Update folder
            if (!FileSystem.ListPath("/game/Update", out s_Leaves))
            {
                // We could not locate the update directory.
                return;
            }

            /*
             * In the case of Battlefield 4 there is
             * /game/Update/Patch 
             * /game/Update/Xpack0 
             * /game/Update/Xpack1 
             * /game/Update/Xpack2 
             * /game/Update/Xpack3 
             * /game/Update/Xpack4 
             * /game/Update/Xpack5
             * /game/Update/Xpack6 
             * /game/Update/Xpack7
             * 
             * packages that we have to parse and load
            */

            // Iterate through each folder that is under /Update
            foreach (var l_Leaf in s_Leaves)
            {
                // This shouldn't happen, but if we have a file skip it
                if (l_Leaf.File)
                    continue;

                byte[] l_ManifestData;

                if (!FileSystem.ReadFile("/game" + l_Leaf.Path + "/package.mft", out l_ManifestData))
                {
                    // There was an error opening the package.mft from file
                    continue;
                }

                // Create a new manifest for each entry
                var l_Manifest = new PackageManifest(l_ManifestData, l_Leaf.Path);

                // We can NEVER (as of 2016) have multiple authoritative packages
                if (l_Manifest.Authoritative && AuthoritativePackage != null)
                    throw new Exception("A game cannot have multiple authoritative packages.");

                // If the package we parsed is authoritative then set it
                if (l_Manifest.Authoritative)
                    AuthoritativePackage = l_Manifest;
                else // Otherwise add it to the list
                    Packages.Add(l_Manifest);
            }
        }

        /// <summary>
        /// This function should discover all superbundles that are paired with the current game
        /// </summary>
        protected override void DiscoverSuperbundles()
        {
            var s_BaseLayout = Layout[0].Value as DbObject;

            if (s_BaseLayout == null)
                return;
                //throw new Exception("No Superbundles found in Layout. This probably means data is corrupted or the engine you're trying to load is unsupported.");

            var s_Superbundles = s_BaseLayout["superBundles"].Value as DbObject;

            if (s_Superbundles == null)
                return;
                //throw new Exception("No Superbundles found in Layout. This probably means data is corrupted or the engine you're trying to load is unsupported.");

            for (var i = 0; i < s_Superbundles.Count; ++i)
            {
                var s_SuperBundleEntry = s_Superbundles[i].Value as DbObject;

                var s_Name = s_SuperBundleEntry?["name"];

                if (s_Name == null)
                    continue;

                var s_SbPath = (string)s_Name.Value;

                var s_Entry = new SuperbundleEntry(s_SbPath);

                // Figure out in which package this entry is in (if any).
                if (!FileSystem.FileExists("/game/Data/" + s_Entry.Name + ".sb") || !FileSystem.FileExists("/game/Data/" + s_Entry.Name + ".toc"))
                {
                    foreach (var s_Package in Packages)
                    {
                        if (!FileSystem.FileExists("/game" + s_Package.Path + "/Data/" + s_Entry.Name + ".sb") || !FileSystem.FileExists("/game" + s_Package.Path + "/Data/" + s_Entry.Name + ".toc"))
                            continue;

                        s_Entry.ContainedPackage = s_Package;
                        break;
                    }

                    // Base superbundle doesn't exist at all.
                    if (s_Entry.ContainedPackage == null)
                        continue;
                }

                // Check whether this entry also exists in the authoritative package.
                if (AuthoritativePackage != null && FileSystem.FileExists("/game" + AuthoritativePackage.Path + "/Data/" + s_Entry.Name + ".sb") &&
                    FileSystem.FileExists("/game" + AuthoritativePackage.Path + "/Data/" + s_Entry.Name + ".toc"))
                    s_Entry.AuthoritativePackage = AuthoritativePackage;

                Superbundles.Add(s_Entry);
            }
        }

        /// <summary>
        /// Parses the layout of the currently loaded Frostbite game
        /// </summary>
        protected override void ParseLayout()
        {
            // Here is where things can change (fifa...)

            var s_LayoutPath = c_BaseLayoutPath;

            // If we have an Authoritative Package then use that instead
            if (AuthoritativePackage != null)
                s_LayoutPath = "/game" + AuthoritativePackage.Path + "/Data/layout.toc";

            RimeReader s_LayoutReader;

            if (!FileSystem.OpenFileRead(s_LayoutPath, out s_LayoutReader, Endianness.LittleEndian))
#if DEBUG
                throw new Exception("Failed to read the Layout file. Please verify that it exists and is readable.");
#else
            return;
#endif

            Layout = ParseReadLayout(s_LayoutReader);

            s_LayoutReader.Dispose();
            s_LayoutReader = null;

            // If we didn't parse a auth package, return happily
            if (s_LayoutPath == c_BaseLayoutPath)
                return;

            // Make sure the base layout path exists and is valid.

            if (!FileSystem.OpenFileRead(c_BaseLayoutPath, out s_LayoutReader, Endianness.LittleEndian))
                throw new Exception("Failed to read the base Layout file. Please verify that it exists and is readable.");

            var s_BaseLayout = ParseReadLayout(s_LayoutReader);

            s_LayoutReader.Dispose();
            s_LayoutReader = null;

            var s_BaseHead = (int)(s_BaseLayout[0].Value as DbObject)["head"].Value;
            var s_PatchBase = (int)(Layout[0].Value as DbObject)["base"].Value;

            // This behavior is closer to what the engine does, which
            // is basically completely ignoring the patched layout and
            // authoritative package. Should we do the same or except?
            if (s_BaseHead != s_PatchBase)
            {
                Layout = s_BaseLayout;
                AuthoritativePackage = null;
            }

        }

        private DbObject ParseReadLayout(RimeReader p_Reader)
        {
            var s_LayoutMagic = p_Reader.ReadUInt32();

            // Check if the file is obfuscated.
            if (s_LayoutMagic == Loader2013_2Plugin.c_Obfuscated_0 ||
                s_LayoutMagic == Loader2013_2Plugin.c_Obfuscated_1)
                p_Reader.EnableDeobfuscation();
            else if (s_LayoutMagic == Loader2013_2Plugin.c_Signed) // Signed
                p_Reader.Seek(0x228, SeekOrigin.Current);
            else
                throw new Exception("The Layout file appears to have an invalid header or is unsupported.");

            return new DbObject(p_Reader);
        }
    }
}
