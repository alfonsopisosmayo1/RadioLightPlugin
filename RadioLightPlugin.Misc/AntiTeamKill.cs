using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class AntiTeamKill
{
	private const int MinimoParaBan = 3;

	private const float RadioFuegoCruzado = 15f;

	private const float SegundosEnCombate = 10f;

	private static readonly Dictionary<string, HashSet<string>> _bajas = new Dictionary<string, HashSet<string>>();

	private static readonly Dictionary<string, float> _ultimoCombate = new Dictionary<string, float>();

	private static readonly Dictionary<Player, Vector3> _dondeMurio = new Dictionary<Player, Vector3>();

	public static bool GanadorElegido { get; private set; }

	public static void AlTerminarRonda()
	{
		GanadorElegido = true;
	}

	public static void Reiniciar()
	{
		GanadorElegido = false;
		_bajas.Clear();
		_ultimoCombate.Clear();
		_dondeMurio.Clear();
	}

	public static void AlRecibirDanio(HurtEventArgs ev)
	{
		try
		{
			Player player = ev?.Attacker;
			Player player2 = ev?.Player;
			if (player == null || player2 == null || player == player2)
			{
				return;
			}
			bool flag = player.Role.Side == player2.Role.Side;
			bool flag2 = flag;
			if (!flag2)
			{
				Side side = player.Role.Side;
				bool flag3 = (uint)(side - 3) <= 1u;
				flag2 = flag3;
			}
			bool flag4 = flag2;
			if (!flag4)
			{
				Side side = player2.Role.Side;
				bool flag3 = (uint)(side - 3) <= 1u;
				flag4 = flag3;
			}
			if (!flag4)
			{
				float time = Time.time;
				if (!string.IsNullOrEmpty(player.UserId))
				{
					_ultimoCombate[player.UserId] = time;
				}
				if (!string.IsNullOrEmpty(player2.UserId))
				{
					_ultimoCombate[player2.UserId] = time;
				}
			}
		}
		catch
		{
		}
	}

	public static void AntesDeMorir(DyingEventArgs ev)
	{
		if (ev?.Player != null)
		{
			_dondeMurio[ev.Player] = ev.Player.Position;
		}
	}

	private static bool EnCombate(Player p)
	{
		float value;
		return p != null && !string.IsNullOrEmpty(p.UserId) && _ultimoCombate.TryGetValue(p.UserId, out value) && Time.time - value <= 10f;
	}

	private static bool EnemigoCerca(Vector3 punto, Side lado, Player excluir1, Player excluir2)
	{
		float num = 225f;
		foreach (Player item in Player.List)
		{
			if (!(item == null) && !(item == excluir1) && !(item == excluir2) && item.IsAlive)
			{
				Side side = item.Role.Side;
				bool flag = (uint)(side - 3) <= 1u;
				if (!flag && side != lado && (item.Position - punto).sqrMagnitude <= num)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static string MotivoFuegoCruzado(Player atacante, Player victima, Side lado)
	{
		Vector3 value;
		Vector3 punto = (_dondeMurio.TryGetValue(victima, out value) ? value : atacante.Position);
		if (EnemigoCerca(punto, lado, atacante, victima))
		{
			return "había un SCP/enemigo cerca del que murió";
		}
		if (EnemigoCerca(atacante.Position, lado, atacante, victima))
		{
			return "había un SCP/enemigo cerca del que disparó";
		}
		if (EnCombate(atacante))
		{
			return "el que disparó estaba peleando contra enemigos";
		}
		if (EnCombate(victima))
		{
			return "el que murió estaba peleando contra enemigos";
		}
		return null;
	}

	public static void AlMorir(DiedEventArgs ev)
	{
		try
		{
			Config config = Plugin.Singleton?.Config;
			if (config == null || !config.AntiTeamkill || ev == null)
			{
				return;
			}
			Player attacker = ev.Attacker;
			Player player = ev.Player;
			if (attacker == null || player == null || attacker == player || attacker.IsNPC || player.IsNPC || !attacker.IsAlive || GanadorElegido || !Round.IsStarted || Round.IsEnded || Var.EventoEnCurso || attacker.Role.Type == RoleTypeId.Tutorial || ev.TargetOldRole == RoleTypeId.Tutorial || (config.AntiTeamkillIgnorarStaff && attacker.RemoteAdminAccess) || Var.Disfrazados.Contains(player) || Var.Impersonator.Contains(player))
			{
				return;
			}
			Side side = attacker.Role.Side;
			Side side2 = PlayerRolesUtils.GetTeam(ev.TargetOldRole).GetSide();
			bool flag = side != side2;
			bool flag2 = flag;
			if (!flag2)
			{
				bool flag3 = (uint)(side - 3) <= 1u;
				flag2 = flag3;
			}
			if (flag2)
			{
				return;
			}
			int num = Math.Max(3, config.AntiTeamkillBajas);
			string text = MotivoFuegoCruzado(attacker, player, side);
			_dondeMurio.Remove(player);
			if (text != null)
			{
				Log.Info("[AntiTK] " + attacker.Nickname + " mató a su compañero " + player.Nickname + ", pero fue fuego cruzado (" + text + "): no cuenta.");
				attacker.Broadcast(4, "<color=#FFD400>Le diste a un compañero en pleno combate.</color> <color=#AAAAAA>(fuego cruzado: no cuenta)</color>");
				return;
			}
			string userId = attacker.UserId;
			if (!_bajas.TryGetValue(userId, out var value))
			{
				value = (_bajas[userId] = new HashSet<string>());
			}
			if (value.Add(player.UserId ?? player.Nickname))
			{
				int count = value.Count;
				Log.Warn($"[AntiTK] {attacker.Nickname} ({userId}) mató a su compañero {player.Nickname}. Teamkills esta ronda: {count}/{num}.");
				AvisarStaff($"<color=#e32433>[AntiTK]</color> {attacker.Nickname} mató a su compañero {player.Nickname} ({count}/{num}).");
				if (count >= num)
				{
					string text2 = $"Teamkill masivo: {count} compañeros en una ronda (automático).";
					Log.Warn($"[AntiTK] Baneando a {attacker.Nickname} ({userId}) por {config.AntiTeamkillBanMinutos} min: {text2}");
					AvisarStaff($"<color=#e32433>[AntiTK]</color> {attacker.Nickname} fue baneado {config.AntiTeamkillBanMinutos} min por teamkill masivo.");
					_bajas.Remove(userId);
					DiscordLog.Baneo(attacker, count, Math.Max(1, config.AntiTeamkillBanMinutos));
					attacker.Ban(TimeSpan.FromMinutes(Math.Max(1, config.AntiTeamkillBanMinutos)), text2);
				}
				else if (count == num - 1)
				{
					attacker.Broadcast(8, "<size=30><color=#e32433><b>¡Cuidado!</b> Mataste a compañeros de tu equipo.</color>\n<color=#FFD400>Uno más y vas a ser baneado automáticamente.</color></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
				}
				else
				{
					attacker.Broadcast(5, "<color=#e32433>Mataste a un compañero de tu equipo.</color>");
				}
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[AntiTK] {arg}");
		}
	}

	private static void AvisarStaff(string texto)
	{
		foreach (Player item in Player.List)
		{
			if (item != null && item.RemoteAdminAccess && !item.IsNPC)
			{
				item.SendConsoleMessage(texto, "red");
			}
		}
	}
}
