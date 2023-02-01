using System;

namespace SmarterEnemies.Utils {
    public static class StringExtensions {

        public static void RemoveComponent<T>(this GameObject gameObject) where T : Component {
            GameObject.Destroy(gameObject.GetComponent<T>());
        }

        public static void RemoveComponents<T>(this GameObject gameObject) where T : Component {
            T[] coms = gameObject.GetComponents<T>();
            for (int i = 0; i < coms.Length; i++) {
                GameObject.Destroy(coms[i]);
            }
        }

        public static bool HasItemsOfTier(this Inventory inventory, ItemTier tier) {
            foreach (ItemIndex index in inventory.itemAcquisitionOrder) {
                if (ItemCatalog.GetItemDef(index)) {
                    ItemDef def = ItemCatalog.GetItemDef(index);
                    if (def.tier == tier) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HasScrapItems(this Inventory inventory, ItemTier tier = ItemTier.NoTier) {
            foreach (ItemIndex index in inventory.itemAcquisitionOrder) {
                if (ItemCatalog.GetItemDef(index)) {
                    ItemDef def = ItemCatalog.GetItemDef(index);
                    if (tier == ItemTier.NoTier) {
                        if (def.ContainsTag(ItemTag.Scrap) || def.ContainsTag(ItemTag.PriorityScrap)) {
                            return true;
                        }
                    }
                    else {
                        if (def.ContainsTag(ItemTag.Scrap) || def.ContainsTag(ItemTag.PriorityScrap) && def.tier == tier) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static int GetRandomScrappableIndex(this Inventory inventory) {
            List<ItemDef> defs = new();
            foreach (ItemIndex index in inventory.itemAcquisitionOrder) {
                if (ItemCatalog.GetItemDef(index)) {
                    ItemDef def = ItemCatalog.GetItemDef(index);
                    if (def && def.tier == ItemTier.Tier1 || def.tier == ItemTier.Tier2 || def.tier == ItemTier.Tier3 && !def.ContainsTag(ItemTag.Scrap) && !def.ContainsTag(ItemTag.PriorityScrap)) {
                        defs.Add(def);
                    }
                }
            }

            return PickupCatalog.FindPickupIndex(defs[Run.instance.runRNG.RangeInt(0, defs.Count)].itemIndex).value;
        }

        public static int GetTotalItemCount(this Inventory inventory) {
            int total = 0;
            foreach (ItemIndex index in inventory.itemAcquisitionOrder) {
                if (ItemCatalog.GetItemDef(index)) {
                    ItemDef def = ItemCatalog.GetItemDef(index);
                    if (def && def.tier == ItemTier.Tier1 || def.tier == ItemTier.Tier2 || def.tier == ItemTier.Tier3 && !def.ContainsTag(ItemTag.Scrap) && !def.ContainsTag(ItemTag.PriorityScrap)) {
                        total += inventory.GetItemCount(def);
                    }
                }
            }
            return total;
        }

        public static T GetClosest<T>(this List<T> list, Vector3 position) where T : Component {
            float distance = int.MaxValue;
            T closest = null;
            foreach (T component in list) {
                float distanceBetween = Vector3.Distance(component.transform.position, position);
                if (distanceBetween < distance) {
                    closest = component;
                    distance = distanceBetween;
                }
            }
            return closest;
        }

        public static void ClearInventory(this Inventory inv) {
            List<ItemIndex> indexes = new();
            indexes = indexes.Concat(inv.itemAcquisitionOrder.ToList()).ToList();
            foreach (ItemIndex index in indexes) {
                inv.RemoveItem(index, inv.GetItemCount(index));
            }
        }

        public static void CopyInventory(this Inventory inv, Inventory from) {
            inv.ClearInventory();
            foreach (ItemIndex index in from.itemAcquisitionOrder) {
                inv.GiveItem(index, from.GetItemCount(index));
            }
        }
    }
}