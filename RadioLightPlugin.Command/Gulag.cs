using System;
using CommandSystem;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Gulag : ICommand
{
	public string Command { get; } = "gulag";


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
		if (Var.EventoEnCurso || Round.IsEnded || !Round.IsStarted)
		{
			response = "El Gulag no está disponible ahora.";
			return false;
		}
		if (player.Role == RoleTypeId.Spectator)
		{
			if (Var.Estuvo.Contains(player.UserId))
			{
				response = "Ya estuviste en el Gulag, no hay más oportunidades.";
				return true;
			}
			player.Role.Set(RoleTypeId.Tutorial);
			Timing.CallDelayed(0.1f, delegate
			{
				if (player.IsConnected && player.Role.Type == RoleTypeId.Tutorial && !Var.GenteGulag.Contains(player) && !Var.Gulag1.Contains(player) && !Var.Gulag2.Contains(player))
				{
					player.Position = ModosGulag.Espera;
					player.ClearInventory();
					Var.GenteGulag.Add(player);
				}
			});
			player.ClearBroadcasts();
			player.ShowHint("\n\n\n\n\n\n\n\n\n\n<color=#003ECA>Líder MTF:</color> <color=#859CC7>Bienvenido al Gulag. Si sobrevives, te ganas tu libertad.</color>");
			response = "Prepárate para la batalla...";
			return false;
		}
		if (player.Role == RoleTypeId.Tutorial)
		{
			player.Role.Set(RoleTypeId.Spectator);
			Var.GenteGulag.Remove(player);
			response = "Sacándote del Gulag...";
			return false;
		}
		response = "No puedes usar este comando estando vivo.";
		return true;
	}
}
