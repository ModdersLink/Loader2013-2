using Loader2013_2.Frostbite;
using RimeCommon.Messages.Mounting;
using RimeCommon.Messaging;
using RimeCommon.Mounting;
using RimeCommon.Plugins;
using RimeLib.Frostbite;
using RimeLib.Frostbite.Containers;
using System.Windows.Forms;

namespace Loader2013_2
{
    /// <summary>
    /// Rime loader plugin for Frostbite Engine 2013.2
    /// This is used for:
    /// Battlefield 4
    /// Need for Speed: The Run
    /// </summary>
    public class Loader2013_2Plugin : BaseMountingPlugin
    {
        public override string Name => "Loader2013_2";
        public override string Author => "ModdersLink, kiwidog";
        public override string Version => "1.0";
        public override string Description => "Provides content mounting and management for Frostbite 2013.2 engine games, written for https://modders.link";
        public override string Extension => "_content-mounting";
        public override bool AutoStart => false;
        public override UserControl MainControl => null;
        public override MountPoint Mount => MountPoint.None;
        public override EngineType Engine => EngineType.Frostbite2013_2;

        public const uint c_Obfuscated_0 = 0x00CED100;
        public const uint c_Obfuscated_1 = 0x01CED100;
        public const uint c_Signed = 0x03CED100;

        /// <summary>
        /// Initialization function
        /// </summary>
        /// <param name="p_Parameters">Ignored argument</param>
        public override void Init(params object[] p_Parameters)
        {
            // Ensure that this plugin is called before UI plugins
            m_MessageListener.IsSystem = true;

            // Subscribe to mounting events
            m_MessageListener.RegisterListener(MessagingSubSystem.Mounting);

            // Listen for mounting a game request
            m_MessageListener.RegisterMessageCallback(typeof(MountBaseGameMessage), OnMountBaseGame);

            // Listen for mounting superbundle bundles request
            m_MessageListener.RegisterMessageCallback(typeof(MountSuperbundleBundlesMessage), OnMountSuperbundleBundles);

            // Listen for mounting a superbundle request
            m_MessageListener.RegisterMessageCallback(typeof(SuperbundleMountedMessage), OnSuperbundleMounted);
        }

        /// <summary>
        /// OnMountSuperbundleBundles is called when the superbundles have already been loaded
        /// and we are requesting for the bundles within that superbundle to be loaded
        /// </summary>
        /// <param name="p_RimeMessage">MountSuperbundleBundlesMessage</param>
        private void OnMountSuperbundleBundles(RimeMessage p_RimeMessage)
        {
            var s_Message = (MountSuperbundleBundlesMessage)p_RimeMessage;

            BundleManager?.MountBundles(s_Message.Superbundle);
        }

        /// <summary>
        /// OnSuperbundleMounted is called when a superbundle has been mounted to Rime
        /// </summary>
        /// <param name="p_RimeMessage">SuperbundleMountedMessage</param>
        private void OnSuperbundleMounted(RimeMessage p_RimeMessage)
        {
            var s_Message = (SuperbundleMountedMessage)p_RimeMessage;

            BundleManager?.DiscoverBundles(s_Message.Superbundle);
        }

        /// <summary>
        /// OnMountBaseGame is called when Rime is requesting to mount a Frostbite Engine game
        /// This function must check the version number, and ignore if they don't match or handle the mounting
        /// </summary>
        /// <param name="p_RimeMessage">MountBaseGameMessage</param>
        private void OnMountBaseGame(RimeMessage p_RimeMessage)
        {
            var s_Message = (MountBaseGameMessage)p_RimeMessage;

            // Version check
            if (s_Message.Engine != EngineType.Frostbite2013_2)
                return;

            // Load the necessary bindings.
            ContainerRegistry.ClearRegistry();

            // TODO: Mounting bindings per-game (should this be done in another plugin?)

            // Create our new managers to handle Frostbite 2013.2 content
            Mount2014_3Branch(s_Message);

            // Figure out the layout of the game
            LayoutManager.DiscoverLayout();

            // Mount the Content Addressable Storage
            ContentManager.MountBaseContent();

            // Mount the superbundles
            SuperbundleManager.MountSuperbundles();
        }

        /// <summary>
        /// This creates all of the managers that will be handling the 2013.2 engine specific data
        /// </summary>
        /// <param name="p_Message">Ignored</param>
        private void Mount2014_3Branch(MountBaseGameMessage p_Message)
        {
            LayoutManager = new LayoutManager(this);
            ContentManager = new ContentManager(this);
            SuperbundleManager = new SuperbundleManager(this);
            BundleManager = new BundleManager(this);
        }
    }
}
