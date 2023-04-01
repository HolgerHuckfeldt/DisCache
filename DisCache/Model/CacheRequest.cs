namespace DisCache.Model
{
    public class CacheRequest
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public int Duration { get; set; }
    }
}
