using System;
using RimeCommon.Mounting;
using RimeCommon.VFS;

namespace Loader2013_2.Frostbite
{
    /// <summary>
    /// Handles parsing of the layout of a Frostbite 2013.2 engine game
    /// </summary>
    class LayoutManager : BaseLayoutManager
    {
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// This function discovers all packages that are currently paired with the current game
        /// aka DLC
        /// </summary>
        protected override void DiscoverPackages()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This function should discover all superbundles that are paired with the current game
        /// </summary>
        protected override void DiscoverSuperbundles()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses the layout of the currently loaded Frostbite game
        /// </summary>
        protected override void ParseLayout()
        {
            throw new NotImplementedException();
        }
    }
}
