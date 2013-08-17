using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SteamTrade
{
    public class ItemsGame
    {
        public string Response;

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
            return new ItemsGame {Response = result};
        }

        protected class ItemsGameResult
        {
            public ItemsGame result { get; set; }
        }
    }
}
