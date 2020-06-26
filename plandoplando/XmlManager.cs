using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Modding.Logger;
using System.Xml;
using System.IO;
using ItemChanger;
using System.Reflection;
using ItemChanger.Default;
using IL.UnityEngine.UI;

namespace plandoplando
{
    internal static class XmlManager
    {
        public static List<ILP> ILPs;
        public static ItemChanger.Default.Shops.DefaultShopItems defaultShopPreset;
        public static string title;
        public static ItemChangerSettings settings;
        public static bool changeStart = false;
        public static StartLocation startLocation;
        public static Dictionary<string, Item> customItems;
        public static Dictionary<string, Location> customLocations;
        public static Dictionary<string, Dictionary<string, string>> customText;
        public static List<ChangeSceneAction> changeSceneActions;
        public static Dictionary<string, int> darknessLevels;

        public static bool TryLoad(string fileName)
        {
            XmlDocument doc;

            try
            {
                FileStream stream = File.OpenRead(fileName);
                doc = new XmlDocument();
                doc.Load(stream);
                stream.Dispose();
            }
            catch (Exception e)
            {
                LogError("Error loading file: " + e);
                return false;
            }

            try
            {
                customItems = new Dictionary<string, Item>();
                foreach (XmlNode node in doc.SelectNodes("randomizer/newItem"))
                {
                    Item item = ItemChanger.XmlManager.ProcessXmlNodeAsItem(node);
                    customItems[item.name] = item;
                }
            }
            catch (Exception e)
            {
                LogError("Error reading custom item data. Xml load aborted: " + e);
                return false;
            }

            try
            {
                customLocations = new Dictionary<string, Location>();
                foreach (XmlNode node in doc.SelectNodes("randomizer/newLocation"))
                {
                    Location location = ItemChanger.XmlManager.ProcessXmlNodeAsLocation(node);
                    customLocations[location.name] = location;
                }
            }
            catch (Exception e)
            {
                LogError("Error reading custom location data. Xml load aborted: " + e);
                return false;
            }

            try
            {
                customText = new Dictionary<string, Dictionary<string, string>>();
                foreach (XmlNode node in doc.SelectNodes("randomizer/languageEntry"))
                {
                    string sheet = null;
                    string key = null;
                    string text = null;
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (child.Name == "sheet") sheet = child.InnerText;
                        else if (child.Name == "key") key = child.InnerText;
                        else if (child.Name == "text") text = child.InnerText;
                    }
                    if (!customText.ContainsKey(sheet))
                    {
                        customText[sheet] = new Dictionary<string, string>();
                    }
                    customText[sheet][key] = text;
                }
            }
            catch (Exception e)
            {
                LogError("Error reading custom text data. Xml load aborted: " + e);
                return false;
            }

            try
            {
                ILPs = new List<ILP>();
                foreach (XmlNode node in doc.SelectNodes("randomizer/ilp"))
                {
                    string item = null;
                    string location = null;
                    int cost = 0;
                    string costType = null;
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        switch (child.Name)
                        {
                            case "item":
                                item = child.InnerText;
                                break;
                            case "location":
                                location = child.InnerText;
                                break;
                            case "cost":
                                cost = Int32.Parse(child.InnerText);
                                break;
                            case "costType":
                                costType = child.InnerText;
                                break;
                        }
                    }
                    if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(location)) continue;

