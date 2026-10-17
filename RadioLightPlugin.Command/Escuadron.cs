using System;
using CommandSystem;
using RadioLightPlugin.Subclases;

namespace RadioLightPlugin.Command;

/// <summary>Comando de admin: escuadron nu7|beta7 [cantidad]. Spawnea el escuadrón ya con espectadores.</summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class Escuadron : ICommand
{
	public bool SanitizeResponse => false;

	public string Command { get; } = "escuadron";

	public string[] Aliases { get; } = new string[1] { "squad" };

	public string Description { get; } = "Spawnea un escuadrón especial con espectadores: escuadron nu7|beta7 [cantidad, por defecto 8].";

	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (arguments.Count == 0)
		{
			response = "Uso: escuadron nu7|beta7 [cantidad]   (ej: escuadron nu7 10)";
			return false;
		}
		string text = arguments.At(0).ToLowerInvariant().Replace("-", string.Empty);
		string tipo = ((text == "nu7" || text == "nu") ? "nu7" : ((text == "beta7" || text == "beta" || text == "b7") ? "beta7" : null));
		if (tipo == null)
		{
			response = "Escuadrón desconocido. Usa: nu7 o beta7.";
			return false;
		}
		int result = 8;
		if (arguments.Count > 1 && (!int.TryParse(arguments.At(1), out result) || result < 1))
		{
			response = "La cantidad tiene que ser un número mayor que 0.";
			return false;
		}
		result = Math.Min(result, 20);
		response = Escuadrones.ForzarSpawn(tipo, result);
		return true;
	}
}
