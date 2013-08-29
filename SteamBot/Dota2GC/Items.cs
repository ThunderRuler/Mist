using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamBot;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.Internal;

namespace MistClient.Dota2GC
{
    public class Items
    {
        /// <summary>
        /// Permanently deletes the specified item
        /// </summary>
        /// <param name="bot">The current Bot</param>
        /// <param name="item">The 64-bit Item ID to delete</param>
        public static void DeleteItem(Bot bot, ulong item)
        {
            var deleteMsg = new ClientGCMsg<MsgDelete>();

            deleteMsg.Write((ulong)item);

            bot.SteamGC.Send(deleteMsg, 570);
        }


        /// <summary>
        /// Sets a list of item positions.
        /// </summary>
        /// <param name="bot">The current Bot</param>
        /// <param name="itemPositions">A dicitonary of itemIds and position</param>
        public static void SetItemPositions(Bot bot, Dictionary<uint, uint> itemPositions)
        {
            var msg = new ClientGCMsgProtobuf<CMsgSetItemPositions>(1077);
            foreach (var pair in itemPositions)
            {
                msg.Body.item_positions.Add(new CMsgSetItemPositions.ItemPosition
                {
                    item_id = pair.Key,
                    position = pair.Value
                });
            }
            bot.SteamGC.Send(msg, 570);
        }

        public static void SortItems(Bot bot, uint sorttype)
        {
            var msg = new ClientGCMsgProtobuf<CMsgSortItems>(1041) {Body = {sort_type = sorttype}};
            bot.SteamGC.Send(msg, 570);
        }

        public static void SetItemPosition(Bot bot, SteamTrade.Inventory.Item item, short position)
        {
            byte[] bPos = BitConverter.GetBytes(position);
            byte[] bClass = BitConverter.GetBytes(item.InventoryToken);

            byte[] nInv = new byte[] { bPos[0], bPos[1], bClass[2], bClass[3] };

            uint newInventoryDescriptor = BitConverter.ToUInt32(nInv, 0);

            var aMsg = new ClientGCMsg<MsgSetItemPosition>();

            aMsg.Write((long)item.Id);
            aMsg.Write((uint)newInventoryDescriptor);

            bot.SteamGC.Send(aMsg, 570);
        }
    }

    public enum SortType
    {
        AscendingName = 1,
        DescendingName,
        Rarity,
        Unknown1,
        Unknown2,
        Unknown3
    }
}
