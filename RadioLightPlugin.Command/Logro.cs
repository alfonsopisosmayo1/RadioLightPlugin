using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Logro : ICommand
{
	public string Command { get; } = "logro";


	public string[] Aliases { get; } = new string[2] { "titulo", "logros" };


	public string Description { get; } = "Ver y equipar tus títulos de logros (.logro, .logro <nombre>, .logro quitar).";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = Logros.Comando(Player.Get(sender), (arguments.Count > 0) ? string.Join(" ", arguments) : null);
		return true;
	}
}
