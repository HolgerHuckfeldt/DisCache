using DisCache.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DisCache.Services
{
    public class KeyService
    {

        private List<KeyException> _exceptions = new List<KeyException>();

        public KeyService()
        {
            //using var db = new Database.DiscContext();
            //_exceptions = new List<KeyException>();

            //var now = DateTime.Now;
            //var exceptions = db.KeyExceptions.Where(
            //    s => (s.ActiveFrom == null || (s.ActiveFrom <= now)) &&
            //         (s.ActiveFrom == null || (s.ActiveTo >= now)) &&
            //         s.IsActive);

            //foreach (var exception in exceptions)
            //{
            //    _exceptions.Add(new KeyException()
            //    {
            //        Action = (KeyAction)exception.Action,
            //        ActionDetail = exception.Detail,
            //        Key = exception.Key
            //    });
            //}
        }

        public KeyAction HasAction(string key)
        {
            //    foreach (var keyException in _exceptions)
            //    {
            //        switch (keyException.Action)
            //        {
            //            case KeyAction.None:
            //                return KeyAction.None;
            //            case KeyAction.Ignore:
            //                if (keyException.ActionDetail == "StartsWith")
            //                {
            //                    if(key.StartsWith(keyException.Key))
            //                        return KeyAction.Ignore;
            //                }
            //                if (keyException.ActionDetail == "EndsWith")
            //                {
            //                    if (key.EndsWith(keyException.Key))
            //                        return KeyAction.Ignore;
            //                }
            //                break;
            //                if (keyException.ActionDetail == "Exact")
            //                {
            //                    if (key.Equals(keyException.Key))
            //                        return KeyAction.Ignore;
            //                }
            //                break;
            //            case KeyAction.Rewrite:
            //                return KeyAction.None;
            //            default:
            //                throw new ArgumentOutOfRangeException();
            //        }
            //    }


            return KeyAction.None;
        }
    }
}
