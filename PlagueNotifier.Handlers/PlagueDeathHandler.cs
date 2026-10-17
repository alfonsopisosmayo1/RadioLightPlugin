using System;
using System.Collections.Generic;
using System.Text;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Features;
using Exiled.Events.Handlers;
using PlayerRoles;
using PlayerStatsSystem;
using RadioLightPlugin.Misc;
using RueI.API;
using RueI.API.Elements;
using UnityEngine;

namespace PlagueNotifier.Handlers;

public class PlagueDeathHandler
{
	private sealed class DeathEntry
	{
		public string Victim { get; }

		public string Cause { get; }

		public float Tiempo { get; }

		public DeathEntry(string victim, string cause)
		{
			Victim = victim;
			Cause = cause;
			Tiempo = Time.time;
		}
	}

	private const int MaxDeaths = 5;

	private static readonly Tag PlagueDisplayTag = new Tag();

	private readonly List<DeathEntry> deathLog = new List<DeathEntry>();

	private const float SegundosEnLista = 60f;

	private const float SegundosEnPantalla = 15f;

	private const float Posicion = 700f;

	public void Register()
	{
		Exiled.Events.Handlers.Player.Died += new CustomEventHandler<DiedEventArgs>(OnDied);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(OnChangingRole);
		Exiled.Events.Handlers.Server.RestartingRound += new CustomEventHandler(OnRestartingRound);
	}

	public void Unregister()
	{
		Exiled.Events.Handlers.Player.Died -= new CustomEventHandler<DiedEventArgs>(OnDied);
		Exiled.Events.Handlers.Player.ChangingRole -= new CustomEventHandler<ChangingRoleEventArgs>(OnChangingRole);
		Exiled.Events.Handlers.Server.RestartingRound -= new CustomEventHandler(OnRestartingRound);
		deathLog.Clear();
	}

	private void OnChangingRole(ChangingRoleEventArgs ev)
	{
		if (ev?.Player == null)
		{
			return;
		}
		try
		{
			RueDisplay.Get(ev.Player).Remove(PlagueDisplayTag);
		}
		catch
		{
		}
	}

	private void OnRestartingRound()
	{
		deathLog.Clear();
	}

	private void OnDied(DiedEventArgs ev)
	{
		if (ev?.Player == null || (ev.TargetOldRole != RoleTypeId.Scp0492 && ev.Player.Role.Type != RoleTypeId.Scp0492))
		{
			return;
		}
		Config config = Plugin.Instance?.Config;
		if (config == null || !config.IsEnabled)
		{
			return;
		}
		string nickname = ev.Player.Nickname;
		string causeDescription = GetCauseDescription(ev.DamageHandler, ev.Attacker, ev.Player);
		if (config.LogToConsole)
		{
			Log.Info("[PlagueNotifier] Zombie '" + nickname + "' (" + ev.Player.UserId + ") murio. Causa: " + causeDescription);
		}
		DeathEntry item = new DeathEntry(nickname, causeDescription);
		deathLog.Insert(0, item);
		while (deathLog.Count > 5)
		{
			deathLog.RemoveAt(deathLog.Count - 1);
		}
		HashSet<Exiled.API.Features.Player> hashSet = new HashSet<Exiled.API.Features.Player>();
		foreach (Exiled.API.Features.Player item2 in Exiled.API.Features.Player.List)
		{
			if (!(item2 == null) && item2.IsConnected && !item2.IsNPC && !(item2 == ev.Player) && item2.IsAlive && item2.Role.Type == RoleTypeId.Scp049)
			{
				hashSet.Add(item2);
			}
		}
		foreach (Exiled.API.Features.Player item3 in hashSet)
		{
			if (!(item3 == null) && item3.IsConnected)
			{
				try
				{
					ShowDeathLog(item3);
				}
				catch (Exception ex)
				{
					Log.Debug("[PlagueNotifier] No se pudo mostrar el log a " + item3.Nickname + ": " + ex.Message);
				}
			}
		}
	}

