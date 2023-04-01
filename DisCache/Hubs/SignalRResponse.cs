namespace DisCache.Hubs
{
    public class SignalRResponse
    {
        public bool Success { get; set; }
        public byte[] Data { get; set; }
        public string Message { get; set; }
    }
}
