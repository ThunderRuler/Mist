using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using BrightIdeasSoftware;
using SteamBot;
using MetroFramework.Forms;
using SteamTrade;

namespace MistClient
{
    public partial class ShowTrade : MetroForm
    {
        Bot bot;
        ulong sid;
        string username;
        public static bool loading = false;
        public static int itemsAdded = 0;
        public static bool accepted = false;
        public bool focused = false;
        public double OtherTotalValue = 0;
        double YourTotalValue = 0;
        Thread acceptTrade;
        bool tradeCompleted;
        private bool controlLoaded;

        public ShowTrade(Bot bot, string name)
        {
            InitializeComponent();
            list_inventory.CellToolTip.InitialDelay = list_inventory.CellToolTip.ReshowDelay = 0;
            list_otherofferings.CellToolTip.InitialDelay = list_otherofferings.CellToolTip.ReshowDelay = 0;
            list_userofferings.CellToolTip.InitialDelay = list_userofferings.CellToolTip.ReshowDelay = 0;
            list_inventory.CellToolTipGetter = (column, modelObject) =>
            {
                if (list_inventory.SelectedItem == null) return null;
                var itemId = (ulong)column_id.GetValue(modelObject);
                if ((ulong)column_id.GetValue(list_inventory.SelectedItem.RowObject) != itemId) return null;
                if (itemId == 0) return null;
                bot.GetInventory();
                foreach (var item in bot.MyInventory.Items)
                {
                    if (item.Id == itemId)
                    {
                        return GetTooltipText(item);
                    }
                }
                return null;
            };
            list_userofferings.CellToolTipGetter = (column, modelObject) =>
            {
                if (list_userofferings.SelectedItem == null) return null;
                var itemId = (ulong)column_id.GetValue(modelObject);
                if ((ulong)column_id.GetValue(list_userofferings.SelectedItem.RowObject) != itemId) return null;
                if (itemId == 0) return null;
                bot.GetInventory();
                foreach (var item in bot.MyInventory.Items)
                {
                    if (item.Id == itemId)
                    {
                        return GetTooltipText(item);
                    }
                }
                return null;
            };
            list_otherofferings.CellToolTipGetter = (column, modelObject) =>
            {
                var itemId = (ulong) column_id.GetValue(modelObject);
                if (itemId == 0) return null;
                foreach (var item in ListOtherOfferings.Get())
                {
                    if (item.ItemID == itemId)
                    {
                        return GetTooltipText(item.Item);
                    }
                }
                return null;
            };
            list_inventory.CellToolTip.SetMaxWidth(450);
            list_otherofferings.CellToolTip.SetMaxWidth(450);
            list_userofferings.CellToolTip.SetMaxWidth(450);
            Util.LoadTheme(metroStyleManager1);
            this.Text = "Trading with " + name;
            this.bot = bot;
            this.sid = bot.CurrentTrade.OtherSID;
            this.username = name;
            this.label_yourvalue.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.label_othervalue.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            column_otherofferings.Text = name + "'s Offerings:";
            ListInventory.ShowTrade = this;
            Thread checkExpired = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (loading)
                        {
                            if (!list_inventory.IsDisposed && controlLoaded)
                                Invoke((Action) (() => list_inventory.EmptyListMsg = "Loading..."));
                        }
                        else
                        {
                            if (!list_inventory.IsDisposed && controlLoaded && !this.IsDisposed)
                            {
                                Invoke((Action) (() => list_inventory.EmptyListMsg = "Empty inventory."));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    if (bot.CurrentTrade == null)
                    {
                        bot.main.Invoke((Action)(this.Close));
                        bot.log.Warn("Trade expired.");
                        if (Friends.chat_opened)
                        {
                            bot.main.Invoke((Action)(() =>
                            {
                                foreach (TabPage tab in Friends.chat.ChatTabControl.TabPages)
                                {
                                    if (tab.Text == bot.SteamFriends.GetFriendPersonaName(sid))
                                    {
                                        tab.Invoke((Action)(() =>
                                        {
                                            foreach (var item in tab.Controls)
                                            {
                                                Friends.chat.chatTab = (ChatTab)item;
                                            }
                                            string result = "The trade session has closed.";
                                            bot.log.Warn(result);
                                            string date = "[" + DateTime.Now + "] ";
                                            Friends.chat.chatTab.UpdateChat("[" + DateTime.Now + "] " + result + "\r\n", false);
                                            ChatTab.AppendLog(sid, "===========[TRADE ENDED]===========\r\n");
                                        }));
                                        break; ;
                                    }
                                }

                            }));
                        }
                        break;
                    }
                    Thread.Sleep(100);
                }
            });
            checkExpired.Start();
        }

