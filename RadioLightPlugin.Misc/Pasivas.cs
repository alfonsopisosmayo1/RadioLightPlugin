using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Command;

namespace RadioLightPlugin.Misc;

public static class Pasivas
{
	private sealed class Registro
	{
		public RoleTypeId Rol;

		public readonly Dictionary<EffectType, byte> Efectos = new Dictionary<EffectType, byte>();
	}

	private static readonly Dictionary<Player, Registro> _registro = new Dictionary<Player, Registro>();

	private static CoroutineHandle _bucle;

	private static readonly EffectType[] NuncaInfinitos = new EffectType[10]
	{
		EffectType.Slowness,
		EffectType.Poisoned,
		EffectType.Ensnared,
		EffectType.Concussed,
		EffectType.Blurred,
		EffectType.Deafened,
		EffectType.Exhausted,
		EffectType.MovementBoost,
		EffectType.DamageReduction,
		EffectType.SilentWalk
	};

	private static readonly Dictionary<Player, RoleTypeId> _exentos = new Dictionary<Player, RoleTypeId>();

	public static void Iniciar()
	{
		Timing.KillCoroutines(_bucle);
		_bucle = Timing.RunCoroutine(Bucle());
	}

	public static void Detener()
	{
		Timing.KillCoroutines(_bucle);
		_registro.Clear();
	}

	public static void Agregar(Player p, EffectType tipo, byte intensidad)
	{
		if (!(p == null) && p.IsAlive)
		{
			p.EnableEffect(tipo, intensidad);
			if (!_registro.TryGetValue(p, out var value) || value.Rol != p.Role.Type)
			{
				Dictionary<Player, Registro> registro = _registro;
				Registro obj = new Registro
				{
					Rol = p.Role.Type
				};
				value = obj;
				registro[p] = obj;
			}
			value.Efectos[tipo] = intensidad;
		}
	}

	public static void Quitar(Player p)
	{
		if (!(p == null))
		{
			_registro.Remove(p);
			_exentos.Remove(p);
		}
	}

	public static void AlCambiarRol(ChangingRoleEventArgs ev)
	{
		Quitar(ev?.Player);
	}

	public static void AlMorir(DiedEventArgs ev)
	{
		Quitar(ev?.Player);
	}

	public static void AlUsarItem(UsedItemEventArgs ev)
	{
		Player p = ev?.Player;
		if (!(p == null) && _registro.ContainsKey(p))
		{
			Timing.CallDelayed(0.3f, delegate
			{
				Revisar(p);
			});
		}
	}

	private static IEnumerator<float> Bucle()
	{
		while (true)
		{
			yield return Timing.WaitForSeconds(2f);
			try
			{
				foreach (Player p in _registro.Keys.ToList())
				{
					Revisar(p);
				}
				SacarEfectosColgados();
			}
			catch (Exception ex)
			{
				Exception e = ex;
				Log.Debug("[Pasivas] " + e.Message);
			}
		}
	}

	private static void SacarEfectosColgados()
	{
		foreach (Player item in Player.List)
		{
			if (item == null || item.IsNPC || !item.IsAlive || item.Role.Type == RoleTypeId.Tutorial)
			{
				continue;
			}
			_registro.TryGetValue(item, out var value);
			EffectType[] nuncaInfinitos = NuncaInfinitos;
			foreach (EffectType effectType in nuncaInfinitos)
			{
				try
				{
					if ((value == null || !value.Efectos.ContainsKey(effectType)) && item.TryGetEffect(effectType, out var statusEffect) && statusEffect.IsEnabled && !(statusEffect.Duration > 0f) && !EsDelJuegoInfinito(item, effectType))
					{
						item.DisableEffect(effectType);
						Log.Debug($"[Pasivas] {item.Nickname} tenía {effectType} infinito sin motivo: se le sacó.");
					}
				}
				catch
				{
				}
			}
		}
	}

	public static void Exento(Player p)
	{
		if (p != null)
		{
			_exentos[p] = p.Role.Type;
		}
	}

	private static bool EsDelJuegoInfinito(Player p, EffectType tipo)
	{
		if (Vip.FantasmaTemp.Contains(p.UserId))
		{
			return true;
		}
		if (_exentos.TryGetValue(p, out var value))
		{
			if (value == p.Role.Type)
			{
				return true;
			}
			_exentos.Remove(p);
		}
		return false;
	}

	private static void Revisar(Player p)
	{
		if (p == null || !_registro.TryGetValue(p, out var value))
		{
			return;
		}
		if (!p.IsConnected || !p.IsAlive || p.Role.Type != value.Rol)
		{
			_registro.Remove(p);
			return;
		}
		foreach (KeyValuePair<EffectType, byte> efecto in value.Efectos)
		{
			try
			{
				if (!p.TryGetEffect(efecto.Key, out var statusEffect) || !statusEffect.IsEnabled || statusEffect.Intensity < efecto.Value)
				{
					p.EnableEffect(efecto.Key, efecto.Value);
				}
			}
			catch
			{
			}
		}
	}
}
