using System;

namespace DisCache
{
    public class RuntimeInformation
    {
        public static DateTime Startup { get; set; }

        public static string Name { get; set; }

        public static string Backend { get; set; }

        public static string BackendInformation { get; set; }

        public static string Version { get; set; }
    }
}
