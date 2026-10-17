using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Player;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using RadioLightPlugin.Command;
using RadioLightPlugin.Handlers;
using RadioLightPlugin.Patches;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Coroutines
{
	public static bool running;

	public static CoroutineHandle AntiLag;

	private const float FantasmaDuracion = 10f;

	private const float FantasmaCooldown = 60f;

	private const float MoralDuracion = 10f;

	private const float MoralCooldown = 90f;

	private const float MoralRadioSqr = 400f;

	private const float ScpMiedoSqr = 100f;

	private const float AvisoCercaSqr = 400f;

	private const float ZombieCuraSqr = 25f;

	private const float TpsMinimos = 6f;

	private static readonly Vector3 Escala1 = Vector3.one;

	private const string SaldrTag = "<size=40>[<b><color=#859CC7>S</color>.<color=#7B90B8>A</color>.<color=#7185AA>L</color>.<color=#67799B>D</color>.<color=#5E6E8D>R</color>.<color=#54627E>L</color>.</b>]\n";

	private static readonly List<Exiled.API.Features.Player> _humanosCache = new List<Exiled.API.Features.Player>();

	private static readonly List<Exiled.API.Features.Player> _scpsCache = new List<Exiled.API.Features.Player>();

	private static readonly List<Exiled.API.Features.Pickups.Pickup> _pickupsCache = new List<Exiled.API.Features.Pickups.Pickup>();

	private static readonly Color[] ColoresNavidad = new Color[8]
	{
		new Color(0f, 1f, 0f),
		new Color(1f, 0f, 0f),
		new Color(1f, 1f, 0.8f),
		new Color(0f, 0.5f, 1f),
		new Color(1f, 1f, 1f),
		new Color(1f, 0f, 1f),
		new Color(1f, 0.84f, 0f),
		new Color(0f, 1f, 0.87f)
	};

	private static readonly Dictionary<Exiled.API.Features.Player, Exiled.API.Features.Player> _ultimoRival = new Dictionary<Exiled.API.Features.Player, Exiled.API.Features.Player>();

	private static readonly Dictionary<Exiled.API.Features.Player, (Exiled.API.Features.Player Rival, float Hasta)> _revancha = new Dictionary<Exiled.API.Features.Player, (Exiled.API.Features.Player, float)>();

	private static readonly Dictionary<Exiled.API.Features.Player, (Exiled.API.Features.Player Retador, float Hasta)> _duelos = new Dictionary<Exiled.API.Features.Player, (Exiled.API.Features.Player, float)>();

	private const float DuracionMaximaPractica = 90f;

	private static readonly Dictionary<Exiled.API.Features.Player, float> _sinAtacarHasta = new Dictionary<Exiled.API.Features.Player, float>();

	public static Vector3 EsperaPractica => ModosGulag.PEspera;

	private static bool GulagLibre => Var.Gulag1.Count == 0 && Var.Gulag2.Count == 0;

	private static bool PracticaLibre => Var.PeleaPractica.Count == 0;

	/// <summary>[fix] true si la posición está en el gulag / la arena de práctica (o en las arenas altas).</summary>
	public static bool EnZonaGulag(Vector3 pos)
	{
		if (pos.y > 2500f)
		{
			return true;
		}
		Vector3[] array = new Vector3[6]
		{
			ModosGulag.Espera,
			ModosGulag.SpawnA,
			ModosGulag.SpawnB,
			ModosGulag.PEspera,
			ModosGulag.PSpawnA,
			ModosGulag.PSpawnB
		};
		foreach (Vector3 vector in array)
		{
			if ((pos - vector).sqrMagnitude < 2500f)
			{
				return true;
			}
		}
		return false;
	}

	private static bool Valido(Exiled.API.Features.Player p)
	{
		return p != null && p.IsConnected && p.IsAlive;
	}

	private static void BroadcastStaff(ushort segundos, string msg)
	{
		foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
		{
			if (item.RemoteAdminAccess)
			{
				item.Broadcast(segundos, msg, Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			}
		}
	}

	public static IEnumerator<float> clLoop()
	{
		while (!Exiled.API.Features.Round.IsEnded)
		{
			DateTime now = DateTime.Now;
			if (now.Hour == 3 && now.Minute == 33)
			{
				Exiled.API.Features.Player humano = Exiled.API.Features.Player.List.FirstOrDefault((Exiled.API.Features.Player p) => p.IsHuman && !p.IsNPC);
				if (humano != null)
				{
					Npc npc = null;
					try
					{
						npc = Npc.Spawn("?", RoleTypeId.Tutorial, humano.Position);
						Sonido.Desde(humano.Id, "miedito.ogg");
						Timing.CallDelayed(1f, delegate
						{
							if (npc?.GameObject != null)
							{
								npc.Scale = new Vector3(1f, 1f, 0f);
							}
						});
						npc.EnableEffect(EffectType.Ghostly);
						npc.EnableEffect(EffectType.Fade);
						npc.ChangeEffectIntensity(EffectType.Fade, 50);
						npc.Follow(humano);
						humano.EnableEffect(EffectType.Traumatized, 30f);
					}
					catch (Exception ex)
					{
						Exception e = ex;
						Log.Error($"[clLoop] 3:33 {e}");
					}
					yield return Timing.WaitForSeconds(60f);
					try
					{
						npc?.Destroy();
					}
					catch
					{
					}
				}
			}
			if (Exiled.API.Features.Server.Tps < 6.0)
			{
				Log.Warn("Server has reached less TPS than allowed. Clear-lag init.");
				if (!running)
				{
					running = true;
					BroadcastStaff(5, "<size=40>[<b><color=#859CC7>S</color>.<color=#7B90B8>A</color>.<color=#7185AA>L</color>.<color=#67799B>D</color>.<color=#5E6E8D>R</color>.<color=#54627E>L</color>.</b>]\nSe detectaron TPS <color=#e32433>bajos</color>, iniciando sistema Anti-Lag...</size>");
				}
				yield return Timing.WaitForSeconds(5f);
				int borrados = 0;
				while (Exiled.API.Features.Server.Tps < 6.0 && !Exiled.API.Features.Round.IsEnded)
				{
					// [fix] antes borraba TODOS los cuerpos y 100 objetos cualquiera por segundo (vitrinas, armas especiales,
					// objetos del mapa...). Ahora solo basura que tiraron los jugadores, lejos de todos, y cuerpos viejos.
					int n = Limpieza.LimpiarBasura(40) + Limpieza.LimpiarCuerposViejos(15);
					borrados += n;
					if (n == 0)
					{
						yield return Timing.WaitForSeconds(5f);
					}
					yield return Timing.WaitForSeconds(1f);
				}
				Log.Info($"[Anti-Lag] Limpieza terminada: {borrados} objetos/cuerpos basura borrados.");
				running = false;
				BroadcastStaff(10, "<size=40>[<b><color=#859CC7>S</color>.<color=#7B90B8>A</color>.<color=#7185AA>L</color>.<color=#67799B>D</color>.<color=#5E6E8D>R</color>.<color=#54627E>L</color>.</b>]\nLos TPS vuelven a ser <color=green>altos</color>, deteniendo sistema Anti-Lag...</size>");
			}
			_humanosCache.Clear();
			_scpsCache.Clear();
			foreach (Exiled.API.Features.Player p2 in Exiled.API.Features.Player.List)
			{
				if (p2.IsHuman)
				{
					_humanosCache.Add(p2);
				}
				else if (p2.Role.Team == Team.SCPs && p2.Role.Type != RoleTypeId.Scp079)
				{
					_scpsCache.Add(p2);
				}
			}
			foreach (Exiled.API.Features.Player player in _humanosCache)
			{
				bool scpCercano = false;
				Vector3 pos = player.Position;
				foreach (Exiled.API.Features.Player scp in _scpsCache)
				{
					if ((scp.Position - pos).sqrMagnitude <= 100f)
					{
						scpCercano = true;
						break;
					}
				}
				EmotionPresetType actual = player.Emotion;
				if (scpCercano && actual != EmotionPresetType.Scared)
				{
					player.Emotion = EmotionPresetType.Scared;
				}
				else if (!scpCercano && actual == EmotionPresetType.Scared)
				{
					player.Emotion = EmotionPresetType.Neutral;
				}
			}
			yield return Timing.WaitForSeconds(10f);
		}
	}

	public static IEnumerator<float> OmegaWarheadTimer()
	{
		while (Var.BetterOmegaWarheadActivated)
		{
			int segundos = Mathf.CeilToInt(Var.OmegaEndTime - Time.time);
			if (segundos <= 0)
			{
				break;
			}
			string hint = $"\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n⚠\ufe0f<color=#1A237E>Detonación en {segundos} segundos</color>⚠\ufe0f\n<color=#B71C1C>DETONACIÓN INCANCELABLE POR MÚLTIPLES BRECHAS</color>";
			foreach (Exiled.API.Features.Player p in Exiled.API.Features.Player.List)
			{
				if (p.IsAlive && p.Role.Type != RoleTypeId.Tutorial)
				{
					p.ShowHint(hint, 1.2f);
				}
			}
			yield return Timing.WaitForSeconds(0.8f);
		}
	}

	public static IEnumerator<float> Navidad()
	{
		int i = 0;
		while (Exiled.API.Features.Round.IsStarted && !Exiled.API.Features.Round.IsEnded)
		{
			Exiled.API.Features.Map.ChangeLightsColor(ColoresNavidad[i]);
			i = (i + 1) % ColoresNavidad.Length;
			yield return Timing.WaitForSeconds(2f);
		}
	}

	public static IEnumerator<float> ShowDisplay(Exiled.API.Features.Player player)
	{
		while (Valido(player) && player.Role.Type == RoleTypeId.NtfCaptain)
		{
			int capitanes = 0;
			int sargentos = 0;
			int cadetes = 0;
			int guardias = 0;
			foreach (Exiled.API.Features.Player p in Exiled.API.Features.Player.List)
			{
				if (p.IsAlive)
				{
					switch (p.Role.Type)
					{
					case RoleTypeId.NtfCaptain:
						capitanes++;
						break;
					case RoleTypeId.NtfSergeant:
						sargentos++;
						break;
					case RoleTypeId.NtfPrivate:
						cadetes++;
						break;
					case RoleTypeId.FacilityGuard:
						guardias++;
						break;
					}
				}
			}
			EffectsHUD.Panel(player, "mtf", "<b><color=#003ECA>Información MTF</color></b>\n" + $"<color=#003ECA>Capitanes: {capitanes}</color>  " + $"<color=#0096FF>Sargentos: {sargentos}</color>\n" + $"<color=#6FC3FF>Cadetes: {cadetes}</color>  " + $"<color=#5B6370>Guardias: {guardias}</color>", 3f);
			yield return Timing.WaitForSeconds(2f);
		}
		EffectsHUD.QuitarPanel(player, "mtf");
	}

	public static IEnumerator<float> HealZombie(Exiled.API.Features.Player player)
	{
		Plugin singleton = Plugin.Singleton;
		string mensaje = ((singleton != null && singleton.Config?.Brazuca == true) ? "<color=green>Você está sendo curado por estar perto de seu pai.</color>" : "<color=green>Estás siendo curado por estar cerca de tu padre.</color>");
		while (Valido(player) && player.Role.Type == RoleTypeId.Scp049)
		{
			Vector3 pos = player.Position;
			foreach (Exiled.API.Features.Player p in Exiled.API.Features.Player.List)
			{
				if (p.Role.Type == RoleTypeId.Scp0492 && !((p.Position - pos).sqrMagnitude > 25f))
				{
					p.Heal(5f);
					EffectsHUD.Panel(p, "cura049", mensaje, 5.5f);
				}
			}
			yield return Timing.WaitForSeconds(5f);
		}
	}

	public static IEnumerator<float> NukeTimer()
	{
		while (!Exiled.API.Features.Round.IsEnded && Exiled.API.Features.Warhead.IsInProgress && !Exiled.API.Features.Warhead.IsDetonated)
		{
			int t = Mathf.FloorToInt(Exiled.API.Features.Warhead.DetonationTimer);
			string message = ((t <= 10) ? $"\n\n\n\n\n\n\n\n\n\n<color=#C62828>Detonación inminente en {t} segundos</color>\n<color=#B0B0B0>EVACUACIÓN INMEDIATA DE LA INSTALACIÓN</color>" : $"\n\n\n\n\n\n\n\n\n\n<color=#C62828>Detonación en {t} segundos</color>\n<color=#B0B0B0>EVACUACIÓN INMEDIATA DE LA INSTALACIÓN</color>");
			foreach (Exiled.API.Features.Player p in Exiled.API.Features.Player.List)
			{
				if (p.IsAlive && p.Role.Type != RoleTypeId.Tutorial)
				{
					p.ShowHint(message, 1f);
				}
			}
			yield return Timing.WaitForSeconds(0.8f);
		}
	}

	public static IEnumerator<float> HeadGuardBC(Exiled.API.Features.Player headGuard)
	{
		while (Valido(headGuard) && headGuard.Role.Type == RoleTypeId.FacilityGuard && Var.HeadGuard.Contains(headGuard))
		{
			string msg = $"<color=#e32433>Atención:</color>\n<color=#859CC7>El cabecilla de los Guardias '{headGuard.Nickname}' se encuentra en</color> <color=#1F618D>{headGuard.Zone}</color>";
			foreach (Exiled.API.Features.Player p in Exiled.API.Features.Player.List)
			{
				if (p.Role.Team == Team.FoundationForces)
				{
					p.Broadcast(8, msg);
				}
			}
			yield return Timing.WaitForSeconds(120f);
		}
	}

	public static IEnumerator<float> FantasmaInvisible(Exiled.API.Features.Player player)
	{
		Var.CooldownFantasma.Add(player);
		RadioLightPlugin.Subclases.Util.MarcarCooldown(player, "Invisibilidad", 70f);
		player.EnableEffect(EffectType.Invisible, 10f);
		EffectsHUD.Notify(player, "<color=#57e5f7><b>¡Te has vuelto invisible!</b></color>");
		for (float restante = 10f; restante > 0f; restante -= 1f)
		{
			if (!Valido(player) || !Var.Fantasma.Contains(player))
			{
				if (player != null)
				{
					player.DisableEffect(EffectType.Invisible);
					Var.CooldownFantasma.Remove(player);
				}
				yield break;
			}
			yield return Timing.WaitForSeconds(1f);
		}
		player.DisableEffect(EffectType.Invisible);
		EffectsHUD.Notify(player, "<color=#e32433>Volviste a ser visible.</color>");
		yield return Timing.WaitForSeconds(60f);
		if (!(player == null))
		{
			Var.CooldownFantasma.Remove(player);
			if (Valido(player) && Var.Fantasma.Contains(player))
			{
				EffectsHUD.Notify(player, "<color=#57e5f7>Tu invisibilidad está disponible otra vez.</color>");
			}
		}
	}

	public static IEnumerator<float> ImpulsoMoral(Exiled.API.Features.Player headGuard)
	{
		Var.CooldownHeadguard.Add(headGuard);
		RadioLightPlugin.Subclases.Util.MarcarCooldown(headGuard, "Impulso de moral", 100f);
		HashSet<Exiled.API.Features.Player> conEscudo = new HashSet<Exiled.API.Features.Player>();
		EffectsHUD.Notify(headGuard, "<color=#859CC7><b>¡Impulso de moral!</b> Vos y los guardias/MTF cerca reciben escudo, vida y velocidad.</color>");
		for (float restante = 10f; restante > 0f; restante -= 1f)
		{
			if (!Valido(headGuard) || !Var.HeadGuard.Contains(headGuard))
			{
				if (headGuard != null)
				{
					Var.CooldownHeadguard.Remove(headGuard);
				}
				yield break;
			}
			Vector3 pos = headGuard.Position;
			foreach (Exiled.API.Features.Player p in Exiled.API.Features.Player.List)
			{
				if (!p.IsAlive || p.Role.Team != Team.FoundationForces || (p.Position - pos).sqrMagnitude > 400f)
				{
					continue;
				}
				p.Heal(5f);
				p.EnableEffect(EffectType.MovementBoost, 10, 2f);
				if (conEscudo.Add(p))
				{
					try
					{
						p.AddAhp(30f, 30f, 1.2f, 0.7f, 10f);
					}
					catch
					{
					}
					if (p != headGuard)
					{
						EffectsHUD.Notify(p, "<color=#859CC7><b>¡Impulso de moral!</b> +30 de escudo.</color>");
					}
				}
			}
			yield return Timing.WaitForSeconds(1f);
		}
		yield return Timing.WaitForSeconds(90f);
		if (!(headGuard == null))
		{
			Var.CooldownHeadguard.Remove(headGuard);
			if (Valido(headGuard) && Var.HeadGuard.Contains(headGuard))
			{
				EffectsHUD.Notify(headGuard, "<color=#859CC7>Tu impulso de moral está disponible otra vez.</color>");
			}
		}
	}

	public static IEnumerator<float> SpyBc(Exiled.API.Features.Player spy, Exiled.API.Features.Player disfrazadoDe)
	{
		bool esChaos = spy.Role.Team == Team.ChaosInsurgency;
		string color = (esChaos ? "blue" : "green");
		string bando = (esChaos ? "Mobile Task Force" : "Insurgente del Caos");
		string message = "<b>Estás disfrazado de " + (disfrazadoDe?.Nickname ?? "alguien") + ".\nComo <color=" + color + ">" + bando + "</color>.</b>";
		while (spy != null && spy.IsConnected && (spy.Role.Side == Side.Mtf || spy.Role.Side == Side.ChaosInsurgency) && Var.Disfrazados.Contains(spy) && Var.Spy.Contains(spy))
		{
			EffectsHUD.Panel(spy, "disfraz", message, 5.5f);
			yield return Timing.WaitForSeconds(5f);
		}
		EffectsHUD.QuitarPanel(spy, "disfraz");
		if (spy != null && spy.IsConnected)
		{
			spy.DisplayNickname = null;
		}
	}

	public static IEnumerator<float> Tasing(Exiled.API.Features.Player victima, Exiled.API.Features.Player taser)
	{
		string messageTaser = "<b><color=#859CC7>Estás electrocutando a</color> <color=#e32433>" + victima.Nickname + "</color></b>";
		string stopMessage = "<color=#859CC7>Dejaste de electrocutar a</color> <color=#e32433>" + victima.Nickname + "</color>";
		for (float t = 0f; t < 6f; t += 0.5f)
		{
			if (!Valido(victima))
			{
				break;
			}
			if (taser != null && taser.IsConnected)
			{
				taser.ShowHint(messageTaser, 1f);
			}
			victima.Rotation = Quaternion.Euler(Var.random.Next(-60, 60), Var.random.Next(0, 360), 0f);
			victima.EnableEffect(EffectType.Exhausted, 1f);
			EffectsHUD.Hint(victima, "<color=#e32433><b>¡Estás siendo electrocutado!</b></color>", 1f);
			yield return Timing.WaitForSeconds(0.5f);
		}
		if (taser != null && taser.IsConnected)
		{
			taser.ShowHint(stopMessage);
		}
	}

	public static IEnumerator<float> GuardCheck(Exiled.API.Features.Player impersonator)
	{
		while (Valido(impersonator) && Var.Impersonator.Contains(impersonator) && impersonator.Role.Type == RoleTypeId.ClassD)
		{
			if (HayCerca(impersonator.Position, Team.FoundationForces))
			{
				impersonator.Broadcast(2, "<color=#e32433>Atención:</color>\n<color=orange>Hay un <color=#0096FF>MTF</color> o <color=#5B6370>Guardia</color> muy cerca.</color>");
			}
			yield return Timing.WaitForSeconds(2f);
		}
	}

	public static IEnumerator<float> ScpCheck(Exiled.API.Features.Player scp181)
	{
		while (Valido(scp181) && Var.Scp181.Contains(scp181) && scp181.Role.Type == RoleTypeId.Scientist)
		{
			if (HayCerca(scp181.Position, Team.SCPs))
			{
				scp181.Broadcast(2, "<color=#e32433>Atención:</color>\n<color=orange>Hay un <color=#e21212>SCP</color> muy cerca.</color>");
			}
			yield return Timing.WaitForSeconds(2f);
		}
	}

	private static bool HayCerca(Vector3 pos, Team team)
	{
		foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
		{
			if (!item.IsAlive || item.Role.Team != team || item.Role.Type == RoleTypeId.Scp079 || !((item.Position - pos).sqrMagnitude < 400f))
			{
				continue;
			}
			return true;
		}
		return false;
	}

	public static IEnumerator<float> RotateCoroutine(Exiled.API.Features.Player player)
	{
		while (Valido(player))
		{
			player.Rotation *= Quaternion.AngleAxis(3f, Vector3.up);
			player.Position += new Vector3(0f, 1.8f, 0f);
			yield return Timing.WaitForSeconds(0.1f);
		}
	}

	public static IEnumerator<float> MoveHat(Exiled.API.Features.Player player, Exiled.API.Features.Pickups.Pickup pickup)
	{
		Quaternion rot = Quaternion.Euler(-90f, 0f, 90f);
		while (Valido(player) && player.IsHuman && pickup?.Base != null)
		{
			pickup.Position = player.Position + Vector3.up;
			pickup.Base.transform.localRotation = rot;
			yield return Timing.WaitForSeconds(0.2f);
		}
		try
		{
			if (pickup?.Base != null)
			{
				pickup.Destroy();
			}
		}
		catch
		{
		}
		if (player != null)
		{
			Vip.PersonasConHat.Remove(player.UserId);
		}
	}

	public static IEnumerator<float> MoveText(Exiled.API.Features.Player player, AdminToys.TextToy textToy)
	{
		Vector3 offset = new Vector3(0f, 1.5f, 0f);
		while (Valido(player) && textToy != null)
		{
			textToy.Position = player.Position + offset;
			textToy.gameObject.transform.localRotation = player.GameObject.transform.localRotation;
			yield return Timing.WaitForSeconds(0.2f);
		}
	}

	public static IEnumerator<float> PetHumano(Exiled.API.Features.Player player, Npc npc)
	{
		while (npc?.GameObject != null)
		{
			if (player == null || !player.IsConnected || !Vip.Pet.Contains(player.UserId))
			{
				npc.Destroy();
				break;
			}
			if (!player.IsAlive)
			{
				Sonido.Desde(npc.Id, $"PH{Var.random.Next(1, 11)}.ogg");
				Vip.Pet.Remove(player.UserId);
				break;
			}
			Transform t = player.GameObject.transform;
			npc.Position = t.position - t.forward * 1.5f;
			npc.GameObject.transform.localRotation = t.localRotation;
			yield return Timing.WaitForSeconds(5f);
		}
	}

	public static IEnumerator<float> TextFollow(Exiled.API.Features.Player player, LabApi.Features.Wrappers.TextToy toy)
	{
		Vector3 offset = new Vector3(0f, 1.8f, 0f);
		Quaternion giro = Quaternion.Euler(0f, 180f, 0f);
		while (Valido(player) && toy != null)
		{
			toy.Transform.position = player.Position + offset;
			toy.Transform.rotation = player.Rotation * giro;
			yield return Timing.WaitForSeconds(0.2f);
		}
	}

	private static void DevolverGanadorPorAbandono(List<Exiled.API.Features.Player> lado)
	{
		foreach (Exiled.API.Features.Player item in lado.ToList())
		{
			if (item != null && item.IsConnected && item.Role.Type == RoleTypeId.Tutorial)
			{
				item.ClearInventory();
				PlayerHandler.DevolverAlCombate(item, item.Health, premio: false, 0);
			}
		}
		lado.Clear();
		Var.Gulag1.Clear();
		Var.Gulag2.Clear();
		Var.Gulag2v2 = false;
	}

	public static IEnumerator<float> GulagCheck()
	{
		while (!Exiled.API.Features.Round.IsEnded)
		{
			Var.Gulag1.RemoveAll((Exiled.API.Features.Player p) => p == null || !p.IsConnected || p.Role.Type != RoleTypeId.Tutorial);
			Var.Gulag2.RemoveAll((Exiled.API.Features.Player p) => p == null || !p.IsConnected || p.Role.Type != RoleTypeId.Tutorial);
			if (Var.Gulag1.Count >= 1 && Var.Gulag2.Count == 0)
			{
				DevolverGanadorPorAbandono(Var.Gulag1);
			}
			else if (Var.Gulag2.Count >= 1 && Var.Gulag1.Count == 0)
			{
				DevolverGanadorPorAbandono(Var.Gulag2);
			}
			else if (Var.Gulag1.Count == 0 && Var.Gulag2.Count == 0)
			{
				Var.Gulag2v2 = false;
			}
			Var.GenteGulag.RemoveAll((Exiled.API.Features.Player p) => p == null || !p.IsConnected);
			int queueCount = Var.GenteGulag.Count;
			for (int i = 0; i < queueCount; i++)
			{
				Exiled.API.Features.Player esperando = Var.GenteGulag[i];
				esperando.ClearBroadcasts();
				esperando.Broadcast(5, ((queueCount == 1) ? "<align=left><b><size=25>\ud83d\udc80 | <color=#859CC7>Esperando al enemigo</color></size></b></align>" : $"<align=left><b><size=25>\ud83d\udc80 {i / 2} | <color=#859CC7>Rondas hasta el combate</color></size></b></align>") + "\n<align=left><size=18><color=#AAAAAA>Próxima pelea: " + ModosGulag.Descripcion + "</color></size></align>");
			}
			if (Var.GenteGulag.Count >= 2 && Var.Gulag1.Count == 0 && Var.Gulag2.Count == 0 && Var.PeleaPractica.Count > 0)
			{
				List<Exiled.API.Features.Player> cortados = new List<Exiled.API.Features.Player>(Var.PeleaPractica);
				CancelarPractica(null);
				foreach (Exiled.API.Features.Player c in cortados)
				{
					if (c != null && c.IsConnected)
					{
						EffectsHUD.Notify(c, "<color=#859CC7>Se cortó la práctica porque empieza una pelea de <b>gulag</b>. Volviste a la cola.</color>", 4f);
					}
				}
			}
			if (Var.GenteGulag.Count >= 2 && Var.Gulag1.Count == 0 && Var.Gulag2.Count == 0 && (!ModosGulag.Comparten || Var.PeleaPractica.Count == 0))
			{
				try
				{
					float prob2v2 = (Plugin.Singleton?.Config?.Gulag2v2Probabilidad).GetValueOrDefault();
					if (Var.GenteGulag.Count >= 4 && UnityEngine.Random.value < prob2v2)
					{
						IniciarCombate2v2();
					}
					else
					{
						IniciarCombate();
					}
				}
				catch (Exception ex)
				{
					Exception e = ex;
					Log.Error($"[Gulag] {e}");
					Var.Gulag1.Clear();
					Var.Gulag2.Clear();
				}
			}
			yield return Timing.WaitForSeconds(5f);
		}
	}

	public static void EntrarPractica(Exiled.API.Features.Player player, string aviso = null)
	{
		player.Role.Set(RoleTypeId.Tutorial);
		Timing.CallDelayed(0.1f, delegate
		{
			if (player.IsConnected && player.Role.Type == RoleTypeId.Tutorial)
			{
				player.Position = EsperaPractica;
				player.ClearInventory();
				if (!Var.ColaPractica.Contains(player))
				{
					Var.ColaPractica.Add(player);
				}
			}
		});
		Timing.CallDelayed(0.5f, delegate
		{
			if (player.IsConnected && player.Role.Type == RoleTypeId.Tutorial)
			{
				EffectsHUD.Notify(player, (aviso ?? "<color=#859CC7>Gulag de <b>práctica</b>: peleas 1v1 sin premio. Cuando venga la oleada de refuerzos entras igual.\nSalís con <b>.practica</b></color>") + "\n<size=18><color=#AAAAAA>Próxima pelea: " + ModosGulag.DescripcionPractica + ". Revancha con <b>.revancha</b> · desafío con <b>.duelo nombre</b>.</color></size>", 6f);
			}
		});
	}

	public static string PedirRevancha(Exiled.API.Features.Player p)
	{
		if (!Var.ColaPractica.Contains(p))
		{
			return "Tenés que estar en la cola de práctica (.practica) para pedir revancha.";
		}
		if (!_ultimoRival.TryGetValue(p, out var value) || value == null || !value.IsConnected)
		{
			return "No tenés un rival reciente en la práctica.";
		}
		float time = Time.time;
		if (_revancha.TryGetValue(value, out (Exiled.API.Features.Player, float) value2) && value2.Item1 == p && time < value2.Item2 && Var.ColaPractica.Contains(value))
		{
			_revancha.Remove(value);
			Var.ColaPractica.Remove(p);
			Var.ColaPractica.Remove(value);
			Var.ColaPractica.Insert(0, value);
			Var.ColaPractica.Insert(1, p);
			EffectsHUD.Notify(value, "<color=#FFD400><b>¡Revancha aceptada!</b></color> <color=#859CC7>Peleás de nuevo contra " + p.Nickname + ".</color>", 4f);
			EffectsHUD.Notify(p, "<color=#FFD400><b>¡Revancha aceptada!</b></color> <color=#859CC7>Peleás de nuevo contra " + value.Nickname + ".</color>", 4f);
			return "Revancha aceptada: son los próximos en pelear.";
		}
		_revancha[p] = (value, time + 30f);
		EffectsHUD.Notify(value, "<color=#FFD400>" + p.Nickname + " te pide <b>revancha</b>.</color> <color=#859CC7>Escribí <b>.revancha</b> para aceptar (30 s).</color>", 6f);
		return "Le pediste revancha a " + value.Nickname + ". Tiene 30 s para aceptar.";
	}

	private static Exiled.API.Features.Player BuscarEnCola(string nombre, Exiled.API.Features.Player excepto)
	{
		if (string.IsNullOrWhiteSpace(nombre))
		{
			return null;
		}
		string q = nombre.Trim().ToLowerInvariant();
		List<Exiled.API.Features.Player> source = Var.ColaPractica.Where((Exiled.API.Features.Player x) => x != null && x.IsConnected && x != excepto).ToList();
		int id;
		return source.FirstOrDefault((Exiled.API.Features.Player x) => x.Nickname.ToLowerInvariant() == q) ?? source.FirstOrDefault((Exiled.API.Features.Player x) => x.Nickname.ToLowerInvariant().StartsWith(q)) ?? source.FirstOrDefault((Exiled.API.Features.Player x) => x.Nickname.ToLowerInvariant().Contains(q)) ?? (int.TryParse(q, out id) ? source.FirstOrDefault((Exiled.API.Features.Player x) => x.Id == id) : null);
	}

	public static string PedirDuelo(Exiled.API.Features.Player p, string nombre)
	{
		if (p == null)
		{
			return "Este comando se usa desde el juego.";
		}
		if (!Var.ColaPractica.Contains(p))
		{
			return "Tenés que estar en la cola de práctica para desafiar (.practica).";
		}
		if (string.IsNullOrWhiteSpace(nombre))
		{
			return "Uso: .duelo <nombre>  (tiene que estar en la cola de práctica).";
		}
		Exiled.API.Features.Player player = BuscarEnCola(nombre, p);
		if (player == null)
		{
			return "No hay nadie con ese nombre en la cola de práctica.";
		}
		if (_duelos.TryGetValue(p, out (Exiled.API.Features.Player, float) value) && value.Item1 == player && Time.time < value.Item2)
		{
			return AceptarDuelo(p);
		}
		_duelos[player] = (p, Time.time + 30f);
		EffectsHUD.Notify(player, "<color=#FFD400>⚔ " + p.Nickname + " te desafía a un <b>duelo</b>.</color> <color=#859CC7>Escribí <b>.aceptar</b> en la consola (30 s).</color>", 8f);
		return "Desafiaste a " + player.Nickname + ". Tiene 30 s para aceptar con .aceptar.";
	}

	public static string AceptarDuelo(Exiled.API.Features.Player p)
	{
		if (p == null)
		{
			return "Este comando se usa desde el juego.";
		}
		if (!_duelos.TryGetValue(p, out (Exiled.API.Features.Player, float) value) || Time.time > value.Item2 || value.Item1 == null || !value.Item1.IsConnected)
		{
			_duelos.Remove(p);
			return "No tenés ningún desafío pendiente.";
		}
		var (player, _) = value;
		_duelos.Remove(p);
		if (!Var.ColaPractica.Contains(p) || !Var.ColaPractica.Contains(player))
		{
			return "Los dos tienen que estar en la cola de práctica.";
		}
		Var.ColaPractica.Remove(p);
		Var.ColaPractica.Remove(player);
		Var.ColaPractica.Insert(0, player);
		Var.ColaPractica.Insert(1, p);
		Exiled.API.Features.Player[] array = new Exiled.API.Features.Player[2] { p, player };
		foreach (Exiled.API.Features.Player player2 in array)
		{
			EffectsHUD.Notify(player2, "<color=#FFD400><b>⚔ ¡Duelo aceptado!</b></color> <color=#859CC7>" + player.Nickname + " vs " + p.Nickname + ": son los próximos en pelear.</color>", 5f);
		}
		return "Duelo aceptado contra " + player.Nickname + ": son los próximos.";
	}

	private static void VolverAColaPractica(Exiled.API.Features.Player p)
	{
		if (!(p == null) && p.IsConnected && p.Role.Type == RoleTypeId.Tutorial)
		{
			p.ClearInventory();
			p.DisableAllEffects();
			p.Health = 100f;
			p.Position = EsperaPractica;
			if (!Var.ColaPractica.Contains(p))
			{
				Var.ColaPractica.Add(p);
			}
		}
	}

	private static float Radio(Vector3 a, Vector3 b, Vector3 espera, Vector3 centro)
	{
		return Mathf.Max(30f, (a - b).magnitude / 2f + 10f, (espera - centro).magnitude + 8f);
	}

	public static void LimpiarArena()
	{
		Timing.CallDelayed(1f, LimpiarArenaAhora);
		Timing.CallDelayed(3.5f, LimpiarArenaAhora);
	}

	public static void LimpiarArenaAhora()
	{
		if (ModosGulag.Comparten)
		{
			if (GulagLibre && PracticaLibre)
			{
				LimpiarZona(ModosGulag.Centro, Radio(ModosGulag.SpawnA, ModosGulag.SpawnB, ModosGulag.Espera, ModosGulag.Centro));
			}
			return;
		}
		if (GulagLibre)
		{
			LimpiarZona(ModosGulag.Centro, Radio(ModosGulag.SpawnA, ModosGulag.SpawnB, ModosGulag.Espera, ModosGulag.Centro));
		}
		if (PracticaLibre)
		{
			LimpiarZona(ModosGulag.PCentro, Radio(ModosGulag.PSpawnA, ModosGulag.PSpawnB, ModosGulag.PEspera, ModosGulag.PCentro));
		}
	}

	private static void LimpiarZona(Vector3 centro, float radio)
	{
		try
		{
			float num = radio * radio;
			List<Vector3> source = (from x in Exiled.API.Features.Player.List
				where x.IsAlive
				select x.Position).ToList();
			foreach (Exiled.API.Features.Pickups.Pickup pk in Exiled.API.Features.Pickups.Pickup.List.ToList())
			{
				if (pk != null && !((pk.Position - centro).sqrMagnitude >= num) && !source.Any((Vector3 j) => (j - pk.Position).sqrMagnitude < 4f))
				{
					pk.Destroy();
				}
			}
			foreach (Exiled.API.Features.Ragdoll item in Exiled.API.Features.Ragdoll.List.ToList())
			{
				if (item != null && (item.Position - centro).sqrMagnitude < num)
				{
					item.Destroy();
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Práctica] Limpiar arena: " + ex.Message);
		}
	}

	public static void AlLevantarItem(PickingUpItemEventArgs ev)
	{
		if (!(ev?.Player == null) && (Var.GenteGulag.Contains(ev.Player) || Var.ColaPractica.Contains(ev.Player)))
		{
			ev.IsAllowed = false;
			EffectsHUD.Notify(ev.Player, "<color=#e32433>No podés agarrar cosas mientras esperás tu pelea.</color>", 2f);
		}
	}

	public static void CancelarPractica(Exiled.API.Features.Player quienSale)
	{
		if (quienSale != null && Var.PracticaUltimoEnPie && Var.PeleaPractica.Count > 2)
		{
			Var.PeleaPractica.Remove(quienSale);
			return;
		}
		List<Exiled.API.Features.Player> list = new List<Exiled.API.Features.Player>(Var.PeleaPractica);
		Var.PeleaPractica.Clear();
		Var.PracticaUltimoEnPie = false;
		if (list.Count > 0)
		{
			LimpiarArena();
		}
		foreach (Exiled.API.Features.Player item in list)
		{
			if (!(item == quienSale))
			{
				VolverAColaPractica(item);
				if (item != null)
				{
					EffectsHUD.Notify(item, "<color=#859CC7>La pelea de práctica se canceló. Volviste a la cola.</color>");
				}
			}
		}
	}

	public static void ResolverPractica(DyingEventArgs ev)
	{
		Exiled.API.Features.Player player = ev.Player;
		if (ev.DamageHandler.Type == DamageType.Warhead)
		{
			ev.IsAllowed = false;
			player.Heal(50f);
			return;
		}
		bool practicaUltimoEnPie = Var.PracticaUltimoEnPie;
		Var.PeleaPractica.Remove(player);
		Estadisticas.PerdioPractica(player);
		if (practicaUltimoEnPie && Var.PeleaPractica.Count > 1)
		{
			Exiled.API.Features.Player attacker = ev.Attacker;
			foreach (Exiled.API.Features.Player item in Var.PeleaPractica)
			{
				if (item != null && item.IsConnected)
				{
					EffectsHUD.Notify(item, $"<color=#859CC7>\ud83d\udc80 {player.Nickname} cayó. Quedan <b>{Var.PeleaPractica.Count}</b>.</color>");
				}
			}
			player.Broadcast(4, string.Format("<size=28><color=#e32433>Caíste{0}.</color> <color=#859CC7>Quedan {1}. Volvés a la cola en un momento.</color></size>", (attacker != null && attacker != player) ? (" ante " + attacker.Nickname) : "", Var.PeleaPractica.Count), Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			VolverPerdedorACola(player);
			return;
		}
		Exiled.API.Features.Player ganador = Var.PeleaPractica.FirstOrDefault();
		Var.PeleaPractica.Clear();
		Var.PracticaUltimoEnPie = false;
		LimpiarArena();
		if (ganador == null || !ganador.IsConnected)
		{
			return;
		}
		Estadisticas.GanoPractica(ganador, practicaUltimoEnPie);
		if (practicaUltimoEnPie)
		{
			EffectsHUD.Notify(ganador, "<color=#D4AF37><b>\ud83d\udc51 ¡Último en pie!</b> Ganaste el todos contra todos.</color>", 5f);
			foreach (Exiled.API.Features.Player item2 in Var.ColaPractica)
			{
				if (item2 != null && item2.IsConnected && item2 != ganador)
				{
					item2.Broadcast(4, "<size=28><color=#D4AF37>\ud83d\udc51 " + ganador.Nickname + "</color> <color=#859CC7>es el último en pie.</color></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
				}
			}
		}
		else
		{
			EffectsHUD.Notify(ganador, "<color=#4CAF50><b>Ganaste</b> la práctica contra " + player.Nickname + ".</color>", 4f);
		}
		player.Broadcast(4, practicaUltimoEnPie ? ("<size=28><color=#e32433>Caíste. " + ganador.Nickname + " es el último en pie.</color> <color=#859CC7>Volvés a la cola en un momento.</color></size>") : ("<size=28><color=#e32433>Perdiste contra " + ganador.Nickname + ".</color> <color=#859CC7>Volvés a la cola en un momento. <b>.revancha</b> para pedir otra.</color></size>"), Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
		VolverPerdedorACola(player);
		Timing.CallDelayed(2f, delegate
		{
			VolverAColaPractica(ganador);
		});
	}

	private static void VolverPerdedorACola(Exiled.API.Features.Player victima)
	{
		Timing.CallDelayed(3f, delegate
		{
			if (!(victima == null) && victima.IsConnected && victima.Role.Type == RoleTypeId.Spectator && !Exiled.API.Features.Round.IsEnded && !Var.EventoEnCurso && !Var.GenteGulag.Contains(victima))
			{
				EntrarPractica(victima, "<color=#859CC7>Volviste a la cola de práctica. Salís con <b>.practica</b>.</color>");
			}
		});
	}

	private static Vector3 PosicionUltimoEnPie(int i, int n)
	{
		Vector3 pSpawnA = ModosGulag.PSpawnA;
		Vector3 pSpawnB = ModosGulag.PSpawnB;
		if (n <= 1)
		{
			return pSpawnA;
		}
		float t = (float)i / (float)(n - 1);
		Vector3 result = Vector3.Lerp(pSpawnA, pSpawnB, t);
		if (i > 0 && i < n - 1)
		{
			Vector3 vector = Vector3.Cross((pSpawnB - pSpawnA).normalized, Vector3.up);
			result += vector * ((i % 2 == 0) ? 3f : (-3f));
		}
		return result;
	}

	public static IEnumerator<float> PracticaCheck()
	{
		while (!Exiled.API.Features.Round.IsEnded)
		{
			Var.ColaPractica.RemoveAll((Exiled.API.Features.Player p) => p == null || !p.IsConnected || p.Role.Type != RoleTypeId.Tutorial);
			if (Var.PeleaPractica.Count > 0 && Time.time - Var.PracticaInicio > (Var.PracticaUltimoEnPie ? 162f : 90f))
			{
				CancelarPractica(null);
			}
			int j = Var.ColaPractica.Count;
			for (int i = 0; i < j; i++)
			{
				Var.ColaPractica[i].Broadcast(5, ((j == 1) ? "<align=left><b><size=25>\ud83e\udd4a | <color=#859CC7>Práctica: esperando rival</color></size></b></align>" : $"<align=left><b><size=25>\ud83e\udd4a {i / 2} | <color=#859CC7>Práctica: peleas hasta tu turno</color></size></b></align>") + "\n<align=left><size=18><color=#AAAAAA>Próxima pelea: " + ModosGulag.DescripcionPractica + "</color></size></align>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			}
			bool arenaLibre = PracticaLibre && (!ModosGulag.Comparten || GulagLibre);
			bool gulagEsperando = Var.GenteGulag.Count >= 2 || Var.Gulag1.Count > 0 || Var.Gulag2.Count > 0;
			if (arenaLibre)
			{
				LimpiarArenaAhora();
			}
			if (arenaLibre && !gulagEsperando && Var.ColaPractica.Count >= 2)
			{
				try
				{
					float prob = (Plugin.Singleton?.Config?.PracticaUltimoEnPieProbabilidad).GetValueOrDefault();
					if (Var.ColaPractica.Count >= 4 && UnityEngine.Random.value < prob)
					{
						IniciarUltimoEnPie();
					}
					else
					{
						Exiled.API.Features.Player a = Var.ColaPractica[0];
						Exiled.API.Features.Player b = Var.ColaPractica[1];
						Var.ColaPractica.RemoveRange(0, 2);
						LimpiarArenaAhora();
						Var.PracticaUltimoEnPie = false;
						Var.PeleaPractica.Add(a);
						Var.PeleaPractica.Add(b);
						Var.PracticaInicio = Time.time;
						PrepararLuchador(a, ModosGulag.PSpawnA, practica: true);
						PrepararLuchador(b, ModosGulag.PSpawnB, practica: true);
						_ultimoRival[a] = b;
						_ultimoRival[b] = a;
						_revancha.Remove(a);
						_revancha.Remove(b);
						ItemType arma = ModosGulag.EmpezarPelea(practica: true);
						DarArma(a, arma);
						DarArma(b, arma);
						Timing.CallDelayed(8f, delegate
						{
							Exiled.API.Features.Player[] array = new Exiled.API.Features.Player[2] { a, b };
							foreach (Exiled.API.Features.Player player in array)
							{
								if (player != null && player.IsConnected && Var.PeleaPractica.Contains(player))
								{
									player.DisableAllEffects();
									ModosGulag.AplicarModificador(player);
								}
							}
						});
					}
				}
				catch (Exception ex)
				{
					Exception e = ex;
					Log.Error($"[Práctica] {e}");
					Var.PeleaPractica.Clear();
					Var.PracticaUltimoEnPie = false;
				}
			}
			yield return Timing.WaitForSeconds(5f);
		}
	}

	private static void IniciarUltimoEnPie()
	{
		int num = Math.Min(6, Var.ColaPractica.Count);
		List<Exiled.API.Features.Player> luchadores = Var.ColaPractica.GetRange(0, num);
		Var.ColaPractica.RemoveRange(0, num);
		LimpiarArenaAhora();
		Var.PracticaUltimoEnPie = true;
		Var.PeleaPractica.AddRange(luchadores);
		Var.PracticaInicio = Time.time;
		ItemType arma = ModosGulag.EmpezarPelea(practica: true);
		for (int i = 0; i < luchadores.Count; i++)
		{
			Exiled.API.Features.Player player = luchadores[i];
			PrepararLuchador(player, PosicionUltimoEnPie(i, num), practica: true, $"<color=#D4AF37><b>\ud83d\udc51 Último en pie</b></color><color=#859CC7>: todos contra todos ({num}). Gana el último que quede vivo.</color>");
			_revancha.Remove(player);
			DarArma(player, arma);
		}
		foreach (Exiled.API.Features.Player item in Var.ColaPractica)
		{
			if (item != null && item.IsConnected)
			{
				EffectsHUD.Notify(item, $"<color=#D4AF37>\ud83d\udc51 Último en pie</color><color=#859CC7>: {num} jugadores pelean todos contra todos.</color>", 4f);
			}
		}
		Timing.CallDelayed(8f, delegate
		{
			foreach (Exiled.API.Features.Player item2 in luchadores)
			{
				if (item2 != null && item2.IsConnected && Var.PeleaPractica.Contains(item2))
				{
					item2.DisableAllEffects();
					ModosGulag.AplicarModificador(item2);
				}
			}
		});
	}

	private static void IniciarCombate()
	{
		Exiled.API.Features.Player player = Var.GenteGulag[0];
		Exiled.API.Features.Player player2 = Var.GenteGulag[1];
		Var.GenteGulag.RemoveRange(0, 2);
		Var.Estuvo.Add(player.UserId);
		Var.Estuvo.Add(player2.UserId);
		if (player.Role.Type != RoleTypeId.Tutorial || player2.Role.Type != RoleTypeId.Tutorial || player.UserId == player2.UserId)
		{
			player.Kill("Sistema Anti-Bug del Gulag.");
			if (player2 != player)
			{
				player2.Kill("Sistema Anti-Bug del Gulag.");
			}
			return;
		}
		LimpiarArenaAhora();
		Var.Gulag1.Add(player);
		Var.Gulag2.Add(player2);
		ItemType arma = ModosGulag.EmpezarPelea(practica: false);
		PrepararLuchador(player, ModosGulag.SpawnA);
		DarArma(player, arma);
		Timing.CallDelayed(0.05f, delegate
		{
			if (!(player2 == null) && player2.IsConnected && Var.Gulag2.Contains(player2))
			{
				PrepararLuchador(player2, ModosGulag.SpawnB);
				DarArma(player2, arma);
			}
		});
		Timing.CallDelayed(8f, delegate
		{
			Exiled.API.Features.Player[] array = new Exiled.API.Features.Player[2] { player, player2 };
			foreach (Exiled.API.Features.Player player3 in array)
			{
				if (!(player3 == null) && player3.IsConnected)
				{
					player3.DisableAllEffects();
					ModosGulag.AplicarModificador(player3);
					Sonido.Para(player3.Id, "Gulag.ogg");
				}
			}
		});
	}

	private static void IniciarCombate2v2()
	{
		List<Exiled.API.Features.Player> cuatro = Var.GenteGulag.GetRange(0, 4);
		Var.GenteGulag.RemoveRange(0, 4);
		foreach (Exiled.API.Features.Player item in cuatro)
		{
			Var.Estuvo.Add(item.UserId);
		}
		if (cuatro.Any((Exiled.API.Features.Player p) => p == null || !p.IsConnected || p.Role.Type != RoleTypeId.Tutorial) || cuatro.Select((Exiled.API.Features.Player p) => p.UserId).Distinct().Count() < 4)
		{
			foreach (Exiled.API.Features.Player item2 in cuatro)
			{
				if (item2 != null && item2.IsConnected && item2.Role.Type == RoleTypeId.Tutorial)
				{
					item2.Kill("Sistema Anti-Bug del Gulag.");
				}
			}
			return;
		}
		LimpiarArenaAhora();
		Var.Gulag2v2 = true;
		Var.Gulag1.Add(cuatro[0]);
		Var.Gulag1.Add(cuatro[1]);
		Var.Gulag2.Add(cuatro[2]);
		Var.Gulag2.Add(cuatro[3]);
		Vector3 vector = Vector3.Cross((ModosGulag.SpawnB - ModosGulag.SpawnA).normalized, Vector3.up) * 1.5f;
		PrepararLuchador(cuatro[0], ModosGulag.SpawnA + vector, practica: false, aviso(cuatro[1]));
		PrepararLuchador(cuatro[1], ModosGulag.SpawnA - vector, practica: false, aviso(cuatro[0]));
		PrepararLuchador(cuatro[2], ModosGulag.SpawnB + vector, practica: false, aviso(cuatro[3]));
		PrepararLuchador(cuatro[3], ModosGulag.SpawnB - vector, practica: false, aviso(cuatro[2]));
		ItemType arma = ModosGulag.EmpezarPelea(practica: false);
		foreach (Exiled.API.Features.Player item3 in cuatro)
		{
			DarArma(item3, arma);
		}
		Timing.CallDelayed(8f, delegate
		{
			foreach (Exiled.API.Features.Player item4 in cuatro)
			{
				if (!(item4 == null) && item4.IsConnected && (Var.Gulag1.Contains(item4) || Var.Gulag2.Contains(item4)))
				{
					item4.DisableAllEffects();
					ModosGulag.AplicarModificador(item4);
					Sonido.Para(item4.Id, "Gulag.ogg");
				}
			}
		});
		static string aviso(Exiled.API.Features.Player compa)
		{
			return "<color=#D4AF37><b>⚔ Gulag 2v2</b></color><color=#859CC7>: tu compañero es <b>" + compa.Nickname + "</b>. Si ganan, vuelven los dos.</color>";
		}
	}

	private static IEnumerator<float> CuentaRegresiva(Exiled.API.Features.Player p, int segundos)
	{
		for (int i = segundos; i > 0; i--)
		{
			if (p == null || !p.IsConnected || p.Role.Type != RoleTypeId.Tutorial)
			{
				yield break;
			}
			int j = i;
			ConsolaLimpia.SinHud(delegate
			{
				p.Broadcast(1, string.Format("<size=60><b><color={0}>{1}</color></b></size>", (j <= 3) ? "#e32433" : "#859CC7", j), Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			});
			yield return Timing.WaitForSeconds(1f);
		}
		if (p != null && p.IsConnected && p.Role.Type == RoleTypeId.Tutorial)
		{
			ConsolaLimpia.SinHud(delegate
			{
				p.Broadcast(2, "<size=60><b><color=#4CAF50>¡YA!</color></b></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			});
		}
	}

	public static bool EnCuentaRegresiva(Exiled.API.Features.Player p)
	{
		if (p == null || !_sinAtacarHasta.TryGetValue(p, out var value))
		{
			return false;
		}
		if (Time.time < value)
		{
			return true;
		}
		_sinAtacarHasta.Remove(p);
		return false;
	}

	private static void PrepararLuchador(Exiled.API.Features.Player p, Vector3 posicion, bool practica = false, string texto = null)
	{
		p.ClearInventory();
		p.IsGodModeEnabled = false;
		p.Health = 100f;
		p.Position = posicion;
		EffectsHUD.Notify(p, (texto != null) ? texto : (practica ? "<color=#859CC7><b>Práctica 1v1</b>: no hay premio, solo el honor.</color>" : "<color=#003ECA>Líder MTF:</color> <color=#859CC7>Gana aquí y volverás al combate. Pierde, y tu lucha habrá terminado.</color>"), 5f);
		p.ClearBroadcasts();
		if (p.Scale != Escala1)
		{
			p.Broadcast(3, "<color=#e32433>Recuerda no usar ningún comando que te cambie de tamaño dentro del Gulag.</color>");
			p.Scale = Escala1;
		}
		Timing.RunCoroutine(CuentaRegresiva(p, 8));
		_sinAtacarHasta[p] = Time.time + 8f;
		p.EnableEffect(EffectType.Ensnared, 8.5f);
	}

	private static void DarArma(Exiled.API.Features.Player p, ItemType arma)
	{
		try
		{
			Exiled.API.Features.Items.Item item2 = (p.CurrentItem = p.AddItem(arma));
			if (item2 is Firearm firearm)
			{
				firearm.MagazineAmmo = firearm.MaxMagazineAmmo;
			}
			else if (item2 is MicroHid microHid)
			{
				microHid.Energy = 1f;
			}
			if (Var.AmmoPorArma.TryGetValue(arma, out var value))
			{
				p.AddAmmo(value, 90);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[Gulag] DarArma {arg}");
		}
	}
}
