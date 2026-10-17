using System;
using System.Globalization;
using CommandSystem;
using Exiled.API.Features;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Aspiradora : ICommand
{
	public string Command { get; } = "aspiradora";


	public string[] Aliases { get; } = new string[0];


	public string Description { get; } = "Pone la aspiradora de Surface donde estás y te da la posición para el config.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null || !player.IsAlive)
		{
			response = "Usalo desde el juego, vivo (por ejemplo con noclip), parado donde la querés.";
			return false;
		}
		Vector3 pos = player.Position + Vector3.down * 0.9f;
		float y = Mathf.Round(player.CameraTransform.eulerAngles.y);
		try
		{
			AspiradoraSurface.Colocar(pos, Quaternion.Euler(0f, y, 0f));
		}
		catch (Exception ex)
		{
			response = "No se pudo poner (¿están ProjectMER y el schematic 'Aspiradora'?): " + ex.Message;
			return false;
		}
		string text = string.Format(CultureInfo.InvariantCulture, "{0:0.00}, {1:0.00}, {2:0.00}", pos.x, pos.y, pos.z);
		response = "Aspiradora puesta acá. Para que quede fija en todas las rondas, poné esto en el config de radio_light_plugin:\naspiradora_posicion: '" + text + "'\naspiradora_rotacion_y: " + y.ToString(CultureInfo.InvariantCulture);
		return true;
	}
}