        public void UpdateChat(string text)
        {
            // If the current thread is not the UI thread, InvokeRequired will be true
            if (text_log.InvokeRequired)
            {
                // If so, call Invoke, passing it a lambda expression which calls
                // UpdateText with the same label and text, but on the UI thread instead.
                text_log.Invoke((Action)(() => UpdateChat(text)));
                return;
            }
            // If we're running on the UI thread, we'll get here, and can safely update 
            // the label's text.
            text_log.AppendText(text);
            text_log.ScrollToCaret();
            if (!focused)
            {
                try
                {
                    string soundsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
                    string soundFile = Path.Combine(soundsFolder + "trade_message.wav");
                    using (System.Media.SoundPlayer player = new System.Media.SoundPlayer(soundFile))
                    {
                        player.Play();
                    }
                }
                catch (Exception ex)
                {
                    bot.log.Error(ex.ToString());
                }
                FlashWindow.Flash(this);
            }
        }

        public void UpdateLabel(string text)
        {
            if (label_othervalue.InvokeRequired)
            {
                label_othervalue.Invoke((Action)(() => UpdateLabel(text)));
                return;
            }
            label_othervalue.Text = text;
        }

        private void label_cancel_MouseEnter(object sender, EventArgs e)
        {
            if (metroStyleManager1.Theme == MetroFramework.MetroThemeStyle.Dark)
            {
                label_cancel.ForeColor = Color.WhiteSmoke;
            }
            else
            {
                label_cancel.ForeColor = SystemColors.ControlText;
            }
        }

        private void label_cancel_MouseLeave(object sender, EventArgs e)
        {
            label_cancel.ForeColor = SystemColors.ControlDarkDark;
        }

        private void label_cancel_Click(object sender, EventArgs e)
        {            
            try
            {
                bot.CurrentTrade.CancelTrade();
                ClearAll();
                Thread.Sleep(2000);
                this.Dispose();
            }
            catch (Exception ex)
            {
                bot.log.Error(ex.ToString());
                ClearAll();
                Thread.Sleep(2000);
                this.Dispose();
            }
        }

