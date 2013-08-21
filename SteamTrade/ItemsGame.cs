using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using ValveFormat;

namespace SteamTrade
{
    public class ItemsGame
    {
        public string Response;

        public ValveFormatParser Parser;

        public Dictionary<string, string> Items;

        public static ItemsGame FetchItemsGame(string url)
        {
            string cachefile = "d2_items_game.cache";
            string result = "";

            if (File.Exists(cachefile) && File.Exists(cachefile + ".dat"))
            {
                var prevurl = File.ReadAllText(cachefile + ".dat");
                if (prevurl != url)
                {
                    using (var wc = new WebClient { Proxy = null })
                    {
                        result = wc.DownloadString(url);
                        File.WriteAllText(cachefile, result);
                        File.WriteAllText(cachefile + ".dat", url);
                    }
                }
            }
            else
            {
                using (var wc = new WebClient {Proxy = null})
                {
                    result = wc.DownloadString(url);
                    File.WriteAllText(cachefile, result);
                    File.WriteAllText(cachefile + ".dat", url);
                }
            }
            var parser = new ValveFormatParser(cachefile);
            parser.LoadFile();
            var dict = ParseFile(parser);
            return new ItemsGame { Response = result, Parser = parser, Items = dict};
        }

        private static Dictionary<string, string> ParseFile(ValveFormatParser parser)
        {
            var root = parser.RootNode;
            var dict = new Dictionary<string, string>();
            foreach (var node1 in root.SubNodes)
            {
                if (node1.Key != "items") continue;
                foreach (var node2 in node1.SubNodes)
                {
                    var defindex = node2.Key;
                    var rarity = "common";
                    foreach (var node3 in node2.SubNodes.Where(node3 => node3.Key == "item_rarity"))
                    {
                        rarity = node3.Value;
                        break;
                    }
                    dict.Add(defindex, rarity);
                }
            }
            return dict;
        }

        public string GetItemRarity(string defindex)
        {
            return Items.ContainsKey(defindex)
                       ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Items[defindex])
                       : "Unknown";
        }

        public string GetRarityColor(string rarity)
        {
            switch (rarity.ToLower())
            {
                case "common":
                    return "#b0c3d9";
                case "uncommon":
                    return "#5e98d9";
                case "rare":
                    return "#4b69ff";
                case "mythical":
                    return "#8847ff";
                case "legendary":
                    return "#d32ce6";
                case "ancient":
                    return "#eb4b4b";
                case "immortal":
                    return "#e4ae39";
                case "arcana":
                    return "#ade55c";
                default:
                    return "#dddddd";
            }
        }

        public string GetRarityColorFromName(string itemname)
        {
            var name = itemname.ToLower();
            if (name.Contains("(common)"))
                return "#b0c3d9";
            if (name.Contains("(uncommon)"))
                return "#5e98d9";
            if (name.Contains("(rare)"))
                return "#4b69ff";
            if (name.Contains("(mythical)"))
                return "#8847ff";
            if (name.Contains("(legendary)"))
                return "#d32ce6";
            if (name.Contains("(ancient)"))
                return "#eb4b4b";
            if (name.Contains("(immortal)"))
                return "#e4ae39";
            if (name.Contains("(arcana)"))
                return "#ade55c";
            return "#dddddd";
        }

        protected class ItemsGameResult
        {
            public ItemsGame result { get; set; }
        }
    }
}
