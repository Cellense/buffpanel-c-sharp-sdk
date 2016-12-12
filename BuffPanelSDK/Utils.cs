using System;
using System.Collections.Generic;


namespace BuffPanel
{

    internal class Utils
    {

        public static double GetCurrentTimestamp()
        {
            var t0 = DateTime.UtcNow;
            var tEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
            return t0.Subtract(tEpoch).TotalMilliseconds / 1000.0;
        }

        public static void ExtendDictionary<K, V>(Dictionary<K, V> destination, Dictionary<K, V> source)
        {
            foreach (KeyValuePair<K, V> pair in source)
            {
                destination[pair.Key] = pair.Value;
            }
        }

        public static string GenerateCookieId()
        {
            Random random = new Random();
            return System.Globalization.CultureInfo.InstalledUICulture.DisplayName       // Language
                 + "-" + String.Format("{0:X}", Convert.ToInt32(GetCurrentTimestamp()))  // Time
                 + "-" + String.Format("{0:X}", Convert.ToInt32(5000))                   // Time in game
                 + "-" + String.Format("{0:X}", random.Next(1000000000));
        }

        public static bool IsDoubleDefined(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

    }
}
