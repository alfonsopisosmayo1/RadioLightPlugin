using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using MapGeneration.Distributors;
using PlayerRoles;
using UnityEngine;

namespace RadioLightPlugin.Misc;

/// <summary>
/// Limpieza anti-lag SEGURA: solo borra basura que tiraron los jugadores y que está lejos de todos.
/// Nunca toca: objetos de vitrinas/lockers/pedestales, objetos del mapa (spawn natural o de MER),
/// objetos creados por plugins, armas/objetos personalizados (CustomItems), objetos SCP, armas especiales,
/// tarjetas altas, Compuesto C ni objetos bloqueados o en uso.
/// </summary>
internal static class Limpieza
{
	private static readonly HashSet<ItemType> TiposProtegidos = new HashSet<ItemType>
	{
		ItemType.ParticleDisruptor,
		ItemType.GunSCP127,
		ItemType.Jailbird,
		ItemType.MicroHID,
		ItemType.GunCom45,
		ItemType.GunFRMG0,
		ItemType.KeycardO5,
		ItemType.KeycardFacilityManager,
		ItemType.KeycardChaosInsurgency,
		ItemType.KeycardContainmentEngineer,
		ItemType.Lantern,
		ItemType.Adrenaline
	};

	private static readonly HashSet<ushort> _enLocker = new HashSet<ushort>();

	private static readonly HashSet<int> _custom = new HashSet<int>();

	private static readonly List<Vector3> _vivos = new List<Vector3>();

	private static readonly List<KeyValuePair<Pickup, float>> _candidatos = new List<KeyValuePair<Pickup, float>>();

	private static PropertyInfo _registrados;

	private static PropertyInfo _seriales;

	private static bool _buscado;

