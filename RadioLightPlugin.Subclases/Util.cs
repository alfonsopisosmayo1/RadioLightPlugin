using System;
using System.Collections.Generic;
using System.Globalization;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

internal static class Util
{
	public static bool Brazuca
	{
		get
		{
			Plugin singleton = Plugin.Singleton;
			return singleton != null && singleton.Config?.Brazuca == true;
		}
	}

	public static void Bc(Player player, ushort segundos, string es, string pt = null)
	{
		player.Broadcast(segundos, (Brazuca && pt != null) ? pt : es);
		Presentaciones.Registrar(player, es);
	}

	public static void SetVida(Player player, float vida)
	{
		player.MaxHealth = vida;
		player.Health = vida;
	}

	public static void SubirVida(Player player, float vida)
	{
		if (player.MaxHealth < vida)
		{
			player.MaxHealth = vida;
		}
		player.Health = vida;
	}

	public static void Pasiva(Player player, EffectType tipo, byte intensidad)
	{
		Pasivas.Agregar(player, tipo, intensidad);
	}

	public static void Curar(Player player, float cantidad)
	{
		player.Health = Mathf.Min(player.MaxHealth, player.Health + cantidad);
	}

	public static void Reemplazar(Player player, ItemType viejo, ItemType nuevo)
	{
		foreach (Item item in player.Items)
		{
			if (item.Type != viejo)
			{
				continue;
			}
			player.RemoveItem(item);
			break;
		}
		if (!player.IsInventoryFull)
		{
			player.AddItem(nuevo);
		}
	}

	private static readonly Dictionary<Player, float> _inmuneCaidaHasta = new Dictionary<Player, float>();

	private static readonly Dictionary<Player, Dictionary<string, float>> _cooldownsVisibles = new Dictionary<Player, Dictionary<string, float>>();

	/// <summary>[fix] Registra un cooldown para que se vea en el HUD (panel de habilidad) con la cuenta regresiva.</summary>
	public static void MarcarCooldown(Player p, string nombre, float segundos)
	{
		if (p == null || string.IsNullOrEmpty(nombre) || segundos <= 0f)
		{
			return;
		}
		if (!_cooldownsVisibles.TryGetValue(p, out var value))
		{
			value = (_cooldownsVisibles[p] = new Dictionary<string, float>());
		}
		value[nombre] = Time.time + segundos;
	}

	public static void QuitarCooldown(Player p, string nombre)
	{
		if (p != null && _cooldownsVisibles.TryGetValue(p, out var value))
		{
			value.Remove(nombre);
		}
	}

	/// <summary>Texto con los cooldowns activos del jugador (null si no hay).</summary>
	public static string TextoCooldowns(Player p)
	{
		if (p == null || !_cooldownsVisibles.TryGetValue(p, out var value) || value.Count == 0)
		{
			return null;
		}
		float time = Time.time;
		System.Text.StringBuilder stringBuilder = null;
		List<string> list = null;
		foreach (KeyValuePair<string, float> item in value)
		{
			float num = item.Value - time;
			if (num <= 0f)
			{
				(list ?? (list = new List<string>())).Add(item.Key);
				continue;
			}
			if (stringBuilder == null)
			{
				stringBuilder = new System.Text.StringBuilder(64);
			}
			else
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append("<size=20><color=#C2D0E6>").Append(item.Key).Append(":</color> <color=#AAAAAA>").Append(Mathf.CeilToInt(num)).Append(" s</color></size>");
		}
		if (list != null)
		{
			foreach (string item2 in list)
			{
				value.Remove(item2);
			}
		}
		return stringBuilder?.ToString();
	}

	/// <summary>[fix] true unos segundos después de revivir como zombie: no recibe daño de caída.</summary>
	public static bool InmuneCaida(Player p)
	{
		return p != null && _inmuneCaidaHasta.TryGetValue(p, out var value) && Time.time < value;
	}

