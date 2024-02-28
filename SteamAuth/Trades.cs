namespace SteamAuth
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public class TradeOffersResponse
    {
        [JsonProperty("response")]
        public TradesResponse TradesResponse { get; set; }

        private List<TradeOffer> _tradeOffers;

        public List<TradeOffer> TradeOffers
        {
            get
            {
                if (_tradeOffers == null)
                {
                    _tradeOffers = new List<TradeOffer>();
                    if (TradesResponse.TradeOffersSent != null)
                    {
                        _tradeOffers.AddRange(TradesResponse.TradeOffersSent);
                    }
                    
                    if (TradesResponse.TradeOffersReceived != null)
                    {
                        _tradeOffers.AddRange(TradesResponse.TradeOffersReceived);
                    }

                    Process();
                }

                return _tradeOffers;
            }
        }

        private void Process()
        {
            ProcessDescriptions();
            ProcessSteamIds();
        }

        private void ProcessDescriptions()
        {
            foreach (TradeOffer tradeOffer in TradeOffers)
            {
                foreach (Item item in tradeOffer.ItemsToGive.Concat(tradeOffer.ItemsToReceive))
                {
                    item.CorrespondingDescription =
                        TradesResponse.Descriptions.FirstOrDefault(d => d.ClassId == item.ClassId && d.InstanceId == item.InstanceId);
                }
            }
        }
        
        

        private void ProcessSteamIds()
        {
            foreach (TradeOffer tradeOffer in TradeOffers)
            {
                tradeOffer.SteamId = GetSteamIdFromAccountId(tradeOffer.AccountIdOther);
            }
        }
        
        private static string GetSteamIdFromAccountId(ulong accountId)
        {
            const ulong universe = 1;
            const ulong type = 1;
            const ulong instance = 1;

            ulong steamId = (universe << 56) | (type << 52) | (instance << 32) | accountId;
            return steamId.ToString();
        }
    }

    public class TradesResponse
    {
        [JsonProperty("trade_offers_sent")]
        public TradeOffer[] TradeOffersSent { get; set; }

        [JsonProperty("trade_offers_received")]
        public TradeOffer[] TradeOffersReceived { get; set; }

        [JsonProperty("descriptions")]
        public Description[] Descriptions { get; set; }

        [JsonProperty("next_cursor")]
        public int NextCursor { get; set; }
    }

    public class TradeOffer
    {
        [JsonProperty("tradeofferid")]
        public ulong TradeOfferId { get; set; }

        [JsonProperty("accountid_other")]
        public ulong AccountIdOther { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("expiration_time")]
        public ulong ExpirationTime { get; set; }

        [JsonProperty("trade_offer_state")]
        public int TradeOfferState { get; set; }

        [JsonProperty("items_to_give")]
        public Item[] ItemsToGive { get; set; }

        [JsonProperty("items_to_receive")]
        public Item[] ItemsToReceive { get; set; }

        [JsonProperty("is_our_offer")]
        public bool IsOurOffer { get; set; }

        [JsonProperty("time_created")]
        public int TimeCreated { get; set; }

        [JsonProperty("time_updated")]
        public int TimeUpdated { get; set; }

        [JsonProperty("from_real_time_trade")]
        public bool FromRealTimeTrade { get; set; }

        [JsonProperty("escrow_end_date")]
        public int EscrowEndDate { get; set; }

        [JsonProperty("confirmation_method")]
        public int ConfirmationMethod { get; set; }

        [JsonProperty("eresult")]
        public int EResult { get; set; }

        [JsonProperty("tradeid")]
        public string TradeId { get; set; }
        
        public string SteamId { get; set; }
    }

    public class Item
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("contextid")]
        public string ContextId { get; set; }

        [JsonProperty("assetid")]
        public string AssetId { get; set; }

        [JsonProperty("classid")]
        public string ClassId { get; set; }

        [JsonProperty("instanceid")]
        public string InstanceId { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("missing")]
        public bool Missing { get; set; }

        [JsonProperty("est_usd")]
        public string EstimatedUsd { get; set; }

        public Description CorrespondingDescription { get; set; }
    }

    public class Description
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("classid")]
        public string ClassId { get; set; }

        [JsonProperty("instanceid")]
        public string InstanceId { get; set; }

        [JsonProperty("currency")]
        public bool Currency { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonProperty("descriptions")]
        public ItemDescription[] Descriptions { get; set; }

        [JsonProperty("tradable")]
        public bool Tradable { get; set; }

        [JsonProperty("actions")]
        public Actions[] Actions { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_color")]
        public string NameColor { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonProperty("market_actions")]
        public MarketActions[] MarketActions { get; set; }

        [JsonProperty("commodity")]
        public bool Commodity { get; set; }

        [JsonProperty("market_tradable_restriction")]
        public int MarketTradableRestriction { get; set; }

        [JsonProperty("marketable")]
        public bool Marketable { get; set; }

        [JsonProperty("tags")]
        public Tags[] Tags { get; set; }
    }

    public class ItemDescription
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class Actions
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class MarketActions
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Tags
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("internal_name")]
        public string InternalName { get; set; }

        [JsonProperty("localized_category_name")]
        public string LocalizedCategoryName { get; set; }

        [JsonProperty("localized_tag_name")]
        public string LocalizedTagName { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}