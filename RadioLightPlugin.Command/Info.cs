using System;
using CommandSystem;
using Exiled.API.Features;
using RoundRestarting;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Info : ICommand
{
	public bool SanitizeResponse => false;

	public string Command { get; } = "info";


	public string[] Aliases { get; } = null;


	public string Description { get; } = null;


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		double tps = Server.Tps;
		response = $"\n\nInfo del servidor:\n\n\n<color=blue>Numero de jugadores:</color> <color=green>{Server.PlayerCount}/{Server.MaxPlayerCount}</color>\n<color=blue>Tiempo de ronda:</color> <color=green>{Round.ElapsedTime}</color>\n<color=blue>Servidor reiniciado hace:</color> <color=green>{RoundRestart.UptimeRounds} rondas.</color>\n<color=blue>TPS:</color> <color=green>{tps}</color>";
		return true;
	}
}
