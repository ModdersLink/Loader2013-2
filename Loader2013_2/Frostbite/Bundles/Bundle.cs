using RimeLib.Frostbite.Bundles;
using RimeLib.Frostbite.Db;
using RimeLib.IO;
using System;

namespace Loader2013_2.Frostbite.Bundles
{
    public class Bundle : BundleBase
    {
        /// <summary>
        /// Magic Salt
        /// </summary>
        public int MagicSalt { get; set; }

        /// <summary>
        /// To align the members or not
        /// </summary>
        public bool AlignMembers { get; set; }

        /// <summary>
        /// Total size of this bundle
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Bundle constructor
        /// </summary>
        /// <param name="p_Object">DbObject containing the information about this bundle</param>
        /// <param name="p_Entry"></param>
        public Bundle(DbObject p_Object, BundleEntry p_Entry)
        {
            Entry = p_Entry;

            Path = (string)p_Object["path"].Value;

            MagicSalt = (int)p_Object["magicSalt"].Value;
            AlignMembers = (bool)p_Object["alignMembers"].Value;
            TotalSize = (long)p_Object["totalSize"].Value;

            // Load all of the resource objects
            if (p_Object["res"] != null)
            {
                var s_Resources = p_Object["res"].Value as DbObject;

                if (s_Resources != null)
                    for (var i = 0; i < s_Resources.Count; ++i)
                        throw new NotImplementedException(); // TODO: Implement Resource
                        //m_Resources.Add(new Resource(s_Resources[i].Value as DbObject, this));
            }

            // Load all of the dbx objects
            if (p_Object["dbx"] != null)
            {
                var s_Dbx = p_Object["dbx"].Value as DbObject;

                if (s_Dbx != null)
                    for (var i = 0; i < s_Dbx.Count; ++i)
                        throw new NotImplementedException(); // TODO: Implement Dbx
                        //m_DBX.Add(new Dbx(s_Dbx[i].Value as DbObject, this));
            }

            // Load all of the ebx objects
            if (p_Object["ebx"] != null)
            {
                var s_Ebx = p_Object["ebx"].Value as DbObject;

                if (s_Ebx != null)
                    for (var i = 0; i < s_Ebx.Count; ++i)
                        throw new NotImplementedException(); // TODO: Implement 
                        //m_EBX.Add(new EBX(s_Ebx[i].Value as DbObject, this));
            }

            var s_ChunkMeta = p_Object["chunkMeta"] != null ? p_Object["chunkMeta"].Value as DbObject : null;

            // Load all of the chunk objects
            if (p_Object["chunks"] != null)
            {
                var s_Chunks = p_Object["chunks"].Value as DbObject;

                if (s_Chunks != null)
                {
                    for (var i = 0; i < s_Chunks.Count; ++i)
                    {
                        throw new NotImplementedException(); // TODO: Implement
                        //var s_Chunk = new Chunk(s_Chunks[i].Value as DbObject, this);

                        //if (s_ChunkMeta != null)
                        //    s_Chunk.Meta = s_ChunkMeta[i].Value as DbObject;

                        //m_Chunks.Add(s_Chunk);
                    }
                }
            }
        }

        /// <summary>
        /// Bundle constructor from BundleManifest
        /// </summary>
        /// <param name="p_Manifest">Bundle manifest</param>
        /// <param name="p_Reader">Opened reader to the start to the</param>
        /// <param name="p_Entry">Bundle Entry Information</param>
        public Bundle(BundleManifest p_Manifest, RimeReader p_Reader, BundleEntry p_Entry)
        {
            Entry = p_Entry;
            Path = p_Entry.ID;

            if (p_Manifest.RealManifest.EbxMode)
                ParseEbxBundle(p_Manifest, p_Reader);
            else
                ParseDbxBundle(p_Manifest, p_Reader);
        }

        /// <summary>
        /// Align a reader to 16 byte alignment
        /// </summary>
        /// <param name="p_Reader"></param>
        private void Align16(RimeReader p_Reader)
        {
            if (p_Reader.Position % 16 == 0)
                return;

            var s_Number = 16 - (p_Reader.Position % 16);
            p_Reader.ReadBytes((int)s_Number);
        }

