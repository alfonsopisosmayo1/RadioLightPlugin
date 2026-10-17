using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using RoundRestarting;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Scp079 : ICommand
{
	private static readonly HashSet<string> NukeStart = new HashSet<string>();

	private static readonly HashSet<string> NukeStop = new HashSet<string>();

	private static int _ronda = -1;

	private static readonly Dictionary<Player, DateTime> ChatCooldown = new Dictionary<Player, DateTime>();

	private readonly string help = ".079 [chat/nukestart/nukestop]";

	public bool SanitizeResponse => false;

	public string Command { get; } = "079";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Habilidades del SCP-079.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (_ronda != RoundRestart.UptimeRounds)
		{
			_ronda = RoundRestart.UptimeRounds;
			NukeStart.Clear();
			NukeStop.Clear();
		}
		if (player == null || player.Role != RoleTypeId.Scp079)
		{
			response = "No puedes usar este comando si no eres SCP-079.";
			return false;
		}
		if (arguments.Count == 0)
		{
			response = help;
			return false;
		}
		switch (arguments.At(0).ToLower())
		{
		case "chat":
		{
			if (ChatCooldown.TryGetValue(player, out var value))
			{
				double num = 6.0 - (DateTime.Now - value).TotalSeconds;
				if (num > 0.0)
				{
					response = $"Debes esperar {Math.Ceiling(num)} segundos antes de usar este comando nuevamente.";
					return false;
				}
			}
			string text = string.Join(" ", arguments.Skip(1));
			if (string.IsNullOrWhiteSpace(text))
			{
				response = "Tu mensaje no puede estar vacío.";
				return false;
			}
			if (text.Length > 80)
			{
				response = "Mensaje muy largo, intenta otro.";
				return false;
			}
			ChatCooldown[player] = DateTime.Now;
			foreach (Player item in Player.List)
			{
				if (item.Role.Team == Team.SCPs)
				{
					item.Broadcast(5, "<color=#e32433>Mensaje del SCP-079 (" + player.Nickname + "):</color>\n <b><color=blue>" + text + "</color></b>");
				}
			}
			response = "Mensaje enviado correctamente.";
			return true;
		}
		case "nukestart":
		{
			if (Warhead.IsDetonated)
			{
				response = "El Alpha Warhead está detonada.";
				return false;
			}
			if (Var.BetterOmegaWarheadActivated || Warhead.IsInProgress)
			{
				response = "Ya hay una detonación en curso.";
				return false;
			}
			if (player.Role.Is<Scp079Role>(out var param2) && param2.Level == 5)
			{
				if (NukeStart.Add(player.UserId))
				{
					param2.Energy = 0f;
					foreach (Player item2 in Player.List)
					{
						item2.EnableEffect(EffectType.Scanned);
					}
					Timing.CallDelayed(5f, delegate
					{
						foreach (Player item3 in Player.List)
						{
							item3.DisableEffect(EffectType.Scanned);
						}
					});
					Warhead.Start();
					Map.Broadcast(5, "<color=#e32433>ATENCIÓN</color>\n<color=#859CC7>El <color=#EC2121>SCP-079</color> activó el Alpha Warhead.</color>");
					response = "Activaste el Alpha Warhead.";
					return true;
				}
				response = "Ya activaste el Alpha Warhead.";
				return false;
			}
			response = "Debes ser nivel 5 para iniciar el Alpha Warhead.";
			return false;
		}
		case "nukestop":
		{
			if (Warhead.IsDetonated)
			{
				response = "El Alpha Warhead está detonada.";
				return false;
			}
			if (Var.BetterOmegaWarheadActivated)
			{
				response = "La Omega Warhead no se puede detener.";
				return false;
			}
			if (player.Role.Is<Scp079Role>(out var param) && param.Level == 5)
			{
				if (NukeStop.Add(player.UserId))
				{
					param.Energy = 0f;
					foreach (Player item4 in Player.List)
					{
						item4.EnableEffect(EffectType.Scanned);
					}
					Timing.CallDelayed(5f, delegate
					{
						foreach (Player item5 in Player.List)
						{
							item5.DisableEffect(EffectType.Scanned);
						}
					});
					Warhead.Stop();
					Map.Broadcast(5, "<color=#e32433>ATENCIÓN</color>\n<color=#859CC7>El <color=#EC2121>SCP-079</color> desactivó el Alpha Warhead.</color>");
					response = "Desactivaste el Alpha Warhead.";
					return true;
				}
				response = "Ya desactivaste el Alpha Warhead.";
				return false;
			}
			response = "Debes ser nivel 5 para parar el Alpha Warhead.";
			return false;
		}
		default:
			response = help;
			return true;
		}
	}
}
