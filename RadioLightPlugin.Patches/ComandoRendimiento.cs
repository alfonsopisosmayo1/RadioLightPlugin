using System;
using CommandSystem;

namespace RadioLightPlugin.Patches;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class ComandoRendimiento : ICommand
{
	public string Command { get; } = "rlrendimiento";


	public string[] Aliases { get; } = new string[1] { "rlperf" };


	public string Description { get; } = "Qué partes de RadioLightPlugin usan más tiempo del servidor. 'rlrendimiento reset' para empezar a medir de cero.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (arguments.Count > 0 && arguments.At(0).Equals("reset", StringComparison.OrdinalIgnoreCase))
		{
			Perfilador.Reiniciar();
			response = "Medición reiniciada.";
			return true;
		}
		response = "\n" + Perfilador.Texto(15);
		return true;
	}
}
