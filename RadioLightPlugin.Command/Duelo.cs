using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Duelo : ICommand
{
	public string Command { get; } = "duelo";


	public string[] Aliases { get; } = new string[2] { "duel", "desafiar" };


	public string Description { get; } = "Desafía a alguien de la cola de .practica (.duelo nombre). Acepta con .aceptar.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = Coroutines.PedirDuelo(Player.Get(sender), (arguments.Count > 0) ? string.Join(" ", arguments) : null);
		return true;
	}
}
