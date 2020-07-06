using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using ItemChanger;
using ItemChanger.Default;
using UnityEngine;
using System.IO;
using System.Reflection;
using SeanprCore;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker.Actions;

namespace plandoplando
{
    public class plandoplando : Mod
    {
        public static string filePath => Path.GetFullPath(Application.dataPath + "/Managed/Mods/Plando/plando.xml");
        public bool xmlLoaded;
        public bool xmlFound = File.Exists(filePath);

        public plandoplando()
        {
            xmlLoaded = XmlManager.TryLoad(filePath);
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (xmlLoaded)
            {
                SavePreloadedObjects(preloadedObjects);
                ModHooks.Instance.LanguageGetHook += OverrideLanguageGet;
                On.FSMUtility.SetInt += OverrideDarknessLevel;
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ChangeSceneActions;

                ItemChanger.ItemChanger.ChangeItems(
                    ItemLocationPairs: ILP.Process(XmlManager.ILPs),
                    settings: XmlManager.settings,
                    defaultShopItems: XmlManager.defaultShopPreset
                    );
                
                if (XmlManager.changeStart)
                {
                    ItemChanger.ItemChanger.ChangeStartGame(XmlManager.startLocation);
                }
            }
        }

        public override string GetVersion()
        {
            return xmlFound ? xmlLoaded ? !string.IsNullOrEmpty(XmlManager.title) ? XmlManager.title : "TITLE NOT FOUND" : "XML NOT LOADED" : "NO XML AT FILEPATH";
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return DeployObjectAction.preloadRequests ?? new List<(string, string)>();
        }

        public void SavePreloadedObjects(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (DeployObjectAction.preload)
            {
                DeployObjectAction.preloadedObjects = preloadedObjects;
                foreach (var kvp in preloadedObjects)
                {
                    foreach (var kvp2 in kvp.Value)
                    {
                        if (!(kvp2.Value is GameObject g))
                        {
                            LogError($"Object {kvp2.Key} was not found in {kvp.Key}");
                            continue;
                        }
                        GameObject.DontDestroyOnLoad(g);
                    }
                }
            }
        }

        private void ChangeSceneActions(Scene from, Scene to)
        {
            foreach (var action in XmlManager.changeSceneActions) action.OnChangeScene(to.name);
        }

        private static void OverrideDarknessLevel(On.FSMUtility.orig_SetInt orig, PlayMakerFSM fsm, string variableName, int value)
        {
            if (variableName == "Darkness Level" && fsm.FsmName == "Darkness Control" && XmlManager.darknessLevels.TryGetValue(GameManager.instance.sceneName, out int level))
            {
                orig(fsm, variableName, level);
                return;
            }
            orig(fsm, variableName, value);
        }

        public static string OverrideLanguageGet(string key, string sheet)
        {
            if (XmlManager.customText.ContainsKey(sheet) && XmlManager.customText[sheet].TryGetValue(key, out string text))
            {
                return text;
            }
            return Language.Language.GetInternal(key, sheet);
        }

    }
}
