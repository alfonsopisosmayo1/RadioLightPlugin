using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class ReemplazoAfk
{
	private sealed class Foto
	{
		public Player Viejo;

		public RoleTypeId Rol;

		public Vector3 Posicion;

		public float Tiempo;

		public float Vida;

		public float VidaMaxima;

		public Vector3 Escala;

		public readonly List<HashSet<Player>> Sets = new List<HashSet<Player>>();

		public readonly List<HashSet<string>> SetsId = new List<HashSet<string>>();

		public readonly List<HashSet<ReferenceHub>> SetsHub = new List<HashSet<ReferenceHub>>();

		public readonly List<FieldInfo> Textos = new List<FieldInfo>();

		public bool? DeadRinger;

		public bool TieneSubclase;

		public readonly HashSet<ushort> Seriales = new HashSet<ushort>();

		public object Habilidad;

		public readonly List<(EffectType Tipo, byte Intensidad, float Duracion)> Efectos = new List<(EffectType, byte, float)>();
	}

	private static readonly Dictionary<Player, float> _ultimoSpawn = new Dictionary<Player, float>();

	private static readonly FieldInfo[] CamposVar = typeof(Var).GetFields(BindingFlags.Static | BindingFlags.Public);

	private static bool Ignorar(string nombre)
	{
		return nombre.StartsWith("Cooldown") || nombre == "HeavyCooldown" || nombre == "Estuvo";
	}

	public static void AlAparecer(Player p)
	{
		if (p != null)
		{
			_ultimoSpawn[p] = Time.time;
		}
	}

	public static void AlSalir(Player p)
	{
		_ultimoSpawn.Remove(p);
	}

	public static void Limpiar()
	{
		_ultimoSpawn.Clear();
	}

	public static void AntesDeIrse(Player p, RoleTypeId nuevoRol, SpawnReason razon)
	{
		if (!(p == null) && p.IsAlive && nuevoRol == RoleTypeId.Spectator && razon != SpawnReason.Died)
		{
			Foto foto = Capturar(p);
			Timing.CallDelayed(1.5f, delegate
			{
				Buscar(foto);
			});
		}
	}

	private static Foto Capturar(Player p)
	{
		Foto foto = new Foto
		{
			Viejo = p,
			Rol = p.Role.Type,
			Posicion = p.Position,
			Tiempo = Time.time,
			Vida = p.Health,
			VidaMaxima = p.MaxHealth,
			Escala = p.Scale,
			Habilidad = Habilidades.Exportar(p)
		};
		FieldInfo[] camposVar = CamposVar;
		foreach (FieldInfo fieldInfo in camposVar)
		{
			if (Ignorar(fieldInfo.Name))
			{
				continue;
			}
			object value = fieldInfo.GetValue(null);
			object obj = value;
			object obj2 = obj;
			if (!(obj2 is HashSet<Player> hashSet))
			{
				if (!(obj2 is HashSet<string> hashSet2))
				{
					if (!(obj2 is HashSet<ReferenceHub> hashSet3))
					{
						if (obj2 is string text && fieldInfo.FieldType == typeof(string) && !string.IsNullOrEmpty(text) && text == p.UserId)
						{
							foto.Textos.Add(fieldInfo);
						}
					}
					else if (hashSet3.Contains(p.ReferenceHub))
					{
						foto.SetsHub.Add(hashSet3);
					}
				}
				else if (hashSet2.Contains(p.UserId))
				{
					foto.SetsId.Add(hashSet2);
				}
			}
			else if (hashSet.Contains(p))
			{
				foto.Sets.Add(hashSet);
			}
		}
		if (Var.DeadRinger.TryGetValue(p, out var value2))
		{
			foto.DeadRinger = value2;
		}
		foreach (Item item in p.Items)
		{
			if (item != null)
			{
				foto.Seriales.Add(item.Serial);
			}
		}
		foto.TieneSubclase = foto.Habilidad != null || foto.Sets.Count > 0 || foto.SetsId.Count > 0 || foto.SetsHub.Count > 0 || foto.Textos.Count > 0;
		if (!foto.TieneSubclase)
		{
			return foto;
		}
		foreach (StatusEffectBase activeEffect in p.ActiveEffects)
		{
			if (!(activeEffect == null) && activeEffect.IsEnabled && activeEffect.TryGetEffectType(out var effect))
			{
				foto.Efectos.Add((effect, activeEffect.Intensity, (activeEffect.TimeLeft > 0f) ? activeEffect.TimeLeft : 0f));
			}
		}
		return foto;
	}

	private static void Buscar(Foto f)
	{
		float value;
		Player player = (from x in Player.List
			where x != f.Viejo && x.IsAlive && !x.IsNPC && x.Role.Type == f.Rol && _ultimoSpawn.TryGetValue(x, out value) && Mathf.Abs(value - f.Tiempo) <= 3f && (f.Rol == RoleTypeId.Scp079 || (x.Position - f.Posicion).sqrMagnitude <= 36f)
			orderby (x.Position - f.Posicion).sqrMagnitude
			select x).FirstOrDefault();
		if (player == null)
		{
			return;
		}
		BorrarCosasDelAfk(f);
		Timing.CallDelayed(1.5f, delegate
		{
			BorrarCosasDelAfk(f);
		});
		if (!f.TieneSubclase)
		{
			return;
		}
		Var.QuitarSubclases(player);
		foreach (HashSet<Player> set in f.Sets)
		{
			set.Add(player);
		}
		foreach (HashSet<string> item in f.SetsId)
		{
			item.Add(player.UserId);
		}
		foreach (HashSet<ReferenceHub> item2 in f.SetsHub)
		{
			item2.Add(player.ReferenceHub);
		}
		foreach (FieldInfo texto in f.Textos)
		{
			texto.SetValue(null, player.UserId);
		}
		if (f.DeadRinger.HasValue)
		{
			Var.DeadRinger[player] = f.DeadRinger.Value;
		}
		if (f.Habilidad != null)
		{
			Habilidades.Importar(player, f.Habilidad);
		}
		if (f.Rol != RoleTypeId.Scp079)
		{
			player.Scale = f.Escala;
			player.MaxHealth = f.VidaMaxima;
			player.Health = Mathf.Clamp(f.Vida, 1f, f.VidaMaxima);
		}
		foreach (var (type, intensity, duration) in f.Efectos)
		{
			player.EnableEffect(type, intensity, duration);
		}
		ReiniciarPasivas(player);
		string text = Habilidades.Nombre(player);
		player.Broadcast(6, "<color=#57e5f7>Heredaste la subclase del jugador AFK.</color>" + ((text != null) ? ("\n<color=#859CC7>Habilidad: <b>" + text + "</b> (ALT)</color>") : string.Empty));
		Log.Info("[AFK] " + player.Nickname + " heredó la subclase de " + f.Viejo?.Nickname + ".");
	}

	private static void BorrarCosasDelAfk(Foto f)
	{
		try
		{
			int num = 0;
			foreach (Pickup item in Pickup.List.ToList())
			{
				if (item != null)
				{
					float sqrMagnitude = (item.Position - f.Posicion).sqrMagnitude;
					bool flag = f.Seriales.Contains(item.Serial);
					bool flag2 = f.Viejo != null && item.PreviousOwner == f.Viejo && sqrMagnitude < 36f;
					bool flag3 = item.Type.IsAmmo() && sqrMagnitude < 9f;
					if (flag || flag2 || flag3)
					{
						item.Destroy();
						num++;
					}
				}
			}
			if (num > 0)
			{
				Log.Info($"[AFK] Se borraron {num} cosas que soltó {f.Viejo?.Nickname}.");
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[AFK] Borrar cosas: " + ex.Message);
		}
	}

	private static void ReiniciarPasivas(Player p)
	{
		if (Var.Medic.Contains(p))
		{
			Var.Coroutines.Add(Timing.RunCoroutine(MTF.MedicHealAura(p)));
		}
		if (Var.Vidente.Contains(p))
		{
			Var.Coroutines.Add(Timing.RunCoroutine(Cientifico.VidenteRadar(p)));
		}
		if (Var.Scp990.Contains(p))
		{
			Var.Coroutines.Add(Timing.RunCoroutine(Cientifico.Scp990Aura(p)));
		}
		if (Var.Scp181.Contains(p))
		{
			Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.ScpCheck(p)));
		}
		if (Var.Impersonator.Contains(p))
		{
			Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.GuardCheck(p)));
		}
		if (Var.HeadGuard.Contains(p))
		{
			Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.HeadGuardBC(p)));
		}
	}
}
