using System;
using CommandSystem;
using RadioLightPlugin.Handlers;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class StopOmega : ICommand
{
	public bool SanitizeResponse => false;

	public string Command { get; } = "apagaromega";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Apaga la Omega Warhead.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		EventHandlers.StopOmegaWarhead();
		response = "Omega Warhead apagada.";
		return true;
	}
}
