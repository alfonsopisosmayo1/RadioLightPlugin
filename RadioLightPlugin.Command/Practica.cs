using System;
using CommandSystem;
using Exiled.API.Features;
using PlayerRoles;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Practica : ICommand
{
	public string Command { get; } = "practica";


	public string[] Aliases { get; } = new string[2] { "1v1", "practice" };


	public string Description { get; } = "Entra o sale del Gulag de práctica (1v1 sin premio, ilimitado).";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null)
		{
			response = "Este comando se usa desde el juego.";
			return false;
		}
		if (Var.ColaPractica.Contains(player) || Var.PeleaPractica.Contains(player))
		{
			Var.ColaPractica.Remove(player);
			if (Var.PeleaPractica.Contains(player))
			{
				Coroutines.CancelarPractica(player);
			}
			if (player.Role.Type == RoleTypeId.Tutorial)
			{
				player.Role.Set(RoleTypeId.Spectator);
			}
			response = "Saliste del Gulag de práctica.";
			return true;
		}
		if (Var.EventoEnCurso || Round.IsEnded || !Round.IsStarted)
		{
			response = "La práctica no está disponible ahora.";
			return false;
		}
		if (player.Role.Type != RoleTypeId.Spectator)
		{
			response = (Var.GenteGulag.Contains(player) ? "Estás en la cola del Gulag de verdad. Salí con .gulag primero." : "Tienes que estar de espectador para practicar.");
			return false;
		}
		Coroutines.EntrarPractica(player);
		response = "Entraste a la cola de práctica.";
		return true;
	}
}
