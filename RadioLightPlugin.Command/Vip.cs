using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandSystem;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using GameCore;
using Hazards;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.Voice;
using RadioLightPlugin.Misc;
using RelativePositioning;
using UnityEngine;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Vip : ICommand
{
	public static DateTime scan = default(DateTime);

	public static HashSet<string> Ruleta = new HashSet<string>();

	public static HashSet<string> Role = new HashSet<string>();

	public static HashSet<string> PersonasConHat = new HashSet<string>();

	public static HashSet<string> Explode = new HashSet<string>();

	public static HashSet<string> Explode2 = new HashSet<string>();

	public static HashSet<string> Flash = new HashSet<string>();

	public static HashSet<string> Diarrea = new HashSet<string>();

	public static HashSet<string> Diarrea2 = new HashSet<string>();

	public static HashSet<string> StopIntercom = new HashSet<string>();

	public static HashSet<string> ResetIntercom = new HashSet<string>();

	public static HashSet<string> ResetIntercom2 = new HashSet<string>();

	public static HashSet<string> Pet = new HashSet<string>();

	public static HashSet<string> CassieVIP = new HashSet<string>();

	public static HashSet<string> Gobierno = new HashSet<string>();

	public static HashSet<string> Nisman = new HashSet<string>();

	public static HashSet<string> Peron = new HashSet<string>();

	public static HashSet<string> Afip = new HashSet<string>();

	public static HashSet<string> Audio = new HashSet<string>();

	public static bool audio = false;

	private static DateTime _ultimoAudio = DateTime.MinValue;

	private static readonly Dictionary<string, string> AudiosVip = new Dictionary<string, string>
	{
		["acv"] = "VIP_acv.ogg",
		["bingo"] = "VIP_bingo.ogg",
		["coscu"] = "VIP_coscu.ogg",
		["esencia"] = "VIP_esencia.ogg",
		["fede"] = "VIP_fede.ogg",
		["loquita"] = "VIP_loquita.ogg",
		["mambo"] = "VIP_mambo.ogg",
		["mani1"] = "VIP_mani1.ogg",
		["mani2"] = "VIP_mani2.ogg",
		["sape"] = "VIP_sape.ogg"
	};

	public static HashSet<string> Desintegracion = new HashSet<string>();

	public static HashSet<string> Fantasma = new HashSet<string>();

	public static HashSet<string> FantasmaTemp = new HashSet<string>();

	public static HashSet<string> intercom = new HashSet<string>();

	public static HashSet<string> caramelo = new HashSet<string>();

	public static HashSet<string> tarjeta = new HashSet<string>();

	private static readonly Dictionary<string, float> _sizeCooldown = new Dictionary<string, float>();

	public static readonly HttpClient _client = new HttpClient();

	public static readonly HashSet<string> UsadosEnPartida = new HashSet<string>();

	private static readonly HashSet<string> SinLimite = new HashSet<string> { "normal", "unpet", "lista", "help", "ayuda" };

	private static readonly string[] Fallos = new string[21]
	{
		"No ", "Ya ", "Uso", "Debes", "Solo puedes", "Eres un SCP", "El texto", "El mensaje", "Tu nombre", "Nombre muy",
		"Mensaje muy", "texto muy", "La clase ya", "Este comando está desactivado", "Necesitas", "Los audios", "Comprobando", "Tenés que", "Mascotas:", "Eres <color",
		"Lista de comandos"
	};

	public static string CmdsVIP = ".vip openintercom (Abre la puerta del Intercom)\n.vip resetintercom (Resetea el tiempo de Intercom)\n.vip ruleta (Te pone una clase aleatoria)\n.vip nick (Te cambia el nombre al que quieras)\n.vip chiquito (Te hace chiquito)\n.vip wide (Te hace mas ancho)\n.vip scan (Permite escanear la instalación, con un cooldown de 2 minutos)\n.vip color (Cambia el color del rango)\n.vip normal (Te hace de altura, anchura y profundidad normal)";

	public static string CmdsVIPp = "\n.vip hat (Te pone un sombrero en la cabeza)\n.vip role (Te pone la clase que quieras)\n.vip diarrea (Spawnea la caca del 173 en la ubicación donde está el jugador)\n.vip widechiquito (Te hace chiquito y ancho)";

	public static string CmdsVIPpp = "\n.vip pet (Spawnea un avión que va atrás tuyo y te sigue en toda la ronda)\n.vip unpet (Elimina el avión que va atrás tuyo)\n.vip grande (Te hace un jugador de básquet)\n.vip flash (Tira una granada cegadora en tu posición)\n.vip cassie (Te permite enviar un mensaje en C.A.S.S.I.E. con un límite de 40 caracteres)\n.vip gobierno (Te da 3 monedas, los cuales los robaste de los impuestos)\n.vip nisman (Te \"suicidas\")\n.vip peron (Te cortan las manos)\n.vip afip (Pagas los impuestos, osea te borra el inventario)\n.vip audio (Te deja reproducir un audio de una lista, el cual lo escuchan todos)\n.vip desintegrar (Te desintegra con el efecto del disruptor de partículas)\n.vip stopintercom (Para la Intercom, útil para cuando alguien está molestando)\n.vip boo (Revives como Tutorial con efecto de fantasma y te teletransportas a alguien aleatorio por 30 segundos)\n.vip helicoptero (Te hace girar y volar, y mueres por daño de caída)\n.vip buff (Meme de Tick Tock de los mercenarios de TF2)\n.vip intercom (Te pone el Intercom por 10 segundos)\n.vip caramelo (Te da un caramelo aleatorio)";

	private static readonly HashSet<string> palabrasRacistas = new HashSet<string>
	{
		"nigga", "n1gga", "nigg@", "n!gga", "nigg3r", "nigg#r", "n1gg3r", "n1gg#r", "nigger", "niggah",
		"n1gger", "n1ggah", "nigg*r", "n!gg*r", "n1gg*r", "n1ggeR", "n1gGeR", "n1gge#", "n!ggeR", "n!gge#",
		"n1gge$", "nigg4", "n1gg4", "nigg5", "n1gg5", "nigg$", "n1gg$", "n!gg4", "n!gg5", "n!gg$",
		"n1gg3r", "n1gg#r", "n1gg*r", "n1gg3#", "n1gg4", "n1gg5", "n1gg$", "n1gge4", "n1gge5", "n1gge$",
		"n!gg3r", "n!gg#r", "n!gg*r", "n!gg3#", "n!gg4", "n!gg5", "n!gg$", "n!gge4", "n!gge5", "n!gge$",
		"nazi", "naz1", "n4zi", "n@zi", "n4z1", "judio", "jud1o", "j4udio", "jud10", "judi0",
		"j00d", "j00di0", "j0udi0", "j0od", "juden", "jud3n", "j4uden", "jud3n", "judeen", "judden",
		"j0oden", "j0uden", "j0od3n", "j0od3n", "hitler", "h1tler", "h1tl3r", "h1tleR", "h1tle#", "heil",
		"h3il", "he1l", "he!l", "fuhrer", "fuhreR", "fuhre#", "fuhre5", "fuhr3r", "fuh#er", "fuh5er",
		"<size", "$PITCH_", "negros"
	};

	public bool SanitizeResponse => false;

	public string Command { get; } = "vip";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Todos los comandos para VIP y VIP+.";


	private static bool PonerTantrum(Vector3 posicion)
	{
		try
		{
			foreach (GameObject value in NetworkClient.prefabs.Values)
			{
				if (value == null || !value.TryGetComponent<TantrumEnvironmentalHazard>(out var component))
				{
					continue;
				}
				TantrumEnvironmentalHazard tantrumEnvironmentalHazard = UnityEngine.Object.Instantiate(component);
				tantrumEnvironmentalHazard.SynchronizedPosition = new RelativePosition(posicion + Vector3.up * 0.25f);
				NetworkServer.Spawn(tantrumEnvironmentalHazard.gameObject);
				return true;
			}
			Log.Warn("[VIP] No se encontró el prefab del tantrum.");
		}
		catch (Exception ex)
		{
			Log.Warn("[VIP] Diarrea: " + ex.Message);
		}
		return false;
	}

	private static bool FueExitoso(string respuesta)
	{
		if (string.IsNullOrWhiteSpace(respuesta))
		{
			return false;
		}
		string text = respuesta.TrimStart('\n', ' ');
		if (text.StartsWith("No tengo nada que ver"))
		{
			return true;
		}
		string[] fallos = Fallos;
		foreach (string value in fallos)
		{
			if (text.StartsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}

	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Exiled.API.Features.Player player = Exiled.API.Features.Player.Get(sender);
		string text = ((arguments.Count > 0) ? arguments.At(0).ToLowerInvariant() : null);
		int num;
		if (player != null && text != null && !SinLimite.Contains(text) && (!(text == "audio") || arguments.Count >= 2))
		{
			if (player.RemoteAdminAccess)
			{
				Plugin singleton = Plugin.Singleton;
				num = ((singleton == null || singleton.Config?.VipStaffSinLimite != true) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		string item = (flag ? (player.UserId + "|" + text) : null);
		if (flag && UsadosEnPartida.Contains(item))
		{
			response = "Ya usaste '.vip " + text + "' en esta partida. Vuelve a estar disponible en la próxima.";
			return false;
		}
		bool result = EjecutarComando(arguments, sender, out response);
		if (flag && FueExitoso(response))
		{
			UsadosEnPartida.Add(item);
		}
		return result;
	}

	private bool EjecutarComando(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Exiled.API.Features.Player player2 = Exiled.API.Features.Player.Get(sender);
		if (Var.Gulag1.Contains(player2) || Var.Gulag2.Contains(player2))
		{
			response = "No puedes usar comandos VIP estando en el arena del Gulag.";
			return false;
		}
		if (arguments.Count == 0)
		{
			response = "\n<color=blue>Lista de comandos:</color>\n.vip [stopintercom/resetintercom/openintercom/role/ruleta/nick/wide/grande/chiquito/widechiquito/color/scan/hat/flash/normal/diarrea/pet/unpet/cassie/gobierno/nisman/peron/afip/audio/desintegrar/boo/helicoptero/buff/intercom/caramelo/tarjeta]";
			return false;
		}
		switch (arguments.At(0).ToLower())
		{
		case "stopintercom":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!StopIntercom.Contains(player2.UserId))
				{
					if (player2.UserId != "76561198317426857@steam")
					{
						StopIntercom.Add(player2.UserId);
					}
					PlayerRoles.Voice.Intercom.State = IntercomState.Cooldown;
					response = "Intercom parado.";
					return true;
				}
				response = "Ya paraste la Intercom.";
				return false;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return false;
		case "resetintercom":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				if (!ResetIntercom.Contains(player2.UserId))
				{
					ResetIntercom.Add(player2.UserId);
					PlayerRoles.Voice.Intercom.State = IntercomState.Ready;
					response = "Intercom reiniciado.";
					return true;
				}
				response = "Ya reiniciaste la Intercom.";
				if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
				{
					if (!ResetIntercom2.Contains(player2.UserId))
					{
						ResetIntercom2.Add(player2.UserId);
						Exiled.API.Features.Intercom.Reset();
						response = "Intercom reiniciado por segunda vez.";
						return true;
					}
					response = "Ya reiniciaste la Intercom 2 veces.";
				}
				else
				{
					response = "Ya reiniciaste la Intercom.";
				}
				return false;
			}
			response = "\nNo eres VIP.\nSi quieres conseguir VIP, entra en nuestro Discord.";
			return false;
		case "openintercom":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				Exiled.API.Features.Doors.Door.Get(DoorType.Intercom).IsOpen = true;
				response = "Intercom abierto.";
				return true;
			}
			response = "\nNo eres VIP.\nSi quieres conseguir VIP, entra en nuestro Discord.";
			return true;
		case "role":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!Role.Contains(player2.UserId))
				{
					if (Exiled.API.Features.Round.IsStarted && RoundStart.RoundLength.TotalSeconds <= 40.0)
					{
						if (arguments.Count == 0)
						{
							response = "Uso del comando: role [clased], [cientifico] o [guardia]";
							return true;
						}
						if (CanChangeRole(arguments.At(1).ToLower()))
						{
							player2.Role.Set(GetRoleTypeId(arguments.At(1).ToLower()));
							Role.Add(player2.UserId);
							response = "Clase cambiada correctamente.";
						}
						else
						{
							response = "La clase ya está ocupada o no existe.";
						}
					}
					else
					{
						response = "No puedes usar este comando si la ronda no ha empezado o han pasado más de 40 segundos desde que empezó la ronda.";
					}
				}
				else
				{
					response = "Ya usaste este comando.";
				}
			}
			else
			{
				response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			}
			return false;
		case "ruleta":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (Ruleta.Contains(player2.UserId))
				{
					response = "Ya usaste este comando.";
					return false;
				}
				if (!Exiled.API.Features.Round.IsStarted)
				{
					response = "No se puede usar este comando si no ha comenzado la partida.";
					return false;
				}
				if (RoundStart.RoundLength.TotalSeconds > 40.0)
				{
					response = "Ya no puedes usar este comando.";
					return false;
				}
				List<RoleTypeId> list = new List<RoleTypeId>
				{
					RoleTypeId.Scp173,
					RoleTypeId.Scp106,
					RoleTypeId.Scp049,
					RoleTypeId.Scp079,
					RoleTypeId.Scp096,
					RoleTypeId.Scp939,
					RoleTypeId.ClassD,
					RoleTypeId.Scientist,
					RoleTypeId.FacilityGuard,
					RoleTypeId.ClassD,
					RoleTypeId.Scientist,
					RoleTypeId.FacilityGuard,
					RoleTypeId.ClassD,
					RoleTypeId.Scientist,
					RoleTypeId.FacilityGuard
				};
				try
				{
					player2.Role.Set(RoleTypeId.Spectator);
					RoleTypeId roleTypeId = list.RandomItem();
					if (roleTypeId.GetFaction() == Faction.SCP && Exiled.API.Features.Player.Get(roleTypeId).Any())
					{
						roleTypeId = RoleTypeId.FacilityGuard;
					}
					player2.Role.Set(roleTypeId);
					player2.DisableAllEffects();
				}
				catch
				{
					player2.Role.Set(RoleTypeId.ClassD);
					player2.DisableAllEffects();
				}
				finally
				{
					Ruleta.Add(player2.UserId);
				}
				response = "A ver qué te toca...";
				return true;
			}
			response = "\nNo eres VIP.\nSi quieres conseguir VIP, entra en nuestro Discord.";
			return false;
		case "nick":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				if (arguments.Count == 0)
				{
					response = "Uso del comando: .vip nick El pepe";
					return false;
				}
				string text3 = string.Join(" ", arguments.Skip(1));
				if (text3.Length > 80)
				{
					response = "Nombre muy largo, intenta otro.";
					return false;
				}
				if (string.IsNullOrWhiteSpace(text3))
				{
					response = "Tu nombre no puede estar vacío.";
					return false;
				}
				if (player2.DoNotTrack)
				{
					response = "Debes desactivar el 'Do Not Track' para usar este comando.";
					return false;
				}
				if (EsPalabraRacista(text3.ToLower()))
				{
					response = "No puedes usar ese nombre.\nSi te cambias el nombre a uno parecido bypasseando, tu VIP será eliminado y serás baneado.";
					return false;
				}
				player2.DisplayNickname = text3;
				response = "Nick cambiado a " + text3 + ".";
				return true;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "grande":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (IsOnCooldown(player2, out response))
				{
					return false;
				}
				if (FantasmaTemp.Contains(player2.UserId))
				{
					response = "No puedes cambiarte de tamaño siendo fantasma.";
					return false;
				}
				if (Exiled.API.Features.Round.IsStarted)
				{
					player2.Scale = new Vector3(1.3f, 1.3f, 1.3f);
					response = "Ahora juegas basquet.";
					return true;
				}
				response = "No puedes usar este comando si la ronda no empezó.";
				return false;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "chiquito":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (IsOnCooldown(player2, out response))
				{
					return false;
				}
				if (Exiled.API.Features.Round.IsStarted)
				{
					if (player2.Role == RoleTypeId.Scp096)
					{
						response = "No puedes usar este comando siendo 096, debido a que da una ventaja.";
						return false;
					}
					if (player2.Role == RoleTypeId.Scp939)
					{
						response = "No puedes usar este comando siendo 939, debido a que da una ventaja.";
						return false;
					}
					player2.Scale = new Vector3(0.88f, 0.88f, 0.88f);
					response = null;
					return false;
				}
				response = "No puedes usar este comando si la ronda no empezó.";
				return false;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "wide":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (IsOnCooldown(player2, out response))
				{
					return false;
				}
				if (Exiled.API.Features.Round.IsStarted)
				{
					player2.Scale = new Vector3(1.2f, 1f, 1.2f);
					response = null;
					return false;
				}
				response = "No puedes usar este comando si la ronda no empezó.";
				return true;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "widechiquito":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (IsOnCooldown(player2, out response))
				{
					return false;
				}
				if (Exiled.API.Features.Round.IsStarted)
				{
					if (FantasmaTemp.Contains(player2.UserId))
					{
						response = "No puedes cambiarte de tamaño siendo fantasma.";
						return false;
					}
					if (player2.Role == RoleTypeId.Scp096)
					{
						response = "No puedes usar este comando siendo SCP-096, debido a que da una ventaja.";
						return false;
					}
					if (player2.Role == RoleTypeId.Scp939)
					{
						response = "No puedes usar este comando siendo SCP-939, debido a que da una ventaja.";
						return false;
					}
					player2.Scale = new Vector3(1.2f, 0.88f, 1.2f);
					response = "Ahora eres chiquito y gordito.";
					return true;
				}
				response = "No puedes usar este comando si la ronda no empezó.";
				return false;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "scan":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				if (RoundStart.RoundLength.TotalMinutes > 5.0)
				{
					if ((DateTime.Now - scan).TotalSeconds >= 30.0)
					{
						Exiled.API.Features.Cassie.MessageTranslated("bell_start . Scan Order Recieve . Scanning . . . . . . . . . .", "Orden de escaneo recibida. Escaneando...", isHeld: false, isNoisy: false);
						int num2 = Exiled.API.Features.Player.Get(Team.ClassD).Count();
						int num3 = Exiled.API.Features.Player.Get(Team.Scientists).Count();
						int num4 = Exiled.API.Features.Player.Get(Team.SCPs).Count();
						int num5 = Exiled.API.Features.Player.Get(Team.FoundationForces).Count();
						int num6 = Exiled.API.Features.Player.Get(Team.ChaosInsurgency).Count();
						string text4 = ((num2 == 1) ? "1 <color=#FF8E00>Personal Clase-D</color>" : $"{num2} <color=#FF8E00>Personales Clase-D</color>");
						string text5 = ((num3 == 1) ? "1 <color=#FFFF7C>Personal Científico</color>" : $"{num3} <color=#FFFF7C>Personales Científico</color>");
						string text6 = ((num4 == 1) ? "1 <color=#EC2121>Sujeto SCP</color>" : $"{num4} <color=#EC2121>Sujetos SCP</color>");
						string text7 = ((num5 == 1) ? "1 <color=#0096FF>Nine Tailed Fox</color>" : $"{num5} <color=#0096FF>Nine Tailed Fox</color>");
						string text8 = ((num6 == 1) ? "1 <color=#008F1E>Insurgente del Caos</color>" : $"{num6} <color=#008F1E>Insurgentes del Caos</color>");
						Announcer.Message($"Scan Complete . Detected . {num2} Class D personnel . {num3} Scientist personnel . {num4} SCPsubjects . {num5} NineTailedFox and {num6} ChaosInsurgency bell_end", "Escaneo completado<split>Detectado: " + text4 + ", " + text5 + ", " + text6 + ", " + text7 + " y " + text8 + ".", playBackground: false);
						scan = DateTime.Now;
						response = "Empezando escaneo...";
						return true;
					}
					response = $"Debes esperar {30 - (int)(DateTime.Now - scan).TotalSeconds} s: hay otro escaneo en curso.";
					return false;
				}
				response = "Debes esperar a que la ronda lleve 5 minutos para escanear.";
				return false;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "color":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				switch (arguments.At(1))
				{
				case "police_blue":
				case "silver_blue":
				case "light_red":
				case "purple":
				case "blue":
				case "teal":
				case "gold":
					response = "No puedes usar ese color, debes usar otro como aqua, army_green, brown, carmine, crimson, cyan, deep_pink, emerald, green, lime, magenta, mint, nickel, orange, pink, red, silver, tomato o yellow.";
					return true;
				default:
					player2.ReferenceHub.serverRoles.Network_myColor = arguments.At(1);
					response = "Color cambiado correctamente.";
					return false;
				}
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "hat":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!PersonasConHat.Contains(player2.UserId))
				{
					if (player2.IsHuman)
					{
						Exiled.API.Features.Pickups.Pickup pickup = Exiled.API.Features.Items.Item.Create(ItemType.SCP268).CreatePickup(player2.Position + Vector3.up);
						PersonasConHat.Add(player2.UserId);
						pickup.IsLocked = true;
						pickup.Rigidbody.isKinematic = false;
						pickup.Rigidbody.useGravity = false;
						pickup.Base.transform.localRotation = Quaternion.Euler(-90f, 0f, 90f);
						pickup.Base.transform.SetParent(player2.ReferenceHub.transform);
						Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.MoveHat(player2, pickup)));
						response = "Comando usado correctamente.";
						return true;
					}
					response = "No puedes usar este comando siendo SCP o espectador.";
					return false;
				}
				response = "Ya tienes un hat.";
				return false;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "flash":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (player2.IsTutorial)
				{
					response = "No puedes usar este comando siendo tutorial.";
					return true;
				}
				if (FantasmaTemp.Contains(player2.UserId))
				{
					response = "No puedes usar este comando siendo fantasma.";
					return true;
				}
				if (player2.ReferenceHub.roleManager.CurrentRole is Scp106Role { IsStalking: not false })
				{
					response = "No puedes usar este comando debajo del suelo.";
					return true;
				}
				if (!Flash.Contains(player2.UserId))
				{
					string mensaje3 = $"{player2.Nickname} tiró una flash en '{player2.CurrentRoom.Name}', que queda en '{player2.Zone}'.";
					Task.Run(() => Logs(mensaje3, Secretos.WebhookVip));
					Otros.FlashExplode(player2);
					Flash.Add(player2.UserId);
					response = "Fire in the hole.";
					return true;
				}
				response = "Ya tiraste una cegadora.";
				return true;
			}
			response = "No tienes permisos para usar este comando.";
			return true;
		case "explode":
			response = "Este comando está desactivado.";
			return false;
		case "normal":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (FantasmaTemp.Contains(player2.UserId))
				{
					response = "No puedes cambiarte de tamaño siendo fantasma.";
					return true;
				}
				player2.Scale = new Vector3(1f, 1f, 1f);
				response = "Ahora eres normal.";
				return false;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "diarrea":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Creador de contenido" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (player2.IsTutorial)
				{
					response = "No puedes usar este comando siendo tutorial.";
					return true;
				}
				if (FantasmaTemp.Contains(player2.UserId))
				{
					response = "No puedes cagar siendo fantasma.";
					return true;
				}
				if (!Diarrea.Contains(player2.UserId))
				{
					string mensaje2 = string.Format("{0} se cagó en '{1}', que queda en '{2}'.", player2.Nickname, player2.CurrentRoom?.Name.ToString() ?? "?", player2.Zone);
					Task.Run(() => Logs(mensaje2, Secretos.WebhookVipCompras));
					if (!PonerTantrum(player2.Position))
					{
						response = "No se pudo poner la diarrea (error del juego). Avisale a un admin.";
						return false;
					}
					if (player2.UserId != "76561198317426857@steam")
					{
						Diarrea.Add(player2.UserId);
					}
					response = "Por comer lentejas con picante.";
					return true;
				}
				if (!Diarrea2.Contains(player2.UserId) && player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+")
				{
					string mensaje = string.Format("{0} se cagó en '{1}', que queda en '{2}'.", player2.Nickname, player2.CurrentRoom?.Name.ToString() ?? "?", player2.Zone);
					Task.Run(() => Logs(mensaje, Secretos.WebhookVip));
					if (!PonerTantrum(player2.Position))
					{
						response = "No se pudo poner la diarrea (error del juego). Avisale a un admin.";
						return false;
					}
					Diarrea2.Add(player2.UserId);
					response = "Por comer lentejas con picante.";
					return true;
				}
				response = "Ya tuviste diarrea.";
				return false;
			}
			response = "No tienes permisos para usar este comando.";
			return false;
		case "pet":
			if (VentajasVip.EsVip(player2))
			{
				response = VentajasVip.PonerMascota(player2, (arguments.Count > 1) ? arguments.At(1) : null);
			}
			else
			{
				response = "No tienes permisos para usar este comando.";
			}
			return true;
		case "unpet":
			if (VentajasVip.TieneMascota(player2))
			{
				VentajasVip.QuitarMascota(player2);
				response = "Pet eliminado.";
			}
			else
			{
				response = "No tienes un pet.";
			}
			return true;
		case "cassie":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!CassieVIP.Contains(player2.UserId))
				{
					if (arguments.Count == 0)
					{
						response = "Uso del comando: cassie hola niños";
						return false;
					}
					string text = string.Join(" ", arguments.Skip(1));
					if (text.Length > 40)
					{
						response = "Mensaje muy largo, pon otro.";
						return false;
					}
					if (string.IsNullOrWhiteSpace(text))
					{
						response = "El mensaje no puede estar vacío.";
						return false;
					}
					if (EsPalabraRacista(text.ToLower()))
					{
						response = "No puedes mandar ese mensaje.\nSi pones un mensaje parecido bypasseando, tu VIP será eliminado y serás baneado permanentemente.";
						return false;
					}
					Gptxdd("Si el siguiente texto contiene alguna palabra discriminadora (chileno de mierda, argentino de mierda, etc), racista (nigger, negros de mierda, nigg, etc), etc., di 'racismo detectado', si no, di 'No racismo'. no debes decir nada más. texto: " + text, player2, text);
					response = "Comprobando si tu mensaje contiene algo no permitido...";
					return true;
				}
				response = "Ya mandaste un mensaje de C.A.S.S.I.E.";
				return true;
			}
			response = "No tienes permisos para usar este comando.";
			return true;
		case "gobierno":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!Gobierno.Contains(player2.UserId))
				{
					Gobierno.Add(player2.UserId);
					player2.AddItem(ItemType.Coin);
					player2.AddItem(ItemType.Coin);
					player2.AddItem(ItemType.Coin);
					response = "Felicidades, robaste la plata de los impuestos.";
					return true;
				}
				response = "Ya robaste la plata de los impuestos.";
				return true;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return true;
		case "nisman":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				if (!Nisman.Contains(player2.UserId))
				{
					Nisman.Add(player2.UserId);
					player2.Kill("Nismeneado");
					response = "No tengo nada que ver con lo que sucedió con Nisman";
					return true;
				}
				response = "Ya te Nismenearon..";
				return true;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return false;
		case "peron":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!Peron.Contains(player2.UserId))
				{
					Peron.Add(player2.UserId);
					player2.EnableEffect(EffectType.SeveredHands);
					response = "La gracia de este comando es que es referencia de cuando a Perón le robaron los brazos...";
					return true;
				}
				response = "Ya te cortaron las manos.";
				return true;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return true;
		case "afip":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				if (!Afip.Contains(player2.UserId))
				{
					Afip.Add(player2.UserId);
					player2.ClearInventory();
					player2.DisableEffect(EffectType.Scp1344);
					response = "Esto tendría que ser renombrado a '.vip arca' pero no importa.";
					return true;
				}
				response = "Ya pagaste los impuestos.";
				return true;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return true;
		case "audio":
			if (player2.ReferenceHub.serverRoles.Group?.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (arguments.Count < 2)
				{
					response = "\nSolo puedes usar uno de los siguientes audios:\n\nacv\nbingo\ncoscu\nesencia\nfede\nloquita\nmambo\nmani1\nmani2\nsape\n\nPones '.vip audio NOMBREDELAUDIO'.";
					return false;
				}
				if (!Exiled.API.Features.Round.IsStarted)
				{
					response = "\nNo puedes reproducir audios en el lobby.";
					return false;
				}
				if (Audio.Contains(player2.UserId))
				{
					response = "\nYa pusiste un audio.";
					return false;
				}
				if (!AudiosVip.TryGetValue(arguments.At(1).ToLowerInvariant(), out var value))
				{
					response = "\nSolo puedes usar uno de los siguientes audios:\n\nacv\nbingo\ncoscu\nesencia\nfede\nloquita\nmambo\nmani1\nmani2\nsape\n\nPones '.vip audio NOMBREDELAUDIO'.";
					return false;
				}
				double num = 30.0 - (DateTime.UtcNow - _ultimoAudio).TotalSeconds;
				if (num > 0.0)
				{
					response = $"\nDebes esperar {Math.Ceiling(num)} s: hay otro audio sonando.";
					return false;
				}
				_ultimoAudio = DateTime.UtcNow;
				Sonido.Mapa(value);
				Audio.Add(player2.UserId);
				response = "\nReproduciendo audio.";
				return true;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return false;
		case "desintegrar":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!Desintegracion.Contains(player2.UserId))
				{
					if (player2.UserId != "76561198317426857@steam")
					{
						Desintegracion.Add(player2.UserId);
					}
					player2.Vaporize(player2);
					response = "\nDesintegrandote...";
					return true;
				}
				response = "\nYa te desintegraste.";
				return false;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return false;
		case "boo":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Miembro Verificado" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!player2.IsAlive)
				{
					if (!Fantasma.Contains(player2.UserId))
					{
						if (player2.UserId != "76561198317426857@steam")
						{
							Fantasma.Add(player2.UserId);
						}
						FantasmaTemp.Add(player2.UserId);
						player2.Role.Set(RoleTypeId.Tutorial);
						player2.Handcuff();
						Timing.CallDelayed(0.3f, delegate
						{
							player2.EnableEffect(EffectType.Ghostly);
							player2.EnableEffect(EffectType.SilentWalk);
							player2.EnableEffect(EffectType.Fade);
							player2.EnableEffect(EffectType.SeveredHands);
							if (player2.TryGetEffect<SilentWalk>(out var statusEffect))
							{
								statusEffect.Intensity = byte.MaxValue;
							}
							if (player2.TryGetEffect<Fade>(out var statusEffect2))
							{
								statusEffect2.Intensity = byte.MaxValue;
							}
							player2.Scale = new Vector3(0f, 1f, 0f);
							player2.Position = Exiled.API.Features.Player.List.Where((Exiled.API.Features.Player player) => player.Role != RoleTypeId.Tutorial && player.Role.Team != Team.Dead && player != null && player.Role != RoleTypeId.Scp079).ToArray().RandomItem()
								.Position;
							player2.Position += new Vector3(0f, 0.5f, 0f);
							player2.Health = 6767f;
						});
						Timing.CallDelayed(10f, delegate
						{
							player2.ShowHint("<color=#859CC7>Tienes 20 segundos para seguir siendo fantasma...", 11f);
							Timing.CallDelayed(10f, delegate
							{
								player2.ShowHint("<color=#e32433>Tienes 10 segundos para seguir siendo fantasma...", 10f);
								Timing.CallDelayed(10f, delegate
								{
									player2.Scale = new Vector3(1f, 1f, 1f);
									FantasmaTemp.Remove(player2.UserId);
									if (!(player2.Role != RoleTypeId.Tutorial) && !Var.Gulag1.Contains(player2) && !Var.Gulag2.Contains(player2))
									{
										player2.IsGodModeEnabled = false;
										player2.Vaporize();
									}
								});
							});
						});
						response = "\nPrepárate para asustar...";
						return true;
					}
					response = "\nYa fuiste a espantar gente.";
					return false;
				}
				response = "\nNecesitas estar muerto para usar este comando.";
				return false;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return false;
		case "helicoptero":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				Timing.RunCoroutine(Coroutines.RotateCoroutine(player2));
				Timing.CallDelayed(5f, delegate
				{
					player2.Kill("Se mareó por ser un helicoptero...");
				});
				response = "\nHelikopter helikopter.";
				return true;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return true;
		case "buff":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.ReferenceHub.serverRoles.Group.BadgeText == "Discord Booster" || player2.RemoteAdminAccess)
			{
				player2.ShowHint(TickTock.Buff1, 10f);
				Timing.CallDelayed(3f, delegate
				{
					player2.ShowHint(TickTock.Buff2, 10f);
				});
				Timing.CallDelayed(10f, delegate
				{
					player2.ShowHint(TickTock.Buff3, 10f);
				});
				Timing.CallDelayed(16f, delegate
				{
					player2.ShowHint(TickTock.Buff4, 10f);
				});
				Sonido.Para(player2.Id, "Buff.ogg");
				response = "\nTick Tock.";
				return true;
			}
			response = "\nNo eres VIP+.\nSi quieres conseguir VIP+, entra en nuestro Discord.";
			return false;
		case "intercom":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!intercom.Contains(player2.UserId))
				{
					if (player2.Role.Side == Side.Scp || player2.Role == RoleTypeId.Spectator)
					{
						response = "Eres un SCP o espectador, no puedes hablar.";
						return false;
					}
					if (!PlayerRoles.Voice.Intercom.TrySetOverride(player2.ReferenceHub, newState: true))
					{
						response = "No se pudo abrir el Intercom ahora. Probá de nuevo en un momento.";
						return false;
					}
					intercom.Add(player2.UserId);
					Timing.CallDelayed(10f, delegate
					{
						try
						{
							PlayerRoles.Voice.Intercom.TrySetOverride(player2.ReferenceHub, newState: false);
						}
						catch
						{
						}
						if (player2 != null && player2.IsConnected)
						{
							player2.ShowHint("<color=#e32433>Se te terminó el Intercom...</color>");
						}
					});
					response = "Ahora estás usando el Intercom: tenés 10 segundos, hablá normal (tecla de voz).";
					return true;
				}
				response = "Ya usaste este comando.";
				return true;
			}
			response = "No eres VIP+. Si quieres conseguir VIP+, entra en nuestro Discord.";
			return true;
		case "caramelo":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "Miembro Verificado" || player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!caramelo.Contains(player2.UserId))
				{
					if (player2.Role.Side == Side.Scp || player2.Role == RoleTypeId.Spectator || player2.IsTutorial)
					{
						response = "Eres un SCP, Tutorial o espectador, no puedes tener un caramelo.";
						return false;
					}
					caramelo.Add(player2.UserId);
					player2.TryAddCandy((CandyKindID)Var.candyTypes.GetValue(Var.random.Next(Var.candyTypes.Length)));
					response = "Disfruta tu caramelo.";
					return true;
				}
				response = "Ya usaste este comando.";
				return true;
			}
			response = "No eres VIP+. Si quieres conseguir VIP+, entra en nuestro Discord.";
			return true;
		case "tarjeta":
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+" || player2.RemoteAdminAccess)
			{
				if (!tarjeta.Contains(player2.UserId))
				{
					if (player2.Role.Side == Side.Scp || player2.Role == RoleTypeId.Spectator || player2.IsTutorial)
					{
						response = "Eres un SCP, Tutorial o espectador, no puedes tener una tarjeta.";
						return false;
					}
					if (arguments.Count == 0)
					{
						response = "Uso del comando: .vip tarjeta 1000 pesos";
						return false;
					}
					string text2 = string.Join(" ", arguments.Skip(1));
					text2 = text2.Replace(" ", "_");
					if (text2.Length > 50)
					{
						response = "texto muy largo, intenta otro.";
						return false;
					}
					if (string.IsNullOrWhiteSpace(text2))
					{
						response = "El texto de tu tarjeta no puede estar vacío.";
						return false;
					}
					if (EsPalabraRacista(text2.ToLower()))
					{
						response = "No puedes usar ese texto.\nSi cambias el texto a uno parecido bypasseando, tu VIP será eliminado y serás baneado.";
						return false;
					}
					tarjeta.Add(player2.UserId);
					Exiled.API.Features.Server.ExecuteCommand($"/customkeycard {player2.Id} keycardcustomsite02 Tarjeta_de_VIP+ 0 0 0 orange orange {text2} orange {player2.Nickname} 1");
					response = "Disfruta tu tarjeta.";
					return true;
				}
				response = "Ya usaste este comando.";
				return true;
			}
			response = "No eres VIP+. Si quieres conseguir VIP+, entra en nuestro Discord.";
			return true;
		default:
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP")
			{
				response = "Eres <color=#9725ff>VIP</color>, puedes usar los siguientes comandos:\n" + CmdsVIP + "+" + CmdsVIPp;
				return true;
			}
			if (player2.ReferenceHub.serverRoles.Group.BadgeText == "VIP+")
			{
				response = "Eres <color=#ffa500>VIP+</color>, puedes usar los siguientes comandos:\n" + CmdsVIP + "+" + CmdsVIPp + "+" + CmdsVIPpp;
				return true;
			}
			if (player2.RemoteAdminAccess)
			{
				response = "Eres <color=#277ecd>Staff</color>, puedes usar los siguientes comandos:\n" + CmdsVIP + "+" + CmdsVIPp + "+" + CmdsVIPpp;
				return true;
			}
			response = "No eres VIP.\nSi quieres conseguir VIP, entra en nuestro Discord.";
			return true;
		}
		static bool CanChangeRole(string className)
		{
			switch (className)
			{
			case "clased":
			case "guardia":
			case "cientifico":
				return true;
			default:
				return false;
			}
		}
		static RoleTypeId GetRoleTypeId(string className)
		{
			return className switch
			{
				"clased" => RoleTypeId.ClassD, 
				"guardia" => RoleTypeId.FacilityGuard, 
				"cientifico" => RoleTypeId.Scientist, 
				_ => RoleTypeId.None, 
			};
		}
	}

	private bool IsOnCooldown(Exiled.API.Features.Player player, out string response)
	{
		response = null;
		float time = Time.time;
		if (_sizeCooldown.TryGetValue(player.UserId, out var value) && time - value < 1f)
		{
			response = "Debes esperar 1 segundo antes de usar este comando.";
			return true;
		}
		_sizeCooldown[player.UserId] = time;
		return false;
	}

	public static async Task Logs(string payload, string whurl)
	{
		if (string.IsNullOrWhiteSpace(whurl))
		{
			return;
		}
		try
		{
			StringContent msg = new StringContent("{\"content\": \"" + payload + "\"}", Encoding.UTF8, "application/json");
			await _client.PostAsync(whurl, msg);
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Error("Error en webhook pe " + ex.Message);
		}
	}

	private async void Gptxdd(string texto, Exiled.API.Features.Player player, string textog)
	{
		string output2;
		try
		{
			output2 = await Otros.SendRequestAndGetResponse(texto).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Exception e = ex;
			Log.Warn("[VIP cassie] No se pudo consultar la IA: " + e.Message);
			output2 = string.Empty;
		}
		output2 = (output2 ?? string.Empty).Replace("\\n", Environment.NewLine).Replace("\n", "").Replace("**", "");
		bool racismo = output2.ToLower().Contains("racismo detectado");
		HiloPrincipal.Ejecutar(delegate
		{
			if (!(player == null) && player.IsConnected)
			{
				if (racismo)
				{
					player.SendConsoleMessage("Tu texto contiene algo no permitido.", "red");
				}
				else
				{
					Task.Run(() => Logs(player.Nickname + " puso el comando cassie '" + textog + "'.", Secretos.WebhookVipCassie));
					if (player.UserId != "76561198317426857@steam")
					{
						CassieVIP.Add(player.UserId);
					}
					Exiled.API.Features.Cassie.MessageTranslated(". . . . .", "<color=#EABE3F>[ANUNCIO DE VIP+]</color> " + textog, isHeld: false, isNoisy: false);
					player.SendConsoleMessage("C.A.S.S.I.E. enviado.", "green");
				}
			}
		});
	}

	public static bool EsPalabraRacista(string palabra)
	{
		foreach (string palabrasRacista in palabrasRacistas)
		{
			if (palabra.Contains(palabrasRacista))
			{
				return true;
			}
		}
		return false;
	}
}
