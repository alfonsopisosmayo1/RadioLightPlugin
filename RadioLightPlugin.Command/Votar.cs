using System;
using CommandSystem;
using Exiled.API.Features;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class Votar : ICommand
{
	public bool SanitizeResponse => false;

	public string Command { get; } = "votar";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Votá en la votación activa. Uso: .votar <número>";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (!Votacion.VotacionActiva || (DateTime.UtcNow - Votacion._inicio).TotalSeconds > 40.0)
		{
			response = "No hay ninguna votación activa.";
			return false;
		}
		if (arguments.Count < 1)
		{
			response = "Uso: .votar <número>";
			return false;
		}
		if (!int.TryParse(arguments.At(0), out var result))
		{
			response = "Número inválido.";
			return false;
		}
		if (result < 1 || result > Votacion.Opciones.Count)
		{
			response = $"Opción inválida. Elige entre 1 y {Votacion.Opciones.Count}.";
			return false;
		}
		Player player = Player.Get(sender);
		if (player == null)
		{
			response = "No se pudo identificar al jugador.";
			return false;
		}
		string userId = player.UserId;
		if (Votacion.JugadoresQueVotaron.Contains(userId))
		{
			response = "Ya votaste.";
			return false;
		}
		Votacion.JugadoresQueVotaron.Add(userId);
		Votacion.Votos[result]++;
		string text = Votacion.Opciones[result - 1];
		response = "Votaste por: " + text;
		return true;
	}
}