        /// <summary>
        /// Parses an ebx bundle
        /// </summary>
        /// <param name="p_Manifest">Bundle Manifest</param>
        /// <param name="p_Reader">Reader opened to the position of the bundle</param>
        protected void ParseEbxBundle(BundleManifest p_Manifest, RimeReader p_Reader)
        {
            // TODO: Implement VeniceBundleManifest
            throw new NotImplementedException();
            //var s_Manifest = p_Manifest.RealManifest as VeniceBundleManifest;

            //// Align to 16 because of the header.
            //Align16(p_Reader);

            //var s_TextBlockReader = new RimeReader(new MemoryStream(s_Manifest.TextBlock), Endianness.BigEndian);

            //// Start reading entries.
            //for (var i = 0; i < p_Manifest.ManifestHeader.DbxCount; ++i)
            //{
            //    var s_Entry = s_Manifest.Records[i];

            //    // Read the name of the entry.
            //    s_TextBlockReader.Seek(s_Entry.NameOffset, SeekOrigin.Begin);
            //    var s_Name = s_TextBlockReader.ReadNullTerminatedString();

            //    // Get the SHA1 hash.
            //    var s_Hash = s_Manifest.HashBase[i];

            //    // Create the EBX entry.
            //    var s_Ebx = new EBX(s_Name, s_Hash, s_Entry, p_Reader.ReadBytes((int)s_Manifest.Records[i].PayloadSize), this);

            //    m_EBX.Add(s_Ebx);

            //    Align16(p_Reader);
            //}

            //// Read resource entries.
            //for (var i = (int)p_Manifest.ManifestHeader.DbxCount; i < p_Manifest.ManifestHeader.DbxCount + p_Manifest.ManifestHeader.ResourceCount; ++i)
            //{
            //    var s_Entry = s_Manifest.Records[i];

            //    // Read the name of the entry.
            //    s_TextBlockReader.Seek(s_Entry.NameOffset, SeekOrigin.Begin);
            //    var s_Name = s_TextBlockReader.ReadNullTerminatedString();

            //    // Get the SHA1 hash.
            //    var s_Hash = s_Manifest.HashBase[i];

            //    // Get the Type hash.
            //    var s_TypeHash = s_Manifest.ResourceTypeHash[(int)(i - p_Manifest.ManifestHeader.DbxCount)];

            //    // Get the Meta block.
            //    var s_Meta = s_Manifest.ResourceMeta[(int)(i - p_Manifest.ManifestHeader.DbxCount)];

            //    // Create the Resource entry.
            //    var s_Resource = new Resource(s_Name, s_Hash, s_TypeHash, p_Reader.ReadBytes((int)s_Manifest.Records[i].PayloadSize), s_Meta, s_Entry, this);

            //    m_Resources.Add(s_Resource);

            //    Align16(p_Reader);
            //}

            //// Read chunk entries.
            //var s_ChunkMeta = s_Manifest.ChunkMeta != null && s_Manifest.ChunkMeta["chunkMeta"] != null ? s_Manifest.ChunkMeta["chunkMeta"].Value as DbObject : null;

            //for (var i = 0; i < p_Manifest.ManifestHeader.ChunkCount; ++i)
            //{
            //    var s_Entry = s_Manifest.Chunks[i];

            //    // Get the SHA1 hash.
            //    var s_Hash = s_Manifest.HashBase[i + (int)(p_Manifest.ManifestHeader.DbxCount + p_Manifest.ManifestHeader.ResourceCount)];

            //    // Get the Meta block.
            //    var s_Meta = s_ChunkMeta != null ? s_ChunkMeta[i].Value as DbObject : null;

            //    // Create the Chunk entry.
            //    var s_Data = p_Reader.ReadBytes((int)(s_Manifest.Chunks[i].RangeEnd - s_Manifest.Chunks[i].RangeStart));
            //    var s_Chunk = new Chunk(s_Hash, s_Meta, s_Entry, s_Data, this);

            //    m_Chunks.Add(s_Chunk);

            //    Align16(p_Reader);
            //}

            //s_TextBlockReader.Dispose();
        }

        /// <summary>
        /// Parse a dbx bundle
        /// </summary>
        /// <param name="p_Manifest">Bundle manifest</param>
        /// <param name="p_Reader">Reader opened to the dbx file</param>
        protected void ParseDbxBundle(BundleManifest p_Manifest, RimeReader p_Reader)
        {
            throw new NotImplementedException("We currently do not support DBX-mode bundles.");
        }
    }
}
