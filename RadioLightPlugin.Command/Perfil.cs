using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Perfil : ICommand
{
	public string Command { get; } = "perfil";


	public string[] Aliases { get; } = new string[1] { "profile" };


	public string Description { get; } = "Muestra tu perfil (o el de otro: .perfil <nombre>) con título, logros y puesto en la temporada.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (!Estadisticas.Activo)
		{
			response = "Las estadísticas están desactivadas.";
			return false;
		}
		Player player = Player.Get(sender);
		FichaJugador fichaJugador = ((arguments.Count > 0) ? Estadisticas.Buscar(string.Join(" ", arguments)) : Estadisticas.Ficha(player));
		if (fichaJugador == null)
		{
			response = "No hay estadísticas de ese jugador.";
			return false;
		}
		string text = Estadisticas.TarjetaPerfil(fichaJugador);
		player?.ShowHint("<align=center>" + text + "</align>", 10f);
		response = "\n" + DiscordLog.SinEtiquetas(text);
		return true;
	}
}
