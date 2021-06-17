using Newtonsoft.Json;

namespace QuickStartApi
{
    public class EventSerializer
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public EventSerializer()
        {
            JsonSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        /// <summary>
        /// Gets the JSON serializer settings.
        /// </summary>
        private static JsonSerializerSettings JsonSerializerSettings { get; set; }

        public static T DeserializeObject<T>(string inputString)
        {
            return string.IsNullOrEmpty(inputString)
                ? default(T)
                : JsonConvert.DeserializeObject<T>(inputString, JsonSerializerSettings);
        }
    }
}
