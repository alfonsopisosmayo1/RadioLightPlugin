using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using Interactables.Interobjects.DoorButtons;
using LabApi.Features.Wrappers;
using MEC;
using MapGeneration;
using PlayerRoles;
using RadioLightPlugin.Command;
using RadioLightPlugin.Misc;
using RadioLightPlugin.Patches;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Handlers;

public class ServerHandler
{
	private static readonly Color LuzBoton = new Color(0.11f, 0.788f, 0.165f);

	private static readonly Color LuzKeycard = new Color(0.172549f, 0.215686f, 0.541176f);

	private static readonly Color LuzAmbiente = new Color(1f, 0.9098f, 0.7686f);

	private static readonly HashSet<ItemType> BasuraRespawn = new HashSet<ItemType>
	{
		ItemType.Flashlight,
		ItemType.Coin,
		ItemType.KeycardJanitor,
		ItemType.KeycardScientist,
		ItemType.Radio,
		ItemType.KeycardResearchCoordinator,
		ItemType.KeycardZoneManager,
		ItemType.KeycardFacilityManager,
		ItemType.KeycardGuard,
		ItemType.KeycardContainmentEngineer,
		ItemType.GrenadeFlash
	};

	private static readonly (string cassie, string texto)[] AnunciosInicio = new(string, string)[11]
	{
		("$PITCH_0.2 .g3 .g3 .g3 $PITCH_0.9 warning . Containment breach detected . evacuate now", "Advertencia, brecha de contención detectada. Evacuen ahora."),
		("$PITCH_0.9 danger . facility lockdown initiated", "Peligro, bloqueo de la instalación iniciado."),
		("attention . Full site lockdown initiated", "Atención, bloqueo completo del sitio iniciado."),
		("attention . evacuation order recieved", "Atención, orden de evacuación recibida."),
		("Error . . . $PITCH_0.85 Containment Breach .g3 .g2 .g4 .g2 $PITCH_0.15 R", "Error... Brecha de contención..."),
		("attention . breach in light containment zone . security proceed and contain scpsubjects . activating tesla doors . await mtfunit", "Atención. Brecha en zona de contención ligera.<split>Seguridad diríjase y recontenga sujetos SCP.<split>Activando puertas Tesla. Esperando Destacamento Móvil."),
		("attention security personnel . breach in Heavy Containment Zone . immediate help of mtfunit required", "Atención personal de seguridad. Brecha en Zona de Contención Pesada.<split>Se requiere ayuda inmediata del Destacamento Móvil."),
		("a warning . scpsubjects breach in light and heavy containment zone . evacuate to entrance zone", "Advertencia: brecha de sujetos SCP en zonas ligera y pesada.<split>Evacuen a la zona de entrada."),
		("$PITCH_0.5 help . . help . . $PITCH_0.3 .g3 $PITCH_1", "ayuda...<split>ayuda..."),
		("warning . reactor instability detected . heavy containment personnel evacuate", "Advertencia, inestabilidad de reactor detectada. Personal de contención pesada evacue."),
		("warning . all doors open for evacuation . doctor nato_b report to heavy containment checkpoint", "Advertencia, todas las puertas abiertas para evacuación.<split>Dr. Bravo, repórtese al punto de control de Contención Pesada.")
	};

	private static readonly (string cassie, string texto)[] AnunciosAbril = new(string, string)[3]
	{
		("$PITCH_0.2 .g3 .g3 .g3 $PITCH_0.9 warning . all the facility is .g7 . there is no escape x d x d", "Advertencia, toda la instalación está [CENSURADO]. No hay escapatoria XdxDX."),
		("$PITCH_0.9 warning . detected Containment breach . all personnel run immediately", "Advertencia, brecha de contención detectada. Todo el personal corra inmediatamente."),
		("Warning . all scpsubjects are free . good luck x d x d d d d", "Advertencia, todos los sujetos <color=#e32433>SCP</color> están libres. Buena suerte XdxDddD")
	};

	private static void DestruirPickups(Func<Exiled.API.Features.Pickups.Pickup, bool> filtro = null)
	{
		foreach (Exiled.API.Features.Pickups.Pickup item in Exiled.API.Features.Pickups.Pickup.List.ToList())
		{
			try
			{
				if (item?.Base != null && (filtro == null || filtro(item)))
				{
					item.Destroy();
				}
			}
			catch (Exception ex)
			{
				Log.Debug("DestruirPickups: " + ex.Message);
			}
		}
	}

