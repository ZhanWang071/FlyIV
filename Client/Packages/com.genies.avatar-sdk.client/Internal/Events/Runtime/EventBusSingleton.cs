namespace Genies.Events
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class EventBusSingleton
#else
    public static class EventBusSingleton
#endif
    {
        private static EventBus _eventBus;

        public static EventBus EventBus
        {
            get
            {
                if (_eventBus == null)
                {
                    _eventBus = new EventBus();
                }

                return _eventBus;
            }
        }
    }
}
