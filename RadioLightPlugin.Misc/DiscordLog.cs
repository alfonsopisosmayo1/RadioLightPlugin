using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;

namespace RadioLightPlugin.Misc;

public static class DiscordLog
{
	public const int Azul = 8756423;

	public const int Verde = 5025616;

	public const int Rojo = 14885939;

	public const int Dorado = 13938487;

	public const int Gris = 7829367;

	private static DateTime _ultimoLleno = DateTime.MinValue;

	private static bool _tlsListo;

	private static Config Cfg => Plugin.Singleton?.Config;

	public static bool Activo => !string.IsNullOrWhiteSpace(Cfg?.DiscordWebhook);

	public static void Enviar(string titulo, string texto, int color)
	{
		if (!Activo)
		{
			return;
		}
		string url = Cfg.DiscordWebhook.Trim();
		string s = (string.IsNullOrWhiteSpace(Cfg.DiscordNombre) ? "RadioLight" : Cfg.DiscordNombre);
		string json = "{\"username\":" + Json(s) + ",\"embeds\":[{\"title\":" + Json(titulo) + ",\"description\":" + Json(texto) + ",\"color\":" + color + ",\"footer\":{\"text\":" + Json($"{DateTime.Now:dd/MM/yyyy HH:mm}") + "}}]}";
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				if (!_tlsListo)
				{
					ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
					_tlsListo = true;
				}
				using WebClient webClient = new WebClient();
				webClient.Encoding = Encoding.UTF8;
				webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
				webClient.UploadString(url, "POST", json);
			}
			catch (Exception ex)
			{
				Log.Warn("[Discord] No se pudo enviar el log: " + ex.Message);
			}
		});
	}

	private static string Json(string s)
	{
		if (s == null)
		{
			return "null";
		}
		StringBuilder stringBuilder = new StringBuilder("\"");
		foreach (char c in s)
		{
			switch (c)
			{
			case '"':
				stringBuilder.Append("\\\"");
				continue;
			case '\\':
				stringBuilder.Append("\\\\");
				continue;
			case '\n':
				stringBuilder.Append("\\n");
				continue;
			case '\t':
				stringBuilder.Append("\\t");
				continue;
			case '\r':
				continue;
			}
			if (c < ' ')
			{
				StringBuilder stringBuilder2 = stringBuilder.Append("\\u");
				int num = c;
				stringBuilder2.Append(num.ToString("x4"));
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.Append('"').ToString();
	}

	public static string SinEtiquetas(string s)
	{
		return Regex.Replace(s ?? "", "<.*?>", "");
	}

	public static void AlEmpezarRonda()
	{
		if (Activo && Cfg.DiscordResumenRonda)
		{
			Enviar("▶\ufe0f Ronda iniciada", $"Jugadores: **{Player.List.Count((Player p) => !p.IsNPC)}**\nGulag: {ModosGulag.Descripcion}", 8756423);
		}
	}

	public static void AlTerminarRonda(RoundEndedEventArgs ev)
	{
		if (Activo && Cfg.DiscordResumenRonda)
		{
			string text = ev.LeadingTeam switch
			{
				LeadingTeam.FacilityForces => "\ud83d\udd35 Fundación (MTF y científicos)", 
				LeadingTeam.ChaosInsurgency => "\ud83d\udfe2 Insurgencia del Caos y Clase-D", 
				LeadingTeam.Anomalies => "\ud83d\udd34 SCPs", 
				_ => "⚪ Empate", 
			};
			TimeSpan elapsedTime = Round.ElapsedTime;
			(string Nick, int Bajas) tuple = Estadisticas.MejorDeLaRonda();
			string item = tuple.Nick;
			int item2 = tuple.Bajas;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Ganador: **" + text + "**");
			stringBuilder.AppendLine($"Duración: **{(int)elapsedTime.TotalMinutes} min {elapsedTime.Seconds:00} s**");
			stringBuilder.AppendLine($"Jugadores: **{Player.List.Count((Player p) => !p.IsNPC)}**");
			stringBuilder.AppendLine($"Escapes: **{Estadisticas.EscapesRonda}** · Gulags ganados: **{Estadisticas.GulagsRonda}**");
			if (item != null)
			{
				stringBuilder.AppendLine($"Más bajas: **{item}** ({item2})");
			}
			string texto = stringBuilder.ToString();
			Enviar("\ud83c\udfc1 Fin de ronda", texto, ev.LeadingTeam switch
			{
				LeadingTeam.FacilityForces => 8756423, 
				LeadingTeam.ChaosInsurgency => 5025616, 
				LeadingTeam.Anomalies => 14885939, 
				_ => 7829367, 
			});
		}
	}

	public static void AlVerificar(VerifiedEventArgs ev)
	{
		if (Activo && Cfg.DiscordServidorLleno)
		{
			int num = Player.List.Count((Player p) => !p.IsNPC);
			int maxPlayerCount = Server.MaxPlayerCount;
			if (maxPlayerCount > 0 && num >= maxPlayerCount && !((DateTime.UtcNow - _ultimoLleno).TotalMinutes < 30.0))
			{
				_ultimoLleno = DateTime.UtcNow;
				Enviar("\ud83d\udd25 ¡Servidor lleno!", $"**{num}/{maxPlayerCount}** jugadores conectados.", 13938487);
			}
		}
	}

	public static void Baneo(Player p, int bajas, int minutos)
	{
		if (Activo && Cfg.DiscordBaneos)
		{
			Enviar("\ud83d\udd28 Ban automático (anti-teamkill)", $"**{p?.Nickname}** (`{p?.UserId}`)\nMató a **{bajas}** compañeros en una ronda.\nBan: **{minutos} min**.", 14885939);
		}
	}

	public static void ScpAfk(string rol, string afk, string reemplazo)
	{
		if (Activo && Cfg.DiscordScpAfk)
		{
			Enviar("\ud83d\udca4 SCP AFK", (reemplazo == null) ? ("**" + afk + "** estaba AFK como **" + rol + "** y ningún espectador lo tomó.") : ("**" + afk + "** estaba AFK como **" + rol + "**. Lo tomó **" + reemplazo + "**."), 7829367);
		}
	}
}
