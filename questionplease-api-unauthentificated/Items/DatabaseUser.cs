using Newtonsoft.Json;

namespace questionplease_api_unauthentificated.Items
{
    public class ReturnedUser
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "login")]
        public string Login { get; set; }

        [JsonProperty(PropertyName = "score")]
        public int Score { get; set; }
    }

    public class DatabaseUser : ReturnedUser
    {
        [JsonProperty(PropertyName = "userName")]
        public string UserName { get; set; }
    }
}
