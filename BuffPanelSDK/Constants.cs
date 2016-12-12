using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel
{

    internal class Constants
    {

        /**
         * SDK
         */
        public static String SDK = "C# SDK";
        public static String VERSION = "2.0.0";

        /**
         * Tracking ids, events and properties
         */
        public static String ID_REGISTERED = "registered";
        public static String ID_COOKIE = "cookie";

        public static String EVENT_SESSION_START = "session_start";
        public static String EVENT_SESSION_END = "session_end";
        public static String EVENT_IDENTIFICATION = "identification";
        public static String EVENT_VIRTUAL_PAYMENT = "virtual_payment";

        public static String PROPERTY_APP_VERSION = "app_version";
        public static String PROPERTY_DURATION = "duration";
        public static String PROPERTY_REGISTRATION_ID = "registration_id";
        public static String PROPERTY_CURRENCY = "currency";
        public static String PROPERTY_AMOUNT = "amount";
        public static String PROPERTY_ITEM_NAME = "item_name";
        public static String PROPERTY_ITEM_TYPE = "item_type";

        /**
         * Sending
         */
        public static int BULK_LIMIT = 50;
        public static int BULK_TIMEOUT_MS = 10000;
        public static int BULK_INTERVAL_MS = 1000;
        public static int BULK_MAX_RETRIES = 20;
        public static int BULK_MAX_RETRY_WAIT_MS = 60 * 1000;

        public static String DEFAULT_TARGET = "https://api.BuffPanel.com";
        public static String BULK_URL = "/bulk";

        public static String ENDPOINT_UPDATE = "crm/customers";
        public static String ENDPOINT_TRACK = "crm/events";

        public static String DATABASE_NAME = "BuffPanel-v2.0.0.db";
    }
}
