using System;
using CommandSystem;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Mute : ICommand
{
	public bool SanitizeResponse => false;

	public string Command { get; } = "mute";


	public string[] Aliases { get; } = null;


	public string Description { get; } = null;


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null)
		{
			response = "Este comando se usa desde el juego.";
			return false;
		}
		if (player.IsEffectActive<SoundtrackMute>())
		{
			player.DisableEffect(EffectType.SoundtrackMute);
			response = "Ahora escuchas el sonido ambiente.";
		}
		else
		{
			player.EnableEffect(EffectType.SoundtrackMute);
			response = "Ahora no escuchas el sonido ambiente.";
		}
		return true;
	}
}
