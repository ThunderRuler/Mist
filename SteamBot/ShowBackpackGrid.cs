using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MetroFramework;
using MetroFramework.Controls;
using MetroFramework.Forms;
using MistClient.Dota2GC;
using SteamBot;
using SteamKit2;
using SteamTrade;
using HtmlRenderer;

namespace MistClient
{
    public partial class ShowBackpackGrid : MetroForm
    {
        public class TileTag
        {
            public string ImageUrl;
            public Inventory.Item Item;
            public bool Selected;
            public string TooltipText;
        }

        Bot bot;
        SteamID SID;
        Thread loadBP;
        private Dictionary<int, Inventory.Item> ItemList = new Dictionary<int, Inventory.Item>();
        private List<Inventory.Item> MisplacedItemList = new List<Inventory.Item>(); 
        private int pageNum = 1;
        private Dictionary<string, Bitmap> ImageCache = new Dictionary<string, Bitmap>();
        private HtmlToolTip ttItem = new HtmlToolTip();
        private int maxPage;
        private bool Self;
        private List<Inventory.Item> selectedItems = new List<Inventory.Item>();
        private MetroTile currentTile;

        public ShowBackpackGrid(Bot bot, SteamID SID)
        {
            InitializeComponent();
            this.bot = bot;
            this.SID = SID;
            if (SID == bot.SteamUser.SteamID)
            {
                Self = true;
                chkManage.Visible = true;
            }
            this.Text = bot.SteamFriends.GetFriendPersonaName(SID) + "'s Backpack";
            Util.LoadTheme(metroStyleManager1);
            lnkPage.Text = pageNum.ToString();
            ttItem.AllowLinksHandling = false;
            ttItem.AutomaticDelay = 0;
            ttItem.BaseStylesheet = 
@".htmltooltip {
    border:solid 1px #767676;
    background-color:#464646;
    background-gradient:#121212;
    padding: 8px; 
    Font: 11pt Tahoma;
    color: #999;
    width: 300px;
}
.name {
    Font: 17pt Tahoma;
    color: #fff;
}
.type {
    color: #b0c0d0;
}
.effect {
    color: #fff;
}";
        }

        private void ShowBackpackGrid_Load(object sender, EventArgs e)
        {
            Invoke((Action)(() =>
            {
                loadBP = new Thread(LoadBP);
                loadBP.Start(false);
            }));
        }

        void LoadBP(object misplacedobj)
        {
            Invoke((Action)(() =>
            {
                metroProgressSpinner1.Size = new Size(970, 666);
            }));
            var misplaced = (bool) misplacedobj;
            ListBackpack.Clear();
            bot.GetOtherInventory(SID);
            Inventory.Item[] inventory = bot.OtherInventory.Items;
            if (inventory == null)
            {
                bot.main.Invoke((Action)(() =>
                {
                    this.Text += " - Could not retrieve backpack contents. Backpack is likely private.";
                    this.metroProgressSpinner1.Spinning = false;
                }));
                return;
            }
            foreach (Inventory.Item item in inventory)
            {
                if (item.ItemPosition != -1 && !ItemList.ContainsKey(item.ItemPosition))
                    ItemList.Add(item.ItemPosition, item);
                if (item.ItemPosition == -1)
                    MisplacedItemList.Add(item);
            }
            maxPage = (int)Math.Ceiling(((double)bot.OtherInventory.NumSlots / 64));
            UpdateBP(misplaced);
        }

