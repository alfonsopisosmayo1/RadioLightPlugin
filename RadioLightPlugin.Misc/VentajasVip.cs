using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class VentajasVip
{
	private static class Modelo
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static object Spawnear(string nombre, Vector3 pos, float escala)
		{
			return Mer.Spawnear(nombre, pos, Quaternion.identity, Vector3.one * escala);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Mover(object m, Vector3 pos, Quaternion rot)
		{
			if (!Mer.Existe(m))
			{
				throw new InvalidOperationException("La mascota ya no existe.");
			}
			Mer.Mover(m, pos, rot);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Destruir(object m)
		{
			Mer.Destruir(m);
		}
	}

	private static int _ronda;

	private static readonly Dictionary<Player, (object Modelo, CoroutineHandle Bucle)> _mascotas = new Dictionary<Player, (object, CoroutineHandle)>();

	public static bool EsVip(Player p)
	{
		if (p == null)
		{
			return false;
		}
		if (p.RemoteAdminAccess)
		{
			return true;
		}
		bool result;
		switch (p.ReferenceHub?.serverRoles?.Group?.BadgeText)
		{
		case "VIP":
		case "VIP+":
		case "Creador de contenido":
		case "Discord Booster":
			result = true;
			break;
		default:
			result = false;
			break;
		}
		return result;
	}

	public static void AlEmpezarRonda()
	{
		_ronda++;
	}

	public static void AlVerificar(VerifiedEventArgs ev)
	{
		string msg = Plugin.Singleton?.Config?.MensajeLobby;
		if (ev?.Player == null || string.IsNullOrWhiteSpace(msg) || Round.IsStarted)
		{
			return;
		}
		Player p = ev.Player;
		Timing.CallDelayed(3f, delegate
		{
			if (p.IsConnected && !Round.IsStarted)
			{
				p.Broadcast(8, msg);
			}
		});
	}

	public static void AlTerminarRonda(RoundEndedEventArgs ev)
	{
		string msg = Plugin.Singleton?.Config?.MensajeFinRonda;
		if (!string.IsNullOrWhiteSpace(msg))
		{
			Timing.CallDelayed(4f, delegate
			{
				Map.Broadcast(8, msg);
			});
		}
	}

	public static void AlGanarGulag(Player ganador)
	{
		if (!EsVip(ganador))
		{
			return;
		}
		Timing.CallDelayed(1.5f, delegate
		{
			if (!(ganador == null) && ganador.IsAlive)
			{
				Map.ExplodeEffect(ganador.Position + Vector3.up * 2f, ProjectileType.FragGrenade);
				Map.Broadcast(4, "<size=30><color=#D4AF37>\ud83d\udc51 " + ganador.Nickname + "</color> <color=#859CC7>ganó el Gulag y vuelve al combate.</color></size>");
			}
		});
	}

	public static bool TieneMascota(Player p)
	{
		return p != null && _mascotas.ContainsKey(p);
	}

	public static string PonerMascota(Player p, string tipo)
	{
		string text = tipo?.ToLower();
		var (text2, escala) = ((text == "aspiradora") ? ("Aspiradora", 0.25f) : ((!(text == "avion")) ? (null, 0f) : ("Avion", 0.05f)));
		if (text2 == null)
		{
			return "Mascotas: .vip pet aspiradora | .vip pet avion";
		}
		if (!p.IsAlive)
		{
			return "Tenés que estar vivo.";
		}
		QuitarMascota(p);
		object obj;
		try
		{
			obj = Modelo.Spawnear(text2, p.Position, escala);
		}
		catch (Exception ex)
		{
			return "No se pudo crear la mascota (¿está el schematic '" + text2 + "'?): " + ex.Message;
		}
		_mascotas[p] = (obj, Timing.RunCoroutine(Seguir(p, obj)));
		return "¡Mascota añadida! Se va cuando morís o cambiás de rol. Quitala con .vip unpet";
	}

	public static void QuitarMascota(Player p)
	{
		if (p == null || !_mascotas.TryGetValue(p, out (object, CoroutineHandle) value))
		{
			return;
		}
		_mascotas.Remove(p);
		Timing.KillCoroutines(value.Item2);
		try
		{
			Modelo.Destruir(value.Item1);
		}
		catch
		{
		}
	}

	private static IEnumerator<float> Seguir(Player p, object modelo)
	{
		RoleTypeId rol = p.Role.Type;
		while (p != null && p.IsConnected && p.IsAlive && p.Role.Type == rol && _mascotas.ContainsKey(p))
		{
			Transform t = p.CameraTransform;
			Vector3 atras2 = t.forward;
			atras2.y = 0f;
			atras2 = ((atras2.sqrMagnitude < 0.01f) ? Vector3.back : (-atras2.normalized));
			Vector3 pos = p.Position + atras2 * 1.3f + Vector3.down * 0.85f;
			try
			{
				Modelo.Mover(modelo, pos, Quaternion.LookRotation(-atras2));
			}
			catch
			{
				break;
			}
			yield return Timing.WaitForSeconds(0.2f);
		}
		QuitarMascota(p);
	}
}
