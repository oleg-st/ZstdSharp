namespace ZstdSharp.Unsafe
{
    public struct algo_time_t
    {
        public uint tableTime;
        public uint decode256Time;
        public algo_time_t(uint tableTime = default, uint decode256Time = default)
        {
            this.tableTime = tableTime;
            this.decode256Time = decode256Time;
        }
    }
}