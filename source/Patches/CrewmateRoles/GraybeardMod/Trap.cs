﻿using HarmonyLib;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TownOfUs.Roles;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfUs.CrewmateRoles.GraybeardMod
{
    public class Trap
    {
        public Dictionary<byte, float> players = new Dictionary<byte, float>();
        public Transform transform;

        public IEnumerator FrameTimer()
        {
            while (transform != null)
            {
                yield return 0;
                Update();
            }
        }

        public void Update()
        {      
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.IsDead || player.IsDevoured()) continue;
                //PluginSingleton<TownOfUs>.Instance.Log.LogMessage($"player with byte {player.PlayerId} is {Vector2.Distance(transform.position, player.GetTruePosition())} away");
                if (Vector2.Distance(transform.position, player.GetTruePosition()) < (CustomGameOptions.ForceFieldSize + 0.01f) * ShipStatus.Instance.MaxLightRadius)
                {
                    if (!players.ContainsKey(player.PlayerId)) players.Add(player.PlayerId, 0f);
                } 
                else
                {
                    if (players.ContainsKey(player.PlayerId)) players.Remove(player.PlayerId);
                }

                var entry = player;
                if (players.ContainsKey(entry.PlayerId))
                {
                    players[entry.PlayerId] += Time.deltaTime;
                    //PluginSingleton<TownOfUs>.Instance.Log.LogMessage($"player with byte {entry} is logged with time {players[entry]}");
                    if (players[entry.PlayerId] > 0.4f)
                    {
                        foreach (Graybeard t in Role.GetRoles(RoleEnum.Graybeard))
                        {
                            if (entry == t.Player) continue;
                            if (!t.trappedPlayers.Keys.Any((key) => key.PlayerId==entry.PlayerId)) 
                                t.trappedPlayers.Add(entry, DateTime.UtcNow);
                            else
                            {
                                var removedPlayer = t.trappedPlayers.Where((key) => key.Key.PlayerId == entry.PlayerId).FirstOrDefault().Key;
                                t.trappedPlayers.Remove(removedPlayer);
                                t.trappedPlayers.Add(entry, DateTime.UtcNow);
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch]
    public static class TrapExtentions
    {
        public static void ClearTraps(this List<Trap> obj)
        {
            foreach (Trap t in obj)
            {
                Object.Destroy(t.transform.gameObject);
                Coroutines.Stop(t.FrameTimer());
            }
            obj.Clear();
        }

        public static Trap CreateTrap(this Vector3 location)
        {
            var TrapPref = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            TrapPref.name = "Trap";
            TrapPref.transform.localScale = new Vector3(CustomGameOptions.ForceFieldSize * ShipStatus.Instance.MaxLightRadius * 2f, 
                CustomGameOptions.ForceFieldSize * ShipStatus.Instance.MaxLightRadius * 2f, CustomGameOptions.ForceFieldSize * ShipStatus.Instance.MaxLightRadius * 2f);
            GameObject.Destroy(TrapPref.GetComponent<SphereCollider>());
            TrapPref.GetComponent<MeshRenderer>().material = Graybeard.trapMaterial;
            TrapPref.transform.position = location;
            var TrapScript = new Trap();
            TrapScript.transform = TrapPref.transform;
            Coroutines.Start(TrapScript.FrameTimer());
            return TrapScript;
        }
    }
}