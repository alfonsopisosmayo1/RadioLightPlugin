using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

/// <summary>
/// Escuadrones especiales del MTF. Cada uno sale UNA sola vez por partida y solo como oleada grande
/// (nunca en un refuerzo / mini-oleada). Si se cumplen las dos condiciones, gana Nu-7.
///  · Nu-7 "Hammer Down": 30+ jugadores en el server y 4+ SCP vivos (sin contar zombis).
///    20 % menos daño de SCP, Clase-D y Caos. Equipo pesado propio según el rango.
///  · Beta-7 "Maz Hatters": 5+ zombis (SCP-049-2) vivos. 30 % menos daño de zombis. Equipo normal del MTF.
/// </summary>
internal static class Escuadrones
{
	internal const int MinJugadoresNu7 = 30;

	internal const int MinScpsNu7 = 4;

	internal const int MinZombisBeta7 = 5;

	private const float ReduccionNu7 = 0.8f;

	private const float ReduccionBeta7 = 0.7f;

	internal static bool Nu7Usado;

	internal static bool Beta7Usado;

	/// <summary>Miembros vivos de cada escuadrón.</summary>
	internal static readonly HashSet<Player> Nu7 = new HashSet<Player>();

	internal static readonly HashSet<Player> Beta7 = new HashSet<Player>();

	/// <summary>Jugadores de una oleada especial que todavía no recibieron su equipo (para que no les toque subclase).</summary>
	internal static readonly HashSet<Player> EnOleada = new HashSet<Player>();

	public static void Reiniciar()
	{
		Nu7Usado = false;
		Beta7Usado = false;
		Nu7.Clear();
		Beta7.Clear();
		EnOleada.Clear();
	}

	/// <summary>True si el jugador es (o está por ser) de un escuadrón especial: no se le asignan subclases.</summary>
	public static bool Es(Player p)
	{
		return p != null && (EnOleada.Contains(p) || Nu7.Contains(p) || Beta7.Contains(p));
	}

	/// <summary>Al cambiar de rol (morir, ser revivido, etc.) deja de ser del escuadrón.</summary>
	public static void AlCambiarRol(Player p)
	{
		if (!(p == null))
		{
			Nu7.Remove(p);
			Beta7.Remove(p);
		}
	}

	/// <summary>
	/// Se llama al entrar una oleada (antes de que aparezcan). Si corresponde, convierte la oleada del MTF en
	/// Nu-7 o Beta-7 y devuelve true (así no se reparten las subclases normales del MTF en esta oleada).
	/// </summary>
	public static bool AlRespawnear(RespawningTeamEventArgs ev, List<Player> jugadores)
	{
		try
		{
			if (ev == null || ev.NextKnownTeam != Faction.FoundationStaff || ev.Wave == null || ev.Wave.IsMiniWave || Round.IsEnded || jugadores == null || jugadores.Count == 0)
			{
				return false;
			}
			int scps = 0;
			int zombis = 0;
			foreach (Player item in Player.List)
			{
				if (!(item == null) && item.IsAlive && item.Role.Team == Team.SCPs)
				{
					if (item.Role.Type == RoleTypeId.Scp0492)
					{
						zombis++;
					}
					else
					{
						scps++;
					}
				}
			}
			string tipo = null;
			if (!Nu7Usado && Server.PlayerCount >= MinJugadoresNu7 && scps >= MinScpsNu7)
			{
				tipo = "nu7";
				Nu7Usado = true;
			}
			else if (!Beta7Usado && zombis >= MinZombisBeta7)
			{
				tipo = "beta7";
				Beta7Usado = true;
			}
			if (tipo == null)
			{
				return false;
			}
			foreach (Player p in jugadores.Where((Player x) => !(x == null)).Distinct().ToList())
			{
				EnOleada.Add(p);
				Var.Coroutines.Add(Timing.CallDelayed(Util.Turno(1f), delegate
				{
					Equipar(p, tipo);
				}));
			}
			Anunciar(tipo, jugadores.Count, scps, zombis);
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("[Escuadrones] " + ex);
			return false;
		}
	}

	private static float _silenciarNtfHasta;

	/// <summary>True (una sola vez) si hay que callar el anuncio normal de entrada del MTF porque entró un escuadrón especial.</summary>
	public static bool SilenciarAnuncioNtf()
	{
		if (Time.time < _silenciarNtfHasta)
		{
			_silenciarNtfHasta = 0f;
			return true;
		}
		return false;
	}

