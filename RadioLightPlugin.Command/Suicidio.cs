using System;
using CommandSystem;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Suicidio : ICommand
{
	public bool SanitizeResponse => false;

	public string Command { get; } = "suicidio";


	public string[] Aliases { get; } = new string[3] { "adminme", "admin", "freeadmin" };


	public string Description { get; } = null;


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null || !player.IsAlive)
		{
			response = "Tienes que estar vivo.";
			return false;
		}
		Item currentItem = player.CurrentItem;
		if (currentItem == null || !currentItem.IsWeapon)
		{
			response = "Tienes que tener un arma en tu mano para suicidarte.";
			return true;
		}
		if (Var.Gulag1.Contains(player) || Var.Gulag2.Contains(player))
		{
			response = "No puedes usar este comando estando en el arena del Gulag.";
			return false;
		}
		player.PlaceBlood(player.Position, player.Position, player.Role.Type, 0);
		player.Kill("Suicidio");
		response = "Te suicidaste.";
		return true;
	}
}