	private void ShowDeathLog(Exiled.API.Features.Player player)
	{
		if (!(player == null) && player.IsConnected && player.IsAlive && player.Role.Type == RoleTypeId.Scp049)
		{
			// [HUD RadioLight] el panel de la plaga ahora es un panel más de la HUD (arriba a la izquierda)
			MEC.Timing.KillCoroutines(_panelPlaga);
			_panelPlaga = MEC.Timing.RunCoroutine(PanelPlaga(player));
		}
	}

	private MEC.CoroutineHandle _panelPlaga;

	private System.Collections.Generic.IEnumerator<float> PanelPlaga(Exiled.API.Features.Player player)
	{
		for (int i = 0; i < 15; i++)
		{
			if (player == null || !player.IsConnected || !player.IsAlive || player.Role.Type != RoleTypeId.Scp049)
			{
				break;
			}
			string text = ArmarPanel();
			if (string.IsNullOrEmpty(text))
			{
				break;
			}
			RadioLightPlugin.Misc.EffectsHUD.Panel(player, "plaga", text, 2f);
			yield return MEC.Timing.WaitForSeconds(1f);
		}
		if (player != null)
		{
			RadioLightPlugin.Misc.EffectsHUD.QuitarPanel(player, "plaga");
		}
	}

	private string ArmarPanel()
	{
		float ahora = Time.time;
		deathLog.RemoveAll((DeathEntry d) => ahora - d.Tiempo > 60f);
		if (deathLog.Count == 0)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(512);
		stringBuilder.Append("<align=right><line-height=105%>");
		stringBuilder.Append("<size=24><color=#7BC67E><b>☣ LA PLAGA</b></color></size> ").Append("<size=16><color=#6E8B6E>bajas recientes</color></size>\n").Append("<size=12><color=#3F5F3F>▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬</color></size>\n");
		for (int i = 0; i < deathLog.Count; i++)
		{
			DeathEntry deathEntry = deathLog[i];
			int s = Mathf.Max(0, (int)(ahora - deathEntry.Tiempo));
			string value = ((i == 0) ? "#FFFFFF" : "#C8C8C8");
			string value2 = ((i == 0) ? "#9E9E9E" : "#707070");
			stringBuilder.Append("<size=19><color=#E74C3C>✖</color> <color=").Append(value).Append("><b>")
				.Append(EscapeRichText(Recortar(deathEntry.Victim, 22)))
				.Append("</b></color> ")
				.Append("<color=")
				.Append(value2)
				.Append("><size=14>· hace ")
				.Append(Hace(s))
				.Append("</size></color></size>\n")
				.Append("<size=15><color=")
				.Append(value2)
				.Append(">")
				.Append(EscapeRichText(Recortar(deathEntry.Cause, 46)))
				.Append("</color></size>\n");
		}
		stringBuilder.Append("</line-height></align>");
		return stringBuilder.ToString();
	}

	private static string Hace(int s)
	{
		return (s < 60) ? $"{s}s" : $"{s / 60}m";
	}

	private static string Recortar(string texto, int max)
	{
		return (string.IsNullOrEmpty(texto) || texto.Length <= max) ? texto : (texto.Substring(0, max - 1) + "…");
	}

