using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Windows.Forms;


namespace SteamTrade
{
    public class Schema
    {
        public static Schema FetchSchema (string apiKey)
        {
            var url = "http://api.steampowered.com/IEconItems_570/GetSchema/v0001/?key=" + apiKey + "&language=en";

            string cachefile="d2_schema.cache";
            string result = "";

            try
            {
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

                if (!System.IO.File.Exists(cachefile) ||
                    (SchemaLastModified > System.IO.File.GetCreationTime(cachefile)))
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
            }
            catch (NullReferenceException ex)
            {
                //.net 4.5 will error out on Request.
                using (var wc = new WebClient() {Proxy = null})
                {
                    result = wc.DownloadString(url);
                }
            }

            SchemaResult schemaResult = JsonConvert.DeserializeObject<SchemaResult> (result);
            return schemaResult.result ?? null;
        }
            
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("items_game_url")]
        public string ItemsGameUrl { get; set; }

        [JsonProperty("items")]
        public Item[] Items { get; set; }

        [JsonProperty("originNames")]
        public ItemOrigin[] OriginNames { get; set; }

        [JsonProperty("attributes")]
        public Attribute[] Attributes { get; set; }

        [JsonProperty("attribute_controlled_attached_particles")]
        public Particle[] Particles { get; set; }

        [JsonProperty("item_levels")]
        public ItemLevel[] ItemLevels { get; set; }

        [JsonProperty("kill_eater_ranks")]
        public KillEaterRank[] KillEaterRanks { get; set; }

        [JsonProperty("kill_eater_score_types")]
        public KillEaterScoreType[] KillEaterScoreTypes { get; set; }

        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Item GetItem (int defindex)
        {
            foreach (Item item in Items)
            {
                if (item.Defindex == defindex)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Returns all Items of the given crafting material.
        /// </summary>
        /// <param name="material">Item's craft_material_type JSON property.</param>
        /// <seealso cref="Item"/>
        public List<Item> GetItemsByCraftingMaterial(string material)
        {
            return Items.Where(item => item.CraftMaterialType == material).ToList();
        }

        public string GetEffectName(float value)
        {
            foreach (var effect in Particles.Where(effect => effect.Id == value))
            {
                return effect.Name;
            }
            return "";
        }

        public string GetHexFromColor(float value)
        {
            return "#" + ((int)value).ToString("X6");
        }

        public Inventory.ItemAttribute GetAttribute(Inventory.ItemAttribute[] attributes, int defindex)
        {
            return attributes.FirstOrDefault(x => x.Defindex == defindex);
        }

        public string GetStyle(int itemindex, int style)
        {
            var item = GetItem(itemindex);
            return item.Styles[style].Name;
        }

        public string GetAttributeName(int defindex, Inventory.ItemAttribute[] attributes, float floatvalue = 0f, string value = "")
        {
            var name = "";
            Attribute attrib = null;
            foreach (var attribute in Attributes.Where(attribute => attribute.Defindex == defindex))
            {
                attrib = attribute;
            }
            if (attrib == null || (attrib.Hidden && attrib.Defindex != 214)) return "";
            switch (defindex)
            {
                //Effect
                case 134:
                {
                    name += "Effect: " + GetEffectName(floatvalue);
                    break;
                }
                //Color
                case 142:
                {
                    name += "Color: " + "<span style=\"color: " + GetHexFromColor(floatvalue) + ";\">" + GetHexFromColor(floatvalue)
                        + "</span>";
                    break;
                }
                //Chest series
                case 187:
                {
                    name += "Chest Series #" + floatvalue;
                    break;
                }
                //Item find
                case 215:
                {
                    name +=
                        string.Format(
                            "Item Find: {0}% increase in the chance of finding items for this hero while playing with this item equipped.",
                            floatvalue);
                    break;
                }
                case 321:
                case 322:
                case 323:
                case 325:
                case 387:
                case 390:
                case 391:
                case 392:
                case 398:
                case 399:
                {
                    name += attrib.DescriptionString.Replace("%s1", floatvalue.ToString());
                    break;
                }
                case 404:
                case 405:
                {
                    name += attrib.DescriptionString.Replace("%s1", value);
                    break;
                }
                //Kill eater score
                case 214:
                {
                    var typeattrib = GetAttribute(attributes, 292);
                    foreach (var type in KillEaterScoreTypes)
                    {
                        if (type.Type == typeattrib.FloatValue)
                        {
                            name += type.TypeName;
                            name += ": " + value;
                            break;
                        }
                    }
                    break;
                }
                case 294:
                {
                    var typeattrib = GetAttribute(attributes, 293);
                    foreach (var type in KillEaterScoreTypes)
                    {
                        if (type.Type == typeattrib.FloatValue)
                        {
                            name += type.TypeName;
                            name += ": " + value;
                            break;
                        }
                    }
                    break;
                }
                default:
                {
                    name += attrib.DescriptionString;
                    break;
                }
            }
            return name;
        }

        public class ItemOrigin
        {
            [JsonProperty("origin")]
            public int Origin { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("item_class")]
            public string ItemClass { get; set; }

            [JsonProperty("item_type_name")]
            public string ItemTypeName { get; set; }

            [JsonProperty("item_name")]
            public string ItemName { get; set; }

            [JsonProperty("item_description")]
            public string ItemDescription { get; set; }

            [JsonProperty("craft_material_type")]
            public string CraftMaterialType { get; set; }

            [JsonProperty("used_by_classes")]
            public string[] UsableByClasses { get; set; }

            [JsonProperty("item_slot")]
            public string ItemSlot { get; set; }

            [JsonProperty("craft_class")]
            public string CraftClass { get; set; }

            [JsonProperty("item_quality")]
            public int ItemQuality { get; set; }

            [JsonProperty("image_url")]
            public string ImageURL { get; set; }

            [JsonProperty("styles")]
            public Style[] Styles { get; set; }
        }

        public class Style
        {
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class Attribute
        {

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("defindex")]
            public int Defindex { get; set; }

            [JsonProperty("attribute_class")]
            public string AttributeClass { get; set; }

            [JsonProperty("description_string")]
            public string DescriptionString { get; set; }

            [JsonProperty("description_format")]
            public string DescriptionFormat { get; set; }

            [JsonProperty("effect_type")]
            public string EffectType { get; set; }

            [JsonProperty("hidden")]
            public bool Hidden { get; set; }

            [JsonProperty("stored_as_integer")]
            public bool StoredAsInteger { get; set; }
        }

        public class Particle
        {
            [JsonProperty("id")]
            public float Id;

            [JsonProperty("name")]
            public string Name;
        }

        public class ItemLevel
        {

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("levels")]
            public LevelInfo[] Levels { get; set; }
        }

        public class KillEaterRank
        {

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("required_score")]
            public int RequiredScore { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class KillEaterScoreType
        {

            [JsonProperty("type")]
            public int Type { get; set; }

            [JsonProperty("type_name")]
            public string TypeName { get; set; }
        }

        public class LevelInfo
        {

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("required_score")]
            public int RequiredScore { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        protected class SchemaResult
        {
            public Schema result { get; set; }
        }

    }
}

