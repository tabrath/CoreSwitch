using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CoreSwitch.CLI")]
namespace CoreSwitch
{
    internal class Logger
    {
        public static readonly Logger Default = new Logger();

        private readonly List<Action<string>> _subscribers = new List<Action<string>>();

        public void Log(string text)
        {
            _subscribers.ForEach(handler => handler(text));
        }

        public Action Subscribe(Action<string> handler)
        {
            if (!_subscribers.Contains(handler))
                _subscribers.Add(handler);

            return () => Unsubscribe(handler);
        }

        private void Unsubscribe(Action<string> handler)
        {
            if (_subscribers.Contains(handler))
                _subscribers.Remove(handler);
        }
    }
}
