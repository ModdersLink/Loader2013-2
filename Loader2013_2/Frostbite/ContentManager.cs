using RimeCommon.Mounting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loader2013_2.Frostbite
{
    /// <summary>
    /// This handles all of the CAS content within a Frostbite 2013.2 engine game
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
            // TODO: Mount the cas/cat format
            throw new NotImplementedException();
        }
    }
}
