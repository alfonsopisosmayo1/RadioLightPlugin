using System;
using CommandSystem;
using Exiled.API.Features;

namespace RadioLightPlugin.Misc;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ComandoScp0871 : ICommand
{
	public string Command { get; } = "rl087";


	public string[] Aliases { get; } = new string[1] { "scp0871" };


	public string Description { get; } = "Hace aparecer a SCP-087-1. 'rl087' = sala de Heavy/Oficinas con más gente; 'rl087 aqui' = tu sala.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (Scp0871.Activo)
		{
			response = "SCP-087-1 ya está afuera.";
			return false;
		}
		Room room = null;
		if (arguments.Count > 0 && arguments.At(0).Equals("aqui", StringComparison.OrdinalIgnoreCase))
		{
			room = Player.Get(sender)?.CurrentRoom;
			if (room == null)
			{
				response = "No se encontró tu sala.";
				return false;
			}
		}
		bool flag = Scp0871.Aparecer(room, forzado: true);
		response = (flag ? "SCP-087-1 apareció." : "No se pudo (¿ronda sin empezar, evento, nuke, o no hay humanos en Heavy/Oficinas?).");
		return flag;
	}
}
