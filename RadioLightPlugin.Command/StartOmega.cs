using System;
using CommandSystem;
using RadioLightPlugin.Handlers;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class StartOmega : ICommand
{
	public bool SanitizeResponse => false;

	public string Command { get; } = "prenderomega";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Prende la Omega Warhead.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		EventHandlers.StartOmegaWarhead();
		response = "Omega Warhead prendida.";
		return true;
	}
}