	private static void DestruirRagdolls()
	{
		foreach (Exiled.API.Features.Ragdoll item in Exiled.API.Features.Ragdoll.List.ToList())
		{
			try
			{
				item.Destroy();
			}
			catch (Exception ex)
			{
				Log.Debug("DestruirRagdolls: " + ex.Message);
			}
		}
	}

	public static void BorrarLucesDeZona(params ZoneType[] zonas)
	{
		for (int num = Var.lucesPuertas.Count - 1; num >= 0; num--)
		{
			Exiled.API.Features.Toys.Light light = Var.lucesPuertas[num];
			try
			{
				Exiled.API.Features.Room room = Exiled.API.Features.Room.Get(light.Position);
				if (room != null && Array.IndexOf(zonas, room.Zone) >= 0)
				{
					light.Destroy();
					Var.lucesPuertas.RemoveAt(num);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				Var.lucesPuertas.RemoveAt(num);
			}
		}
	}

	public void OnRoundEnd(RoundEndedEventArgs ev)
	{
		try
		{
			Scp0871.Limpiar();
		}
		catch
		{
		}
		DestruirPickups();
		DestruirRagdolls();
		AntiTeamKill.AlTerminarRonda();
		try
		{
			Estadisticas.AnunciarMvp();
		}
		catch
		{
		}
		try
		{
			TimerRespawn.AlTerminarRonda();
		}
		catch
		{
		}
		try
		{
			DiscordLog.AlTerminarRonda(ev);
		}
		catch (Exception ex)
		{
			Log.Warn("[Discord] " + ex.Message);
		}
		try
		{
			Estadisticas.AlTerminarRonda();
			Logros.QuitarInsignias();
		}
		catch (Exception ex2)
		{
			Log.Warn("[Estadísticas] " + ex2.Message);
		}
		Sonido.Mapa($"Final{Var.random.Next(1, 7)}.ogg");
	}

	public void OnRespawn(RespawningTeamEventArgs ev)
	{
		if (!ev.IsAllowed || ev.Wave == null || ev.Players == null || ev.Players.Count == 0)
		{
			return;
		}
		DestruirRagdolls();
		List<Exiled.API.Features.Player> sumados = new List<Exiled.API.Features.Player>();
		foreach (Exiled.API.Features.Player item in Var.GenteGulag.Concat(Var.ColaPractica).Concat(Var.PeleaPractica).Distinct()
			.ToList())
		{
			if (item != null && item.IsConnected && !ev.Players.Contains(item))
			{
				sumados.Add(item);
			}
		}
		Var.GenteGulag.Clear();
		Var.ColaPractica.Clear();
		Var.PeleaPractica.Clear();
		Var.PracticaUltimoEnPie = false;
		RoleTypeId rolSumados = ((ev.NextKnownTeam == Faction.FoundationEnemy) ? RoleTypeId.ChaosRifleman : RoleTypeId.NtfPrivate);
		if (sumados.Count > 0)
		{
			foreach (Exiled.API.Features.Player item2 in sumados)
			{
				Var.Coroutines.Add(Timing.CallDelayed(RadioLightPlugin.Subclases.Util.Turno(0.5f), delegate
				{
					try
					{
						if (!(item2 == null) && item2.IsConnected && !Exiled.API.Features.Round.IsEnded && !Var.Gulag1.Contains(item2) && !Var.Gulag2.Contains(item2) && !Var.PeleaPractica.Contains(item2) && (item2.Role.Type == RoleTypeId.Spectator || item2.Role.Type == RoleTypeId.Tutorial))
						{
							item2.Role.Set(rolSumados, SpawnReason.Respawn, RoleSpawnFlags.All);
						}
					}
					catch (Exception ex)
					{
						Log.Debug("[Oleada] No se pudo sumar a " + item2?.Nickname + ": " + ex.Message);
					}
				}));
			}
		}
		if (Var.NoEsPrimerSpawnEquipo)
		{
			// [fix] antes borraba TODO lo de LCZ (aunque no estuviera descontaminada), la superficie de ítems especiales y vitrinas.
			// Ahora solo basura soltada por jugadores, y LCZ entera solo si ya se descontaminó.
			Limpieza.Preparar();
			DestruirPickups((Exiled.API.Features.Pickups.Pickup p) => Limpieza.BorrableEnOleada(p, BasuraRespawn.Contains(p.Type) || p.Type.IsAmmo()));
		}
		else
		{
			Var.NoEsPrimerSpawnEquipo = true;
		}
		List<Exiled.API.Features.Player> list = new List<Exiled.API.Features.Player>(ev.Players);
		list.AddRange(sumados);
		if (Var.EventoEnCurso)
		{
			return;
		}
		if (ev.NextKnownTeam == Faction.FoundationEnemy)
		{
			int cantidad = list.Count;
			Var.Coroutines.Add(Timing.CallDelayed(Var.random.Next(8, 16), delegate
			{
				AnunciarChaos(cantidad);
			}));
			{
				foreach (Exiled.API.Features.Player player2 in list)
				{
					if (Var.random.Next(100) >= 60)
					{
						continue;
					}
					int sub2 = Var.random.Next(1, 101);
					Var.Coroutines.Add(Timing.CallDelayed(RadioLightPlugin.Subclases.Util.Turno(2f), delegate
					{
						if (Util.Vivo(player2) && player2.Role.Team == Team.ChaosInsurgency)
						{
							if (sub2 <= 15 && Var.Heavys.Count == 0)
							{
								IdC.HeavySpawn(player2);
							}
							else if (sub2 <= 30)
							{
								IdC.ScoutSpawn(player2);
							}
							else if (sub2 <= 45 && Var.Demoman.Count == 0)
							{
								IdC.DemomanSpawn(player2);
							}
							else if (sub2 <= 60)
							{
								IdC.SpySpawn(player2);
							}
							else if (sub2 <= 73)
							{
								IdC.SilentChaosSpawn(player2);
							}
							else if (sub2 <= 87)
							{
								Extra.SaboteadorSpawn(player2);
							}
							else
							{
								Extra.SanguinarioSpawn(player2);
							}
						}
					}));
				}
				return;
			}
		}
		// escuadrones especiales (Nu-7 / Beta-7): una vez por partida, solo en oleadas grandes; en esa oleada no hay subclases
		if (Escuadrones.AlRespawnear(ev, list))
		{
			return;
		}
		if (ev.NextKnownTeam != Faction.FoundationStaff)
		{
			return;
		}
		foreach (Exiled.API.Features.Player player in list)
		{
			if (Var.random.Next(100) >= 35)
			{
				continue;
			}
			int sub = Var.random.Next(1, 101);
			Var.Coroutines.Add(Timing.CallDelayed(RadioLightPlugin.Subclases.Util.Turno(2f), delegate
			{
				if (Util.Vivo(player) && player.Role.Type == RoleTypeId.NtfPrivate)
				{
					if (sub <= 2 && !Var.MachetazoSpawned)
					{
						MTF.MachetazoSpawn(player);
					}
					else if (sub <= 5 && Var.Bulldozers.Count == 0)
					{
						MTF.BulldozerSpawn(player);
					}
					else if (sub <= 40)
					{
						MTF.SpySpawn(player);
					}
					else if (sub <= 60)
					{
						MTF.TaserSpawn(player);
					}
					else if (sub <= 80 && Var.HammerDown.Count == 0)
					{
						MTF.HammerDown(player);
					}
					else if (sub > 80)
					{
						MTF.MedicSpawn(player);
					}
				}
			}));
		}
	}

	private static void AnunciarChaos(int cantidad)
	{
		switch (Var.random.Next(3))
		{
		case 0:
			Announcer.Message($"attention . {cantidad} unauthorized personnel detected in gate a . mtfunit report to entrance zone immediately for termination", $"Atención, {cantidad} personales <color=#e32433>no autorizados</color> detectados en Gate A. Unidad <color=#0096FF>MTF</color> repórtese a Entrance Zone inmediatamente para la terminación.");
			break;
		case 1:
			Announcer.Message("attention to all the foundation guard . chaosinsurgency detected in gate a . report to entrance zone and secure the facility", "Atención a todos los <color=#5B6370>Guardias</color> de la fundación, <color=#008F1E>Insurgentes del Caos</color> detectados en Gate A. Repórtense en Entrance Zone y aseguren la instalación.");
			break;
		default:
			Announcer.Message("$PITCH_0.2 .g3 .g3 .g3 $PITCH_1 Warning . unauthorized personnel in surface zone . please stay in evacuation shelter until mtfunit terminate unauthorized personnel", "Advertencia, personal <color=#e32433>no autorizado</color> en zona de superficie. Por favor quedarse en un refugio de evacuación hasta que la unidad <color=#0096FF>Mobile Task Force</color> termine al personal no autorizado.");
			break;
		}
	}

	public void OnDetonating(DetonatingEventArgs ev)
	{
		if (ev.IsAllowed)
		{
			List<Exiled.API.Features.Player> victimas = Exiled.API.Features.Player.List.Where(delegate(Exiled.API.Features.Player player)
			{
				bool isAlive = player.IsAlive;
				bool flag = isAlive;
				if (flag)
				{
					ZoneType zone = player.Zone;
					bool flag2 = (((uint)(zone - 1) <= 1u || zone == ZoneType.Entrance) ? true : false);
					flag = flag2 || player.Lift != null || player.Role.Type == RoleTypeId.Scp079;
				}
				return flag;
			}).ToList();
			Timing.RunCoroutine(MatarDeAPoco(victimas));
		}
		Timing.CallDelayed(1f, delegate
		{
			try
			{
				DestruirPickups((Exiled.API.Features.Pickups.Pickup p) => p.Room != null && p.Room.Zone != ZoneType.Surface && p.Transform.parent == null && !p.IsLocked);
				DestruirRagdolls();
				Exiled.API.Features.Server.ExecuteCommand("/cleanup bulletholes");
				Exiled.API.Features.Server.ExecuteCommand("/cleanup blood");
				foreach (Exiled.API.Features.Room room in Var.RoomList)
				{
					room.Color = Color.white;
				}
				BorrarLucesDeZona(ZoneType.Entrance, ZoneType.HeavyContainment, ZoneType.LightContainment);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		});
	}

	private static IEnumerator<float> MatarDeAPoco(List<Exiled.API.Features.Player> victimas)
	{
		for (int i = 0; i < victimas.Count; i++)
		{
			Exiled.API.Features.Player p = victimas[i];
			try
			{
				if (p != null && p.IsConnected && p.IsAlive)
				{
					p.Kill(DamageType.Warhead);
				}
			}
			catch (Exception e)
			{
				Log.Debug("[Nuke] " + e.Message);
			}
			if (i % 4 == 3)
			{
				yield return float.NegativeInfinity;
			}
		}
	}

	public void OnWarheadStart(StartingEventArgs ev)
	{
		Timing.KillCoroutines(Var.NukeTimerHandle);
		Timing.CallDelayed(1f, delegate
		{
			Timing.KillCoroutines(Var.NukeTimerHandle);
			Var.NukeTimerHandle = Timing.RunCoroutine(Coroutines.NukeTimer());
		});
	}

	public void OnRoundStarted()
	{
		try
		{
			DateTime now = DateTime.Now;
			DateTime utcNow = DateTime.UtcNow;
			GeneratorHandler.Reset();
			Timing.KillCoroutines(Coroutines.AntiLag);
			Coroutines.AntiLag = Timing.RunCoroutine(Coroutines.clLoop());
			Var.RoomList = Exiled.API.Features.Room.List?.ToList() ?? new List<Exiled.API.Features.Room>();
			Var.Coroutines.Add(Timing.RunCoroutine(Scp0871.Bucle()));
			bool flag = now.Month == 4 && now.Day == 1;
			if (flag && Var.random.Next(50) == 0)
			{
				foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
				{
					item.Scale = new Vector3(1f, 1f, 0.01f);
				}
			}
			(string, string)[] array = (flag ? AnunciosAbril : AnunciosInicio);
			var (message, customSubtitles) = array[Var.random.Next(array.Length)];
			Announcer.Message(message, customSubtitles);
			MarcarItemsEspeciales();
			if (Var.random.Next(100) < 40)
			{
				Var.Coroutines.Add(Timing.CallDelayed(Var.random.Next(120, 300), delegate
				{
					Sonido.Mapa("AnnouncAfter1.ogg");
				}));
			}
			if (Var.random.Next(100) < 30)
			{
				Var.Coroutines.Add(Timing.CallDelayed(Var.random.Next(320, 600), ChequeoCamaras));
			}
			if (Var.random.Next(100) < 10)
			{
				Var.Coroutines.Add(Timing.CallDelayed(Var.random.Next(300, 500), BloqueoGateB));
			}
			int num = ((Var.random.Next(100) < 35) ? 1500 : 1800);
			Var.OmegaCoroutines.Add(Timing.CallDelayed(num, EventHandlers.StartOmegaWarhead));
			if (utcNow.Month == 12)
			{
				if (Var.random.Next(100) < 1)
				{
					Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.Navidad()));
				}
				if (utcNow.Day == 24 && Var.random.Next(100) < 50)
				{
					Sonido.Mapa("Grinch.ogg");
				}
			}
			Exiled.API.Features.Map.ChangeLightsColor(LuzAmbiente);
			Exiled.API.Features.Room.Get(RoomType.EzVent)?.ResetColor();
			Exiled.API.Features.Room.Get(RoomType.EzShelter)?.ResetColor();
			Var.Coroutines.Add(Timing.CallDelayed(Var.random.Next(300, 600), delegate
			{
				Sonido.Mapa($"ThreatAnnounc{Var.random.Next(1, 4)}.ogg");
			}));
			if (now.DayOfWeek == DayOfWeek.Sunday && Var.random.Next(100) < 10)
			{
				EventoRandom.Riot();
			}
			foreach (CoroutineHandle item2 in Var.GulagCoroutine)
			{
				Timing.KillCoroutines(item2);
			}
			Var.GulagCoroutine.Clear();
			Var.GenteGulag.Clear();
			Var.Estuvo.Clear();
			Var.Gulag1.Clear();
			Var.Gulag2.Clear();
			Var.Coroutines.Add(Timing.CallDelayed(4f, delegate
			{
				Var.GulagCoroutine.Add(Timing.RunCoroutine(Coroutines.GulagCheck()));
			}));
			Var.Coroutines.Add(Timing.CallDelayed(5f, delegate
			{
				Var.GulagCoroutine.Add(Timing.RunCoroutine(Coroutines.PracticaCheck()));
			}));
			ModosGulag.Elegir();
			AntiTeamKill.Reiniciar();
			Dinamica.AlEmpezarRonda();
			VentajasVip.AlEmpezarRonda();
			Scp3114Natural.AlEmpezarRonda();
			AspiradoraSurface.AlEmpezarRonda();
			Estadisticas.AlEmpezarRonda();
			AfkScp.AlEmpezarRonda();
			DiscordLog.AlEmpezarRonda();
		}
		catch (Exception arg)
		{
			Log.Error($"[OnRoundStarted] {arg}");
		}
	}