	private static void Anunciar(string tipo, int cantidad, int scps, int zombis)
	{
		_silenciarNtfHasta = Time.time + 40f;
		if (tipo == "nu7")
		{
			Log.Info($"[Escuadrones] Entra Nu-7 \"Hammer Down\" ({cantidad} jugadores, {scps} SCP vivos).");
			// música de entrada del Nu-7 para todo el mapa (Configs/Audios/Nu7.ogg)
			Timing.CallDelayed(1.5f, delegate
			{
				Sonido.Mapa("Nu7.ogg");
			});
			// mensaje de CASSIE propio (todas las palabras existen en el vocabulario de CASSIE; "Hammer Down" no, por eso va como NATO H D)
			LabApi.Features.Wrappers.Announcer.Message($"$PITCH_0.8 .g4 . .g6 . $PITCH_1 attention all personnel . emergency override protocol activated . mtfunit nu 7 designated nato_h nato_d hasentered the facility . lethal force authorized . allremaining {scps} scpsubjects . you can not escape . you can not survive . $PITCH_0.85 this is the end", "Atención a todo el personal. Protocolo de anulación de emergencia activado.<split>La unidad <color=#1F5FD1><b>Nu-7 «Hammer Down»</b></color> ha entrado a la instalación. Fuerza letal autorizada.<split>Quedan " + scps + " <color=#EC2121>SCP</color>. No pueden escapar. No pueden sobrevivir.<split><color=#1F5FD1><b>Este es el fin.</b></color>");
		}
		else
		{
			Log.Info($"[Escuadrones] Entra Beta-7 \"Maz Hatters\" ({cantidad} jugadores, {zombis} zombis vivos).");
			LabApi.Features.Wrappers.Announcer.Message("$PITCH_0.9 .g3 . .g5 . $PITCH_1 warning . biological hazard detected . scp 0 4 9 2 infection is critical . emergency decontamination protocol activated . mtfunit beta 7 designated nato_m nato_h hasentered the facility . purge all corrupted scpsubjects . $PITCH_0.9 the plague must stop now", "Advertencia: riesgo biológico detectado. La infección de <color=#EC2121>SCP-049-2</color> es crítica (" + zombis + " zombis).<split>Protocolo de descontaminación de emergencia activado.<split>La unidad <color=#8BC34A><b>Beta-7 «Maz Hatters»</b></color> ha entrado a la instalación.<split>Purguen a todos los sujetos corrompidos. <color=#8BC34A><b>La plaga debe detenerse ya.</b></color>");
		}
	}

