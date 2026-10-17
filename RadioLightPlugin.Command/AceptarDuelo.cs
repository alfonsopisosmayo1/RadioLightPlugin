using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class AceptarDuelo : ICommand
{
	public string Command { get; } = "aceptar";


	public string[] Aliases { get; } = new string[1] { "accept" };


	public string Description { get; } = "Acepta el desafío de duelo que te hicieron.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = Coroutines.AceptarDuelo(Player.Get(sender));
		return true;
	}
}