	/// <summary>
	/// [fix] Convierte a un jugador en zombie en una posición. Antes se cambiaba el rol con RoleSpawnFlags.None
	/// y recién después se lo movía: el zombie aparecía donde estaba su "cuerpo" de espectador (muchas veces el gulag,
	/// que está muy alto) y al teletransportarlo abajo el juego le contaba la caída y moría.
	/// Ahora se lo pone en la posición ANTES de cambiar el rol (como hace el juego al revivir con SCP-049),
	/// se repite la posición por las dudas y tiene 6 s sin daño de caída. Nunca aparece en el gulag.
	/// </summary>
	public static bool ConvertirEnZombie(Player p, Vector3? posicion, bool usarSpawnpoint = false)
	{
		if (p == null || !p.IsConnected)
		{
			return false;
		}
		if (posicion.HasValue && Coroutines.EnZonaGulag(posicion.Value))
		{
			if (!usarSpawnpoint)
			{
				return false;
			}
			posicion = null;
		}
		Var.GenteGulag.Remove(p);
		Var.ColaPractica.Remove(p);
		_inmuneCaidaHasta[p] = Time.time + 6f;
		if (posicion.HasValue)
		{
			Vector3 destino = posicion.Value + Vector3.up * 0.2f;
			try
			{
				p.ReferenceHub.transform.position = destino;
			}
			catch
			{
			}
			p.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.None);
			p.Position = destino;
			MEC.Timing.CallDelayed(0.3f, delegate
			{
				if (Vivo(p) && p.Role.Type == RoleTypeId.Scp0492 && (p.Position - destino).sqrMagnitude > 9f)
				{
					p.Position = destino;
				}
			});
		}
		else
		{
			p.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.UseSpawnpoint);
		}
		return true;
	}

	private static float _proximoTurno;

	/// <summary>
	/// [rendimiento] Reparte trabajo pesado (subclases, kits, cambios de rol) para que no caiga todo en el mismo frame.
	/// Devuelve el retraso a usar: al menos <paramref name="minimo"/> y separado <paramref name="separacion"/> s del anterior.
	/// Con una oleada de 20 jugadores, en vez de un tirón de 500 ms hay 20 frames normales.
	/// </summary>
	internal static float Turno(float minimo, float separacion = 0.06f)
	{
		float time = Time.time;
		float num = Mathf.Max(time + minimo, _proximoTurno);
		_proximoTurno = num + separacion;
		return num - time;
	}

	public static bool Vivo(Player player)
	{
		return player != null && player.IsConnected && player.IsAlive;
	}

	public static bool EsEnemigo(Player a, Player b)
	{
		return b != null && b != a && b.IsAlive && !b.IsNPC && b.Role.Side != a.Role.Side && b.Role.Side != Side.Tutorial && b.Role.Side != Side.None;
	}

	public static List<Player> Cercanos(Vector3 centro, float radio, Func<Player, bool> filtro)
	{
		List<Player> list = new List<Player>();
		float num = radio * radio;
		foreach (Player item in Player.List)
		{
			if (item.IsAlive && !((item.Position - centro).sqrMagnitude > num) && (filtro == null || filtro(item)))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<Player> HumanosCerca(Player scp, float radio)
	{
		return Cercanos(scp.Position, radio, (Player p) => p.IsHuman && EsEnemigo(scp, p));
	}

	public static string Flecha(Player desde, Vector3 objetivo)
	{
		Vector3 to = objetivo - desde.Position;
		to.y = 0f;
		Vector3 forward = desde.CameraTransform.forward;
		forward.y = 0f;
		if (to.sqrMagnitude < 0.01f || forward.sqrMagnitude < 0.01f)
		{
			return "•";
		}
		float num = Vector3.SignedAngle(forward, to, Vector3.up);
		if (num >= -45f && num <= 45f)
		{
			return "↑";
		}
		if (num > 45f && num < 135f)
		{
			return "→";
		}
		if (num < -45f && num > -135f)
		{
			return "←";
		}
		return "↓";
	}

	public static string Sala(Player p)
	{
		Room currentRoom = p.CurrentRoom;
		return (currentRoom == null) ? "Desconocido" : $"{currentRoom.Type} ({currentRoom.Zone})";
	}

	public static void KitMinimo(Player player)
	{
		if (!Vivo(player) || player.IsNPC || !player.IsHuman)
		{
			return;
		}
		(ItemType, bool, ItemType) tuple = player.Role.Type switch
		{
			RoleTypeId.Scientist => (ItemType.KeycardScientist, false, ItemType.None), 
			RoleTypeId.FacilityGuard => (ItemType.KeycardGuard, true, ItemType.ArmorLight), 
			RoleTypeId.NtfPrivate => (ItemType.KeycardMTFPrivate, true, ItemType.ArmorCombat), 
			RoleTypeId.NtfSergeant => (ItemType.KeycardMTFOperative, true, ItemType.ArmorCombat), 
			RoleTypeId.NtfSpecialist => (ItemType.KeycardMTFOperative, true, ItemType.ArmorCombat), 
			RoleTypeId.NtfCaptain => (ItemType.KeycardMTFCaptain, true, ItemType.ArmorHeavy), 
			RoleTypeId.ChaosConscript => (ItemType.KeycardChaosInsurgency, false, ItemType.ArmorCombat), 
			RoleTypeId.ChaosRifleman => (ItemType.KeycardChaosInsurgency, false, ItemType.ArmorCombat), 
			RoleTypeId.ChaosRepressor => (ItemType.KeycardChaosInsurgency, false, ItemType.ArmorCombat), 
			RoleTypeId.ChaosMarauder => (ItemType.KeycardChaosInsurgency, false, ItemType.ArmorHeavy), 
			_ => (ItemType.None, false, ItemType.None), 
		};
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		foreach (Item item in player.Items)
		{
			if (item.Type.IsKeycard())
			{
				flag = true;
			}
			else if (item.Type == ItemType.Radio)
			{
				flag2 = true;
			}
			else if (item.Type.IsArmor())
			{
				flag3 = true;
			}
		}
		if (tuple.Item1 != ItemType.None && !flag)
		{
			Dar(player, tuple.Item1);
		}
		if (tuple.Item2 && !flag2)
		{
			Dar(player, ItemType.Radio);
		}
		if (tuple.Item3 != ItemType.None && !flag3)
		{
			Dar(player, tuple.Item3);
		}
	}

	private static void Dar(Player player, ItemType tipo)
	{
		if (!player.IsInventoryFull)
		{
			player.AddItem(tipo);
		}
		else
		{
			Pickup.CreateAndSpawn(tipo, player.Position + Vector3.up * 0.3f, Quaternion.identity, player);
		}
	}

	public static Vector3? SalidaAscensor(ElevatorType tipo, float adelante, float costado = 0f)
	{
		Lift lift = Lift.Get(tipo);
		if (lift == null)
		{
			return null;
		}
		Door door = null;
		foreach (ElevatorDoor door2 in lift.Doors)
		{
			if (door == null || door2.Position.y > door.Position.y)
			{
				door = door2;
			}
		}
		if (door == null)
		{
			return null;
		}
		Vector3 vector = door.Position - lift.Position;
		vector.y = 0f;
		if (vector.sqrMagnitude < 0.01f)
		{
			vector = door.Transform.forward;
		}
		vector.y = 0f;
		vector.Normalize();
		Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
		return door.Position + vector * adelante + vector2 * costado;
	}

	public static bool EsCheckpoint(Door door)
	{
		if (door == null)
		{
			return false;
		}
		if (door.IsCheckpoint || door.IsPartOfCheckpoint)
		{
			return true;
		}
		if (door.Type.ToString().IndexOf("Checkpoint", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		foreach (Room room in door.Rooms)
		{
			if (room != null && room.Type.ToString().IndexOf("Checkpoint", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		foreach (Door item in Door.List)
		{
			if (item != door && item.IsCheckpoint && (item.Position - door.Position).sqrMagnitude < 16f)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryLeerVector(string texto, out Vector3 v)
	{
		v = default(Vector3);
		if (string.IsNullOrWhiteSpace(texto))
		{
			return false;
		}
		string[] array = texto.Split(new char[1] { ',' });
		if (array.Length != 3)
		{
			return false;
		}
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		NumberStyles style = NumberStyles.Float;
		if (!float.TryParse(array[0].Trim(), style, invariantCulture, out var result) || !float.TryParse(array[1].Trim(), style, invariantCulture, out var result2) || !float.TryParse(array[2].Trim(), style, invariantCulture, out var result3))
		{
			return false;
		}
		v = new Vector3(result, result2, result3);
		return true;
	}

	public static string NombreRol(RoleTypeId rol)
	{
		return rol switch
		{
			RoleTypeId.ClassD => "Clase-D", 
			RoleTypeId.Scientist => "Científico", 
			RoleTypeId.FacilityGuard => "Guardia", 
			RoleTypeId.NtfPrivate => "Cadete MTF", 
			RoleTypeId.NtfSergeant => "Sargento MTF", 
			RoleTypeId.NtfSpecialist => "Especialista MTF", 
			RoleTypeId.NtfCaptain => "Capitán MTF", 
			RoleTypeId.ChaosConscript => "Recluta IdC", 
			RoleTypeId.ChaosRifleman => "Fusilero IdC", 
			RoleTypeId.ChaosRepressor => "Represor IdC", 
			RoleTypeId.ChaosMarauder => "Merodeador IdC", 
			RoleTypeId.Scp049 => "SCP-049", 
			RoleTypeId.Scp0492 => "SCP-049-2", 
			RoleTypeId.Scp079 => "SCP-079", 
			RoleTypeId.Scp096 => "SCP-096", 
			RoleTypeId.Scp106 => "SCP-106", 
			RoleTypeId.Scp173 => "SCP-173", 
			RoleTypeId.Scp939 => "SCP-939", 
			RoleTypeId.Scp3114 => "SCP-3114", 
			_ => rol.ToString(), 
		};
	}
}