                    ILPs.Add(new ILP(item, location, cost, costType));
                }
            }
            catch (Exception e)
            {
                LogError("Error reading placement data. Xml load aborted: " + e);
                return false;
            }

            try
            {
                defaultShopPreset = ItemChanger.Default.Shops.DefaultShopItems.None;
                if (doc.SelectSingleNode("randomizer/defaultShopItems") is XmlNode node)
                {
                    if (typeof(Shops).GetField(node.InnerText) is FieldInfo field && field.GetRawConstantValue() is int value)
                    {
                        defaultShopPreset = (Shops.DefaultShopItems)value;
                    }
                    else if (Int32.TryParse(node.InnerText, out value))
                    {
                        defaultShopPreset = (Shops.DefaultShopItems)value;
                    }
                    else
                    {
                        defaultShopPreset = (Shops.DefaultShopItems)Enum.Parse(typeof(Shops.DefaultShopItems), node.InnerText);
                    }
                }
            }
            catch (Exception e)
            {
                LogError("Error reading shop data. Xml load aborted: " + e);
                return false;
            }

            try
            {
                title = string.Empty;
                if(doc.SelectSingleNode("randomizer/title") is XmlNode node)
                {
                    title = node.InnerText;
                }
            }
            catch (Exception e)
            {
                LogError("Error reading title. Xml load aborted: " + e);
                return false;
            }

            try
            {
                settings = new ItemChangerSettings();
                foreach (FieldInfo field in typeof(ItemChangerSettings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    XmlNode node = doc.SelectSingleNode("randomizer/" + field.Name);
                    if (node is XmlNode && bool.TryParse(node.InnerText, out bool value))
                    {
                        field.SetValue(settings, value);
                    }
                }
            }
            catch (Exception e)
            {
                LogError("Error reading ItemChangerSettings. Xml load aborted: " + e);
                return false;
            }

            try
            {
                if (doc.SelectSingleNode("randomizer/startSceneName") is XmlNode startNode 
                    && doc.SelectSingleNode("randomizer/startX") is XmlNode xNode
                    && doc.SelectSingleNode("randomizer/startY") is XmlNode yNode)
                {
                    changeStart = true;
                    startLocation = new StartLocation
                    {
                        startSceneName = startNode.InnerText,
                        startX = float.Parse(xNode.InnerText),
                        startY = float.Parse(yNode.InnerText)
                    };
                }
            }
            catch (Exception e)
            {
                LogError("Error reading start location. Xml load aborted: " + e);
                return false;
            }

            try
            {
                changeSceneActions = new List<ChangeSceneAction>();
                foreach (XmlNode node in doc.SelectNodes("randomizer/addPreloadGameObject"))
                {
                    changeSceneActions.Add(new DeployObjectAction(node["originalSceneName"].InnerText, node["originalObjectName"].InnerText)
                    {
                        deploySceneName = node["deploySceneName"].InnerText,
                        x = float.Parse(node["deployX"].InnerText),
                        y = float.Parse(node["deployY"].InnerText)
                    });
                }
                foreach (XmlNode node in doc.SelectNodes("randomizer/addDestroyGameObject"))
                {
                    changeSceneActions.Add(new DestroyObjectAction
                    {
                        originalSceneName = node["originalSceneName"].InnerText,
                        originalObjectName = node["originalObjectName"].InnerText,
                        destroyAllThatMatch = bool.Parse(node["destroyAllThatMatch"].InnerText)
                    }) ;
                }

                foreach (XmlNode node in doc.SelectNodes("randomizer/addSpecialGameObject"))
                {
                    switch (node["specialName"].InnerText)
                    {
                        case "Toll Gate":
                            changeSceneActions.Add(new TollGateAction
                            {
                                tollMachine = new DeployObjectAction("Mines_33", "Toll Gate Machine")
                                {
                                    deploySceneName = node["deploySceneName"].InnerText,
                                    x = float.Parse(node["deployOneX"].InnerText),
                                    y = float.Parse(node["deployOneY"].InnerText)
                                },
                                gate = new DeployObjectAction("Mines_33", "Toll Gate")
                                {
                                    deploySceneName = node["deploySceneName"].InnerText,
                                    x = float.Parse(node["deployTwoX"].InnerText),
                                    y = float.Parse(node["deployTwoY"].InnerText)
                                },
                                cost = Int32.Parse(node["paramOne"].InnerText),
                            });
                            break;

                        case "Switch Gate":
                            changeSceneActions.Add(new SwitchGateAction
                            {
                                gateSwitch = new DeployObjectAction("Fungus3_05", "Gate Switch")
                                {
                                    deploySceneName = node["deploySceneName"].InnerText,
                                    x = float.Parse(node["deployOneX"].InnerText),
                                    y = float.Parse(node["deployOneY"].InnerText)
                                },
                                gate = new DeployObjectAction("Fungus3_05", "Metal Gate v2")
                                {
                                    deploySceneName = node["deploySceneName"].InnerText,
                                    x = float.Parse(node["deployTwoX"].InnerText),
                                    y = float.Parse(node["deployTwoY"].InnerText)
                                },
                            });
                            break;

                        case "Vengefly":
                            changeSceneActions.Add(new DeployEnemyAction
                            {
                                enemy = new DeployObjectAction("Fungus1_28", "Buzzer (3)")
                                {
                                    deploySceneName = node["deploySceneName"].InnerText,
                                    x = float.Parse(node["deployOneX"].InnerText),
                                    y = float.Parse(node["deployOneY"].InnerText)
                                },
                                hp = Int32.Parse(node["paramOne"].InnerText)
                            });
                            break;

                        case "Baldur":
                            changeSceneActions.Add(new DeployBaldurAction
                            {
                                enemy = new DeployObjectAction("Fungus1_28", "Battle Music/Blocker 1")
                                {
                                    deploySceneName = node["deploySceneName"].InnerText,
                                    x = float.Parse(node["deployOneX"].InnerText),
                                    y = float.Parse(node["deployOneY"].InnerText)
                                },
                                hp = Int32.Parse(node["paramOne"].InnerText),
                                facingRight = bool.Parse(node["paramTwo"].InnerText)
                            });
                            break;

                        case "Platform":
                            changeSceneActions.Add(new DeployObjectAction("Fungus1_28", "plat_float_03")
                            {
                                deploySceneName = node["deploySceneName"].InnerText,
                                x = float.Parse(node["deployOneX"].InnerText),
                                y = float.Parse(node["deployOneY"].InnerText)
                            });
                            break;

                        case "Shadow Gate":
                            changeSceneActions.Add(new DeployObjectAction("Fungus3_44", "shadow_gate")
                            {
                                deploySceneName = node["deploySceneName"].InnerText,
                                x = float.Parse(node["deployOneX"].InnerText),
                                y = float.Parse(node["deployOneY"].InnerText)
                            });
                            break;

                        case "Quake Floor":
                            changeSceneActions.Add(new DeployQuakeFloorAction
                            {
                                floor = new DeployObjectAction("Crossroads_52", "Quake Floor")
                                {
                                    deploySceneName = node["deploySceneName"].InnerText,
                                    x = float.Parse(node["deployOneX"].InnerText),
                                    y = float.Parse(node["deployOneY"].InnerText)
                                },
                                entryGate = node["paramOne"].InnerText
                            });
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                LogError("Error reading deploy/destroy actions. Xml load aborted: " + e);
                return false;
            }

            try
            {
                darknessLevels = new Dictionary<string, int>();
                foreach (XmlNode node in doc.SelectNodes("randomizer/overrideDarkness"))
                {
                    darknessLevels[node["sceneName"].InnerText] = Int32.Parse(node["darknessLevel"].InnerText);
                }
            }
            catch (Exception e)
            {
                LogError("Error reading dark room actions. Xml load aborted: " + e);
                return false;
            }

            return true;
        }
    }
}