	/// <summary>
	/// [comando de admin] Spawnea YA un escuadrón con espectadores (1 capitán, sargentos y soldados rasos).
	/// Cuenta como su única aparición de la partida (después no vuelve a salir solo). Devuelve el mensaje para la consola.
	/// </summary>
	public static string ForzarSpawn(string tipo, int cantidad)
	{
		if (!Round.IsStarted || Round.IsEnded)
		{
			return "La ronda no está en curso.";
		}
		List<Player> list = (from p in Player.List
			where p != null && p.IsConnected && !p.IsNPC && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled && !Var.GenteGulag.Contains(p) && !Var.ColaPractica.Contains(p) && !Var.PeleaPractica.Contains(p)
			orderby Var.random.Next()
			select p).Take(cantidad).ToList();
		if (list.Count == 0)
		{
			return "No hay espectadores para spawnear el escuadrón.";
		}
		if (tipo == "nu7")
		{
			Nu7Usado = true;
		}
		else
		{
			Beta7Usado = true;
		}
		int scps = 0;
		int zombis = 0;
		foreach (Player item in Player.List)
		{
			if (!(item == null) && item.IsAlive && item.Role.Team == Team.SCPs)
			{
				if (item.Role.Type == RoleTypeId.Scp0492)
				{
					zombis++;
				}
				else
				{
					scps++;
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			Player p2 = list[i];
			RoleTypeId rol = ((i == 0) ? RoleTypeId.NtfCaptain : ((i % 3 == 1) ? RoleTypeId.NtfSergeant : RoleTypeId.NtfPrivate));
			EnOleada.Add(p2);
			Var.Coroutines.Add(Timing.CallDelayed(Util.Turno(0.1f), delegate
			{
				try
				{
					if (p2 == null || !p2.IsConnected || p2.Role.Type != RoleTypeId.Spectator)
					{
						EnOleada.Remove(p2);
						return;
					}
					p2.Role.Set(rol, SpawnReason.Respawn, RoleSpawnFlags.All);
					Timing.CallDelayed(1f, delegate
					{
						Equipar(p2, tipo);
					});
				}
				catch (Exception ex)
				{
					EnOleada.Remove(p2);
					Log.Debug("[Escuadrones] spawn forzado " + p2?.Nickname + ": " + ex.Message);
				}
			}));
		}
		Anunciar(tipo, list.Count, scps, zombis);
		return ((tipo == "nu7") ? "Nu-7 «Hammer Down»" : "Beta-7 «Maz Hatters»") + $" spawneado con {list.Count} jugador(es).";
	}

	private static void Equipar(Player p, string tipo)
	{
		EnOleada.Remove(p);
		try
		{
			if (!Util.Vivo(p) || p.Role.Team != Team.FoundationForces)
			{
				return;
			}
			if (tipo == "nu7")
			{
				Nu7.Add(p);
				EquipoNu7(p);
				Util.Bc(p, 6, "<color=#1F5FD1>[ Eres del escuadrón <b>Nu-7 «Hammer Down»</b>. ]</color>", "<color=#1F5FD1>[ Você é do esquadrão <b>Nu-7 «Hammer Down»</b>. ]</color>");
				Util.Bc(p, 8, "<color=#8FB3F0>[ Recibes 20% menos daño de SCPs, Clase-D y Caos. Equipo pesado. ]</color>", "<color=#8FB3F0>[ Você recebe 20% menos dano de SCPs, Classe-D e Caos. Equipamento pesado. ]</color>");
			}
			else
			{
				Beta7.Add(p);
				Util.Bc(p, 6, "<color=#8BC34A>[ Eres del escuadrón <b>Beta-7 «Maz Hatters»</b>. ]</color>", "<color=#8BC34A>[ Você é do esquadrão <b>Beta-7 «Maz Hatters»</b>. ]</color>");
				Util.Bc(p, 8, "<color=#C5E1A5>[ Recibes 30% menos daño de los zombis (SCP-049-2). ]</color>", "<color=#C5E1A5>[ Você recebe 30% menos dano dos zumbis (SCP-049-2). ]</color>");
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Escuadrones] Equipar " + p?.Nickname + ": " + ex.Message);
		}
	}

	/// <summary>Equipo del Nu-7 según el rango (máximo 8 objetos por inventario).</summary>
	private static void EquipoNu7(Player p)
	{
		p.ClearInventory();
		switch (p.Role.Type)
		{
		case RoleTypeId.NtfCaptain:
			p.AddItem(ItemType.KeycardMTFCaptain);
			p.AddItem(ItemType.GunLogicer);
			p.AddItem(ItemType.GunRevolver);
			p.AddItem(ItemType.ArmorHeavy);
			p.AddItem(ItemType.Medkit);
			p.AddItem(ItemType.Medkit);
			p.AddItem(ItemType.Adrenaline);
			p.AddItem(ItemType.GrenadeHE);
			p.AddAmmo(AmmoType.Nato762, 150);
			p.AddAmmo(AmmoType.Ammo44Cal, 24);
			break;
		case RoleTypeId.NtfSergeant:
		case RoleTypeId.NtfSpecialist:
			p.AddItem(ItemType.KeycardMTFOperative);
			p.AddItem(ItemType.GunE11SR);
			p.AddItem(ItemType.GunShotgun);
			p.AddItem(ItemType.ArmorHeavy);
			p.AddItem(ItemType.Medkit);
			p.AddItem(ItemType.Medkit);
			p.AddItem(ItemType.GrenadeHE);
			p.AddItem(ItemType.Radio);
			p.AddAmmo(AmmoType.Nato556, 120);
			p.AddAmmo(AmmoType.Ammo12Gauge, 28);
			break;
		default:
			p.AddItem(ItemType.KeycardMTFPrivate);
			p.AddItem(ItemType.GunCrossvec);
			p.AddItem(ItemType.GunCOM18);
			p.AddItem(ItemType.ArmorHeavy);
			p.AddItem(ItemType.Medkit);
			p.AddItem(ItemType.Medkit);
			p.AddItem(ItemType.Radio);
			p.AddAmmo(AmmoType.Nato9, 160);
			break;
		}
	}

	/// <summary>Reducción de daño de los escuadrones (se registra en Player.Hurting).</summary>
	public static void AlHerir(HurtingEventArgs ev)
	{
		try
		{
			Player player = ev?.Player;
			Player attacker = ev?.Attacker;
			if (player == null || attacker == null || player == attacker || ev.Amount <= 0f)
			{
				return;
			}
			if (Nu7.Count > 0 && Nu7.Contains(player))
			{
				Team team = attacker.Role.Team;
				if (team == Team.SCPs || team == Team.ChaosInsurgency || attacker.Role.Type == RoleTypeId.ClassD)
				{
					ev.Amount *= ReduccionNu7;
				}
			}
			else if (Beta7.Count > 0 && Beta7.Contains(player) && attacker.Role.Type == RoleTypeId.Scp0492)
			{
				ev.Amount *= ReduccionBeta7;
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Escuadrones] daño: " + ex.Message);
		}
	}
}
