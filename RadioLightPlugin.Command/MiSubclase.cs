using System;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class MiSubclase : ICommand
{
	public string Command { get; } = "subclase";


	public string[] Aliases { get; } = new string[2] { "sub", "misubclase" };


	public string Description { get; } = "Muestra la descripción de tu subclase y cómo usar su habilidad.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		response = Presentaciones.Texto(player);
		if (player != null && response.StartsWith("\n"))
		{
			player.Broadcast(8, "<size=24><color=#859CC7>" + response.Trim().Replace("\n", "\n").Replace("\ud83c\udfad ", "") + "</color></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
		}
		return true;
	}
}
