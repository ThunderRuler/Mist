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

            HttpWebResponse response = SteamWeb.Request(url, "GET");
            DateTime SchemaLastModified = DateTime.Now;

            try
            {
                SchemaLastModified = DateTime.Parse(response.Headers["Last-Modified"]);
            }
            catch
            {
                SchemaLastModified = DateTime.Now;
            }

            if (!System.IO.File.Exists(cachefile) || (SchemaLastModified > System.IO.File.GetCreationTime(cachefile)))
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                result = reader.ReadToEnd();
                File.WriteAllText(cachefile, result);
                System.IO.File.SetCreationTime(cachefile, SchemaLastModified);
            }
            else
            {
                TextReader reader = new StreamReader(cachefile);
                result = reader.ReadToEnd();
                reader.Close();
            }
            response.Close();
            return new ItemsGame {Response = result};
        }

        protected class ItemsGameResult
        {
            public ItemsGame result { get; set; }
        }
    }
}
