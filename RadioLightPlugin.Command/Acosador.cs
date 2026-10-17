using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Acosador : ICommand
{
	private int lastPlayed = -1;

	public bool SanitizeResponse => false;

	public string Command { get; } = "acosador";


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
		Npc npc = Npc.Spawn("Terminator", RoleTypeId.Tutorial, player.Position);
		Timing.CallDelayed(1f, delegate
		{
			npc.Position = player.Position;
			Timing.RunCoroutine(FullNpcCoroutine(npc));
			npc.EnableEffect(EffectType.MovementBoost);
			npc.ChangeEffectIntensity(EffectType.MovementBoost, 100);
			npc.EnableEffect(EffectType.Ghostly);
		});
		response = "SPAWNEANDO TERMINATOR";
		return true;
	}

	private IEnumerator<float> FullNpcCoroutine(Npc npc)
	{
		Door nearestDoor = null;
		Player closestPlayer = null;
		int tick = 0;
		while (!(npc == null) && !(npc.GameObject == null))
		{
			try
			{
				if (tick % 4 == 0)
				{
					if (!npc.IsAlive)
					{
						npc.Destroy();
						npc.Disconnect();
						break;
					}
					List<Player> alivePlayers = Player.List.Where((Player p) => p.IsAlive && p.Role != RoleTypeId.Tutorial).ToList();
					if (alivePlayers.Count > 0)
					{
						Player closestPlayer2 = alivePlayers.GetRandomValue();
						closestPlayer = closestPlayer2;
						npc.Position = closestPlayer.Position;
						npc.Follow(closestPlayer, 20000f, 1.5f, 35f);
					}
					if (nearestDoor == null || Vector3.Distance(npc.Position, nearestDoor.Position) > 50f)
					{
						nearestDoor = Door.List.OrderBy((Door door) => Vector3.Distance(npc.Position, door.Position)).FirstOrDefault();
					}
					if (npc.CurrentRoom.Type == RoomType.Unknown || npc.Zone == ZoneType.Unspecified)
					{
						npc.Position = nearestDoor.Position + Vector3.up * 20f;
					}
				}
				int next;
				do
				{
					next = Var.random.Next(1, 21);
				}
				while (next == lastPlayed);
				lastPlayed = next;
				Sonido.Desde(npc.Id, $"T{next}.ogg");
				if (!(closestPlayer == null) && closestPlayer.IsConnected && closestPlayer.IsAlive)
				{
					Otros.GptInteract(string.Format(arg1: closestPlayer.CurrentItem?.Type.ToString() ?? "null", format: "Eres un NPC humano (te llamas Terminator) en la fundacion SCP:SL, cada 1 minuto te teletransportas a alguien aleatorio para molestarlo. Eres inmortal y solo los administradores pueden spawnearte. Hablale de forma intiminadora e inquietante, haciendo que se sienta 'aterrado' cuando le hables (de forma resumida, texto corto de menos de 150 caracteres), y capaz hablando sobre su nombre item o rol (SIN USAR TILDES POR FAVOR, ni ¿, ni Ñ, solo letras de ingles) (no hagas Roleplay eso de **se acerca** etc etc, debes simplemente contestar sin nada mas que un mensaje a la persona) a '{0}', item en la mano (EN CASO QUE SEA NULL, NO NOMBRAR EL ITEM, YA QUE NO TIENE. DEBES TRADUCIR EL ITEM, POR EJ SI ES GUNREVOLVER ES REVOLVER, ESTÁ EN INGLÉS.) '{1}', rol  (lo mismo que el item, traducelo por ej classd clase d, está en ingles) '{2}'", arg0: closestPlayer.Nickname, arg2: closestPlayer.Role.Type), closestPlayer);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
			tick++;
			yield return Timing.WaitForSeconds(15f);
		}
	}
}
