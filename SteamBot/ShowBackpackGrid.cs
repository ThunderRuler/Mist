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
using SteamBot;
using SteamKit2;
using SteamTrade;

namespace MistClient
{
    public partial class ShowBackpackGrid : MetroForm
    {
        Bot bot;
        SteamID SID;
        Thread loadBP;
        private Dictionary<int, Inventory.Item> ItemList = new Dictionary<int, Inventory.Item>();
        private List<Inventory.Item> MisplacedItemList = new List<Inventory.Item>(); 
        private int pageNum = 1;
        private Dictionary<string, Bitmap> ImageCache = new Dictionary<string, Bitmap>(); 

        public ShowBackpackGrid(Bot bot, SteamID SID)
        {
            InitializeComponent();
            this.bot = bot;
            this.SID = SID;
            this.Text = bot.SteamFriends.GetFriendPersonaName(SID) + "'s Backpack";
            Util.LoadTheme(metroStyleManager1);
            lnkPage.Text = pageNum.ToString();
        }

        private void ShowBackpackGrid_Load(object sender, EventArgs e)
        {
            Invoke((Action)(() =>
            {
                loadBP = new Thread(LoadBP);
                loadBP.Start();
            }));
        }

        void LoadBP()
        {
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
            BackpackTF.CurrentSchema = BackpackTF.FetchSchema();
            foreach (Inventory.Item item in inventory)
            {
                if (item.ItemPosition != -1 && !ItemList.ContainsKey(item.ItemPosition))
                    ItemList.Add(item.ItemPosition, item);
                if (item.ItemPosition == -1)
                    MisplacedItemList.Add(item);
            }
            UpdateBP(false);
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
                Invoke((Action) (() =>
                                     {
                                         var tile = (MetroTile)Controls.Find("metroTile" + (64 - h), true)[0];
                                         if (img != null)
                                         {
                                             tile.TileImage = img;
                                             tile.UseTileImage = true;
                                         }
                                         tile.Tag = new TileTag { ImageUrl = currentItem.ImageURL , Item = invitem};
                                         tile.Text = GetItemName(currentItem, invitem);
                                         tile.ForeColor =
                                                 ColorTranslator.FromHtml(
                                                     Trade.CurrentItemsGame.GetRarityColor(
                                                         Trade.CurrentItemsGame.GetItemRarity(
                                                             currentItem.Defindex.ToString())));
                                         tile.TileTextFontSize = MetroTileTextSize.Small;
                                     }));
            }
            Invoke((Action) (() =>
                                 {
                                     metroProgressSpinner1.Size = new Size(0, 0);
                                 }));
        }

        void ClearBP()
        {
            for (var i = 1; i <= 64; i++)
            {
                var tile = (MetroTile)Controls.Find("metroTile" + i, true)[0];
                tile.TileImage = new Bitmap(1, 1);
                tile.UseTileImage = false;
                tile.Text = "";
            }
        }

        string QualityToName(int quality)
        {
            switch (quality)
            {
                case 1:
                    return "Genuine";
                case 2:
                    return "Vintage";
                case 3:
                    return "Unusual";
                case 4:
                    return "Unique";
                case 5:
                    return "Community";
                case 6:
                    return "Valve";
                case 7:
                    return "Self-Made";
                case 8:
                    return "Customized";
                case 9:
                    return "Strange";
                case 10:
                    return "Completed";
                case 11:
                    return "Haunted";
                case 12:
                    return "Tournament";
                case 13:
                    return "Favored";
                default:
                    return "";
            }
        }

        public static string EffectToName(float effect)
        {
            if (effect == 6)
                return "Green Confetti";
            if (effect == 7)
                return "Purple Confetti";
            if (effect == 8)
                return "Haunted Ghosts";
            if (effect == 9)
                return "Green Energy";
            if (effect == 10)
                return "Purple Energy";
            if (effect == 11)
                return "Circling TF Logo";
            if (effect == 12)
                return "Massed Flies";
            if (effect == 13)
                return "Burning Flames";
            if (effect == 14)
                return "Scorching Flames";
            if (effect == 15)
                return "Searing Plasma";
            if (effect == 16)
                return "Vivid Plasma";
            if (effect == 17)
                return "Sunbeams";
            if (effect == 18)
                return "Circling Peace Sign";
            if (effect == 19)
                return "Circling Heart";
            if (effect == 29)
                return "Stormy Storm";
            if (effect == 30)
                return "Blizzardy Storm";
            if (effect == 31)
                return "Nuts n' Bolts";
            if (effect == 32)
                return "Orbiting Planets";
            if (effect == 33)
                return "Orbiting Fire";
            if (effect == 34)
                return "Bubbling";
            if (effect == 35)
                return "Smoking";
            if (effect == 36)
                return "Steaming";
            if (effect == 37)
                return "Flaming Lantern";
            if (effect == 38)
                return "Cloudy Moon";
            if (effect == 39)
                return "Cauldron Bubbles";
            if (effect == 40)
                return "Eerie Orbiting Fire";
            if (effect == 43)
                return "Knifestorm";
            if (effect == 44)
                return "Misty Skull";
            if (effect == 45)
                return "Harvest Moon";
            if (effect == 46)
                return "It's a Secret to Everybody";
            if (effect == 47)
                return "Stormy 13th Hour";
            return "";
        }

