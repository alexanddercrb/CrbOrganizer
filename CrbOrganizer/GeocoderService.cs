using ReverseGeocoding;

namespace CrbOrganizer
{
    public sealed class GeocoderService
    {
        private static ReverseGeocoder _instance = null;
        private static readonly object _lock = new object();

        private GeocoderService() { }

        public static ReverseGeocoder Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ReverseGeocoder("cities500.txt");
                    }
                    return _instance;
                }
            }
        }

        public static void InitGeocoder()
        {
            _instance = new ReverseGeocoder("cities500.txt");
        }
    }
}
