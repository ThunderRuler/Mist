using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SteamTrade;

namespace MistClient
{
    class ListUserOfferings
    {
        string itemName;
        ulong itemID;
        string price;
        private Inventory.Item item;

        static List<ListUserOfferings> list = new List<ListUserOfferings>();

        public ListUserOfferings(string itemName, ulong itemID, string price, Inventory.Item item)
        {
            this.itemName = itemName;
            this.itemID = itemID;
            this.price = price;
            this.item = item;
        }

        public string ItemName
        {
            get { return itemName; }
            set { itemName = value; }
        }

        public ulong ItemID
        {
            get { return itemID; }
            set { itemID = value; }
        }

        public string ItemPrice
        {
            get { return price; }
            set { }
        }

        public Inventory.Item Item
        {
            get { return item; }
            set { }
        }

        public static void Add(string itemName, ulong itemID, Inventory.Item item, string price = null)
        {
            ListUserOfferings ite = new ListUserOfferings(itemName, itemID, price, item);
            list.Add(ite);
        }

        public static void Remove(string itemName, ulong itemID)
        {
            ListUserOfferings item = list.Find(x => x.itemName == itemName && x.itemID == itemID);
            list.Remove(item);
        }

        public static void Clear()
        {
            list.Clear();
        }

        static internal List<ListUserOfferings> Get()
        {
            return list;
        }
    }
}
