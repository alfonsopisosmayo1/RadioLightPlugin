using System;
using CommandSystem;
using Exiled.API.Features;
using MEC;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class AdP : ICommand
{
	private const float RadioDaño = 30f;

	public bool SanitizeResponse => false;

	public string Command { get; } = "adp";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Pide un ataque de precisión.";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Player player = Player.Get(sender);
		if (player == null)
		{
			response = "No se pudo identificar al jugador.";
			return false;
		}
		if (!Physics.Raycast(player.CameraTransform.position + player.CameraTransform.forward * 1.5f, player.CameraTransform.forward, out var hit, 500f))
		{
			response = "No hay punto de impacto válido.";
			return false;
		}
		foreach (Player item in Player.List)
		{
			if (Vector3.Distance(hit.point, item.Position) <= 30f)
			{
				item.ShowHint("\n<color=#e32433>//////////// ATAQUE DE PRECISIÓN AÉREO HOSTIL EN CAMINO ////////////</color>");
			}
		}
		Sonido.Mapa("A10.ogg");
		Timing.CallDelayed(7f, delegate
		{
			foreach (Player item2 in Player.List)
			{
				if (Vector3.Distance(hit.point, item2.Position) <= 30f)
				{
					item2.Hurt(500f, "Ataque de precisión aéreo.");
				}
			}
		});
		try
		{
			object schematic = Mer.Spawnear("A10", new Vector3(hit.point.x, hit.point.y + 120f, hit.point.z), player.GameObject.transform.localRotation, new Vector3(1f, 1f, 1f));
			if (schematic != null)
			{
				Timing.CallDelayed(13.5f, delegate
				{
					try
					{
						Mer.Destruir(schematic);
					}
					catch
					{
					}
				});
			}
		}
		catch (Exception ex)
		{
			Log.Warn("[AdP] No se pudo spawnear el schematic A10: " + ex.Message);
		}
		response = "Ataque de precisión ejecutado.";
		return true;
	}
}
