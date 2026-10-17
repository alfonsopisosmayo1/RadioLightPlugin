using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Dimension106
{
	private static readonly RoomType[] SalasSeguras = new RoomType[14]
	{
		RoomType.HczCrossing,
		RoomType.HczStraight,
		RoomType.HczCurve,
		RoomType.HczIntersection,
		RoomType.HczStraightC,
		RoomType.HczIntersectionJunk,
		RoomType.HczCornerDeep,
		RoomType.EzStraight,
		RoomType.EzCrossing,
		RoomType.EzCurve,
		RoomType.EzIntercom,
		RoomType.LczStraight,
		RoomType.LczCrossing,
		RoomType.LczCurve
	};

	public static bool EstaAdentro(Player p)
	{
		if (p == null)
		{
			return false;
		}
		if (!p.IsInPocketDimension)
		{
			Room currentRoom = p.CurrentRoom;
			if ((object)currentRoom == null || currentRoom.Type != RoomType.Pocket)
			{
				Room room = Room.Get(RoomType.Pocket);
				return room != null && Vector3.Distance(p.Position, room.Position) < 45f;
			}
		}
		return true;
	}

	public static void Revisar(Player p, Vector3 destino)
	{
		try
		{
			if (p == null || !p.IsConnected || !p.IsAlive || p.Role.Type == RoleTypeId.Scp106)
			{
				return;
			}
			if (p.IsEffectActive<PocketCorroding>())
			{
				p.DisableEffect(EffectType.PocketCorroding);
			}
			if (!EstaAdentro(p))
			{
				return;
			}
			Vector3 vector = destino;
			if (vector == Vector3.zero || EstaCerca(vector))
			{
				vector = SalaSegura();
			}
			if (vector == Vector3.zero)
			{
				return;
			}
			p.Position = vector;
			p.DisableEffect(EffectType.PocketCorroding);
			p.EnableEffect(EffectType.Flashed, 0.1f);
			Log.Info("[Dimensión 106] " + p.Nickname + " escapó pero seguía adentro: se lo sacó.");
			Timing.CallDelayed(1.5f, delegate
			{
				if (p.IsConnected && p.IsAlive && EstaAdentro(p))
				{
					Vector3 vector2 = SalaSegura();
					if (vector2 != Vector3.zero)
					{
						p.Position = vector2;
					}
				}
			});
		}
		catch (Exception arg)
		{
			Log.Error($"[Dimensión 106] {arg}");
		}
	}

	private static bool EstaCerca(Vector3 pos)
	{
		Room room = Room.Get(RoomType.Pocket);
		return room != null && Vector3.Distance(pos, room.Position) < 45f;
	}

	private static Vector3 SalaSegura()
	{
		List<Room> list = Room.List.Where((Room r) => r != null && SalasSeguras.Contains(r.Type) && (r.Zone != ZoneType.LightContainment || !Map.IsLczDecontaminated) && (!Warhead.IsDetonated || r.Zone == ZoneType.Surface)).ToList();
		if (list.Count == 0)
		{
			Room room = Room.Get(RoomType.Surface);
			return (room != null) ? RoleTypeId.NtfPrivate.GetRandomSpawnLocation().Position : Vector3.zero;
		}
		return list[Var.random.Next(list.Count)].Position + Vector3.up * 1.5f;
	}
}
