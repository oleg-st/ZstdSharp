using System;

namespace ZstdSharp.Unsafe
{
    public partial struct ZDICT_params_t
    {
        /*< optimize for a specific zstd compression level; 0 means default */
        public int compressionLevel;

        /*< Write log to stderr; 0 = none (default); 1 = errors; 2 = progression; 3 = details; 4 = debug; */
        public uint notificationLevel;

        /*< force dictID value; 0 means auto mode (32-bits random value) */
        public uint dictID;
    }
}