        private void text_input_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 1)
            {
                text_input.SelectAll();
            }
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (text_input.Text != "")
                {
                    e.Handled = true;                
                    bot.CurrentTrade.SendMessage(text_input.Text);
                    text_log.AppendText(Bot.displayName + ": " + text_input.Text + " [" + DateTime.Now.ToLongTimeString() + "]\r\n");
                    text_log.ScrollToCaret();
                    ChatTab.AppendLog(sid, "[Trade Chat] " + Bot.displayName + ": " + text_input.Text + " [" + DateTime.Now.ToLongTimeString() + "]\r\n");
                    clear();
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        public void ResetTradeStatus()
        {
            this.check_userready.Checked = false;
            this.check_otherready.Checked = false;
            this.button_accept.Enabled = false;
            button_accept.Enabled = false;
            button_accept.Highlight = false;
            button_accept.Text = "Accept Trade";
            accepted = false;
            if (acceptTrade != null)
            {
                if (acceptTrade.IsAlive)
                    acceptTrade.Abort();
            }
        }

        public void AppendText(string message)
        {
            text_log.AppendText(message + " [" + DateTime.Now.ToLongTimeString() + "]\r\n");
            text_log.ScrollToCaret();
        }

        public void AppendText(string message, string itemName)
        {
            Color prevColor = text_log.SelectionColor;
            text_log.AppendText(message);            
            if (itemName.Contains("Strange"))
            {
                text_log.SelectionColor = ColorTranslator.FromHtml("#CF6A32");
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            else if (itemName.Contains("Vintage"))
            {
                text_log.SelectionColor = ColorTranslator.FromHtml("#476291");
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            else if (itemName.Contains("Unusual"))
            {
                text_log.SelectionColor = ColorTranslator.FromHtml("#8650AC");
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            else if (itemName.Contains("Geniune"))
            {
                text_log.SelectionColor = ColorTranslator.FromHtml("#4D7455");
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            else if (itemName.Contains("Haunted"))
            {
                text_log.SelectionColor = ColorTranslator.FromHtml("#38F3AB");
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            else if (itemName.Contains("Community") || itemName.Contains("Self-Made"))
            {
                text_log.SelectionColor = ColorTranslator.FromHtml("#70B04A");
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            else if (itemName.Contains("Valve"))
            {
                text_log.SelectionColor = ColorTranslator.FromHtml("#A50F79");
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            else
            {
                text_log.SelectionColor = ColorTranslator.FromHtml(SteamTrade.Trade.CurrentItemsGame.GetRarityColorFromName(itemName));
                text_log.AppendText(itemName);
                text_log.SelectionColor = prevColor;
            }
            text_log.AppendText(" [" + DateTime.Now.ToLongTimeString() + "]\r\n");
            text_log.ScrollToCaret();
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            if (text_input.Text != "")
            {
                bot.CurrentTrade.SendMessage(text_input.Text);
                text_log.AppendText(Bot.displayName + ": " + text_input.Text + " [" + DateTime.Now.ToLongTimeString() + "]\r\n");
                text_log.ScrollToCaret();
                ChatTab.AppendLog(sid, "[Trade Chat] " + Bot.displayName + ": " + text_input.Text + " [" + DateTime.Now.ToLongTimeString() + "]\r\n");
                clear();
            }
        }

        void clear()
        {
            text_input.Select(0, 0);
            text_input.Clear();
        }

        private void button_accept_Click(object sender, EventArgs e)
        {
            accepted = true;
            button_accept.Enabled = false;
            button_accept.Highlight = false;
            button_accept.Text = "Waiting for other user...";
            Thread.Sleep(500);
            acceptTrade = new Thread(() =>
            {
                while (!bot.otherAccepted)
                {

                }
                bool success = false;
                for (int count = 0; count < 5; count++)
                {
                    try
                    {
                        success = tradeCompleted = bot.CurrentTrade.AcceptTrade();
                    }
                    catch
                    {

                    }
                    if (success)
                        break;
                    else
                        Thread.Sleep(250);
                }
                if (Friends.chat_opened)
                {
                    bot.main.Invoke((Action)(() =>
                    {
                        foreach (TabPage tab in Friends.chat.ChatTabControl.TabPages)
                        {
                            if (tab.Text == bot.SteamFriends.GetFriendPersonaName(sid))
                            {
                                tab.Invoke((Action)(() =>
                                {
                                    foreach (var item in tab.Controls)
                                    {
                                        Friends.chat.chatTab = (ChatTab)item;
                                    }
                                    if (success)
                                    {
                                        string result = String.Format("Trade completed successfully with {0}!", bot.SteamFriends.GetFriendPersonaName(sid));
                                        bot.log.Success(result);
                                        Friends.chat.chatTab.UpdateChat("[" + DateTime.Now + "] " + result + "\r\n", false);
                                    }
                                    else
                                    {
                                        string result = "The trade may have failed.";
                                        bot.log.Warn(result);
                                        Friends.chat.chatTab.UpdateChat("[" + DateTime.Now + "] " + result + "\r\n", false);
                                    }
                                }));
                                break; ;
                            }
                        }

                    }));
                }
            });
            acceptTrade.Start();
        }

        private void list_inventory_ItemActivate(object sender, EventArgs e)
        {
            try
            {
                ulong itemID = Convert.ToUInt64(column_id.GetValue(list_inventory.SelectedItem.RowObject));
                if (itemID != 0)
                {
                    try
                    {
                        var itemName = list_inventory.SelectedItem.Text.Trim();
                        bool valid = false;
                        Inventory.Item invItem = null;
                        bot.GetInventory();
                        foreach (var item in bot.MyInventory.Items)
                        {
                            if (item.Id == itemID)
                            {
                                invItem = item;
                                valid = true;
                                break;
                            }
                        }
                        if (valid)
                        {
                            try
                            {
                                Color prevColor = text_log.SelectionColor;
                                bot.CurrentTrade.AddItem(itemID);
                                itemsAdded++;
                                if (itemsAdded > 0)
                                {
                                    check_userready.Enabled = true;                                    
                                }
                                text_log.AppendText("You added: ");
                                if (itemName.Contains("Strange"))
                                {
                                    text_log.SelectionColor = ColorTranslator.FromHtml("#CF6A32");
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                else if (itemName.Contains("Vintage"))
                                {
                                    text_log.SelectionColor = ColorTranslator.FromHtml("#476291");
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                else if (itemName.Contains("Unusual"))
                                {
                                    text_log.SelectionColor = ColorTranslator.FromHtml("#8650AC");
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                else if (itemName.Contains("Geniune"))
                                {
                                    text_log.SelectionColor = ColorTranslator.FromHtml("#4D7455");
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                else if (itemName.Contains("Haunted"))
                                {
                                    text_log.SelectionColor = ColorTranslator.FromHtml("#38F3AB");
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                else if (itemName.Contains("Community") || itemName.Contains("Self-Made"))
                                {
                                    text_log.SelectionColor = ColorTranslator.FromHtml("#70B04A");
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                else if (itemName.Contains("Valve"))
                                {
                                    text_log.SelectionColor = ColorTranslator.FromHtml("#A50F79");
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                else
                                {
                                    text_log.SelectionColor =
                                        ColorTranslator.FromHtml(
                                            SteamTrade.Trade.CurrentItemsGame.GetRarityColorFromName(itemName));
                                    text_log.AppendText(itemName);
                                    text_log.SelectionColor = prevColor;
                                }
                                text_log.AppendText(" [" + DateTime.Now.ToLongTimeString() + "]\r\n");
                                text_log.ScrollToCaret();
                                ResetTradeStatus();
                                list_inventory.SelectedItem.Remove();
                                ListUserOfferings.Add(itemName, itemID, invItem);
                                ListInventory.Remove(itemName, itemID);
                                list_userofferings.SetObjects(ListUserOfferings.Get());
                                var count = ListUserOfferings.Get().Count(x => x.Item.Defindex == invItem.Defindex);
                                AppendText(string.Format("Current count of {0}: {1}", Trade.CurrentSchema.GetItem(invItem.Defindex).ItemName, count));
                            }
                            catch (SteamTrade.Exceptions.TradeException ex)
                            {
                                bot.main.Invoke((Action)(() =>
                                {
                                    bot.log.Error(ex.ToString());
                                    MessageBox.Show(ex + "\nYou can ignore this error. Just restart the trade. Sorry about that :(",
                                        "Trade Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error,
                                        MessageBoxDefaultButton.Button1);
                                }));
                            }
                        }
                        else
                        {
                            bot.log.Warn("Invalid item, skipping");
                        }
                    }
                    catch (Exception ex)
                    {
                        bot.log.Error(ex.ToString());
                        bot.main.Invoke((Action)(() =>
                        {
                            MessageBox.Show("\nSomething weird happened. Here's the error:\n" + ex,
                                        "Trade Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error,
                                        MessageBoxDefaultButton.Button1);
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                bot.log.Error(ex.ToString());
            }
        }

        public static void ClearAll()
        {
            ListInventory.Clear();
            ListUserOfferings.Clear();
            ListOtherOfferings.Clear();
        }

        private void addAllItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Don't click Yes unless you haven't added any items to the trade yet. Are you sure you wish to continue?", "WARNING: Experimental Feature", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                bot.GetInventory();
                foreach (var item in bot.MyInventory.Items)
                {
                    if (!item.IsNotTradeable)
                    {
                        var currentItem = SteamTrade.Trade.CurrentSchema.GetItem(item.Defindex);
                        var itemName = GetItemName(currentItem, item);
                        bot.log.Info("Adding " + itemName + ", " + item.Id);
                        try
                        {
                            bot.CurrentTrade.AddItem(item.Id);
                            itemsAdded++;
                            if (itemsAdded > 0)
                            {
                                check_userready.Enabled = true;
                            }
                            Color prevColor = text_log.SelectionColor;
                            text_log.AppendText("You added: ");
                            if (itemName.Contains("Strange"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#CF6A32");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Vintage"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#476291");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Unusual"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#8650AC");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Geniune"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#4D7455");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Haunted"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#38F3AB");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Community") || itemName.Contains("Self-Made"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#70B04A");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Valve"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#A50F79");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else
                            {
                                text_log.SelectionColor =
                                        ColorTranslator.FromHtml(
                                            SteamTrade.Trade.CurrentItemsGame.GetRarityColorFromName(itemName));
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            text_log.AppendText(" [" + DateTime.Now.ToLongTimeString() + "]\r\n");
                            text_log.ScrollToCaret();
                            ResetTradeStatus();
                            ListUserOfferings.Add(itemName, item.Id, item);
                            ListInventory.Remove(itemName, item.Id);
                            list_userofferings.SetObjects(ListUserOfferings.Get());
                            list_inventory.SetObjects(ListInventory.Get());
                            var count = ListUserOfferings.Get().Count(x => x.Item.Defindex == item.Defindex);
                            AppendText(string.Format("Current count of {0}: {1}", Trade.CurrentSchema.GetItem(item.Defindex).ItemName, count));
                        }
                        catch (Exception ex)
                        {
                            bot.log.Error(ex.ToString());
                        }
                    }
                }
                bot.log.Info("Done adding all items!");
            }
            else if (dialogResult == DialogResult.No)
            {
                return;
            }
        }
         
        string GetItemName(SteamTrade.Schema.Item schemaItem, SteamTrade.Inventory.Item inventoryItem, bool id = false)
        {
            var currentItem = SteamTrade.Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            string name = "";
            var type = Convert.ToInt32(inventoryItem.Quality.ToString());
            if (Util.QualityToName(type) != "Unique")
                name += Util.QualityToName(type) + " ";
            name += currentItem.ItemName;
            name += " (" + SteamTrade.Trade.CurrentItemsGame.GetItemRarity(schemaItem.Defindex.ToString()) + ")";

            if (currentItem.CraftMaterialType == "supply_crate")
            {
                for (int count = 0; count < inventoryItem.Attributes.Length; count++)
                {
                    name += " #" + (inventoryItem.Attributes[count].FloatValue);
                }
            }
            try
            {
                int size = inventoryItem.Attributes.Length;
                for (int count = 0; count < size; count++)
                {
                    if (inventoryItem.Attributes[count].Defindex == 186)
                    {
                        name += " (Gifted)";
                    }
                }
            }
            catch
            {
                // Item has no attributes... or something.
            }
            if (currentItem.Name == "Wrapped Gift")
            {
                // Untested!
                try
                {
                    int size = inventoryItem.Attributes.Length;
                    for (int count = 0; count < size; count++)
                    {
                        var containedItem = SteamTrade.Trade.CurrentSchema.GetItem(inventoryItem.ContainedItem.Defindex);
                        name += " (Contains: " + containedItem.ItemName + ")";
                    }
                }
                catch
                {
                    // Item has no attributes... or something.
                }
            }
            if (!string.IsNullOrWhiteSpace(inventoryItem.CustomName))
                name += " (Custom Name: " + inventoryItem.CustomName + ")";
            if (!string.IsNullOrWhiteSpace(inventoryItem.CustomDescription))
                name += " (Custom Desc.: " + inventoryItem.CustomDescription + ")";
            if (id)
                name += " :" + inventoryItem.Id;
            return name;
        }

        private void list_userofferings_ItemActivate(object sender, EventArgs e)
        {
            ulong itemID = Convert.ToUInt64(column_uo_id.GetValue(list_userofferings.SelectedItem.RowObject));
            if (itemID != 0)
            {
                try
                {
                    var itemName = list_userofferings.SelectedItem.Text.Trim();
                    bool valid = false;
                    string img = "";
                    bot.GetInventory();
                    foreach (var item in bot.MyInventory.Items)
                    {
                        if (item.Id == itemID)
                        {
                            valid = true;
                            img = SteamTrade.Trade.CurrentSchema.GetItem(item.Defindex).ImageURL;
                        }
                    }
                    if (valid)
                    {
                        try
                        {
                            bot.CurrentTrade.RemoveItem(itemID);
                            itemsAdded--;
                            if (itemsAdded < 1)
                            {
                                check_userready.Enabled = true;
                            }
                            Color prevColor = text_log.SelectionColor;
                            text_log.AppendText("You removed: ");
                            if (itemName.Contains("Strange"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#CF6A32");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Vintage"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#476291");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Unusual"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#8650AC");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Geniune"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#4D7455");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Haunted"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#38F3AB");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Community") || itemName.Contains("Self-Made"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#70B04A");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else if (itemName.Contains("Valve"))
                            {
                                text_log.SelectionColor = ColorTranslator.FromHtml("#A50F79");
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            else
                            {
                                text_log.SelectionColor =
                                        ColorTranslator.FromHtml(
                                            SteamTrade.Trade.CurrentItemsGame.GetRarityColorFromName(itemName));
                                text_log.AppendText(itemName);
                                text_log.SelectionColor = prevColor;
                            }
                            text_log.AppendText(" [" + DateTime.Now.ToLongTimeString() + "]\r\n");
                            text_log.ScrollToCaret();
                            ResetTradeStatus();
                            list_userofferings.SelectedItem.Remove();
                            ListInventory.Add(itemName, itemID, img);
                            ListUserOfferings.Remove(itemName, itemID);
                            //list_inventory.SetObjects(ListInventory.Get());
                            list_userofferings.SetObjects(ListUserOfferings.Get());
                        }
                        catch (SteamTrade.Exceptions.TradeException ex)
                        {
                            bot.log.Error(ex.ToString());
                        }
                    }
                    else
                    {
                        bot.log.Warn("Invalid item, skipping");
                    }
                }
                catch (Exception ex)
                {
                    bot.log.Error(ex.ToString());
                }
            }
        }

        private void check_userready_CheckedChanged(object sender, EventArgs e)
        {
            bool Checked = check_userready.Checked;
            if (Checked)
                AppendText("You are ready.");
            else
                AppendText("You are not ready.");
            bot.CurrentTrade.SetReady(Checked);
            if (Checked && check_otherready.Checked)
            {
                button_accept.Enabled = true;
                button_accept.Highlight = true;
            }
        }

        private void ShowTrade_Activated(object sender, EventArgs e)
        {
            focused = true;
        }

        private void ShowTrade_Deactivate(object sender, EventArgs e)
        {
            focused = false;
        }

        private void ShowTrade_Load(object sender, EventArgs e)
        {
            label_yourvalue.Visible = false;
            label_othervalue.Visible = false;
            focused = false;
            ToolTip priceTip = new ToolTip();
            priceTip.ToolTipIcon = ToolTipIcon.Info;
            priceTip.IsBalloon = true;
            priceTip.ShowAlways = true;
            priceTip.ToolTipTitle = "Item prices are from backpack.tf";
            string caution = "What the price checker doesn't do:\n-Factor in the cost of paint\n-Factor in the cost of strange parts\n-Calculate values of low craft numbers\nPrices are not guaranteed to be accurate.";
            priceTip.SetToolTip(label_yourvalue, caution);
            priceTip.SetToolTip(label_othervalue, caution);
            controlLoaded = true;
        }

        public static string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.LastIndexOf(Find);
            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }

        private void disableGroupingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool checkedState = disableGroupingToolStripMenuItem.Checked;
            list_inventory.ShowGroups = !checkedState;
            list_userofferings.ShowGroups = !checkedState;
            list_otherofferings.ShowGroups = !checkedState;
        }

        private void disableItemGroupingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool checkedState = disableItemGroupingToolStripMenuItem.Checked;
            list_inventory.ShowGroups = !checkedState;
            list_userofferings.ShowGroups = !checkedState;
            list_otherofferings.ShowGroups = !checkedState;
        }

        private void ShowTrade_FormClosed(object sender, FormClosedEventArgs e)
        {
            ClearAll();
            if (bot.CurrentTrade != null && !tradeCompleted)
            {
                bot.CurrentTrade.CancelTrade();
            }
        }

        private void text_search_Enter(object sender, EventArgs e)
        {
            text_search.Clear();
            text_search.ForeColor = SystemColors.WindowText;
            text_search.Font = new Font(text_search.Font, FontStyle.Regular);
        }

        private void text_search_Leave(object sender, EventArgs e)
        {
            if (text_search.Text == null)
            {
                this.list_inventory.SetObjects(ListInventory.Get());
                text_search.ForeColor = Color.Gray;
                text_search.Font = new Font(text_search.Font, FontStyle.Italic);
                text_search.Text = "Search for an item in your inventory...";
            }
            if (this.list_inventory.Columns == null)
                this.list_inventory.SetObjects(ListInventory.Get());
        }

        private void text_search_TextChanged(object sender, EventArgs e)
        {
            if (text_search.Text == "")
                this.list_inventory.SetObjects(ListInventory.Get());
            else
                this.list_inventory.SetObjects(ListInventory.Get(text_search.Text));
        }

        private void text_search_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                text_search.Clear();
                this.list_inventory.SetObjects(ListInventory.Get());
            }
        }

        private string GetTooltipText(Inventory.Item item)
        {
            var text = "";
            var schemaitem = Trade.CurrentSchema.GetItem(item.Defindex);
            var itemname = (Util.QualityToName(int.Parse(item.Quality)) == "" ||
                Util.QualityToName(int.Parse(item.Quality)) == "Unique"
                ? ""
                : Util.QualityToName(int.Parse(item.Quality)) + " ")
                           + schemaitem.ItemName;
            var name = string.IsNullOrWhiteSpace(item.CustomName)
                           ? itemname
                           : string.Format("\"{0}\" ({1})", item.CustomName, itemname);
            var type = (Util.QualityToName(int.Parse(item.Quality)) == "" ||
                Util.QualityToName(int.Parse(item.Quality)) == "Unique"
                ? ""
                : Util.QualityToName(int.Parse(item.Quality)) + " ") +
                       (Trade.CurrentItemsGame.GetItemRarity(item.Defindex.ToString())) + " " + schemaitem.ItemTypeName;
            var desc = string.IsNullOrWhiteSpace(item.CustomDescription)
                           ? schemaitem.ItemDescription
                           : string.Format("\"{0}\" ({1})", item.CustomDescription, schemaitem.ItemDescription);
            text += string.Format(@"{0} | ", type);
            if (item.Attributes != null)
            {
                foreach (var attribute in item.Attributes)
                {
                    var attribname = Trade.CurrentSchema.GetAttributeName(attribute.Defindex, item.Attributes,
                        attribute.FloatValue != null ? attribute.FloatValue : 0f,
                        attribute.Value ?? "");
                    if (attribname != "")
                    {
                        text += string.Format(@"{0} | ", attribname);
                    }
                }
            }
            if (item.Style != null)
            {
                text += string.Format(@"Style: {0}",
                    Trade.CurrentSchema.GetStyle(item.Defindex, (int)item.Style));
            }
            text = text.TrimEnd(new[] {'|', ' '});
            return text;
        }

        private void list_otherofferings_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
        {
            /*if (e.HitTest == null || e.HitTest.RowObject == null) return;
            var itemId = (ulong)column_id.GetValue(e.HitTest.RowObject);
            if (itemId == 0) return;
            foreach (var item in ListOtherOfferings.Get())
            {
                if (item.ItemID == itemId)
                {
                    e.Text = GetTooltipText(item.Item);
                    break;
                }
            }*/
        }

        private void list_inventory_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
        {
            /*if (e.HitTest == null || e.HitTest.RowObject == null) return;
            var itemId = (ulong)column_id.GetValue(e.HitTest.RowObject);
            bot.GetInventory();
            foreach (var item in bot.MyInventory.Items)
            {
                if (item.Id == itemId)
                {
                    e.Text = GetTooltipText(item);
                    break;
                }
            }*/
        }
    }
}
