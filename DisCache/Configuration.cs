namespace DisCache
{
    public class Configuration
    {
        public static string DbPath { get; set; }
        public static bool UseMemoryCacheAccelerator { get; set; }

        public static int UseMemoryCacheAcceleratorTime { get; set; }

        public static bool UseQueuedRefresh { get; set; } = false;
    }
}
