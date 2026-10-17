using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Dinamica
{
	private static readonly HashSet<Player> _ultimoAliento = new HashSet<Player>();

	private static Config Cfg => Plugin.Singleton?.Config;

	public static void AlEmpezarRonda()
	{
		_ultimoAliento.Clear();
	}

	public static void AlCambiarRol(ChangingRoleEventArgs ev)
	{
		if (!(ev?.Player == null))
		{
			_ultimoAliento.Remove(ev.Player);
		}
	}

	public static void AlMorir(DiedEventArgs ev)
	{
		try
		{
			Player player = ev?.Player;
			Player player2 = ev?.Attacker;
			if (!(player == null) && !(player2 == null) && !(player2 == player) && !Var.EventoEnCurso)
			{
				PantallaDeMuerte(player, player2, ev);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Dinamica] AlMorir: " + ex.Message);
		}
	}

	private static void PantallaDeMuerte(Player victima, Player atacante, DiedEventArgs ev)
	{
		Config cfg = Cfg;
		if (cfg != null && cfg.PantallaDeMuerte && !victima.IsNPC)
		{
			string text = ((atacante.CurrentItem != null && (ev.DamageHandler?.Type.ToString().Length ?? 0) > 0) ? atacante.CurrentItem.Type.ToString() : (ev.DamageHandler?.Type.ToString() ?? "?"));
			if (atacante.IsScp)
			{
				text = Util.NombreRol(atacante.Role.Type);
			}
			int num = Mathf.CeilToInt(atacante.Health);
			string text2 = Habilidades.Nombre(atacante);
			string text3 = ((text2 != null) ? (" · <color=#FFD400>" + text2 + "</color>") : string.Empty);
			victima.ShowHint("<size=30><color=#e32433>\ud83d\udc80 Te mató " + atacante.Nickname + "</color></size>\n" + $"<size=22><color=#C2D0E6>{Util.NombreRol(atacante.Role.Type)}{text3} · con {text} · le quedaron <b>{num}</b> HP</color></size>", 6f);
		}
	}

	public static void AlRecibirDanio(HurtEventArgs ev)
	{
		try
		{
			Player player = ev?.Player;
			Config cfg = Cfg;
			if (cfg != null && cfg.UltimoAliento && !(player == null) && player.IsAlive && player.IsHuman && player.Role.Type != RoleTypeId.Tutorial && !(player.Health > 20f) && _ultimoAliento.Add(player))
			{
				player.EnableEffect(EffectType.MovementBoost, 25, 4f);
				player.EnableEffect(EffectType.Invigorated, 4f);
				EffectsHUD.Notify(player, "<color=#e32433><b>¡Último aliento!</b></color> <color=#859CC7>Corré: tenés 4 s más rápido.</color>");
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Dinamica] Último aliento: " + ex.Message);
		}
	}
}
