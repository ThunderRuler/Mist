using System;
using System.Collections.Generic;
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

            using (var wc = new WebClient { Proxy = null })
            {
                result = wc.DownloadString(url);
                File.WriteAllText(cachefile, result);
            }

            var parser = new ValveFormat.ValveFormatParser(cachefile);
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

        protected class ItemsGameResult
        {
            public ItemsGame result { get; set; }
        }
    }
}