	/// <summary>Borra como mucho <paramref name="max"/> objetos basura (primero munición y lo más lejano). Devuelve cuántos borró.</summary>
	internal static int LimpiarBasura(int max)
	{
		try
		{
			RefrescarProtegidos();
			_vivos.Clear();
			foreach (Player player in Player.List)
			{
				if (player != null && player.IsAlive)
				{
					_vivos.Add(player.Position);
				}
			}
			_candidatos.Clear();
			foreach (Pickup pickup in Pickup.List)
			{
				if (!EsBasura(pickup))
				{
					continue;
				}
				Vector3 position = pickup.Position;
				float num = float.MaxValue;
				for (int i = 0; i < _vivos.Count; i++)
				{
					float sqrMagnitude = (_vivos[i] - position).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
					}
				}
				if (!(num < 144f))
				{
					float value = Mathf.Min(num, 1000000f) + (pickup.Category == ItemCategory.Ammo ? 2000000f : 0f);
					_candidatos.Add(new KeyValuePair<Pickup, float>(pickup, value));
				}
			}
			if (_candidatos.Count == 0)
			{
				return 0;
			}
			_candidatos.Sort((KeyValuePair<Pickup, float> a, KeyValuePair<Pickup, float> b) => b.Value.CompareTo(a.Value));
			int num2 = 0;
			for (int j = 0; j < _candidatos.Count && num2 < max; j++)
			{
				try
				{
					_candidatos[j].Key.Destroy();
					num2++;
				}
				catch
				{
				}
			}
			_candidatos.Clear();
			return num2;
		}
		catch (Exception ex)
		{
			Log.Debug("[Limpieza] " + ex.Message);
			return 0;
		}
	}

	/// <summary>Borra cuerpos viejos (más de 40 s) que no estén cerca de un SCP-049. Devuelve cuántos borró.</summary>
	internal static int LimpiarCuerposViejos(int max)
	{
		int num = 0;
		try
		{
			List<Vector3> list = (from p in Player.List
				where p != null && p.Role.Type == RoleTypeId.Scp049
				select p.Position).ToList();
			foreach (Ragdoll item in Ragdoll.List.ToList())
			{
				if (num >= max)
				{
					break;
				}
				if (item != null && !(item.ExistenceTime < 40f))
				{
					Vector3 pos = item.Position;
					if (!list.Any((Vector3 d) => (d - pos).sqrMagnitude < 400f))
					{
						item.Destroy();
						num++;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Limpieza] cuerpos: " + ex.Message);
		}
		return num;
	}

	/// <summary>Llamar una vez antes de recorrer los objetos con <see cref="BorrableEnOleada"/>.</summary>
	internal static void Preparar()
	{
		RefrescarProtegidos();
	}

	/// <summary>
	/// Limpieza al entrar una oleada: solo la basura que soltaron los jugadores (munición, linternas, monedas, tarjetas bajas…)
	/// y TODO lo que quedó en LCZ si ya se descontaminó (nadie puede volver). Nunca vitrinas, lockers, objetos del mapa/MER,
	/// armas personalizadas ni objetos especiales.
	/// </summary>
	internal static bool BorrableEnOleada(Pickup pk, bool tipoBasura)
	{
		try
		{
			if (pk?.Base == null || pk.IsLocked || pk.InUse)
			{
				return false;
			}
			ushort serial = pk.Serial;
			if (_custom.Contains(serial) || Var.CompuestoCSeriales.Contains(serial))
			{
				return false;
			}
			if (Map.IsLczDecontaminated && pk.Room != null && pk.Room.Zone == Exiled.API.Enums.ZoneType.LightContainment)
			{
				return true;
			}
			return tipoBasura && EsBasura(pk);
		}
		catch
		{
			return false;
		}
	}

	private static bool EsBasura(Pickup pk)
	{
		if (pk?.Base == null || !pk.GameObject.activeInHierarchy)
		{
			return false;
		}
		if (pk.IsLocked || pk.InUse)
		{
			return false;
		}
		// solo lo que soltó o dejó al morir un jugador; lo del mapa, MER y plugins no tiene dueño jugador
		Player previousOwner = pk.PreviousOwner;
		if (previousOwner == null || previousOwner.IsHost)
		{
			return false;
		}
		if (pk.Transform.parent != null)
		{
			return false;
		}
		ItemType type = pk.Type;
		if (TiposProtegidos.Contains(type) || type.ToString().IndexOf("Custom", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		string text = pk.Category.ToString();
		if (text == "SCPItem" || text == "SpecialWeapon")
		{
			return false;
		}
		ushort serial = pk.Serial;
		if (_enLocker.Contains(serial) || _custom.Contains(serial) || Var.CompuestoCSeriales.Contains(serial))
		{
			return false;
		}
		return true;
	}

	private static void RefrescarProtegidos()
	{
		_enLocker.Clear();
		try
		{
			foreach (Exiled.API.Features.Lockers.Locker item in Exiled.API.Features.Lockers.Locker.List)
			{
				LockerChamber[] array = item?.Base?.Chambers;
				if (array == null)
				{
					continue;
				}
				foreach (LockerChamber lockerChamber in array)
				{
					if (lockerChamber == null)
					{
						continue;
					}
					foreach (InventorySystem.Items.Pickups.ItemPickupBase item2 in lockerChamber.Content)
					{
						if (item2 != null)
						{
							_enLocker.Add(item2.Info.Serial);
						}
					}
					foreach (InventorySystem.Items.Pickups.ItemPickupBase item3 in lockerChamber.ToBeSpawned)
					{
						if (item3 != null)
						{
							_enLocker.Add(item3.Info.Serial);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Limpieza] lockers: " + ex.Message);
		}
		_custom.Clear();
		try
		{
			if (!_buscado)
			{
				_buscado = true;
				Type type = AppDomain.CurrentDomain.GetAssemblies().Select((Assembly a) => a.GetType("Exiled.CustomItems.API.Features.CustomItem", throwOnError: false)).FirstOrDefault((Type t) => t != null);
				_registrados = type?.GetProperty("Registered", BindingFlags.Static | BindingFlags.Public);
				_seriales = type?.GetProperty("TrackedSerials", BindingFlags.Instance | BindingFlags.Public);
			}
			if (_registrados == null || _seriales == null || !(_registrados.GetValue(null) is IEnumerable enumerable))
			{
				return;
			}
			foreach (object item4 in enumerable)
			{
				if (item4 != null && _seriales.GetValue(item4) is IEnumerable<int> enumerable2)
				{
					foreach (int item5 in enumerable2)
					{
						_custom.Add(item5);
					}
				}
			}
		}
		catch (Exception ex2)
		{
			Log.Debug("[Limpieza] custom items: " + ex2.Message);
		}
	}
}
