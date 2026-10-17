using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using LabApi.Features.Wrappers;
using MEC;
using PlayerStatsSystem;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class ModosGulag
{
	private sealed class Modo
	{
		public string Nombre;

		public ItemType[] Armas;
	}

	private sealed class Tematica
	{
		public Modo Modo = Modos[0];

		public Modificador Mod = Modificador.Ninguno;

		public string NombreMod = "Sin modificador";

		public string Texto
		{
			get
			{
				Modificador mod = Mod;
				bool flag = ((mod == Modificador.Granadas || mod == Modificador.ArmasCambiantes) ? true : false);
				return flag ? NombreMod : (Modo.Nombre + ((Mod == Modificador.Ninguno) ? "" : (" · " + NombreMod)));
			}
		}
	}

	public enum Modificador
	{
		Ninguno,
		BajaGravedad,
		Vida50,
		AOscuras,
		SoloCabeza,
		Velocidad,
		Vampiro,
		Gigantes,
		Enanos,
		Silencio,
		Sangrado,
		MuerteSubita,
		Granadas,
		Ralentizado,
		CiegoPorMomentos,
		ArmasCambiantes,
		Rebote,
		ArenaSeCierra
	}

	private static class Modelo
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool Spawnear(string nombre, Vector3 pos)
		{
			return Mer.Spawnear(nombre, pos) != null;
		}
	}

	private static readonly Modo[] Modos = new Modo[8]
	{
		new Modo
		{
			Nombre = "Arma al azar",
			Armas = null
		},
		new Modo
		{
			Nombre = "Solo escopetas",
			Armas = new ItemType[1] { ItemType.GunShotgun }
		},
		new Modo
		{
			Nombre = "Solo pistolas",
			Armas = new ItemType[3]
			{
				ItemType.GunCOM15,
				ItemType.GunCOM18,
				ItemType.GunCom45
			}
		},
		new Modo
		{
			Nombre = "Solo revólver",
			Armas = new ItemType[1] { ItemType.GunRevolver }
		},
		new Modo
		{
			Nombre = "Ametralladoras",
			Armas = new ItemType[2]
			{
				ItemType.GunFRMG0,
				ItemType.GunLogicer
			}
		},
		new Modo
		{
			Nombre = "Rifles",
			Armas = new ItemType[3]
			{
				ItemType.GunE11SR,
				ItemType.GunAK,
				ItemType.GunA7
			}
		},
		new Modo
		{
			Nombre = "Solo Disruptor",
			Armas = new ItemType[1] { ItemType.ParticleDisruptor }
		},
		new Modo
		{
			Nombre = "Solo SCP-127",
			Armas = new ItemType[1] { ItemType.GunSCP127 }
		}
	};

	private static readonly ItemType[] ArmasAlAzar = Var.Primaria.Concat(new ItemType[1] { ItemType.ParticleDisruptor }).Distinct().ToArray();

	private static Tematica _gulagActual = new Tematica();

	private static Tematica _gulagProxima = new Tematica();

	private static Tematica _practActual = new Tematica();

	private static Tematica _practProxima = new Tematica();

	private static ArenaGulag _arena = new ArenaGulag();

	private static readonly (Modificador Tipo, string Nombre, int Peso)[] Modificadores = new(Modificador, string, int)[15]
	{
		(Modificador.Ninguno, "Sin modificador", 4),
		(Modificador.BajaGravedad, "Baja gravedad", 2),
		(Modificador.Vida50, "Todos a 50 de vida", 2),
		(Modificador.AOscuras, "A oscuras", 2),
		(Modificador.Velocidad, "Velocidad", 2),
		(Modificador.Gigantes, "Gigantes", 2),
		(Modificador.Enanos, "Enanos", 2),
		(Modificador.Silencio, "Silencio total", 2),
		(Modificador.Sangrado, "Sangrado", 2),
		(Modificador.MuerteSubita, "Muerte súbita", 2),
		(Modificador.Ralentizado, "Cámara lenta", 2),
		(Modificador.CiegoPorMomentos, "Ciego por momentos", 2),
		(Modificador.ArmasCambiantes, "Armas cambiantes", 2),
		(Modificador.Rebote, "Rebote (parte del daño vuelve)", 2),
		(Modificador.ArenaSeCierra, "La arena se cierra", 2)
	};

	private static readonly HashSet<Exiled.API.Features.Player> _conGravedad = new HashSet<Exiled.API.Features.Player>();

	private static readonly HashSet<Exiled.API.Features.Player> _conEscala = new HashSet<Exiled.API.Features.Player>();

	private static string _nombrePractica = "Arena clásica";

	private static readonly Vector3 DesplazoCopia = new Vector3(0f, 0f, -150f);

	public static Vector3 SpawnA { get; private set; } = new Vector3(0f, 203f, -26f);


	public static Vector3 SpawnB { get; private set; } = new Vector3(0f, 203f, -54f);


	public static Vector3 Espera { get; private set; } = new Vector3(0f, 207f, -21f);


	public static Vector3 Centro => (SpawnA + SpawnB) / 2f + Vector3.up;

	public static string Descripcion => _arena.Nombre + " · " + _gulagProxima.Texto;

	public static Modificador ModActual => _gulagActual.Mod;

	public static string NombreModificador => _gulagActual.NombreMod;

	private static Vector3 GravedadNormal => new Vector3(0f, -19.6f, 0f);

	public static Vector3 PSpawnA { get; private set; } = new Vector3(0f, 203f, -26f);


	public static Vector3 PSpawnB { get; private set; } = new Vector3(0f, 203f, -54f);


	public static Vector3 PEspera { get; private set; } = new Vector3(0f, 207f, -21f);


	public static Vector3 PCentro => (PSpawnA + PSpawnB) / 2f + Vector3.up;

	public static bool Comparten { get; private set; } = true;


	public static string DescripcionPractica => _nombrePractica + " · " + _practProxima.Texto;

	private static Tematica Nueva()
	{
		Config config = Plugin.Singleton?.Config;
		Tematica tematica = new Tematica();
		tematica.Modo = ((config == null || config.GulagModosDeArmas) ? Modos[Var.random.Next(Modos.Length)] : Modos[0]);
		if (config != null && !config.GulagModificadores)
		{
			return tematica;
		}
		int maxValue = Modificadores.Sum(((Modificador Tipo, string Nombre, int Peso) m) => m.Peso);
		int num = Var.random.Next(maxValue);
		(Modificador, string, int)[] modificadores = Modificadores;
		for (int i = 0; i < modificadores.Length; i++)
		{
			(Modificador, string, int) tuple = modificadores[i];
			num -= tuple.Item3;
			if (num < 0)
			{
				(tematica.Mod, tematica.NombreMod, _) = tuple;
				break;
			}
		}
		return tematica;
	}

	public static ItemType EmpezarPelea(bool practica)
	{
		Tematica tematica;
		if (practica)
		{
			_practActual = _practProxima;
			_practProxima = Nueva();
			tematica = _practActual;
		}
		else
		{
			_gulagActual = _gulagProxima;
			_gulagProxima = Nueva();
			tematica = _gulagActual;
		}
		ItemType[] array = tematica.Modo.Armas ?? ArmasAlAzar;
		return array[Var.random.Next(array.Length)];
	}

	private static Modificador ModDe(Exiled.API.Features.Player p)
	{
		return Var.PeleaPractica.Contains(p) ? _practActual.Mod : _gulagActual.Mod;
	}

	public static void AplicarModificador(Exiled.API.Features.Player p)
	{
		if (p == null || !p.IsConnected || !p.IsAlive)
		{
			return;
		}
		try
		{
			switch (ModDe(p))
			{
			case Modificador.BajaGravedad:
				LabApi.Features.Wrappers.Player.Get(p.ReferenceHub).Gravity = GravedadNormal * 0.35f;
				_conGravedad.Add(p);
				break;
			case Modificador.Vida50:
				p.Health = Mathf.Min(p.Health, 50f);
				break;
			case Modificador.AOscuras:
				p.EnableEffect(EffectType.Blinded, 1, 150f);
				break;
			case Modificador.Velocidad:
				p.EnableEffect(EffectType.MovementBoost, 35, 150f);
				break;
			case Modificador.Gigantes:
				p.Scale = new Vector3(1.3f, 1.3f, 1.3f);
				_conEscala.Add(p);
				break;
			case Modificador.Enanos:
				p.Scale = new Vector3(0.6f, 0.6f, 0.6f);
				_conEscala.Add(p);
				break;
			case Modificador.Silencio:
				p.EnableEffect(EffectType.SilentWalk, 10, 150f);
				p.EnableEffect(EffectType.Deafened, 1, 150f);
				break;
			case Modificador.MuerteSubita:
				p.Health = 1f;
				p.AddAhp(100f, 100f, 0f, 1f);
				break;
			case Modificador.Granadas:
				foreach (Exiled.API.Features.Items.Item item in p.Items.ToList())
				{
					if (item.IsWeapon)
					{
						p.RemoveItem(item);
					}
				}
				p.AddItem(ItemType.GrenadeHE);
				p.AddItem(ItemType.GrenadeFlash);
				break;
			case Modificador.Ralentizado:
				p.EnableEffect(EffectType.Slowness, 30, 150f);
				break;
			}
			bool flag;
			switch (ModDe(p))
			{
			case Modificador.Sangrado:
			case Modificador.Granadas:
			case Modificador.CiegoPorMomentos:
			case Modificador.ArmasCambiantes:
			case Modificador.ArenaSeCierra:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				Timing.RunCoroutine(BucleModificador(p, ModDe(p)));
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Gulag] Modificador: " + ex.Message);
		}
	}

	private static IEnumerator<float> BucleModificador(Exiled.API.Features.Player p, Modificador mod)
	{
		float inicio = Time.time;
		float ultimaGranada = 0f;
		float ultimoCambio = Time.time;
		float ultimoCiego = Time.time;
		while (p != null && p.IsConnected && p.IsAlive && EnPelea(p) && ModDe(p) == mod)
		{
			try
			{
				float t = Time.time;
				switch (mod)
				{
				case Modificador.Sangrado:
					Desangrar(p, 1.5f);
					break;
				case Modificador.ArenaSeCierra:
					if (t - inicio >= 30f)
					{
						Desangrar(p, 4f);
						if (t - inicio < 31f)
						{
							p.Broadcast(3, "<size=30><color=#e32433><b>¡La arena se cierra!</b></color></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
						}
					}
					break;
				case Modificador.CiegoPorMomentos:
					if (t - ultimoCiego >= 10f)
					{
						ultimoCiego = t;
						p.EnableEffect(EffectType.Flashed, 1, 1f);
					}
					break;
				case Modificador.Granadas:
					if (t - ultimaGranada >= 1.5f && !p.Items.Any((Exiled.API.Features.Items.Item i) => i.Type == ItemType.GrenadeHE))
					{
						ultimaGranada = t;
						p.AddItem(ItemType.GrenadeHE);
					}
					if (!p.Items.Any((Exiled.API.Features.Items.Item i) => i.Type == ItemType.GrenadeFlash) && t - ultimaGranada >= 1f)
					{
						p.AddItem(ItemType.GrenadeFlash);
					}
					break;
				case Modificador.ArmasCambiantes:
					if (t - ultimoCambio >= 15f)
					{
						ultimoCambio = t;
						CambiarArma(p);
					}
					break;
				}
			}
			catch (Exception e)
			{
				Log.Debug("[Gulag] Bucle modificador: " + e.Message);
			}
			yield return Timing.WaitForSeconds(1f);
		}
	}

	private static void Desangrar(Exiled.API.Features.Player p, float cuanto)
	{
		if (p.ArtificialHealth > 0f)
		{
			p.ArtificialHealth = Mathf.Max(0f, p.ArtificialHealth - cuanto);
		}
		else if (p.Health > 1f)
		{
			p.Health = Mathf.Max(1f, p.Health - cuanto);
		}
	}

	private static void CambiarArma(Exiled.API.Features.Player p)
	{
		foreach (Exiled.API.Features.Items.Item item3 in p.Items.ToList())
		{
			if (item3.IsWeapon)
			{
				p.RemoveItem(item3);
			}
		}
		ItemType itemType = ArmasAlAzar[Var.random.Next(ArmasAlAzar.Length)];
		Exiled.API.Features.Items.Item item2 = (p.CurrentItem = p.AddItem(itemType));
		if (item2 is Firearm firearm)
		{
			firearm.MagazineAmmo = firearm.MaxMagazineAmmo;
		}
		if (Var.AmmoPorArma.TryGetValue(itemType, out var value) && p.GetAmmo(value) < 60)
		{
			p.AddAmmo(value, 90);
		}
		EffectsHUD.Notify(p, $"<color=#859CC7>\ud83d\udd00 Arma nueva: <b>{itemType}</b></color>");
	}

	/// <summary>True si el tamaño raro del jugador lo puso el modificador de la pelea (Gigantes/Enanos), no un comando.</summary>
	public static bool EscalaDelModo(Exiled.API.Features.Player p)
	{
		return p != null && _conEscala.Contains(p);
	}

	public static void AlCambiarRolEv(ChangingRoleEventArgs ev)
	{
		AlCambiarRol(ev?.Player);
	}

	public static void AlCambiarRol(Exiled.API.Features.Player p)
	{
		if (p != null && _conEscala.Remove(p))
		{
			try
			{
				p.Scale = Vector3.one;
			}
			catch
			{
			}
		}
		if (p == null || !_conGravedad.Remove(p))
		{
			return;
		}
		try
		{
			LabApi.Features.Wrappers.Player.Get(p.ReferenceHub).Gravity = GravedadNormal;
		}
		catch
		{
		}
	}

	private static bool EnPelea(Exiled.API.Features.Player p)
	{
		return p != null && (Var.Gulag1.Contains(p) || Var.Gulag2.Contains(p) || Var.PeleaPractica.Contains(p));
	}

	public static void AlHerir(HurtingEventArgs ev)
	{
		try
		{
			Exiled.API.Features.Player player = ev?.Attacker;
			Exiled.API.Features.Player player2 = ev?.Player;
			if (player != null && player == player2 && EnPelea(player2) && ModDe(player2) == Modificador.Granadas)
			{
				ev.IsAllowed = false;
			}
			else
			{
				if (player == null || player2 == null || player == player2 || !EnPelea(player) || !EnPelea(player2))
				{
					return;
				}
				if ((Var.Gulag1.Contains(player) && Var.Gulag1.Contains(player2)) || (Var.Gulag2.Contains(player) && Var.Gulag2.Contains(player2)))
				{
					ev.IsAllowed = false;
					return;
				}
				Modificador modificador = ModDe(player);
				if (modificador == Modificador.SoloCabeza && ev.DamageHandler?.Base is FirearmDamageHandler { Hitbox: not HitboxType.Headshot })
				{
					ev.IsAllowed = false;
					return;
				}
				if (modificador == Modificador.Vampiro && ev.Amount > 0f)
				{
					player.Heal(ev.Amount * 0.5f);
				}
				if (modificador == Modificador.Rebote && ev.Amount > 0f)
				{
					Desangrar(player, ev.Amount * 0.3f);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Gulag] Daño en pelea: " + ex.Message);
		}
	}

	private static void ElegirPractica(List<ArenaGulag> arenas, ArenaGulag gulag, bool gulagEsSchematic, Vector3 posGulag)
	{
		Comparten = true;
		_nombrePractica = gulag.Nombre;
		PSpawnA = SpawnA;
		PSpawnB = SpawnB;
		PEspera = Espera;
		foreach (ArenaGulag item in from x in arenas
			where x != gulag && !string.IsNullOrWhiteSpace(x.Schematic)
			select x into _
			orderby Var.random.Next()
			select _)
		{
			if (Util.TryLeerVector(item.SpawnA, out var v) && Util.TryLeerVector(item.SpawnB, out var v2) && Util.TryLeerVector(item.Espera, out var v3) && Util.TryLeerVector(item.PosicionSchematic, out var v4))
			{
				bool flag = false;
				try
				{
					flag = Modelo.Spawnear(item.Schematic, v4);
				}
				catch
				{
				}
				if (flag)
				{
					PSpawnA = v;
					PSpawnB = v2;
					PEspera = v3;
					_nombrePractica = item.Nombre;
					Comparten = false;
					return;
				}
			}
		}
		if (gulagEsSchematic)
		{
			bool flag2 = false;
			try
			{
				flag2 = Modelo.Spawnear(gulag.Schematic, posGulag + DesplazoCopia);
			}
			catch
			{
			}
			if (flag2)
			{
				PSpawnA = SpawnA + DesplazoCopia;
				PSpawnB = SpawnB + DesplazoCopia;
				PEspera = Espera + DesplazoCopia;
				_nombrePractica = gulag.Nombre + " (copia)";
				Comparten = false;
			}
		}
	}

	public static void Elegir()
	{
		try
		{
			List<ArenaGulag> list = (Plugin.Singleton?.Config)?.GulagArenas?.Where((ArenaGulag a) => a != null).ToList() ?? new List<ArenaGulag>();
			if (list.Count == 0)
			{
				list.Add(new ArenaGulag());
			}
			ArenaGulag arenaGulag = list[Var.random.Next(list.Count)];
			if (!Util.TryLeerVector(arenaGulag.SpawnA, out var v) || !Util.TryLeerVector(arenaGulag.SpawnB, out var v2) || !Util.TryLeerVector(arenaGulag.Espera, out var v3))
			{
				Log.Warn("[Gulag] La arena '" + arenaGulag.Nombre + "' tiene posiciones mal escritas (usá 'x, y, z'). Se usa la clásica.");
				arenaGulag = null;
				v3 = default(Vector3);
				v = (v2 = v3);
			}
			if (arenaGulag != null && !string.IsNullOrWhiteSpace(arenaGulag.Schematic))
			{
				bool flag = false;
				if (Util.TryLeerVector(arenaGulag.PosicionSchematic, out var v4))
				{
					try
					{
						flag = Modelo.Spawnear(arenaGulag.Schematic, v4);
					}
					catch (Exception ex)
					{
						Log.Warn("[Gulag] No se pudo poner el schematic '" + arenaGulag.Schematic + "': " + ex.Message);
					}
				}
				if (!flag)
				{
					Log.Warn("[Gulag] El schematic '" + arenaGulag.Schematic + "' no está en ProjectMER/Schematics (o posicion_schematic está mal). Se usa la arena clásica.");
					arenaGulag = null;
				}
			}
			if (arenaGulag == null)
			{
				arenaGulag = list.FirstOrDefault((ArenaGulag x) => string.IsNullOrWhiteSpace(x.Schematic)) ?? new ArenaGulag();
				if (!Util.TryLeerVector(arenaGulag.SpawnA, out v) || !Util.TryLeerVector(arenaGulag.SpawnB, out v2) || !Util.TryLeerVector(arenaGulag.Espera, out v3))
				{
					arenaGulag = new ArenaGulag();
					Util.TryLeerVector(arenaGulag.SpawnA, out v);
					Util.TryLeerVector(arenaGulag.SpawnB, out v2);
					Util.TryLeerVector(arenaGulag.Espera, out v3);
				}
			}
			_arena = arenaGulag;
			SpawnA = v;
			SpawnB = v2;
			Espera = v3;
			Vector3 v5;
			bool gulagEsSchematic = !string.IsNullOrWhiteSpace(arenaGulag.Schematic) && Util.TryLeerVector(arenaGulag.PosicionSchematic, out v5);
			Util.TryLeerVector(arenaGulag.PosicionSchematic, out var v6);
			ElegirPractica(list, arenaGulag, gulagEsSchematic, v6);
			_gulagProxima = Nueva();
			_practProxima = Nueva();
			_gulagActual = _gulagProxima;
			_practActual = _practProxima;
			Log.Info("[Gulag] Esta ronda: " + Descripcion + ". Práctica: " + DescripcionPractica + (Comparten ? " (comparte arena con el gulag)" : " (arena propia)") + ".");
		}
		catch (Exception arg)
		{
			Log.Error($"[Gulag] Elegir arena/modo: {arg}");
		}
	}
}
