using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using MEC;
using PlayerRoles;

namespace RadioLightPlugin.Handlers;

public static class GeneratorHandler
{
	private static bool _scp079Handled;

	public static void OnScp079Recontained(RecontainedEventArgs ev)
	{
		SendScp079ToSpectator("SCP-079 ha sido recontenido.");
	}

	public static void OnPlayerDying(DyingEventArgs ev)
	{
		if (_scp079Handled || ev.Player == null || ev.Player.Role.Team != 0 || ev.Player.Role.Type == RoleTypeId.Scp079 || !QuedaScp079() || QuedanOtrosScps(ev.Player))
		{
			return;
		}
		Player muerto = ev.Player;
		Timing.CallDelayed(0.2f, delegate
		{
			if (QuedaScp079() && !QuedanOtrosScps(muerto))
			{
				SendScp079ToSpectator("Todos los SCP han muerto.");
			}
		});
	}

	public static void OnWarheadDetonated()
	{
		SendScp079ToSpectator("La Alpha Warhead ha detonado.");
	}

	private static bool QuedaScp079()
	{
		return Player.List.Any((Player p) => p.Role.Type == RoleTypeId.Scp079);
	}

	private static bool QuedanOtrosScps(Player excluido)
	{
		return Player.List.Any((Player p) => p != excluido && p.IsAlive && p.Role.Team == Team.SCPs && p.Role.Type != RoleTypeId.Scp079);
	}

	private static void SendScp079ToSpectator(string reason)
	{
		if (!_scp079Handled)
		{
			Player player = Player.List.FirstOrDefault((Player p) => p.Role.Type == RoleTypeId.Scp079);
			if (!(player == null))
			{
				_scp079Handled = true;
				Log.Info("SCP-079 enviado a espectador. Motivo: " + reason);
				player.Role.Set(RoleTypeId.Spectator);
				player.ShowHint("<color=#FF0000><b>SCP-079 HA SIDO TERMINADO</b></color>\n<color=#FFFFFF>" + reason + "</color>\n<color=#AAAAAA>Has sido enviado a espectador.</color>", 6f);
			}
		}
	}

	public static void Reset()
	{
		_scp079Handled = false;
	}
}
