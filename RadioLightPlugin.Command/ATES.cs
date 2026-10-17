using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using InventorySystem.Items.Autosync;
using MEC;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ATES : ICommand
{
	private class NpcState
	{
		public List<Vector3> Path = new List<Vector3>();

		public int WpIdx = 0;

		public Player Target = null;

		public bool Dead = false;

		public bool IsFleeing;

		public Door LastDoorUsed;

		public Player FleeingFrom = null;
	}

	private static readonly int LosMask = WaypointGraph.WallMask & -257 & -8193;

	public bool SanitizeResponse => false;

	public string Command { get; } = "ates";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Spawna un NPC con pathfinding A* por waypoints.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null)
		{
			response = "Solo jugadores.";
			return false;
		}
		Timing.RunCoroutine(BuildAndSpawn(player));
		response = "Construyendo grafo de waypoints, el NPC aparecerá en unos segundos...";
		return true;
	}

	private IEnumerator<float> BuildAndSpawn(Player player)
	{
		yield return Timing.WaitForSeconds(0.1f);
		WaypointGraph.Build();
		yield return Timing.WaitForSeconds(0.5f);
		Npc npc = Npc.Spawn("Terminator", RoleTypeId.Tutorial, player.Position);
		yield return Timing.WaitForSeconds(1f);
		npc.CurrentItem = Item.Create(ItemType.GunCOM15);
		npc.Position = player.Position;
		NpcState state = new NpcState();
		Timing.RunCoroutine(PlannerLoop(npc, state));
		Timing.RunCoroutine(MoverLoop(npc, state));
		Timing.RunCoroutine(ShootNpc(npc));
	}

	private IEnumerator<float> PlannerLoop(Npc npc, NpcState state)
	{
		float recalcTimer = 0f;
		Vector3 lastPos = Vector3.zero;
		Vector3 lastTarget = Vector3.zero;
		float stuckTimer = 0f;
		while (true)
		{
			bool alive = false;
			try
			{
				alive = npc.IsAlive;
			}
			catch
			{
			}
			if (!alive)
			{
				break;
			}
			Player closestEnemy = null;
			try
			{
				List<Player> enemies = (from p in Player.List
					where p.IsAlive && p.Role.Side != npc.Role.Side
					orderby (npc.Position - p.Position).sqrMagnitude
					select p).ToList();
				closestEnemy = enemies.FirstOrDefault();
				state.IsFleeing = false;
				state.Target = null;
				if (closestEnemy != null)
				{
					float distToEnemy = (npc.Position - closestEnemy.Position).sqrMagnitude;
					if (closestEnemy.Role.Team == Team.SCPs && distToEnemy < 100f)
					{
						Player ally = (from p in Player.List
							where p.IsAlive && p.Role.Side == npc.Role.Side && p != npc
							orderby (npc.Position - p.Position).sqrMagnitude
							select p).FirstOrDefault();
						if (ally != null)
						{
							state.Target = ally;
							state.IsFleeing = true;
							state.FleeingFrom = closestEnemy;
						}
						else
						{
							state.Target = closestEnemy;
							state.IsFleeing = false;
							state.FleeingFrom = null;
						}
					}
					else
					{
						state.Target = closestEnemy;
						state.IsFleeing = false;
						state.FleeingFrom = null;
					}
				}
			}
			catch (Exception ex3)
			{
				Log.Error("[ATES][Planner] FindTarget: " + ex3.Message);
			}
			if (state.Target == null)
			{
				yield return Timing.WaitForSeconds(0.5f);
				continue;
			}
			try
			{
				foreach (Door d in Door.List)
				{
					float dist = (npc.Position - d.Position).sqrMagnitude;
					if (dist > 9f)
					{
						continue;
					}
					if (!d.IsOpen && d.IsAllowToInteract(npc))
					{
						d.IsOpen = true;
						if (state.IsFleeing)
						{
							state.LastDoorUsed = d;
						}
					}
					break;
				}
				if (state.IsFleeing && state.LastDoorUsed != null)
				{
					float distToLastDoor = (npc.Position - state.LastDoorUsed.Position).sqrMagnitude;
					if (distToLastDoor > 16f)
					{
						if (state.LastDoorUsed.IsOpen)
						{
							state.LastDoorUsed.IsOpen = false;
							Log.Info("[ATES] Cerrando puerta para escapar.");
						}
						state.LastDoorUsed = null;
					}
				}
			}
			catch
			{
			}
			float moved = Vector3.Distance(npc.Position, lastPos);
			lastPos = npc.Position;
			stuckTimer = ((moved < 0.15f) ? (stuckTimer + 0.25f) : 0f);
			if (stuckTimer >= 1.5f)
			{
				stuckTimer = 0f;
				recalcTimer = 1.5f;
				Log.Info("[ATES] Stuck — recalculando.");
			}
			Vector3 destination = state.Target.Position;
			if (state.IsFleeing && closestEnemy != null)
			{
				Vector3 fleeDir = (npc.Position - closestEnemy.Position).normalized;
				Vector3 allyDir = (state.Target.Position - npc.Position).normalized;
				Vector3 finalDir = (fleeDir * 0.7f + allyDir * 0.3f).normalized;
				destination = npc.Position + finalDir * 10f;
			}
			recalcTimer += 0.25f;
			bool targetMoved = (destination - lastTarget).sqrMagnitude > 4f;
			if (recalcTimer >= 1.5f || state.Path.Count == 0 || targetMoved)
			{
				try
				{
					List<Vector3> newPath = WaypointGraph.GetPath(npc.Position, destination);
					state.Path = newPath;
					state.WpIdx = 0;
					lastTarget = destination;
					recalcTimer = 0f;
				}
				catch (Exception ex4)
				{
					Exception ex2 = ex4;
					Log.Error("[ATES][Planner] A*: " + ex2.Message);
				}
			}
			else if (state.Path.Count > 0)
			{
				state.Path[state.Path.Count - 1] = destination;
			}
			try
			{
				HandleScpBehavior(npc, state.Target);
			}
			catch (Exception ex)
			{
				Log.Error("[ATES][Planner] SCP: " + ex.Message);
			}
			yield return Timing.WaitForSeconds(0.25f);
		}
		state.Dead = true;
	}

	private IEnumerator<float> MoverLoop(Npc npc, NpcState state)
	{
		while (true)
		{
			if (state.Dead)
			{
				yield break;
			}
			bool alive = false;
			try
			{
				alive = npc.IsAlive;
			}
			catch
			{
			}
			if (!alive)
			{
				break;
			}
			try
			{
				if (state.IsFleeing && state.FleeingFrom != null)
				{
					if (state.FleeingFrom.Role.Is<Exiled.API.Features.Roles.Scp096Role>(out var _))
					{
						npc.Rotation = Quaternion.LookRotation(Vector3.down);
					}
					else
					{
						npc.Follow(state.FleeingFrom, 100f, 0f, 0f);
					}
				}
				else if (state.Target != null)
				{
					npc.Follow(state.Target, 100f, 0f, 0f);
				}
			}
			catch
			{
			}
			try
			{
				List<Vector3> path = state.Path;
				if (path != null && path.Count > 0 && state.WpIdx < path.Count)
				{
					Vector3 wp = path[state.WpIdx];
					float dist = Vector3.Distance(npc.Position, wp);
					if (dist < 0.45f)
					{
						state.WpIdx++;
					}
					else
					{
						float dt = Time.deltaTime;
						Vector3 dir = (wp - npc.Position).normalized;
						Vector3 origin = npc.Position + Vector3.up * 0.8f;
						if (!Physics.Raycast(origin, dir, 0.55f, WaypointGraph.WallMask))
						{
							npc.Position = Vector3.MoveTowards(npc.Position, wp, 4.5f * dt);
						}
						else
						{
							Vector3 right = Quaternion.Euler(0f, 50f, 0f) * dir;
							Vector3 left = Quaternion.Euler(0f, -50f, 0f) * dir;
							if (!Physics.Raycast(origin, right, 0.55f, WaypointGraph.WallMask))
							{
								npc.Position = Vector3.MoveTowards(npc.Position, npc.Position + right, 4.5f * dt);
							}
							else if (!Physics.Raycast(origin, left, 0.55f, WaypointGraph.WallMask))
							{
								npc.Position = Vector3.MoveTowards(npc.Position, npc.Position + left, 4.5f * dt);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("[ATES][Mover] " + ex.Message);
			}
			yield return float.NegativeInfinity;
		}
		state.Dead = true;
	}

	private void HandleScpBehavior(Npc npc, Player target)
	{
		float num = Vector3.Distance(npc.Position, target.Position);
		if (npc.Role.Is<Scp106Role>(out var param))
		{
			if (num < 1.5f)
			{
				param.CapturePlayer(target);
			}
			return;
		}
		if (npc.Role.Is<Scp173Role>(out var param2))
		{
			if (param2.BlinkCooldown <= 0f)
			{
				param2.Blink(target.Position);
			}
			return;
		}
		if (npc.Role.Is<Exiled.API.Features.Roles.Scp096Role>(out var param3))
		{
			if (param3.RageState == Scp096RageState.Docile && param3.EnrageCooldown <= 0f && target.Role.Type != RoleTypeId.Spectator)
			{
				param3.Enrage();
			}
			if (!param3.HasTarget(target))
			{
				param3.AddTarget(target);
			}
			if (num < 1.5f && param3.AttackPossible)
			{
				param3.Attack();
			}
			return;
		}
		if (npc.Role.Is<Scp049Role>(out var scp49))
		{
			scp49.Sense(target);
			if (num < 1.5f && scp49.RemainingAttackCooldown <= 0f)
			{
				scp49.Attack(target);
			}
			Ragdoll ragdoll = (from x in Ragdoll.List
				where scp49.CanResurrect(x)
				select x into r
				orderby Vector3.Distance(npc.Position, r.Position)
				select r).FirstOrDefault();
			if (ragdoll != null && Vector3.Distance(npc.Position, ragdoll.Position) < 3f)
			{
				scp49.Resurrect(ragdoll.Owner);
			}
		}
		if (npc.Role.Is<Scp3114Role>(out var param4))
		{
			DanceType[] array = (from DanceType d in Enum.GetValues(typeof(DanceType))
				where d != DanceType.None
				select d).ToArray();
			DanceType danceType = array[Var.random.Next(array.Length)];
			if (num < 1.5f)
			{
				param4.StartDancing(danceType);
			}
		}
	}

	private IEnumerator<float> ShootNpc(Npc npc)
	{
		Firearm gun = default(Firearm);
		while (true)
		{
			bool alive = false;
			try
			{
				alive = npc.IsAlive;
			}
			catch
			{
			}
			if (!alive)
			{
				break;
			}
			try
			{
				Player target = (from p in Player.List
					where p.IsAlive && p.Role.Side != npc.Role.Side
					orderby Vector3.Distance(npc.Position, p.Position)
					select p).FirstOrDefault();
				int num;
				if (target != null)
				{
					Item currentItem = npc.CurrentItem;
					gun = currentItem as Firearm;
					if (gun != null && target.Zone == npc.Zone && Vector3.Distance(npc.Position, target.Position) < 40f)
					{
						num = ((!target.IsCuffed) ? 1 : 0);
						goto IL_01a9;
					}
				}
				num = 0;
				goto IL_01a9;
				IL_01a9:
				if (num != 0 && !Physics.Linecast(npc.CameraTransform.position, target.CameraTransform.position, LosMask))
				{
					gun.MagazineAmmo = gun.TotalMaxAmmo;
					Shoot(npc);
				}
				gun = null;
			}
			catch (Exception ex)
			{
				Log.Error("[ATES] ShootNpc: " + ex.Message);
			}
			yield return Timing.WaitForSeconds(0.8f);
		}
		try
		{
			npc.Destroy();
			npc.Disconnect();
		}
		catch
		{
		}
	}

	private void Shoot(Npc npc)
	{
		if (npc.CurrentItem is Firearm firearm)
		{
			AutosyncItem @base = firearm.Base;
			if ((object)@base != null && typeof(AutosyncItem).GetProperty("DummyEmulator", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(@base) is DummyKeyEmulator obj)
			{
				typeof(AutosyncItem).GetMethod("AddEntry", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(obj, new object[2]
				{
					ActionName.Shoot,
					false
				});
				typeof(AutosyncItem).GetMethod("RemoveEntry", new Type[1] { typeof(ActionName) })?.Invoke(obj, new object[1] { ActionName.Shoot });
			}
		}
	}
}