        void UpdateBP(object misplacedobj)
        {
            var misplaced = (bool) misplacedobj;
            Invoke((Action) (() => lnkPage.Text = pageNum.ToString()));
            Invoke((Action)(() =>
            {
                metroProgressSpinner1.Size = new Size(970, 666);
            }));
            var h = 0;
            for (var i = (1 + (64*(pageNum-1))); i <= (64*(pageNum)); i++, h++)
            {
                Inventory.Item invitem;
                if (misplaced)
                {
                    if (MisplacedItemList.Count >= i)
                        invitem = MisplacedItemList[i - 1];
                    else
                        break;
                }
                else
                {
                    if (!ItemList.ContainsKey(i)) continue;
                    invitem = ItemList[i];
                }
                var currentItem = Trade.CurrentSchema.GetItem(invitem.Defindex);
                var img = getImageFromURL(currentItem.ImageURL);
                var selected = false;
                if (selectedItems.Contains(invitem))
                {
                    selected = true;
                    var img2 = new Bitmap(img);
                    using (var g = Graphics.FromImage(img2))
                    {
                        g.DrawRectangle(new Pen(Brushes.DarkRed, 3), new Rectangle(0, 0, img2.Width, img2.Height));
                    }
                    img = img2;
                }
                Invoke((Action) (() =>
                                     {
                                         var tile = (MetroTile)Controls.Find("metroTile" + (64 - h), true)[0];
                                         if (img != null)
                                         {
                                             tile.TileImage = img;
                                             tile.UseTileImage = true;
                                         }
                                         tile.Tag = new TileTag
                                                        {
                                                            ImageUrl = currentItem.ImageURL,
                                                            Item = invitem,
                                                            TooltipText = GetTooltipText(invitem),
                                                            Selected = selected
                                                        };
                                         tile.Text = GetItemName(currentItem, invitem);
                                         tile.ForeColor =
                                                 ColorTranslator.FromHtml(
                                                     Trade.CurrentItemsGame.GetRarityColor(
                                                         Trade.CurrentItemsGame.GetItemRarity(
                                                             currentItem.Defindex.ToString())));
                                         tile.CustomForeColor = true;
                                         tile.TileTextFontSize = MetroTileTextSize.Small;
                                     }));
            }
            Invoke((Action) (() =>
                                 {
                                     metroProgressSpinner1.Size = new Size(0, 0);
                                 }));
        }

        private void ClearBP(bool clearlist = false)
        {
            if (clearlist)
            {
                ItemList.Clear();
                MisplacedItemList.Clear();
            }
            for (var i = 1; i <= 64; i++)
            {
                var tile = (MetroTile) Controls.Find("metroTile" + i, true)[0];
                tile.TileImage = new Bitmap(1, 1);
                tile.UseTileImage = false;
                tile.CustomForeColor = false;
                tile.Text = "";
                tile.Tag = null;
            }
            lblItemStatus.Text = "";
        }

        string GetItemName(Schema.Item schemaItem, Inventory.Item inventoryItem, bool id = false)
        {
            var currentItem = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            string name = "";
            var type = Convert.ToInt32(inventoryItem.Quality.ToString());
            if (Util.QualityToName(type) != "Unique")
                name += Util.QualityToName(type) + " ";
            name += string.IsNullOrWhiteSpace(inventoryItem.CustomName) ? currentItem.ItemName : "\"" + inventoryItem.CustomName + "\"";
            if (id)
                name += " :" + inventoryItem.Id;
            return name;
        }

        public Bitmap getImageFromURL(string url)
        {
            System.Drawing.Bitmap bmp;
            var cache = Path.Combine("cache", Path.GetFileName(new Uri(url).LocalPath));
            if (ImageCache.ContainsKey(url))
            {
                bmp = ImageCache[url];
            }
            else if (File.Exists(cache))
            {
                bmp = new Bitmap(cache);
                ImageCache.Add(url, bmp);
            }
            else
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Method = "GET";
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                var newbmp = new System.Drawing.Bitmap(myResponse.GetResponseStream());
                myResponse.Close();
                bmp = ResizeImage(newbmp, new Size(116, 78));
                bmp.Save(cache);
                ImageCache.Add(url, bmp);
            }

