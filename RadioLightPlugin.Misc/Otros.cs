using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.EventArgs.Map;
using LabApi.Features.Wrappers;
using MEC;
using Newtonsoft.Json.Linq;
using PlayerRoles;
using RadioLightPlugin.Command;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Otros
{
	public static class Message
	{
		public static string Content { get; set; }

		public static object[] Embeds { get; set; }

		public static object[] Attachments { get; set; }
	}

	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(20.0)
	};

	private static string[] _framesDespliegue;

	private static readonly Dictionary<ushort, Side> _flashesVip = new Dictionary<ushort, Side>();


	private const string GeminiModel = "gemini-2.5-flash";

	public static readonly Dictionary<RoleTypeId, string> mensajes = new Dictionary<RoleTypeId, string>
	{
		{
			RoleTypeId.NtfCaptain,
			"<color=#003ECA>Capitán de los Nine-Tailed Fox</color>"
		},
		{
			RoleTypeId.NtfPrivate,
			"<color=#6FC3FF>Soldado Raso de los Nine-Tailed Fox</color>"
		},
		{
			RoleTypeId.NtfSergeant,
			"<color=#0096FF>Sargento de los Nine-Tailed Fox</color>"
		},
		{
			RoleTypeId.NtfSpecialist,
			"<color=#0096FF>Especialista de los Nine-Tailed Fox</color>"
		},
		{
			RoleTypeId.FacilityGuard,
			"<color=#5B6370>Guardia de las instalaciones</color>"
		},
		{
			RoleTypeId.Scientist,
			"<color=#FFFF7C>Científico</color>"
		},
		{
			RoleTypeId.ChaosConscript,
			"<color=#008F1E>Recluta de la Insurgencia del Caos</color>"
		},
		{
			RoleTypeId.ChaosMarauder,
			"<color=#0A7D34>Merodeador de la Insurgencia del Caos</color>"
		},
		{
			RoleTypeId.ChaosRepressor,
			"<color=#006728>Represor de la Insurgencia del Caos</color>"
		},
		{
			RoleTypeId.ChaosRifleman,
			"<color=#008F1E>Fusilero de la Insurgencia del Caos</color>"
		},
		{
			RoleTypeId.ClassD,
			"<color=#FF8E00>Clase-D</color>"
		},
		{
			RoleTypeId.Scp049,
			"<color=#EC2121>SCP-049</color>"
		},
		{
			RoleTypeId.Scp0492,
			"<color=#EC2121>SCP-049-2</color>"
		},
		{
			RoleTypeId.Scp079,
			"<color=#EC2121>SCP-079</color>"
		},
		{
			RoleTypeId.Scp096,
			"<color=#EC2121>SCP-096</color>"
		},
		{
			RoleTypeId.Scp106,
			"<color=#EC2121>SCP-106</color>"
		},
		{
			RoleTypeId.Scp173,
			"<color=#EC2121>SCP-173</color>"
		},
		{
			RoleTypeId.Scp939,
			"<color=#EC2121>SCP-939</color>"
		},
		{
			RoleTypeId.Scp3114,
			"<color=#EC2121>SCP-3114</color>"
		}
	};


	private const string model = "gpt-3.5-turbo";

	public static void AprilFools()
	{
	}

	public static void MtfRandom(AnnouncingNtfEntranceEventArgs ev)
	{
		ev.IsAllowed = false;
		Sonido.Mapa("Announc.ogg");
	}

	public static void Navidad(AnnouncingNtfEntranceEventArgs ev)
	{
		ev.IsAllowed = false;
		string arg = (string.IsNullOrEmpty(ev.UnitName) ? "P" : ev.UnitName.Substring(0, 1));
		Announcer.Clear();
		Announcer.Message($"Xmas_JingleBells Xmas_Epsilon11 NATO_{arg} {ev.UnitNumber} Xmas_HasEntered {ev.ScpsLeft} Xmas_ScpSubjects", $"Jingle bells \ud83c\udfb6<split><split><split><split><split><split><split><split>Unidad de vacaciones táctica EPSILON 11, designado {ev.UnitName} {ev.UnitNumber} ha entrado en el taller.<split>Se aconseja a todos los elfos restantes que busquen refugio en la casa de pan de jengibre más cercana, hasta que la unidad haya festejado la instalación.<split>A la espera de recontención de {ev.ScpsLeft} aguafiestas que odian las vacaciones.");
	}

	public static void PoliceAssault()
	{
		if (_framesDespliegue == null)
		{
			_framesDespliegue = GenerarFramesDespliegue();
		}
		string[] framesDespliegue = _framesDespliegue;
		foreach (string message in framesDespliegue)
		{
			Exiled.API.Features.Map.Broadcast(1, message);
		}
	}

	private static string[] GenerarFramesDespliegue()
	{
		List<string> list = new List<string> { "<color=#859CC7><align=right><b><size=30>[ DESPLIEGUE MTF EN CURSO ]△</size></b></align></color>" };
		for (int i = 1; i <= 20; i++)
		{
			string text = "DESPLIEGUE MTF EN CURSO /// DESPLIEGUE MTF EN CURSO /// ".Substring(i, 24);
			list.Add("<color=#859CC7><align=right><b><size=30>[ " + text + " ]△</size></b></align></color>");
		}
		string item = "<color=#859CC7><align=right><b><size=30>                              △</size></b></align></color>";
		list.Add(item);
		list.Add(" ");
		list.Add(item);
		list.Add(" ");
		list.Add(item);
		return list.ToArray();
	}

	public static void GrenadeExplode(Exiled.API.Features.Player attacker = null)
	{
		if (!(attacker == null) && attacker.IsAlive)
		{
			attacker.Health /= 2f;
			ExplosiveGrenade explosiveGrenade = (ExplosiveGrenade)Exiled.API.Features.Items.Item.Create(ItemType.GrenadeHE);
			explosiveGrenade.MaxRadius /= 4f;
			explosiveGrenade.DeafenDuration /= 4f;
			explosiveGrenade.ConcussDuration /= 4f;
			explosiveGrenade.ScpDamageMultiplier /= 4f;
			explosiveGrenade.BurnDuration /= 4f;
			explosiveGrenade.FuseTime = 0.01f;
			explosiveGrenade.SpawnActive(attacker.Position, attacker);
			explosiveGrenade.Base.transform.SetParent(attacker.ReferenceHub.transform);
		}
	}

	public static void FlashExplode(Exiled.API.Features.Player attacker)
	{
		if (!(attacker == null) && attacker.IsAlive)
		{
			FlashGrenade flashGrenade = (FlashGrenade)Exiled.API.Features.Items.Item.Create(ItemType.GrenadeFlash);
			flashGrenade.MinimalDurationEffect /= 10f;
			flashGrenade.SurfaceDistanceIntensifier /= 10f;
			flashGrenade.AdditionalBlindedEffect /= 10f;
			flashGrenade.FuseTime = 0.01f;
			Exiled.API.Features.Pickups.Projectiles.FlashbangProjectile flashbangProjectile = flashGrenade.SpawnActive(attacker.Position, attacker);
			// [cambio] como antes: el flash VIP también ciega a los compañeros (fuego amigo)
			flashGrenade.Base.transform.SetParent(attacker.ReferenceHub.transform);
		}
	}

	public static void AlExplotarGranada(ExplodingGrenadeEventArgs ev)
	{
		try
		{
			if (ev?.Projectile != null && _flashesVip.TryGetValue(ev.Projectile.Serial, out var bando))
			{
				_flashesVip.Remove(ev.Projectile.Serial);
				Exiled.API.Features.Player quien = ev.Player;
				ev.TargetsToAffect.RemoveWhere((Exiled.API.Features.Player t) => t != null && t != quien && t.Role.Side == bando);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[VIP flash] " + ex.Message);
		}
	}

	public static async void Gptxddd(string texto, Exiled.API.Features.Player player)
	{
		try
		{
			string output = Limpiar(await SendRequestAndGetResponse(texto).ConfigureAwait(continueOnCapturedContext: false));
			Log.Info(texto);
			Log.Info(output);
			if (!output.ToLowerInvariant().Contains("tk detectado"))
			{
				return;
			}
			string nick = player?.Nickname ?? "?";
			HiloPrincipal.Ejecutar(delegate
			{
				foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
				{
					if (item.RemoteAdminAccess)
					{
						item.Broadcast(6, nick + " hizo TK intencional segun GPT.");
					}
				}
			});
		}
		catch (Exception ex)
		{
			Exception e = ex;
			Log.Error(e);
		}
	}

	public static async Task generates(string input)
	{
		Log.Info(Limpiar(await SendRequestAndGetResponse(input).ConfigureAwait(continueOnCapturedContext: false)));
	}

	public static async Task<string> SendRequestAndGetResponse(string userInput)
	{
		JObject body = new JObject
		{
			["contents"] = new JArray
			{
				new JObject
				{
					["role"] = "user",
					["parts"] = new JArray
					{
						new JObject { ["text"] = userInput ?? string.Empty }
					}
				}
			},
			["generationConfig"] = new JObject
			{
				["temperature"] = 0.9,
				["topK"] = 50,
				["topP"] = 0.95,
				["maxOutputTokens"] = 1000
			}
		};
		if (string.IsNullOrEmpty(Secretos.GeminiApiKey))
		{
			return "Error: falta gemini_api_key en el config.";
		}
		string url = "https://generativelanguage.googleapis.com/v1beta/models/" + GeminiModel + ":generateContent?key=" + Secretos.GeminiApiKey;
		using StringContent content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await Http.PostAsync(url, content).ConfigureAwait(continueOnCapturedContext: false);
		if (!response.IsSuccessStatusCode)
		{
			return $"Error: {response.StatusCode} - {response.ReasonPhrase}";
		}
		string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			return JObject.Parse(responseBody).SelectToken("candidates[0].content.parts[0].text")?.ToString() ?? string.Empty;
		}
		catch (Exception e)
		{
			Log.Error("[Gemini] Respuesta inválida: " + e.Message);
			return string.Empty;
		}
	}

	private static string Limpiar(string s)
	{
		return (s ?? string.Empty).Replace("\\n", " ").Replace("\n", " ").Replace("**", "");
	}

	public static IEnumerator<float> PetHumano(Npc npc, Exiled.API.Features.Player dueño)
	{
		npc.Scale = new Vector3(1f, 0.75f, 1f);
		npc.EnableEffect(EffectType.SilentWalk);
		npc.EnableEffect(EffectType.MovementBoost);
		npc.EnableEffect(EffectType.Ghostly);
		npc.ChangeEffectIntensity(EffectType.SilentWalk, byte.MaxValue);
		npc.ChangeEffectIntensity(EffectType.MovementBoost, byte.MaxValue);
		npc.Follow(dueño, 1000f, 1.5f);
		while (npc?.GameObject != null)
		{
			if (dueño == null || !dueño.IsConnected || !Vip.Pet.Contains(dueño.UserId))
			{
				Timing.RunCoroutine(ImplosionarPet(npc));
				break;
			}
			if (!dueño.IsAlive)
			{
				Vip.Pet.Remove(dueño.UserId);
				npc.FollowedPlayer = null;
				Sonido.Desde(npc.Id, $"TP{Var.random.Next(1, 5)}.ogg");
				Timing.CallDelayed(6f, delegate
				{
					Timing.RunCoroutine(ImplosionarPet(npc));
				});
				break;
			}
			if ((npc.Position - dueño.Position).sqrMagnitude > 400f)
			{
				npc.Position = dueño.Position;
				npc.Follow(dueño, 1000f, 1.5f);
			}
			yield return Timing.WaitForSeconds(5f);
		}
	}

	private static IEnumerator<float> ImplosionarPet(Npc obj)
	{
		Vector3 paso = new Vector3(0.008f, 0.008f, 0.008f);
		while (obj?.GameObject != null && obj.Scale.x >= 0.1f && obj.Scale.y >= 0.1f && obj.Scale.z >= 0.1f)
		{
			obj.Scale -= paso;
			yield return Timing.WaitForSeconds(0.1f);
		}
		DesconectarNPC(obj);
	}

	public static void DesconectarNPC(Npc npc)
	{
		if (npc?.GameObject == null)
		{
			return;
		}
		try
		{
			npc.Destroy();
		}
		catch (Exception ex)
		{
			Log.Debug("DesconectarNPC: " + ex.Message);
		}
	}

	public static async Task GptInteract(string texto, Exiled.API.Features.Player player)
	{
		try
		{
			string content = Limpiar(ExtractContentFromJson(await SendRequest(texto).ConfigureAwait(continueOnCapturedContext: false)));
			Log.Info("Texto enviado: " + texto);
			Log.Info("Respuesta procesada: " + content);
			HiloPrincipal.Ejecutar(delegate
			{
				if (player != null && player.IsConnected)
				{
					player.ShowHint("\n\nTerminator:\n" + content, 10f);
				}
			});
		}
		catch (Exception ex)
		{
			Exception e = ex;
			Log.Error($"Error al interactuar con GPT: {e}");
		}
	}

	private static string ExtractContentFromJson(string json)
	{
		try
		{
			return JObject.Parse(json)["content"]?.ToString() ?? "Contenido no disponible";
		}
		catch (Exception ex)
		{
			Log.Error("Error al procesar el JSON: " + ex.Message);
			return "Error al procesar la respuesta";
		}
	}

	public static async Task Generates(string input)
	{
		Log.Info(Limpiar(await SendRequest(input).ConfigureAwait(continueOnCapturedContext: false)));
	}

	public static async Task<string> SendRequest(string userInput)
	{
		string prompt = WebUtility.UrlEncode(userInput ?? string.Empty);
		if (string.IsNullOrEmpty(Secretos.GptApiUrl))
		{
			return "Error: falta gpt_api_url en el config.";
		}
		string apiUrl = Secretos.GptApiUrl + (Secretos.GptApiUrl.Contains("?") ? "&" : "?") + "prompt=" + prompt;
		using HttpResponseMessage response = await Http.GetAsync(apiUrl).ConfigureAwait(continueOnCapturedContext: false);
		return (!response.IsSuccessStatusCode) ? $"Error: {response.StatusCode} - {response.ReasonPhrase}" : (await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false));
	}

	public static IEnumerator<float> Transition(Color from, Color to, float duration = 2f)
	{
		float start = Timing.LocalTime;
		while (true)
		{
			float t = (Timing.LocalTime - start) / duration;
			if (t >= 1f)
			{
				break;
			}
			Exiled.API.Features.Map.ChangeLightsColor(Color.Lerp(from, to, t));
			yield return Timing.WaitForSeconds(0.1f);
		}
		Exiled.API.Features.Map.ChangeLightsColor(to);
	}
}
