namespace DisCache.Model
{
    public class KeyException
    {
        public string Key { get; set; }
        public KeyAction Action { get; set; }
        public string ActionDetail { get; set; }
    }

    public enum KeyAction
    {
        None = 0,
        Ignore = 1,
        Rewrite = 2
    }

}