	private static void MarcarItemsEspeciales()
	{
		try
		{
			Var.CompuestoCSeriales.Clear();
			List<Exiled.API.Features.Pickups.Pickup> list = new List<Exiled.API.Features.Pickups.Pickup>();
			List<Vector3> list2 = new List<Vector3>();
			foreach (Exiled.API.Features.Pickups.Pickup item in Exiled.API.Features.Pickups.Pickup.List)
			{
				if (item?.Room == null || item.Room.Type == RoomType.Pocket || item.Room.Zone == ZoneType.Surface)
				{
					continue;
				}
				if (item.Type == ItemType.Adrenaline)
				{
					list.Add(item);
					continue;
				}
				ItemType type = item.Type;
				if ((type == ItemType.Medkit || type == ItemType.Painkillers) ? true : false)
				{
					list2.Add(item.Position);
				}
			}
			foreach (Exiled.API.Features.Pickups.Pickup item2 in list.OrderBy((Exiled.API.Features.Pickups.Pickup _) => Var.random.Next()).Take(4))
			{
				Var.CompuestoCSeriales.Add(item2.Serial);
				Log.Info($"Compuesto C spawneado en room: {item2.Room?.Type}");
			}
			foreach (Vector3 item3 in list2.OrderBy((Vector3 _) => Var.random.Next()).Take(4 - Var.CompuestoCSeriales.Count))
			{
				Exiled.API.Features.Pickups.Pickup pickup = Exiled.API.Features.Pickups.Pickup.CreateAndSpawn(ItemType.Adrenaline, item3 + Vector3.up * 0.15f, Quaternion.identity);
				if (pickup != null)
				{
					Var.CompuestoCSeriales.Add(pickup.Serial);
					Log.Info($"Compuesto C (extra) spawneado en room: {pickup.Room?.Type}");
				}
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[OnRoundStarted] customitems {arg}");
		}
	}

	private static void ChequeoCamaras()
	{
		Sonido.Mapa("AnnouncCameraCheck.ogg");
		Var.Coroutines.Add(Timing.CallDelayed(Var.random.Next(30, 60), delegate
		{
			if (Exiled.API.Features.Player.List.Any((Exiled.API.Features.Player x) => x.Role.Team == Team.ChaosInsurgency))
			{
				Sonido.Mapa("AnnouncCameraFound.ogg");
			}
			else if (Exiled.API.Features.Player.List.Count((Exiled.API.Features.Player x) => x.Role.Team == Team.ClassD) == 1)
			{
				Sonido.Mapa("AnnouncCameraFound2.ogg");
			}
			else
			{
				Sonido.Mapa("AnnouncNoFound.ogg");
			}
		}));
	}

	private static void BloqueoGateB()
	{
		Exiled.API.Features.Doors.Door door = Exiled.API.Features.Doors.Door.Get(DoorType.GateB);
		if (door != null && !Var.BetterOmegaWarheadActivated)
		{
			Sonido.Mapa("AnnouncAfter2.ogg");
			door.IsOpen = false;
			door.ChangeLock(DoorLockType.Isolation);
			Var.Coroutines.Add(Timing.CallDelayed(Var.random.Next(60, 120), delegate
			{
				Sonido.Mapa("GateB.ogg");
				Exiled.API.Features.Doors.Door.Get(DoorType.GateB)?.Unlock();
			}));
		}
	}

	public void OnRoundRestart()
	{
		try
		{
			Scp0871.Limpiar();
		}
		catch
		{
		}
		Timing.KillCoroutines(Coroutines.AntiLag);
		Coroutines.running = false;
		Var.ResetRonda();
		ArenaPractica.Destruir();
		GeneratorHandler.Reset();
		EffectsHUD.Stop();
		Vip.UsadosEnPartida.Clear();
		Vip.Diarrea.Clear();
		Vip.Diarrea2.Clear();
		Vip.Flash.Clear();
		Vip.Explode.Clear();
		Vip.Explode2.Clear();
		Vip.PersonasConHat.Clear();
		Vip.Ruleta.Clear();
		Vip.Role.Clear();
		Vip.StopIntercom.Clear();
		Vip.ResetIntercom.Clear();
		Vip.ResetIntercom2.Clear();
		Vip.Pet.Clear();
		Vip.CassieVIP.Clear();
		Vip.Gobierno.Clear();
		Vip.Nisman.Clear();
		Vip.Peron.Clear();
		Vip.Afip.Clear();
		Vip.Audio.Clear();
		Vip.Desintegracion.Clear();
		Vip.Fantasma.Clear();
		Vip.FantasmaTemp.Clear();
		Vip.intercom.Clear();
		Vip.caramelo.Clear();
		Vip.tarjeta.Clear();
		Vip.scan = DateTime.Now;
	}

	public void OnWaiting()
	{
		HiloPrincipal.Iniciar();
		// sonidos del SCP-087-1 cargados de antemano: si se cargan recién al aparecer, la primera vez puede no sonar
		Sonido.Precargar("miedito.ogg", "miedito1.ogg", "miedito2.ogg");
		EffectsHUD.Start();
		try
		{
			Logros.QuitarInsignias();
			Estadisticas.Guardar(enSegundoPlano: true);
		}
		catch (Exception ex)
		{
			Log.Warn("[Estadísticas] " + ex.Message);
		}
		Habilidades.Asegurar();
		if (Var.lucesPuertas.Count <= 0 && ConsolaLimpia.Opcion("LucesBotonesPuertas", porDefecto: false))
		{
			BasicDoorButton[] array = UnityEngine.Object.FindObjectsByType<BasicDoorButton>(FindObjectsSortMode.None);
			foreach (BasicDoorButton basicDoorButton in array)
			{
				Vector3 vector = basicDoorButton.transform.position + basicDoorButton.transform.forward * 0.15f;
				vector += ((vector.GetZone() == FacilityZone.HeavyContainment) ? (-0.1f) : 0.1f) * Vector3.up;
				CrearLuz(vector, LuzBoton);
			}
			KeycardButton[] array2 = UnityEngine.Object.FindObjectsByType<KeycardButton>(FindObjectsSortMode.None);
			foreach (KeycardButton keycardButton in array2)
			{
				CrearLuz(keycardButton.transform.position + keycardButton.transform.forward * 0.15f + 0.1f * Vector3.up, LuzKeycard);
			}
		}
		static void CrearLuz(Vector3 pos, Color color)
		{
			Exiled.API.Features.Toys.Light light = Exiled.API.Features.Toys.Light.Create(pos, null, Vector3.one * 0.01f, spawn: true, color);
			light.Range = 0.5f;
			light.Intensity = 0.2f;
			Var.lucesPuertas.Add(light);
		}
	}
}
