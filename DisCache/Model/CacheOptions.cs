using System;

namespace DisCache.Model
{
    public class CacheOptions
    {
        public DateTime? AbsoluteExpiration { get; set; }
        public double? AbsoluteExpirationRelativeToNow { get; set; }
        public double? SlidingExpiration { get; set; }
    }
}
