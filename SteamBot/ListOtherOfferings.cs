using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC.TF2.Internal;
using SteamTrade;

namespace MistClient
{
    class ListOtherOfferings
    {
        string itemName;
        ulong itemID;
        string price;
        private Inventory.Item item;

        static List<ListOtherOfferings> list = new List<ListOtherOfferings>();

        public ListOtherOfferings(string itemName, ulong itemID, string price, Inventory.Item item)
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

        public static void Add(string itemName, ulong itemID, string price, Inventory.Item item)
        {
            ListOtherOfferings ite = new ListOtherOfferings(itemName, itemID, price, item);
            list.Add(ite);
        }

        public static void Remove(string itemName, ulong itemID)
        {
            ListOtherOfferings item = list.Find(x => x.itemName == itemName && x.itemID == itemID);
            list.Remove(item);
        }

        public static void Clear()
        {
            list.Clear();
        }

        static internal List<ListOtherOfferings> Get()
        {
            return list;
        }
    }
}