	private static string EscapeRichText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}

	private static string GetCauseDescription(CustomDamageHandler handler, Exiled.API.Features.Player attacker, Exiled.API.Features.Player victim)
	{
		string text = ((attacker != null && attacker != victim) ? attacker.Nickname : null);
		string text2 = ((attacker != null && attacker != victim) ? GetRoleSpanish(attacker.Role.Type) : null);
		if (handler == null)
		{
			return (text != null) ? (text + " [" + text2 + "]") : "Causa desconocida";
		}
		string text3;
		switch (handler.Type)
		{
		case DamageType.Shotgun:
			text3 = "Escopeta";
			break;
		case DamageType.E11Sr:
			text3 = "Fusil E-11-SR";
			break;
		case DamageType.Crossvec:
			text3 = "Subfusil Crossvec";
			break;
		case DamageType.Logicer:
			text3 = "Ametralladora Logicer";
			break;
		case DamageType.Revolver:
			text3 = "Revólver .44";
			break;
		case DamageType.AK:
			text3 = "Fusil AK";
			break;
		case DamageType.Com15:
			text3 = "Pistola COM-15";
			break;
		case DamageType.Com18:
			text3 = "Pistola COM-18";
			break;
		case DamageType.Fsp9:
			text3 = "Subfusil FSP-9";
			break;
		case DamageType.Frmg0:
			text3 = "Ametralladora FR-MG-0";
			break;
		case DamageType.A7:
			text3 = "Fusil A7";
			break;
		case DamageType.Com45:
			text3 = "COM-45";
			break;
		case DamageType.ParticleDisruptor:
			text3 = "Disyuntor de Partículas";
			break;
		case DamageType.Jailbird:
			text3 = "Jailbird";
			break;
		case DamageType.MicroHid:
			text3 = "Micro-HID";
			break;
		case DamageType.Explosion:
			text3 = "Granada HE / Explosión";
			break;
		case DamageType.Scp018:
			text3 = "SCP-018 (Pelota rebotante)";
			break;
		case DamageType.Tesla:
			return "Puerta Tesla";
		case DamageType.Falldown:
			return "Caída / Impacto contra el suelo";
		case DamageType.Warhead:
			return "Detonación de la Nuke (Ojiva Alpha)";
		case DamageType.Decontamination:
			return "Descontaminación de LCZ";
		case DamageType.Crushed:
			return "Aplastado por compuerta pesada";
		case DamageType.PocketDimension:
			return "Dimensión de Bolsillo (SCP-106)";
		case DamageType.Poison:
			return "Veneno / Toxicidad";
		case DamageType.Bleeding:
			return "Hemorragia";
		case DamageType.Asphyxiation:
			return "Asfixia / Gas";
		case DamageType.Scp207:
			return "Sobredosis de refresco SCP-207";
		case DamageType.Scp049:
			return "Contacto letal de SCP-049";
		case DamageType.Scp096:
			return "Ataque frenético de SCP-096";
		case DamageType.Scp173:
			return "Cuello fracturado por SCP-173";
		case DamageType.Scp939:
			return "Garras y mordedura de SCP-939";
		case DamageType.Scp106:
			return "Ataque corrosivo de SCP-106";
		case DamageType.Scp3114:
			return "Estrangulado por esqueleto SCP-3114";
		case DamageType.Scp1507:
			return "Picoteo feroz de SCP-1507 (Flamenco)";
		case DamageType.FriendlyFireDetector:
			return "Fuego Amigo";
		default:
			text3 = ((!(handler.Base is PlayerStatsSystem.FirearmDamageHandler { WeaponType: var weaponType })) ? handler.Type.ToString() : weaponType.ToString());
			break;
		}
		if (text != null)
		{
			return text + " [" + text2 + "] con " + text3;
		}
		return text3;
	}

	private static string GetRoleSpanish(RoleTypeId role)
	{
		return role switch
		{
			RoleTypeId.ClassD => "Clase-D", 
			RoleTypeId.Scientist => "Científico", 
			RoleTypeId.FacilityGuard => "Guardia", 
			RoleTypeId.NtfPrivate => "MTF Recluta", 
			RoleTypeId.NtfSergeant => "MTF Sargento", 
			RoleTypeId.NtfSpecialist => "MTF Especialista", 
			RoleTypeId.NtfCaptain => "MTF Capitán", 
			RoleTypeId.ChaosConscript => "Chaos Conscripto", 
			RoleTypeId.ChaosRifleman => "Chaos Fusilero", 
			RoleTypeId.ChaosMarauder => "Chaos Merodeador", 
			RoleTypeId.ChaosRepressor => "Chaos Represor", 
			RoleTypeId.Tutorial => "Tutorial", 
			RoleTypeId.Scp049 => "SCP-049", 
			RoleTypeId.Scp0492 => "SCP-049-2", 
			RoleTypeId.Scp096 => "SCP-096", 
			RoleTypeId.Scp106 => "SCP-106", 
			RoleTypeId.Scp173 => "SCP-173", 
			RoleTypeId.Scp939 => "SCP-939", 
			RoleTypeId.Scp3114 => "SCP-3114", 
			_ => role.ToString(), 
		};
	}
}
