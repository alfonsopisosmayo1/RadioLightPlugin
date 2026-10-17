using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Patches;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Scp3114Natural
{
	private static readonly RoomType[] SalasSeguras = new RoomType[11]
	{
		RoomType.HczCrossing,
		RoomType.HczStraight,
		RoomType.HczCurve,
		RoomType.HczIntersection,
		RoomType.HczStraightC,
		RoomType.HczStraightVariant,
		RoomType.HczStraightPipeRoom,
		RoomType.HczIntersectionJunk,
		RoomType.HczCornerDeep,
		RoomType.HczArmory,
		RoomType.HczHid
	};

	private static readonly HashSet<RoleTypeId> _repartidos = new HashSet<RoleTypeId>();

	private static bool _dado;

	public static void Reiniciar()
	{
		_repartidos.Clear();
		_dado = false;
	}

	public static void AlCambiarRol(ChangingRoleEventArgs ev)
	{
		try
		{
			if (ev?.Player == null || ev.Reason != SpawnReason.RoundStart || !ev.IsAllowed || (Round.IsStarted && Round.ElapsedTime.TotalSeconds > 3.0) || !ConsolaLimpia.Opcion("Scp3114EnVezDeRepetido", porDefecto: true) || Var.EventoEnCurso)
			{
				return;
			}
			RoleTypeId newRole = ev.NewRole;
			if (newRole.GetTeam() != 0 || newRole == RoleTypeId.Scp0492)
			{
				return;
			}
			if (newRole == RoleTypeId.Scp3114)
			{
				_dado = true;
			}
			else
			{
				if (_repartidos.Add(newRole) || _dado)
				{
					return;
				}
				_dado = true;
				ev.NewRole = RoleTypeId.Scp3114;
				Log.Info($"[SCP-3114] A {ev.Player.Nickname} le tocaba un {newRole} repetido: aparece como SCP-3114.");
				Player p = ev.Player;
				Timing.CallDelayed(1f, delegate
				{
					if (p.IsConnected && p.Role.Type == RoleTypeId.Scp3114 && p.Zone == ZoneType.LightContainment)
					{
						LlevarAHeavy(p);
					}
				});
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[SCP-3114] {arg}");
		}
	}

	public static void AlEmpezarRonda()
	{
		if (ConsolaLimpia.Opcion("Scp3114EnVezDeRepetido", porDefecto: true))
		{
			Var.Coroutines.Add(Timing.CallDelayed(2f, Revisar));
		}
	}

	private static void Revisar()
	{
		try
		{
			if (Var.EventoEnCurso || _dado || Player.List.Any((Player p) => p.Role.Type == RoleTypeId.Scp3114))
			{
				return;
			}
			Player repetido = (from p in Player.List
				where p.IsAlive && !p.IsNPC && p.Role.Team == Team.SCPs && p.Role.Type != RoleTypeId.Scp0492
				group p by p.Role.Type into g
				where g.Count() > 1
				select g.Last()).FirstOrDefault();
			if (repetido == null)
			{
				return;
			}
			_dado = true;
			RoleTypeId type = repetido.Role.Type;
			repetido.Role.Set(RoleTypeId.Scp3114, SpawnReason.RoundStart, RoleSpawnFlags.All);
			Log.Info($"[SCP-3114] {repetido.Nickname} era un {type} repetido: ahora es SCP-3114.");
			Timing.CallDelayed(0.5f, delegate
			{
				if (repetido.IsConnected && repetido.Role.Type == RoleTypeId.Scp3114 && repetido.Zone == ZoneType.LightContainment)
				{
					LlevarAHeavy(repetido);
				}
			});
		}
		catch (Exception arg)
		{
			Log.Error($"[SCP-3114] {arg}");
		}
	}

	private static void LlevarAHeavy(Player p)
	{
		List<Room> list = Room.List.Where((Room r) => r.Zone == ZoneType.HeavyContainment && SalasSeguras.Contains(r.Type)).ToList();
		if (list.Count == 0)
		{
			list = Room.List.Where((Room r) => r.Zone == ZoneType.HeavyContainment && r.Type != RoomType.HczTestRoom && r.Type != RoomType.Hcz106 && r.Type != RoomType.Hcz049).ToList();
		}
		if (list.Count != 0)
		{
			Room room = list[Var.random.Next(list.Count)];
			p.Position = room.Position + Vector3.up * 1.5f;
		}
	}
}
