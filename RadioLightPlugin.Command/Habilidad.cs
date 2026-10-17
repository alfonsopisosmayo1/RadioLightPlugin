using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Subclases;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Habilidad : ICommand
{
	public string Command { get; } = "habilidad";


	public string[] Aliases { get; } = new string[2] { "hab", "ability" };


	public string Description { get; } = "Usa la habilidad de tu subclase.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null || !player.IsAlive)
		{
			response = "Tienes que estar vivo.";
			return false;
		}
		string text = Habilidades.Nombre(player);
		if (text == null)
		{
			response = "Tu subclase actual no tiene habilidad.";
			return false;
		}
		Habilidades.Usar(player);
		response = "Habilidad: " + text + ".";
		return true;
	}
}
