// ReSharper disable StringLiteralTypo
namespace SteamAuth
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class Player
    {
        [JsonProperty("steamid")]
        public string SteamId { get; set; }

        [JsonProperty("communityvisibilitystate")]
        public int CommunityVisibilityState { get; set; }

        [JsonProperty("profilestate")]
        public int ProfileState { get; set; }

        [JsonProperty("personaname")]
        public string PersonaName { get; set; }

        [JsonProperty("profileurl")]
        public string ProfileUrl { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("avatarmedium")]
        public string AvatarMedium { get; set; }

        [JsonProperty("avatarfull")]
        public string AvatarFull { get; set; }

        [JsonProperty("avatarhash")]
        public string AvatarHash { get; set; }

        [JsonProperty("personastate")]
        public int PersonaState { get; set; }

        [JsonProperty("realname")]
        public string RealName { get; set; }

        [JsonProperty("primaryclanid")]
        public string PrimaryClanId { get; set; }

        [JsonProperty("timecreated")]
        public long TimeCreated { get; set; }

        [JsonProperty("personastateflags")]
        public int PersonaStateFlags { get; set; }

        [JsonProperty("loccountrycode")]
        public string LocCountryCode { get; set; }

        [JsonProperty("locstatecode")]
        public string LocStateCode { get; set; }

        [JsonProperty("loccityid")]
        public int LocCityId { get; set; }
    }

    public class PlayersResponse
    {
        [JsonProperty("players")]
        public List<Player> Players { get; set; }
    }

    public class PlayerSummary
    {
        [JsonProperty("response")]
        public PlayersResponse Response { get; set; }
    }
}