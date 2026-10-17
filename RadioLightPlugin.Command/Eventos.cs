using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Pickups;
using MEC;
using Newtonsoft.Json.Linq;
using PlayerRoles;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Eventos : ICommand
{
	private static readonly HttpClient HttpSteam = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15.0)
	};

	public static Dictionary<string, string> CountryColors = new Dictionary<string, string>
	{
		{ "República_de_RadioLight", "purple" },
		{ "Imperio_Arstotzkano", "red" },
		{ "República_Democrática_de_Hydras", "blue" },
		{ "República_Federal_de_ReyManí", "yellow" },
		{ "Tercer_Imperio_Alemán", "black" }
	};

	public bool SanitizeResponse => false;

	public string Command { get; } = "evento";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "evento [nukerun/tdm/railgunroyale]";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (arguments.Count == 0)
		{
			response = "evento [nukerun/tdm/railgunroyale/pp/foto]";
			return false;
		}
		switch (arguments.At(0).ToLower())
		{
		case "nukerun":
			Nukerun();
			response = "Iniciando Nukerun...";
			return false;
		case "tdm":
			Tdm();
			response = "Iniciando Team Deathmatch...";
			return false;
		case "railgunroyale":
			Railgun();
			response = "Iniciando Railgun Royale...";
			return false;
		case "foto":
		{
			Player player = Player.Get(sender);
			if (player == null)
			{
				response = "Este comando se usa desde el juego.";
				return false;
			}
			if (string.IsNullOrEmpty(Secretos.SteamApiKey))
			{
				response = "Falta steam_api_key en el config del plugin.";
				return false;
			}
			string url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=" + Secretos.SteamApiKey + "&steamids=" + player.RawUserId + "&format=json";
			Task.Run(async delegate
			{
				try
				{
					string avatarUrl = JObject.Parse(await HttpSteam.GetStringAsync(url).ConfigureAwait(continueOnCapturedContext: false))["response"]?["players"]?[0]?["avatarfull"]?.ToString();
					if (avatarUrl != null)
					{
						Claudio.ConvertirImagenAHTML(avatarUrl, player);
					}
					else
					{
						Log.Info("No se encontró la URL del avatar.");
					}
				}
				catch (Exception ex2)
				{
					Exception ex = ex2;
					Log.Info("Error obteniendo el avatar de Steam: " + ex.Message);
				}
			});
			response = "fotito";
			return true;
		}
		case "pp":
		{
			(string, string) randomCountryColor = GetRandomCountryColor();
			string realCountry = randomCountryColor.Item1;
			string item = randomCountryColor.Item2;
			List<Player> list = Player.List.ToList();
			int count = list.Count;
			if (count == 0)
			{
				response = "no hay jugadores disponibles.";
				return true;
			}
			int count2 = (int)Math.Ceiling((double)count * 0.35);
			HashSet<Player> hashSet = list.OrderBy((Player j) => Var.random.Next()).Take(count2).ToHashSet();
			foreach (Player jugador in list)
			{
				string text = realCountry;
				string text2 = item;
				string text3 = Regex.Replace(jugador.Nickname ?? string.Empty, "[^\\w]", "_");
				if (hashSet.Contains(jugador))
				{
					int num = Var.random.Next(1, 4);
					if (num == 1 || num == 3)
					{
						List<string> list2 = CountryColors.Keys.Where((string k) => k != realCountry).ToList();
						if (list2.Count > 0)
						{
							string text4 = list2[Var.random.Next(list2.Count)];
							text = text4;
							if (CountryColors.TryGetValue(text4, out var value))
							{
								text2 = value;
							}
						}
						else
						{
							text2 = item;
							text = realCountry;
						}
					}
					if (num == 2 || num == 3)
					{
						List<Player> list3 = list.Where((Player j) => j != jugador).ToList();
						if (list3.Count > 0)
						{
							string input = list3[Var.random.Next(list3.Count)].Nickname ?? string.Empty;
							text3 = Regex.Replace(input, "[^\\w]", "_");
						}
					}
					jugador.Broadcast(10, "Tienes un pasaporte <color=#e32433>falso</color>, debes actuar normal para evitar ser fusilado o sobornar al guardia fronterizo.");
				}
				else
				{
					jugador.Broadcast(10, "Tienes un pasaporte <color=green>verdadero</color>, por lo que no debes preocuparte.");
				}
				Server.ExecuteCommand($"/customkeycard {jugador.Id} keycardcustomsite02 Pasaporte 0 0 0 {text2} {text2} {text} {text2} {text3} 1");
			}
			response = "todos tienen pasaportes (algunos falsos \ud83d\udc40)";
			return true;
		}
		default:
			response = "evento [nukerun/tdm/railgunroyale/pp]";
			return false;
		}
	}

	public static (string country, string color) GetRandomCountryColor()
	{
		List<KeyValuePair<string, string>> list = CountryColors.ToList();
		KeyValuePair<string, string> keyValuePair = list[Var.random.Next(list.Count)];
		return (country: keyValuePair.Key, color: keyValuePair.Value);
	}

	public void Nukerun()
	{
		Round.IsLocked = true;
		Var.Nukerun = true;
		Var.EventoEnCurso = true;
		foreach (Player item in Player.List)
		{
			item.Role.Set(RoleTypeId.ClassD);
			item.ClearInventory();
			for (int i = 0; i < 4; i++)
			{
				item.AddItem(ItemType.Medkit);
				item.AddItem(ItemType.SCP207);
			}
		}
		foreach (Door item2 in Door.List)
		{
			item2.Lock(10f, DoorLockType.Warhead);
		}
		Map.Broadcast(5, "<b><color=#859CC7>Corran lo más rápido que puedan hasta la salida de la superficie, el primero que escape y se convierta en Insurgente del Caos gana.</color></b>");
		for (int num = 5; num > 0; num--)
		{
			Map.Broadcast(1, $"<color=#859CC7>Iniciando Alpha Warhead en {num} segundos.</color>");
		}
		Timing.CallDelayed(10f, delegate
		{
			Warhead.Start();
			Warhead.IsLocked = true;
		});
		foreach (Pickup item3 in Pickup.List.ToList())
		{
			item3.Destroy();
		}
	}

	public void Railgun()
	{
		Var.EventoEnCurso = true;
		foreach (Player item in Player.List)
		{
			item.Role.Set(RoleTypeId.Spectator);
			item.Role.Set(RoleTypeId.ClassD);
			item.ClearInventory();
		}
		for (int num = 25; num > 0; num--)
		{
			Map.Broadcast(1, $"<color=#e32433>Atención:</color>\n<color=#859CC7>En {num} segundos todos obtendrán armas.</color>");
		}
		Timing.CallDelayed(25f, delegate
		{
			Round.IsLocked = true;
			foreach (Player item2 in Player.List)
			{
				item2.ClearInventory();
				for (int i = 0; i < 7; i++)
				{
					item2.AddItem(ItemType.ParticleDisruptor);
				}
				item2.AddItem(ItemType.SCP500);
				Var.railgun = true;
			}
		});
	}

	public void Tdm()
	{
		Round.IsLocked = true;
		Var.EventoEnCurso = true;
		Player[] array = Player.List.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			RoleTypeId newRole = ((i % 2 == 0) ? RoleTypeId.ChaosMarauder : RoleTypeId.NtfCaptain);
			array[i].Role.Set(newRole);
		}
	}
}
