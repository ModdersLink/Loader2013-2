using RimeCommon.Mounting;
using RimeCommon.VFS;
using RimeCommon.VFS.Backends;
using RimeLib.Frostbite.Storage;
using RimeLib.IO;
using RimeLib.IO.Conversion;

namespace Loader2013_2.Frostbite
{
    /// <summary>
    /// This handles all of the CAS content within a Frostbite 2013.2 engine game
    /// This has changed in Frostbite 2015
    /// </summary>
    class ContentManager : BaseContentAddressableStorageManager
    {
        /// <summary>
        /// Constructor that takes in a parent plugin
        /// </summary>
        /// <param name="p_Plugin">Loader2013_2Plugin</param>
        public ContentManager(Loader2013_2Plugin p_Plugin) : base(p_Plugin)
        {
        }

        /// <summary>
        /// This mounts the base content to the virtual file system
        /// </summary>
        public override void MountBaseContent()
        {
            const string c_BaseCatalogPath = "/game/Data/cas.cat";

            // Create a new content addressable storage backend and mount it
            var s_FsBackend = new CasBackend();
            FileSystem.Mount(s_FsBackend, "/cas");

            // Open a reader to the base catalog path
            RimeReader s_BaseCatalogReader;
            if (FileSystem.OpenFileRead(c_BaseCatalogPath, out s_BaseCatalogReader, Endianness.LittleEndian))
            {
                // Create a new catalog
                BaseCatalog = new Catalog(s_BaseCatalogReader)
                {
                    Name = "cas",
                    AuthoritativePackage = null
                };

                // Mount this catalog to the Cas Backend
                s_FsBackend.Mount(BaseCatalog);

                // Close the reader
                s_BaseCatalogReader.Dispose();
            }

            // If the authoritative package is loaded, read the cas from there
            if (BasePlugin.LayoutManager.AuthoritativePackage != null)
            {
                var s_CatalogPath = "/game" + BasePlugin.LayoutManager.AuthoritativePackage.Path + "/Data/cas.cat";

                // Open a reader to the authoritative catalog
                RimeReader s_AuthoritativeCatalogReader;
                if (FileSystem.OpenFileRead(s_CatalogPath, out s_AuthoritativeCatalogReader, Endianness.LittleEndian))
                {
                    // Create a new catalog
                    AuthoritativeCatalog = new Catalog(s_AuthoritativeCatalogReader)
                    {
                        Name = "cas",
                        AuthoritativePackage = BasePlugin.LayoutManager.AuthoritativePackage
                    };

                    // Mount the authoritative catalog
                    s_FsBackend.Mount(AuthoritativeCatalog);

                    // Close the reader
                    s_AuthoritativeCatalogReader.Dispose();
                }
            }
        }
    }
}
