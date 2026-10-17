using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class KillFeed
{
	private const int Maximo = 4;

	private const float Segundos = 5f;

	private static readonly List<(string Texto, float Cuando)> _entradas = new List<(string, float)>();

	public static void Reiniciar()
	{
		_entradas.Clear();
	}

	public static void AlMorir(DiedEventArgs ev)
	{
		try
		{
			Plugin singleton = Plugin.Singleton;
			if ((singleton != null && singleton.Config?.KillFeedEstilizado == false) || ev?.Player == null || !Round.IsStarted || Var.EventoEnCurso)
			{
				return;
			}
			RoleTypeId targetOldRole = ev.TargetOldRole;
			if ((targetOldRole == RoleTypeId.None || targetOldRole == RoleTypeId.Spectator || targetOldRole == RoleTypeId.Tutorial) ? true : false)
			{
				return;
			}
			Player attacker = ev.Attacker;
			string text = Icono(ev.DamageHandler?.Type ?? DamageType.Unknown);
			string text2 = Nombre(ev.Player, targetOldRole);
			string item = ((!(attacker == null) && !(attacker == ev.Player)) ? (Nombre(attacker, attacker.Role.Type) + " <color=#DDDDDD>" + text + "</color> " + text2) : ("<color=#AAAAAA>\ud83d\udc80</color> " + text2));
			float ahora = Time.time;
			_entradas.Add((item, ahora));
			_entradas.RemoveAll(((string Texto, float Cuando) e) => ahora - e.Cuando > 5f);
			while (_entradas.Count > 4)
			{
				_entradas.RemoveAt(0);
			}
			string texto = string.Join("\n", _entradas.Select(((string Texto, float Cuando) e) => e.Texto));
			foreach (Player item2 in Player.List)
			{
				if (item2 != null && !item2.IsNPC && item2.IsConnected)
				{
					HudRueI.MostrarKillFeed(item2, texto, 5f);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[KillFeed] " + ex.Message);
		}
	}

	private static string Nombre(Player p, RoleTypeId rol)
	{
		string text = "#FFFFFF";
		try
		{
			text = "#" + ColorUtility.ToHtmlStringRGB(rol.GetColor());
		}
		catch
		{
		}
		string text2 = p.DisplayNickname ?? p.Nickname;
		if (text2.Length > 18)
		{
			text2 = text2.Substring(0, 17) + "…";
		}
		return "<color=" + text + ">" + text2 + "</color>";
	}

	private static string Icono(DamageType t)
	{
		switch (t)
		{
		case DamageType.Explosion:
			return "\ud83d\udca3";
		case DamageType.Jailbird:
			return "\ud83d\udd28";
		case DamageType.Tesla:
			return "⚡";
		default:
			if (t.IsWeapon())
			{
				return "\ud83d\udd2b";
			}
			if (t.IsScp())
			{
				return "⚔";
			}
			return "\ud83d\udc80";
		}
	}
}
