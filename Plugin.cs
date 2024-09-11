using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModdedMagnet
{
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInPlugin("spapi.etg.modmagnet", "Modded Magnet", "1.0.0")] //im not even using harmony here so no consts this time
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> itemRarityIncrease;
        public static ConfigEntry<float> gunRarityIncrease;
        public static ConfigEntry<bool> isEnabled;

        public void Awake()
        {
            itemRarityIncrease = Config.Bind("ItemWeight", "ModdedItemRarityMultiplier", 3f, "How much more common modded passive and active items should be.");
            itemRarityIncrease.SettingChanged += (x, x2) =>
            {
                if (fullyLoaded)
                {
                    ReloadItems();
                }
            };
            gunRarityIncrease = Config.Bind("ItemWeight", "ModdedGunRarityMultiplier", 6f, "How much more common modded guns should be.");
            gunRarityIncrease.SettingChanged += (x, x2) =>
            {
                if (fullyLoaded)
                {
                    ReloadGuns();
                }
            };
            isEnabled = Config.Bind("Enabled", "ModdedMagnetEnabled", true, "Enable/disable modded item/gun rarity increase.");
            isEnabled.SettingChanged += (x, x2) =>
            {
                if (fullyLoaded)
                {
                    ReloadItems();
                    ReloadGuns();
                }
            };
        }

        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(gm =>
            {
                try
                {
                    ETGModConsole.Commands.AddGroup("modded_magnet");
                    var mmgroup = ETGModConsole.Commands.GetGroup("modded_magnet");
                    ETGModConsole.CommandDescriptions.Add("modded_magnet", "The command group for the Modded Magnet mod.");
                    mmgroup.AddUnit("item_weight", x =>
                    {
                        if(x.Length < 1)
                        {
                            ETGModConsole.Log("New weight not given!");
                            return;
                        }
                        if (!float.TryParse(x[0], out var weight))
                        {
                            ETGModConsole.Log("Invalid argument! Argument must be a number.");
                            return;
                        }
                        itemRarityIncrease.Value = weight;
                        Config.Save();
                        ETGModConsole.Log("Modded item weight multiplier successfully set to " + weight);
                    });
                    ETGModConsole.CommandDescriptions.Add("modded_magnet item_weight", "Sets the weight multiplier for modded passive and active items.");
                    mmgroup.AddUnit("gun_weight", x =>
                    {
                        if (x.Length < 1)
                        {
                            ETGModConsole.Log("New weight not given!");
                            return;
                        }
                        if (!float.TryParse(x[0], out var weight))
                        {
                            ETGModConsole.Log("Invalid argument! Argument must be a number.");
                            return;
                        }
                        gunRarityIncrease.Value = weight;
                        Config.Save();
                        ETGModConsole.Log("Modded gun weight multiplier successfully set to " + weight);
                    });
                    ETGModConsole.CommandDescriptions.Add("modded_magnet gun_weight", "Sets the weight multiplier for modded guns.");
                    mmgroup.AddUnit("enable", x =>
                    {
                        isEnabled.Value = true;
                        ETGModConsole.Log("Modded item weight increase successfully enabled.");
                    });
                    ETGModConsole.CommandDescriptions.Add("modded_magnet enable", "Enables the weight increase for modded items and guns.");
                    mmgroup.AddUnit("disable", x =>
                    {
                        isEnabled.Value = false;
                        ETGModConsole.Log("Modded item weight increase successfully disabled.");
                    });
                    ETGModConsole.CommandDescriptions.Add("modded_magnet disable", "Disables the weight increase for modded items and guns.");
                    mmgroup.AddUnit("reload", x =>
                    {
                        ReloadItems();
                        ReloadGuns();
                        ETGModConsole.Log("Modded item weight increase successfully reloaded.");
                    });
                    ETGModConsole.CommandDescriptions.Add("modded_magnet reload", "Reloads the modded item and gun weight increase, in case any item or gun was missed.");
                    mmgroup.AddUnit("stats", x =>
                    {
                        ETGModConsole.Log("Modded item weight multiplier: " + itemRarityIncrease.Value);
                        ETGModConsole.Log("Modded gun weight multiplier: " + gunRarityIncrease.Value);
                        ETGModConsole.Log("Modded item weight increase is enabled: " + isEnabled.Value);
                    });
                    ETGModConsole.CommandDescriptions.Add("modded_magnet stats", "Shows the current weight increase for modded items and guns, as well as if the weight increase is currently enabled.");
                    ETGModConsole.Log("Modded Magnet mod successfully loaded.");
                    ETGModConsole.Log("Modded item weight multiplier: " + itemRarityIncrease.Value);
                    ETGModConsole.Log("Modded gun weight multiplier: " + gunRarityIncrease.Value);
                    ETGModConsole.Log("Modded item weight increase is enabled: " + isEnabled.Value);
                    ETGModConsole.Log("Modded Magnet console command group: modded_magnet");
                    IEnumerator Delay()
                    {
                        yield return null;
                        ReloadItems();
                        ReloadGuns();
                        fullyLoaded = true;
                        yield break;
                    }
                    StartCoroutine(Delay());
                }
                catch(Exception ex)
                {
                    ETGModConsole.Log("An error occured when loading Modded Magnet: " + ex);
                }
            });
        }

        public static void ReloadItems()
        {
            if (objectsIEditedForItems != null && oldItemWeight != 0f)
            {
                foreach(var obj in objectsIEditedForItems)
                {
                    obj.weight /= oldItemWeight;
                }
            }
            objectsIEditedForItems = null;
            if (objectsIRemovedForItems != null)
            {
                GameManager.Instance?.RewardManager?.ItemsLootTable?.defaultItemDrops?.elements?.AddRange(objectsIRemovedForItems);
                objectsIRemovedForItems = null;
            }
            objectsIRemovedForItems = null;
            if(isEnabled.Value && GameManager.Instance?.RewardManager.ItemsLootTable?.defaultItemDrops?.elements != null)
            {
                foreach(var elem in GameManager.Instance.RewardManager.ItemsLootTable.defaultItemDrops.elements)
                {
                    if(elem.weight == 0f)
                    {
                        continue;
                    }
                    var pickupId = elem.pickupId;
                    if(pickupId < 0 && elem.gameObject != null && elem.gameObject.GetComponent<PickupObject>() != null)
                    {
                        pickupId = elem.gameObject.GetComponent<PickupObject>().PickupObjectId;
                    }
                    if(pickupId <= 823)
                    {
                        continue;
                    }
                    if (itemRarityIncrease.Value == 0f)
                    {
                        objectsIRemovedForItems = objectsIRemovedForItems ?? new List<WeightedGameObject>();
                        objectsIRemovedForItems.Add(elem);
                    }
                    else
                    {
                        objectsIEditedForItems = objectsIEditedForItems ?? new List<WeightedGameObject>();
                        objectsIEditedForItems.Add(elem);
                        elem.weight *= itemRarityIncrease.Value;
                    }
                }
            }
            if (objectsIRemovedForItems != null)
            {
                foreach(var elem in objectsIRemovedForItems)
                {
                    GameManager.Instance.RewardManager.ItemsLootTable.defaultItemDrops.elements.Remove(elem);
                }
            }
            oldItemWeight = itemRarityIncrease.Value;
        }

        public static void ReloadGuns()
        {
            if (objectsIEditedForGuns != null && oldGunWeight != 0f)
            {
                foreach (var obj in objectsIEditedForGuns)
                {
                    obj.weight /= oldGunWeight;
                }
            }
            objectsIEditedForGuns = null;
            if (objectsIRemovedForGuns != null)
            {
                GameManager.Instance?.RewardManager?.GunsLootTable?.defaultItemDrops?.elements?.AddRange(objectsIRemovedForGuns);
                objectsIRemovedForGuns = null;
            }
            objectsIRemovedForGuns = null;
            if (isEnabled.Value && GameManager.Instance?.RewardManager.GunsLootTable?.defaultItemDrops?.elements != null)
            {
                foreach (var elem in GameManager.Instance.RewardManager.GunsLootTable.defaultItemDrops.elements)
                {
                    if (elem.weight == 0f)
                    {
                        continue;
                    }
                    var pickupId = elem.pickupId;
                    if (pickupId < 0 && elem.gameObject != null && elem.gameObject.GetComponent<PickupObject>() != null)
                    {
                        pickupId = elem.gameObject.GetComponent<PickupObject>().PickupObjectId;
                    }
                    if (pickupId <= 823)
                    {
                        continue;
                    }
                    if (gunRarityIncrease.Value == 0f)
                    {
                        objectsIRemovedForGuns = objectsIRemovedForGuns ?? new List<WeightedGameObject>();
                        objectsIRemovedForGuns.Add(elem);
                    }
                    else
                    {
                        objectsIEditedForGuns = objectsIEditedForGuns ?? new List<WeightedGameObject>();
                        objectsIEditedForGuns.Add(elem);
                        elem.weight *= gunRarityIncrease.Value;
                    }
                }
            }
            if (objectsIRemovedForGuns != null)
            {
                foreach(var elem in objectsIRemovedForGuns)
                {
                    GameManager.Instance.RewardManager.GunsLootTable.defaultItemDrops.elements.Remove(elem);
                }
            }
            oldGunWeight = gunRarityIncrease.Value;
        }

        public static List<WeightedGameObject> objectsIRemovedForItems;
        public static List<WeightedGameObject> objectsIEditedForItems;
        public static List<WeightedGameObject> objectsIRemovedForGuns;
        public static List<WeightedGameObject> objectsIEditedForGuns;
        public static bool fullyLoaded;
        private static float oldGunWeight = 1f;
        private static float oldItemWeight = 1f;
    }
}
