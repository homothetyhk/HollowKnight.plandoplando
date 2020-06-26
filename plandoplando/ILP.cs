using ItemChanger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemChanger.Default;
using Modding;

namespace plandoplando
{
    internal class ILP
    {
        public readonly string item;
        public readonly string location;
        public readonly int cost;
        public readonly string costType;

        public ILP(string _item, string _location, int _cost = 0, string _costType = null)
        {
            item = _item;
            location = _location;
            cost = _cost;
            costType = _costType;
        }

        public static List<(Item, Location)> Process(List<ILP> ILPs)
        {
            List<(Item, Location)> processed = new List<(Item, Location)>();

            foreach (ILP ilp in ILPs)
            {
                try
                {
                    if (!XmlManager.customItems.TryGetValue(ilp.item, out Item item))
                    {
                        item = new Item(ilp.item);
                    }
                    if (!XmlManager.customLocations.TryGetValue(ilp.location, out Location location))
                    {
                        location = new Location(ilp.location);
                    }

                    if (ilp.costType != null)
                    {
                        if (location.shop) item.shopPrice = ilp.cost;
                        else
                        {
                            location.costType = (Location.CostType)Enum.Parse(typeof(Location.CostType), ilp.costType);
                            location.cost = ilp.cost;
                        }
                    }
                    processed.Add((item, location));
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error processing {ilp}: " + e);
                    throw;
                }
            }

            return processed;
        }

        public override string ToString()
        {
            return $"Item {item} at location {location}" + (cost != 0 ? $" with cost {cost}" : string.Empty);
        }
    }
}
