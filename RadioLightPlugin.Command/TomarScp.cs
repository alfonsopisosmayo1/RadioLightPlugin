using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class TomarScp : ICommand
{
	public string Command { get; } = "tomarscp";


	public string[] Aliases { get; } = new string[1] { "takescp" };


	public string Description { get; } = "Toma el SCP de un jugador AFK (cuando se ofrece).";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = AfkScp.Tomar(Player.Get(sender));
		return response.StartsWith("¡");
	}
}
