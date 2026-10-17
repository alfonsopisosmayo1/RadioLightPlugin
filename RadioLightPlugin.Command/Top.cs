using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Top : ICommand
{
	public string Command { get; } = "top";


	public string[] Aliases { get; } = new string[1] { "ranking" };


	public string Description { get; } = "Ranking de la temporada (mes): .top · .top bajas · .top semana · .top total · .top uep.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (!Estadisticas.Activo)
		{
			response = "Las estadísticas están desactivadas.";
			return false;
		}
		response = Estadisticas.TextoTop((arguments.Count > 0) ? arguments.At(0) : null, Player.Get(sender));
		return true;
	}
}