        public static string PaintToName(float color)
        {
            if (color == 3100495)
                return "A Color Similar to Slate";
            if (color == 7511618)
                return "Indubitably Green";
            if (color == 8208497)
                return "A Deep Commitment to Purple";
            if (color == 13595446)
                return "Mann Co. Orange";
            if (color == 1315860)
                return "A Distinctive Lack of Hue";
            if (color == 10843461)
                return "Muskelmannbraun";
            if (color == 12377523)
                return "A Mann's Mint";
            if (color == 5322826)
                return "Noble Hatter's Violet";
            if (color == 2960676)
                return "After Eight";
            if (color == 12955537)
                return "Peculiarly Drab Tincture";
            if (color == 8289918)
                return "Aged Moustache Grey";
            if (color == 16738740)
                return "Pink as Hell";
            if (color == 15132390)
                return "An Extraordinary Abundance of Tinge";
            if (color == 6901050)
                return "Radigan Conagher Brown";
            if (color == 15185211)
                return "Australium Gold";
            if (color == 3329330)
                return "The Bitter Taste of Defeat and Lime";
            if (color == 14204632)
                return "Color No. 216-190-216";
            if (color == 15787660)
                return "The Color of a Gentlemann's Business Pants";
            if (color == 15308410)
                return "Dark Salmon Injustice";
            if (color == 8154199)
                return "Ye Olde Rustic Colour";
            if (color == 8421376)
                return "Drably Olive";
            if (color == 4345659)
                return "Zepheniah's Greed";
            if (color == 6637376 || color == 2636109)
                return "An Air of Debonair";
            if (color == 12073019 || color == 5801378)
                return "Team Spirit";
            if (color == 3874595 || color == 1581885)
                return "Balaclavas Are Forever";
            if (color == 8400928 || color == 2452877)
                return "The Value of Teamwork";
            if (color == 12807213 || color == 12091445)
                return "Cream Spirit";
            if (color == 11049612 || color == 8626083)
                return "Waterlogged Lab Coat";
            if (color == 4732984 || color == 3686984)
                return "Operator's Overalls";
            return "Unknown";
        }

        string GetItemName(Schema.Item schemaItem, Inventory.Item inventoryItem, bool id = false)
        {
            var currentItem = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            string name = "";
            var type = Convert.ToInt32(inventoryItem.Quality.ToString());
            if (QualityToName(type) != "Unique")
                name += QualityToName(type) + " ";
            name += currentItem.ItemName;
            if (QualityToName(type) == "Unusual")
            {
                try
                {
                    for (int count = 0; count < inventoryItem.Attributes.Length; count++)
                    {
                        if (inventoryItem.Attributes[count].Defindex == 134)
                        {
                            name += " (Effect: " + EffectToName(inventoryItem.Attributes[count].FloatValue) + ")";
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
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
                    if (inventoryItem.Attributes[count].Defindex == 261)
                    {
                        string paint = ShowBackpack.PaintToName(inventoryItem.Attributes[count].FloatValue);
                        name += " (Painted: " + paint + ")";
                    }
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
                        var containedItem = Trade.CurrentSchema.GetItem(inventoryItem.ContainedItem.Defindex);
                        var containedName = GetItemName(containedItem, inventoryItem.ContainedItem);
                        name += " (Contains: " + containedName + ")";
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
            if (pageNum == 10) return;
            pageNum++;
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

        private void metroTile_MouseEnter(object sender, EventArgs e)
        {
            var tile = (MetroTile) sender;
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

        private void metroTile_MouseLeave(object sender, EventArgs e)
        {
            var tile = (MetroTile)sender;
            var bmp = tile.TileImage;
            if (bmp.Size == new Size(116, 78) && !((TileTag)tile.Tag).Selected)
            {
                tile.TileImage = getImageFromURL(((TileTag) tile.Tag).ImageUrl);
            }
        }

        private void metroTile_Click(object sender, EventArgs e)
        {
            var tile = (MetroTile)sender;
            var tag = (TileTag) tile.Tag;
            tag.Selected = !tag.Selected;
            if (!tag.Selected) return;
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
            var tile = (MetroTile)sender;
            if (tile.Tag == null) return;
            var tag = (TileTag)tile.Tag;
            if (tag.Item == null) return;
            var item = tag.Item;
            if (item == null) return;
            var schemaitem = Trade.CurrentSchema.GetItem(item.Defindex);
            var name = string.IsNullOrWhiteSpace(item.CustomName)
                           ? schemaitem.ItemName
                           : string.Format("\"{0}\" ({1})", item.CustomName, schemaitem.ItemName);
            var type = schemaitem.ItemTypeName;
            var desc = string.IsNullOrWhiteSpace(item.CustomDescription)
                           ? schemaitem.ItemDescription
                           : string.Format("\"{0}\" ({1})", item.CustomDescription, schemaitem.ItemDescription);
        }

        public class TileTag
        {
            public string ImageUrl;
            public Inventory.Item Item;
            public bool Selected;
        }
    }
}
