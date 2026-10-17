using System.Collections.Generic;
using Exiled.API.Features;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class EventoRandom
{
	private static readonly ItemType[] ArmasRiot = new ItemType[11]
	{
		ItemType.GunFSP9,
		ItemType.GunE11SR,
		ItemType.GunAK,
		ItemType.GunLogicer,
		ItemType.GunShotgun,
		ItemType.GunCrossvec,
		ItemType.GunCOM15,
		ItemType.GunCOM18,
		ItemType.GunRevolver,
		ItemType.GunFRMG0,
		ItemType.GunA7
	};

	public static void Riot()
	{
		Announcer.Clear();
		Announcer.Message("warning . warning . classd personnel has been detected using gun . protocol r i o t activated . all facility guard must terminate all classd personnel in light containment zone", "Advertencia. Advertencia. Se ha detectado personal <color=#FF8E00>Clase-D</color> utilizando armas; protocolo R I O T activado.<split>Todo el personal de <color=#5B6370>seguridad</color> de la instalación debe eliminar a todo el personal de <color=#FF8E00>Clase-D</color> en la zona de contención ligera.");
		foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
		{
			item.ShowHint("\n<color=#e32433>//////////// DISTURBIOS DE <color=#FF8E00>CLASES-D</color> EN PROGRESO ////////////</color>", 10f);
			if (item.Role.Type == RoleTypeId.ClassD)
			{
				ItemType itemType = ArmasRiot[Var.random.Next(ArmasRiot.Length)];
				item.AddItem(itemType);
				item.AddItem(ItemType.ArmorCombat);
				if (Var.AmmoPorArma.TryGetValue(itemType, out var value))
				{
					item.AddAmmo(value, 60);
				}
			}
		}
	}

	public static void Scp008()
	{
		Var.Scp008 = true;
		foreach (Exiled.API.Features.Room room in Var.RoomList)
		{
			room.Color = new Color(0.8f, 0.8f, 0.5f);
		}
		Announcer.Clear();
		Announcer.Message("warning . warning . scp 0 0 8 containment breach has been detected in light containment zone . Immediate evacuation of personnel", "Advertencia. Advertencia. Se ha detectado una brecha de contención del <color=#b3e108>SCP-008</color> en la zona de contención ligera. Evacuación inmediata del personal.");
		List<Exiled.API.Features.Player> list = new List<Exiled.API.Features.Player>(Exiled.API.Features.Player.List);
		for (int i = 0; i < list.Count; i++)
		{
			Exiled.API.Features.Player p = list[i];
			p.ShowHint("\n<color=#e32433>//////////// BRECHA DE CONTENCIÓN DEL <color=#b3e108>SCP-008</color> EN PROGRESO ////////////</color>", 10f);
			if (p.IsNPC || p.IsHost)
			{
				continue;
			}
			if (i % 2 == 0)
			{
				p.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.UseSpawnpoint);
				Timing.CallDelayed(1f, delegate
				{
					if (Util.Vivo(p) && p.Role.Type == RoleTypeId.Scp0492)
					{
						Util.SetVida(p, 1500f);
						p.AddAhp(150f, 150f, 0f);
					}
				});
			}
			else
			{
				p.Role.Set(RoleTypeId.FacilityGuard);
			}
		}
	}
}
