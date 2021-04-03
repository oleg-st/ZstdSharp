using System;

namespace ZstdSharp
{
    public partial struct ZSTD_bounds
    {
        public nuint error;

        public int lowerBound;

        public int upperBound;
    }
}
