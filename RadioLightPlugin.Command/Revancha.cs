using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Revancha : ICommand
{
	public string Command { get; } = "revancha";


	public string[] Aliases { get; } = new string[1] { "rematch" };


	public string Description { get; } = "Pide o acepta una revancha contra tu último rival de .practica.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null)
		{
			response = "Este comando se usa desde el juego.";
			return false;
		}
		response = Coroutines.PedirRevancha(player);
		return true;
	}
}
