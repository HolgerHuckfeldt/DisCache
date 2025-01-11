using MediatR;

namespace DisCache.Mediator
{

    public class KeyUpdatedNotification : INotification
    {
        public string Action { get; }
        public string Key { get; }

        public KeyUpdatedNotification(string action, string key)
        {
            Action = action;
            Key = key;
        }
    }

    public class KeyDeletedNotification : INotification
    {
        public string Key { get; }

        public KeyDeletedNotification(string key)
        {
            Key = key;
        }
    }
}
