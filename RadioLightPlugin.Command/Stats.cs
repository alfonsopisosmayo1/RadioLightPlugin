using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Stats : ICommand
{
	public string Command { get; } = "stats";


	public string[] Aliases { get; } = new string[2] { "estadisticas", "est" };


	public string Description { get; } = "Tus estadísticas (o las de otro: .stats <nombre>).";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (!Estadisticas.Activo)
		{
			response = "Las estadísticas están desactivadas.";
			return false;
		}
		Player p = Player.Get(sender);
		FichaJugador fichaJugador = ((arguments.Count > 0) ? Estadisticas.Buscar(string.Join(" ", arguments)) : Estadisticas.Ficha(p));
		response = Estadisticas.TextoStats(fichaJugador);
		return fichaJugador != null;
	}
}
