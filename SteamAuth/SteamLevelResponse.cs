namespace SteamAuth
{
    using Newtonsoft.Json;

    public class SteamLevelResponse
    {
        
        [JsonProperty("response")]
        public Level Response { get; set; }
    }

    public class Level
    {
        [JsonProperty("player_level")]
        public int PlayerLevel { get; set; }
    }
}