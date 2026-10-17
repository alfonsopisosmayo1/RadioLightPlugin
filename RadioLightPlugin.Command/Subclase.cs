using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using RadioLightPlugin.Subclases;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Subclase : ICommand
{
	private static readonly Dictionary<string, Dictionary<string, Action<Player>>> Lista = new Dictionary<string, Dictionary<string, Action<Player>>>
	{
		["Clase-D"] = new Dictionary<string, Action<Player>>
		{
			["candyman"] = ClaseD.CandymanSpawn,
			["impersonator"] = ClaseD.ImpersonatorSpawn,
			["kamikaze"] = ClaseD.KamikazeSpawn,
			["silencioso"] = ClaseD.SilenciosoSpawn,
			["small"] = ClaseD.SmallSpawn,
			["veterano"] = ClaseD.VeteranoSpawn,
			["ratero"] = Extra.RateroSpawn,
			["escapista"] = Extra.EscapistaSpawn
		},
		["Cientifico"] = new Dictionary<string, Action<Player>>
		{
			["ejecutivo"] = Cientifico.CientEjecSpawn,
			["073"] = Cientifico.Scp073Spawn,
			["181"] = Cientifico.Scp181Spawn,
			["invisible"] = Cientifico.FantasmaSpawn,
			["expertocient"] = Cientifico.ExpertoSpawn,
			["vidente"] = Cientifico.VidenteSpawn,
			["machetero"] = Cientifico.MacheteroSpawn,
			["bomba"] = Cientifico.BombaSpawn,
			["990"] = Cientifico.SCP990Spawn,
			["507"] = Cientifico.SCP507Spawn,
			["1915"] = Cientifico.SCP1915Spawn,
			["observador"] = Cientifico.ObservadorSpawn,
			["quimico"] = Extra.QuimicoSpawn,
			["hacker"] = Extra.HackerSpawn
		},
		["Guardia"] = new Dictionary<string, Action<Player>>
		{
			["cabecilla"] = Guardia.HeadguardSpawn,
			["traidor"] = Guardia.TraidorSpawn,
			["noob"] = Guardia.NoobSpawn,
			["militarguardia"] = Guardia.MilitarSpawn,
			["expertoguardia"] = Guardia.ExpertoSpawn,
			["expertoscp"] = Guardia.ExpertoEnScpSpawn,
			["carcelero"] = Guardia.CarceleroSpawn,
			["vigia"] = Guardia.VigiaSpawn,
			["antidisturbios"] = Extra.AntidisturbiosSpawn
		},
		["MTF Cadete"] = new Dictionary<string, Action<Player>>
		{
			["bulldozer"] = MTF.BulldozerSpawn,
			["hammer"] = MTF.HammerDown,
			["medic"] = MTF.MedicSpawn,
			["spy"] = MTF.SpySpawn,
			["taser"] = MTF.TaserSpawn,
			["machetazo"] = MTF.MachetazoSpawn
		},
		["MTF Sargento"] = new Dictionary<string, Action<Player>>
		{
			["grr"] = MTF.GrupoDeRespuestaSpawn,
			["artillero"] = MTF.ArtilleroSpawn,
			["rompebrechas"] = MTF.RompebrechasSpawn,
			["granadero"] = Extra.GranaderoSpawn
		},
		["MTF Especialista"] = new Dictionary<string, Action<Player>>
		{
			["ingeniero"] = MTF.IngenieroSpawn,
			["tirador"] = MTF.TiradorSpawn,
			["explorador"] = Extra.ExploradorSpawn
		},
		["MTF Capitan"] = new Dictionary<string, Action<Player>>
		{
			["coordinador"] = MTF.CoordinadorSpawn,
			["estratega"] = MTF.EstrategaSpawn,
			["comandante"] = Extra.ComandanteSpawn
		},
		["Insurgencia del Caos"] = new Dictionary<string, Action<Player>>
		{
			["heavy"] = IdC.HeavySpawn,
			["scout"] = IdC.ScoutSpawn,
			["demoman"] = IdC.DemomanSpawn,
			["spychaos"] = IdC.SpySpawn,
			["silent"] = IdC.SilentChaosSpawn,
			["saboteador"] = Extra.SaboteadorSpawn,
			["sanguinario"] = Extra.SanguinarioSpawn
		},
		["SCP-049"] = new Dictionary<string, Action<Player>>
		{
			["cirujano"] = ScpMayores.CirujanoSpawn,
			["pastor"] = ScpMayores.PastorSpawn,
			["resucitador"] = Extra.ResucitadorSpawn
		},
		["SCP-049-2 (Zombies)"] = new Dictionary<string, Action<Player>>
		{
			["infectado"] = SCP.InfectadoSpawn,
			["atleta"] = SCP.AtletaSpawn,
			["tanque"] = SCP.TanqueSpawn,
			["militar"] = SCP.MilitarSpawn,
			["escopetero"] = SCP.MilitarEscopeteroSpawn,
			["zombiesilencioso"] = SCP.SilenciosoSpawn,
			["explosivo"] = Extra.ExplosivoSpawn,
			["putrefacto"] = Extra.PutrefactoSpawn
		},
		["SCP-079"] = new Dictionary<string, Action<Player>>
		{
			["apagon"] = ScpMayores.ApagonSpawn,
			["sobrecarga"] = ScpMayores.SobrecargaSpawn,
			["interferencia"] = Extra.InterferenciaSpawn
		},
		["SCP-096"] = new Dictionary<string, Action<Player>>
		{
			["colerico"] = ScpMayores.ColericoSpawn,
			["escurridizo"] = ScpMayores.EscurridizoSpawn,
			["hipnotizador"] = Extra.HipnotizadorSpawn
		},
		["SCP-106"] = new Dictionary<string, Action<Player>>
		{
			["corrosivo"] = ScpMayores.CorrosivoSpawn,
			["hambriento"] = ScpMayores.HambrientoSpawn,
			["hundidor"] = Extra.HundidorSpawn
		},
		["SCP-173"] = new Dictionary<string, Action<Player>>
		{
			["acorazado"] = ScpMayores.AcorazadoSpawn,
			["gargola"] = ScpMayores.GargolaSpawn,
			["acechador"] = Extra.AcechadorSpawn,
			["mani"] = Extra.ManiExplosivoSpawn
		},
		["SCP-939"] = new Dictionary<string, Action<Player>>
		{
			["aullador"] = ScpMayores.AulladorSpawn,
			["rastreador"] = ScpMayores.RastreadorSpawn,
			["saltador"] = Extra.SaltadorSpawn
		},
		["SCP-3114"] = new Dictionary<string, Action<Player>>
		{
			["titiritero"] = ScpMayores.TitiriteroSpawn,
			["espectro"] = ScpMayores.EspectroSpawn,
			["carronero"] = Extra.CarroneroSpawn
		}
	};

	private static readonly HashSet<string> NoElegibles = new HashSet<string> { "grr" };

	public bool SanitizeResponse => false;

	public string Command { get; } = "subclase";


	public string[] Aliases { get; } = new string[0];


	public string Description { get; } = "Te permite ser una subclase.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (arguments.Count == 0)
		{
			response = GetLista();
			return false;
		}
		string text;
		Player player;
		if (arguments.Count >= 2)
		{
			text = arguments.At(1).ToLower();
			player = Player.Get(arguments.At(0));
			if (player == null)
			{
				response = "Jugador no encontrado.";
				return false;
			}
		}
		else
		{
			text = arguments.At(0).ToLower();
			player = Player.Get(sender);
			if (player == null)
			{
				response = "Jugador no encontrado.";
				return false;
			}
		}
		foreach (Dictionary<string, Action<Player>> value2 in Lista.Values)
		{
			if (!value2.TryGetValue(text, out var value))
			{
				continue;
			}
			if (!player.IsConnected)
			{
				response = "El jugador no está conectado.";
				return false;
			}
			if (!player.IsAlive)
			{
				response = "El jugador tiene que estar vivo para recibir una subclase.";
				return false;
			}
			try
			{
				if (Var.Disfrazados.Contains(player))
				{
					player.DisplayNickname = null;
				}
				Var.QuitarSubclases(player);
				value(player);
				Timing.CallDelayed(0.5f, delegate
				{
					Util.KitMinimo(player);
				});
				response = "Comando usado";
				return true;
			}
			catch (Exception ex)
			{
				Log.Error($"[Subclase] Falló '{text}' ({value.Method.DeclaringType?.Name}.{value.Method.Name}) en {player.Nickname}: {ex}");
				response = "Error al aplicar '" + text + "': " + ex.GetType().Name + " - " + ex.Message + ". Mira la consola del servidor.";
				return false;
			}
		}
		response = GetLista();
		return false;
	}

	private static bool CategoriaDeRol(string categoria, RoleTypeId rol)
	{
		return categoria switch
		{
			"Clase-D" => rol == RoleTypeId.ClassD, 
			"Cientifico" => rol == RoleTypeId.Scientist, 
			"Guardia" => rol == RoleTypeId.FacilityGuard, 
			"MTF Cadete" => rol == RoleTypeId.NtfPrivate, 
			"MTF Sargento" => rol == RoleTypeId.NtfSergeant, 
			"MTF Especialista" => rol == RoleTypeId.NtfSpecialist, 
			"MTF Capitan" => rol == RoleTypeId.NtfCaptain, 
			"Insurgencia del Caos" => (rol == RoleTypeId.ChaosConscript || (uint)(rol - 18) <= 2u) ? true : false, 
			"SCP-049" => rol == RoleTypeId.Scp049, 
			"SCP-049-2 (Zombies)" => rol == RoleTypeId.Scp0492, 
			"SCP-079" => rol == RoleTypeId.Scp079, 
			"SCP-096" => rol == RoleTypeId.Scp096, 
			"SCP-106" => rol == RoleTypeId.Scp106, 
			"SCP-173" => rol == RoleTypeId.Scp173, 
			"SCP-939" => rol == RoleTypeId.Scp939, 
			"SCP-3114" => rol == RoleTypeId.Scp3114, 
			_ => false, 
		};
	}

	public static List<string> ElegiblesPara(RoleTypeId rol)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, Dictionary<string, Action<Player>>> listum in Lista)
		{
			if (!CategoriaDeRol(listum.Key, rol))
			{
				continue;
			}
			foreach (string key in listum.Value.Keys)
			{
				if (!NoElegibles.Contains(key))
				{
					list.Add(key);
				}
			}
		}
		return list;
	}

	public static bool AplicarElegida(Player player, string clave, out string error)
	{
		error = null;
		clave = clave.ToLower();
		foreach (KeyValuePair<string, Dictionary<string, Action<Player>>> listum in Lista)
		{
			if (!CategoriaDeRol(listum.Key, player.Role.Type) || NoElegibles.Contains(clave) || !listum.Value.TryGetValue(clave, out var value))
			{
				continue;
			}
			if (Var.Disfrazados.Contains(player))
			{
				player.DisplayNickname = null;
			}
			Var.QuitarSubclases(player);
			value(player);
			Timing.CallDelayed(0.5f, delegate
			{
				Util.KitMinimo(player);
			});
			return true;
		}
		error = "Esa subclase no existe para tu rol.";
		return false;
	}

	private static string GetLista()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, Dictionary<string, Action<Player>>> listum in Lista)
		{
			stringBuilder.Append(listum.Key).Append(":\r\n");
			foreach (string key in listum.Value.Keys)
			{
				stringBuilder.Append("- ").Append(char.ToUpper(key[0])).Append(key.Substring(1))
					.Append("\r\n");
			}
			stringBuilder.Append("\r\n");
		}
		return stringBuilder.ToString();
	}
}
