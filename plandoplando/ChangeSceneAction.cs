using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SeanprCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace plandoplando
{
    public interface ChangeSceneAction
    {
        void OnChangeScene(string sceneName);
    }

    public class DeployObjectAction : ChangeSceneAction
    {
        public static Dictionary<string, Dictionary<string, GameObject>> preloadedObjects;
        public static List<(string, string)> preloadRequests;
        public static bool preload;

        public string deploySceneName;
        public string objSceneName;
        public string objName;
        public float x;
        public float y;

        public DeployObjectAction(string objSceneName, string objName)
        {
            this.objSceneName = objSceneName;
            this.objName = objName;

            preload = true;
            if (preloadRequests is null) preloadRequests = new List<(string, string)>();
            if (!preloadRequests.Contains((objSceneName, objName))) preloadRequests.Add((objSceneName, objName));
        }

        public void OnChangeScene(string sceneName)
        {
            if (sceneName != deploySceneName) return;

            GameObject g = GameObject.Instantiate(preloadedObjects[objSceneName][objName]);
            g.SetActive(true);
            g.transform.SetPosition2D(new Vector2(x, y));
        }

        public GameObject DeployAndReturn()
        {
            GameObject g = GameObject.Instantiate(preloadedObjects[objSceneName][objName]);
            g.SetActive(true);
            g.transform.SetPosition2D(new Vector2(x, y));
            if (g.GetComponent<PersistentBoolItem>() is PersistentBoolItem item)
            {
                g.name = objName + (int)x + (int)y;
                item.persistentBoolData.id = g.name;
                item.persistentBoolData.sceneName = GameManager.instance.sceneName;
                item.persistentBoolData.activated = GameManager.instance.sceneData.FindMyState(item.persistentBoolData)?.activated ?? false;
            }

            return g;
        }

    }

    public class SwitchGateAction : ChangeSceneAction
    {
        public DeployObjectAction gate;
        public DeployObjectAction gateSwitch;

        public static void SetGateSwitchTarget(GameObject gateSwitch, GameObject gate)
        {
            gateSwitch.LocateFSM("Switch Control").FsmVariables.GetFsmGameObject("Target").Value = gate;
        }

        public static void FixGateSwitchAnimation(GameObject gateSwitch)
        {
            gateSwitch.LocateFSM("Switch Control").GetState("Open").GetActionOfType<SendEventByName>().sendEvent = "OPEN";
        }

        public void OnChangeScene(string sceneName)
        {
            if (sceneName != gate.deploySceneName) return;

            GameObject g = gate.DeployAndReturn();
            GameObject s = gateSwitch.DeployAndReturn();
            SetGateSwitchTarget(s, g);
            FixGateSwitchAnimation(s);
        }
    }

    public class TollGateAction : ChangeSceneAction
    {
        public DeployObjectAction gate;
        public DeployObjectAction tollMachine;
        public int cost;

        public static void SetTollCost(GameObject tollMachine, int cost)
        {
            FsmState getPrice = tollMachine.LocateMyFSM("Toll Machine").GetState("Get Price");
            getPrice.Actions = new FsmStateAction[] { };
            tollMachine.LocateMyFSM("Toll Machine").FsmVariables.GetFsmInt("Toll Cost").Value = cost;
            Modding.Logger.Log("Changed toll cost");
        }

        public static void ResetFSMs(GameObject tollMachine)
        {
            tollMachine.GetComponent<BoxCollider2D>().enabled = true;
            tollMachine.GetComponent<tk2dBaseSprite>().color = Color.white;
            GameObject pm = new GameObject();
            pm.name = "Prompt Marker";
            pm.transform.parent = tollMachine.transform;
            pm.transform.position = tollMachine.transform.position + new Vector3(0, 2f);
            tollMachine.LocateFSM("Toll Machine").FsmVariables.FindFsmGameObject("Prompt Marker").Value = pm;
        }

        public static void AddCheckDarknessLevel(GameObject tollMachine)
        {
            FsmState check = tollMachine.LocateMyFSM("Disable if No Lantern").GetState("Check");
            check.Actions = new FsmStateAction[] { };
            check.AddFirstAction(new Lambda(() =>
                {
                    if (!PlayerData.instance.hasLantern && HeroController.instance.vignetteFSM.FsmVariables.GetFsmInt("Darkness Level").Value == 2)
                    {
                        check.Fsm.Event("DISABLE");
                    }
                }
                ));
        }

        public void OnChangeScene(string sceneName)
        {
            if (sceneName != gate.deploySceneName) return;

            gate.DeployAndReturn();
            GameObject t = tollMachine.DeployAndReturn();
            SetTollCost(t, cost);
            AddCheckDarknessLevel(t);
            ResetFSMs(t);
        }
    }

    public class DeployEnemyAction : ChangeSceneAction
    {
        public DeployObjectAction enemy;
        public int hp;

        public void OnChangeScene(string sceneName)
        {
            if (sceneName != enemy.deploySceneName) return;

            GameObject e = enemy.DeployAndReturn();
            e.GetComponent<HealthManager>().hp = hp;
        }
    }

    public class DeployBaldurAction : ChangeSceneAction
    {
        public DeployObjectAction enemy;
        public int hp;
        public bool facingRight;

        public void OnChangeScene(string sceneName)
        {
            if (sceneName != enemy.deploySceneName) return;

            GameObject e = enemy.DeployAndReturn();
            e.LocateFSM("Blocker Control").FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
            e.LocateFSM("Blocker Control").GetState("Can Roller?").RemoveActionsOfType<IntCompare>();
            e.GetComponent<HealthManager>().hp = hp;
        }
    }

    public class DeployQuakeFloorAction : ChangeSceneAction
    {
        public DeployObjectAction floor;
        public string entryGate;

        public void OnChangeScene(string sceneName)
        {
            if (sceneName != floor.deploySceneName) return;
            GameObject f = floor.DeployAndReturn();
            if (GameManager.instance.entryGateName == entryGate)
            {
                PersistentBoolData data = f.GetComponent<PersistentBoolItem>().persistentBoolData;
                data.activated = true;
                GameManager.instance.sceneData.SaveMyState(data);
            }
        }
    }

    public class DestroyObjectAction : ChangeSceneAction
    {
        public string originalSceneName;
        public string originalObjectName;
        public bool destroyAllThatMatch;

        public void OnChangeScene(string sceneName)
        {
            if (sceneName != originalSceneName) return;

            if (GameObject.Find(originalObjectName) is GameObject obj)
            {
                GameObject.Destroy(obj);
            }
        }
    }

    public class Lambda : FsmStateAction
    {
        readonly Action action;
        public Lambda(Action a) { action = a; }
        public override void OnEnter()
        {
            action();
            Finish();
        }
    }

}