            return bmp;
        }

        private Bitmap ResizeImage(Bitmap imgToResize, Size size)
        {
            try
            {
                Bitmap b = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage((Image)b))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
                }
                return b;
            }
            catch { }
            return null;
        }

        private void ShowBackpackGrid_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                loadBP.Abort();
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (pageNum == 1) return;
            pageNum--;
            metroProgressSpinner1.Size = new Size(970, 666);
            ClearBP();
            try
            {
                loadBP.Abort();
                Invoke((Action) (() =>
                                     {
                                         loadBP = new Thread(UpdateBP);
                                         loadBP.Start(chkMisplaced.Checked);
                                     }));
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (pageNum == maxPage) return;
            pageNum++;
            metroProgressSpinner1.Size = new Size(970, 666);
            ClearBP();
            try
            {
                loadBP.Abort();
                Invoke((Action)(() =>
                {
                    loadBP = new Thread(UpdateBP);
                    loadBP.Start(chkMisplaced.Checked);
                }));
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }

        private void chkMisplaced_CheckedChanged(object sender, EventArgs e)
        {
            metroProgressSpinner1.Size = new Size(970, 666);
            ClearBP(true);
            try
            {
                loadBP.Abort();
                Invoke((Action)(() =>
                {
                    loadBP = new Thread(LoadBP);
                    loadBP.Start(chkMisplaced.Checked);
                }));
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }

        private void metroTile_MouseEnter(object sender, EventArgs e)
        {
            var tile = (MetroTile) sender;
            var oldbmp = tile.TileImage;
            var bmp = new Bitmap(oldbmp);
            var tag = (TileTag) tile.Tag;
            if (tag == null) return;
            if (bmp.Size == new Size(116, 78) && !((TileTag)tile.Tag).Selected)
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawRectangle(new Pen(Brushes.DarkRed, 3), new Rectangle(0, 0, bmp.Width, bmp.Height));
                }
            }
            tile.TileImage = bmp;
            if (ttItem.Tag == tile) return;
            ttItem.Tag = tile;
            ttItem.Show(tag.TooltipText, tile);
        }

        private void metroTile_MouseLeave(object sender, EventArgs e)
        {
            var tile = (MetroTile)sender;
            if (tile.Tag == null) return;
            var bmp = tile.TileImage;
            if (bmp.Size == new Size(116, 78) && !((TileTag)tile.Tag).Selected)
            {
                tile.TileImage = getImageFromURL(((TileTag) tile.Tag).ImageUrl);
            }
            //if ((DateTime.UtcNow - LastPopup).TotalMilliseconds < 1000) return;
            if (ttItem.Tag == tile) return;
            ttItem.Hide(tile);
        }

        private void metroTile_Click(object sender, EventArgs e)
        {
            if (!Self || !chkManage.Checked) return;
            var tile = (MetroTile)sender;
            if (tile.Tag == null) return;
            var tag = (TileTag) tile.Tag;
            tag.Selected = !tag.Selected;
            if (!tag.Selected)
            {
                selectedItems.Remove(tag.Item);
                return;
            }
            selectedItems.Add(tag.Item);
            var oldbmp = tile.TileImage;
            var bmp = new Bitmap(oldbmp);
            if (bmp.Size == new Size(116, 78) && !((TileTag)tile.Tag).Selected)
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawRectangle(new Pen(Brushes.DarkRed, 3), new Rectangle(0, 0, bmp.Width, bmp.Height));
                }
            }
            tile.TileImage = bmp;
        }

        private void metroTile_MouseMove(object sender, MouseEventArgs e)
        {
            /*var tile = (MetroTile)sender;
            if (tile.Tag == null) return;
            var tag = (TileTag)tile.Tag;
            if (tag.Item == null) return;
            var item = tag.Item;
            if (item == null) return;*/
        }

        private void metroTile_MouseUp(object sender, MouseEventArgs e)
        {
            if (!e.Button.HasFlag(MouseButtons.Right)) return;
            var tile = (MetroTile)sender;
            btnMoveItems.Enabled = true;
            if (tile.Tag != null)
            {
                var tag = (TileTag) tile.Tag;
                if (tag.Item != null)
                {
                    btnMoveItems.Enabled = false;
                }
            }
            if (chkMisplaced.Checked || !chkManage.Checked || selectedItems.Count == 0) 
                btnMoveItems.Enabled = false;
            currentTile = tile;
            ctxItem.Show(tile, e.Location);
        }

        private string GetTooltipText(Inventory.Item item)
        {
            var text = "<div align=\"center\">";
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
            text += string.Format(@"<span class=""name"" style=""color:{0}"">{1}</span><br>",
                Util.GetQualityColor(item.Quality), name);
            text += string.Format(@"<span class=""type"" style=""color:{0}"">{1}</span><br>",
                Trade.CurrentItemsGame.GetRarityColor(Trade.CurrentItemsGame.GetItemRarity(item.Defindex.ToString())), type);
            if (item.Attributes != null)
            {
                foreach (var attribute in item.Attributes)
                {
                    var attribname = Trade.CurrentSchema.GetAttributeName(attribute.Defindex, item.Attributes,
                        attribute.FloatValue != null ? attribute.FloatValue : 0f,
                        attribute.Value ?? "");
                    if (attribname != "")
                    {
                        text += string.Format(@"<span class=""effect"">{0}</span><br>", attribname);
                    }
                }
            }
            if (item.Style != null)
            {
                text += string.Format(@"<span class=""effect"">Style: {0}</span><br>",
                    Trade.CurrentSchema.GetStyle(item.Defindex, (int)item.Style));
            }
            text += string.Format(@"<span class=""description"">{0}</span>", desc);
            return text;
        }

        private void chkManage_CheckedChanged(object sender, EventArgs e)
        {
            btnTakeAll.Visible = btnDeselect.Visible = chkManage.Checked;
            if (chkManage.Checked)
                bot.ConnectToGC(570);
            else
                bot.DisconnectFromGC();
        }

        private void btnTakeAll_Click(object sender, EventArgs e)
        {
            metroProgressSpinner1.Size = new Size(970, 666);
            var dict = new Dictionary<uint, uint>();
            foreach (var item in MisplacedItemList)
            {
                if (dict.ContainsKey((uint)item.Id)) continue;
                var pos = 1;
                while (true)
                {
                    if (!ItemList.ContainsKey(pos) && !dict.ContainsValue((uint)pos))
                        break;
                    pos++;
                }
                dict.Add((uint)item.Id, (uint)pos);
            }
            Items.SetItemPositions(bot, dict);
            ClearBP(true);
            try
            {
                loadBP.Abort();
                Invoke((Action)(() =>
                {
                    loadBP = new Thread(LoadBP);
                    loadBP.Start(chkMisplaced.Checked);
                }));
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }

        private void btnDeselect_Click(object sender, EventArgs e)
        {
            selectedItems.Clear();
            try
            {
                loadBP.Abort();
                Invoke((Action)(() =>
                {
                    loadBP = new Thread(UpdateBP);
                    loadBP.Start(chkMisplaced.Checked);
                }));
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }

        private void btnMoveItems_Click(object sender, EventArgs e)
        {
            var slot = (64 - int.Parse(currentTile.Name.Split('e')[2]) + 1) + ((pageNum - 1)*64);
            var dict = new Dictionary<uint, uint>();
            var force = false;
            var h = 0;
            for (int i = 0; i < selectedItems.Count; i++)
            {
                var newslot = slot + i + h;
                if (ItemList.ContainsKey(newslot))
                {
                    if (force)
                    {
                        h++;
                        i--;
                        continue;
                    }
                    else
                    {
                        var response =
                            MessageBox.Show("There is not enough consecutive spaces to move selected items.\r\n" +
                                            "Do you wish to continue?", "Backpack", MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);
                        if (response == DialogResult.Yes)
                        {
                            i--;
                            h++;
                            force = true;
                            continue;
                        }
                        else
                            return;
                    }
                }
                if (!dict.ContainsKey((uint) selectedItems[i].Id))
                    dict.Add((uint) selectedItems[i].Id, (uint) newslot);
            }
            Items.SetItemPositions(bot, dict);
            Thread.Sleep(500);
            ClearBP(true);
            Invoke((Action)(() => lblItemStatus.Text = "Items moved, inventory updating."));
            try
            {
                loadBP.Abort();
                Invoke((Action)(() =>
                {
                    loadBP = new Thread(LoadBP);
                    loadBP.Start(chkMisplaced.Checked);
                }));
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            ClearBP(true);
            try
            {
                loadBP.Abort();
                Invoke((Action)(() =>
                {
                    loadBP = new Thread(LoadBP);
                    loadBP.Start(chkMisplaced.Checked);
                }));
            }
            catch (Exception ex)
            {
                Bot.Print(ex);
            }
        }
    }
}