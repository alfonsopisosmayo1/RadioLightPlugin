using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Var
{
	public static readonly List<ItemType> Primaria = new List<ItemType>
	{
		ItemType.GunAK,
		ItemType.GunCOM15,
		ItemType.GunCOM18,
		ItemType.GunCrossvec,
		ItemType.GunE11SR,
		ItemType.GunFRMG0,
		ItemType.GunFSP9,
		ItemType.GunRevolver,
		ItemType.GunSCP127,
		ItemType.GunShotgun
	};

	public static readonly Dictionary<ItemType, AmmoType> AmmoPorArma = new Dictionary<ItemType, AmmoType>
	{
		{
			ItemType.GunA7,
			AmmoType.Nato762
		},
		{
			ItemType.GunAK,
			AmmoType.Nato762
		},
		{
			ItemType.GunLogicer,
			AmmoType.Nato762
		},
		{
			ItemType.GunCOM15,
			AmmoType.Nato9
		},
		{
			ItemType.GunCOM18,
			AmmoType.Nato9
		},
		{
			ItemType.GunCom45,
			AmmoType.Nato9
		},
		{
			ItemType.GunCrossvec,
			AmmoType.Nato9
		},
		{
			ItemType.GunFSP9,
			AmmoType.Nato9
		},
		{
			ItemType.GunE11SR,
			AmmoType.Nato556
		},
		{
			ItemType.GunFRMG0,
			AmmoType.Nato556
		},
		{
			ItemType.GunRevolver,
			AmmoType.Ammo44Cal
		},
		{
			ItemType.GunShotgun,
			AmmoType.Ammo12Gauge
		}
	};

	public static List<Player> GenteGulag = new List<Player>();

	public static List<Player> ColaPractica = new List<Player>();

	public static List<Player> PeleaPractica = new List<Player>();

	public static float PracticaInicio;

	public static bool PracticaUltimoEnPie;

	public static bool Gulag2v2;

	public static HashSet<Player> ManiExplosivo = new HashSet<Player>();

	public static HashSet<string> Estuvo = new HashSet<string>();

	public static List<Player> Gulag1 = new List<Player>();

	public static List<Player> Gulag2 = new List<Player>();

	public static List<CoroutineHandle> GulagCoroutine = new List<CoroutineHandle>();

	public static List<Exiled.API.Features.Toys.Light> lucesPuertas = new List<Exiled.API.Features.Toys.Light>();

	public static readonly Dictionary<string, float> HurtingHintCooldown = new Dictionary<string, float>();

	public const float HurtingHintCooldownSeconds = 0.2f;

	public static readonly Dictionary<string, float> LastHpInfoUpdate = new Dictionary<string, float>();

	public const float HpInfoUpdateInterval = 0.1f;

	public static readonly CandyKindID[] candyTypes = new CandyKindID[7]
	{
		CandyKindID.Rainbow,
		CandyKindID.Yellow,
		CandyKindID.Purple,
		CandyKindID.Red,
		CandyKindID.Green,
		CandyKindID.Blue,
		CandyKindID.Pink
	};

	public static bool Scp008;

	public static bool MachetazoSpawned;

	public static bool GrupoDeRespuestaSpawned;

	public static bool YaSpawneó;

	public static bool BetterOmegaWarheadActivated;

	public static bool Nukerun;

	public static bool railgun;

	public static bool TraidorTP;

	public static bool TeslaCassieBool;

	public static bool NoEsPrimerSpawnEquipo;

	public static bool AñoNuevo;

	public static bool EventoEnCurso;

	public static float OmegaTimer;

	public static float OmegaEndTime;

	public static int papanum = 1;

	public static readonly System.Random random = new System.Random();

	public static List<Room> RoomList = new List<Room>();

	public static List<CoroutineHandle> Coroutines = new List<CoroutineHandle>();

	public static List<CoroutineHandle> OmegaCoroutines = new List<CoroutineHandle>();

	public static CoroutineHandle NukeTimerHandle;

	public static HashSet<string> Bulldozers = new HashSet<string>();

	public static HashSet<string> Heavys = new HashSet<string>();

	public static HashSet<string> Traidor = new HashSet<string>();

	public static HashSet<string> HeavyCooldown = new HashSet<string>();

	public static HashSet<string> Scout = new HashSet<string>();

	public static string CientEjec = string.Empty;

	public static string Militar = string.Empty;

	public static string Machetero = string.Empty;

	public static string Bomba = string.Empty;

	public static HashSet<Player> HeadGuard = new HashSet<Player>();

	public static readonly HashSet<ushort> CompuestoCSeriales = new HashSet<ushort>();

	public static HashSet<Player> CooldownHeadguard = new HashSet<Player>();

	public static HashSet<Player> Spy = new HashSet<Player>();

	public static HashSet<Player> CooldownSpy = new HashSet<Player>();

	public static HashSet<Player> Disfrazados = new HashSet<Player>();

	public static Dictionary<Player, bool> DeadRinger = new Dictionary<Player, bool>();

	public static HashSet<Player> DeadRingerUsado = new HashSet<Player>();

	public static HashSet<Player> Taser = new HashSet<Player>();

	public static HashSet<Player> CooldownTaser = new HashSet<Player>();

	public static HashSet<Player> Demoman = new HashSet<Player>();

	public static HashSet<Player> Impersonator = new HashSet<Player>();

	public static HashSet<Player> CooldownImp = new HashSet<Player>();

	public static HashSet<Player> Scp181 = new HashSet<Player>();

	public static HashSet<Player> SCP073 = new HashSet<Player>();

	public static HashSet<Player> Fantasma = new HashSet<Player>();

	public static HashSet<Player> CooldownFantasma = new HashSet<Player>();

	public static HashSet<Player> Vidente = new HashSet<Player>();

	public static HashSet<Player> Medic = new HashSet<Player>();

	public static HashSet<ReferenceHub> HammerDown = new HashSet<ReferenceHub>();

	public static HashSet<Player> Observador = new HashSet<Player>();

	public static HashSet<Player> Scp990 = new HashSet<Player>();

	public static HashSet<Player> Scp507 = new HashSet<Player>();

	public static HashSet<Player> CooldownScp507 = new HashSet<Player>();

	public static HashSet<Player> Scp1915 = new HashSet<Player>();

	public static HashSet<Player> ZombiesInfectados = new HashSet<Player>();

	public static int MaxZombiesInfectados = 2;

	public static HashSet<int> AudioMute = new HashSet<int>();

	public static Dictionary<Player, float> Tripode = new Dictionary<Player, float>();

	public static Dictionary<Player, float> Hambre106 = new Dictionary<Player, float>();

	public static Dictionary<Player, float> Marcados = new Dictionary<Player, float>();

	public static Dictionary<Player, float> Coraza096 = new Dictionary<Player, float>();

	public static CandyKindID RandomCandy()
	{
		return candyTypes[random.Next(candyTypes.Length)];
	}

	public static bool EsCompuestoC(ushort serial)
	{
		return serial == 6942 || CompuestoCSeriales.Contains(serial);
	}

	public static bool Activo(Dictionary<Player, float> estado, Player player)
	{
		float value;
		return player != null && estado.TryGetValue(player, out value) && Time.time < value;
	}

	public static void QuitarSubclases(Player player)
	{
		if (!(player == null))
		{
			Pasivas.Quitar(player);
			string userId = player.UserId;
			Bulldozers.Remove(userId);
			Heavys.Remove(userId);
			Traidor.Remove(userId);
			Scout.Remove(userId);
			if (CientEjec == userId)
			{
				CientEjec = string.Empty;
			}
			if (Militar == userId)
			{
				Militar = string.Empty;
			}
			if (Machetero == userId)
			{
				Machetero = string.Empty;
			}
			if (Bomba == userId)
			{
				Bomba = string.Empty;
			}
			HeadGuard.Remove(player);
			Spy.Remove(player);
			Disfrazados.Remove(player);
			DeadRinger.Remove(player);
			DeadRingerUsado.Remove(player);
			Taser.Remove(player);
			Demoman.Remove(player);
			Impersonator.Remove(player);
			Scp181.Remove(player);
			SCP073.Remove(player);
			Fantasma.Remove(player);
			Vidente.Remove(player);
			Medic.Remove(player);
			if (player.ReferenceHub != null)
			{
				HammerDown.Remove(player.ReferenceHub);
			}
			Observador.Remove(player);
			Scp990.Remove(player);
			Scp507.Remove(player);
			Scp1915.Remove(player);
			ZombiesInfectados.Remove(player);
			Tripode.Remove(player);
			Hambre106.Remove(player);
			Marcados.Remove(player);
			Coraza096.Remove(player);
			ManiExplosivo.Remove(player);
			Habilidades.Quitar(player);
			EffectsHUD.Limpiar(player);
		}
	}

	public static void ResetRonda()
	{
		foreach (CoroutineHandle coroutine in Coroutines)
		{
			Timing.KillCoroutines(coroutine);
		}
		Coroutines.Clear();
		foreach (CoroutineHandle omegaCoroutine in OmegaCoroutines)
		{
			Timing.KillCoroutines(omegaCoroutine);
		}
		OmegaCoroutines.Clear();
		foreach (CoroutineHandle item in GulagCoroutine)
		{
			Timing.KillCoroutines(item);
		}
		GulagCoroutine.Clear();
		Timing.KillCoroutines(NukeTimerHandle);
		GenteGulag.Clear();
		RadioLightPlugin.Subclases.Escuadrones.Reiniciar();
		Estuvo.Clear();
		Gulag1.Clear();
		Gulag2.Clear();
		ColaPractica.Clear();
		PeleaPractica.Clear();
		PracticaUltimoEnPie = false;
		Gulag2v2 = false;
		ManiExplosivo.Clear();
		ReemplazoAfk.Limpiar();
		lucesPuertas.Clear();
		HurtingHintCooldown.Clear();
		LastHpInfoUpdate.Clear();
		RoomList.Clear();
		Scp008 = (MachetazoSpawned = (GrupoDeRespuestaSpawned = (YaSpawneó = false)));
		BetterOmegaWarheadActivated = (Nukerun = (railgun = (TraidorTP = (TeslaCassieBool = false))));
		NoEsPrimerSpawnEquipo = false;
		EventoEnCurso = false;
		papanum = 1;
		Bulldozers.Clear();
		Heavys.Clear();
		Traidor.Clear();
		HeavyCooldown.Clear();
		Scout.Clear();
		CientEjec = (Militar = (Machetero = (Bomba = string.Empty)));
		HeadGuard.Clear();
		CooldownHeadguard.Clear();
		Spy.Clear();
		CooldownSpy.Clear();
		Disfrazados.Clear();
		DeadRinger.Clear();
		DeadRingerUsado.Clear();
		Taser.Clear();
		CooldownTaser.Clear();
		Demoman.Clear();
		Impersonator.Clear();
		CooldownImp.Clear();
		Scp181.Clear();
		SCP073.Clear();
		Fantasma.Clear();
		CooldownFantasma.Clear();
		Vidente.Clear();
		Medic.Clear();
		HammerDown.Clear();
		Observador.Clear();
		Scp990.Clear();
		Scp507.Clear();
		CooldownScp507.Clear();
		Scp1915.Clear();
		ZombiesInfectados.Clear();
		Tripode.Clear();
		Hambre106.Clear();
		Marcados.Clear();
		Coraza096.Clear();
		Habilidades.Limpiar();
	}
}
