using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class ScpMayores
{
	private const string Rojo = "#EC2121";

	private static void Presentar(Player p, string nombre, string color, string descEs, string descPt)
	{
		Util.Bc(p, 6, "<color=#EC2121>[ " + Util.NombreRol(p.Role.Type) + ": <color=" + color + ">" + nombre + "</color> ]</color>", "<color=#EC2121>[ " + Util.NombreRol(p.Role.Type) + ": <color=" + color + ">" + nombre + "</color> ]</color>");
		bool flag = p.Role.Type == RoleTypeId.Scp079;
		Util.Bc(p, 12, "<color=#EC2121>[ " + descEs + " Se usa con <color=#FFD400>" + (flag ? "el PING (si está lista)" : "[ALT]") + "</color>. ]</color>", "<color=#EC2121>[ " + descPt + " Use com <color=#FFD400>" + (flag ? "o PING (se estiver pronta)" : "[ALT]") + "</color>. ]</color>");
	}

	private static void Ok(Player p, string texto)
	{
		EffectsHUD.Notify(p, "<color=#EC2121><b>" + texto + "</b></color>");
	}

	private static void Falla(Player p, string texto)
	{
		EffectsHUD.Notify(p, "<color=#AAAAAA>" + texto + "</color>", 2f);
	}

	public static void CirujanoSpawn(Player p)
	{
		Presentar(p, "El Cirujano", "#B7C9E2", "Tu anestesia deja a los humanos a 6 m lentos y sordos durante 4 s.", "Sua anestesia deixa os humanos a 6 m lentos e surdos por 4 s.");
		Habilidades.Asignar(p, "Anestesia", 40f, delegate(Player s)
		{
			List<Player> list = Util.HumanosCerca(s, 6f);
			if (list.Count == 0)
			{
				Falla(s, "No hay pacientes cerca.");
				return false;
			}
			foreach (Player item in list)
			{
				item.EnableEffect(EffectType.Slowness, 40, 4f);
				item.EnableEffect(EffectType.Deafened, 4f);
				item.ShowHint("<color=#B7C9E2>Sientes el cuerpo pesado... <i>anestesia</i>.</color>");
			}
			Ok(s, $"Anestesia aplicada a {list.Count}.");
			return true;
		}, linterna: false);
	}

	public static void PastorSpawn(Player p)
	{
		Presentar(p, "Pastor de la Plaga", "#6D704F", "Tu llamado cura el 30% de la vida de TODOS tus zombies y los acelera 10 s.", "Seu chamado cura 30% da vida de TODOS os seus zumbis e os acelera por 10 s.");
		Habilidades.Asignar(p, "Llamado de la plaga", 80f, delegate(Player s)
		{
			int num = 0;
			foreach (Player item in Player.List)
			{
				if (item.IsAlive && item.Role.Type == RoleTypeId.Scp0492)
				{
					Util.Curar(item, item.MaxHealth * 0.3f);
					item.EnableEffect(EffectType.MovementBoost, 15, 10f);
					item.ShowHint("<color=#6D704F>Tu padre te llama. <b>¡Levántate!</b></color>");
					num++;
				}
			}
			if (num == 0)
			{
				Falla(s, "No tienes zombies todavía.");
				return false;
			}
			Ok(s, $"{num} zombies respondieron al llamado.");
			return true;
		}, linterna: false);
	}

	public static void ApagonSpawn(Player p)
	{
		Presentar(p, "Apagón", "#6A00FF", "Cortas la energía de toda la zona de tu cámara durante 15 s.", "Você corta a energia de toda a zona da sua câmera por 15 s.");
		Habilidades.Asignar(p, "Corte de energía", 120f, delegate(Player s)
		{
			ZoneType zoneType = ZonaDeCamara(s);
			if (zoneType == ZoneType.Unspecified || zoneType == ZoneType.Surface)
			{
				Falla(s, "Desde esta cámara no controlas la energía.");
				return false;
			}
			Map.TurnOffAllLights(15f, zoneType);
			foreach (Player item in Player.List)
			{
				if (item.IsHuman && item.Zone == zoneType)
				{
					item.ShowHint("<color=#6A00FF>Las luces se apagan de golpe...</color>");
				}
			}
			Ok(s, $"Apagón en {zoneType}.");
			return true;
		}, linterna: false);
	}

	public static void SobrecargaSpawn(Player p)
	{
		Presentar(p, "Sobrecarga", "#FFD400", "Descargas todas las puertas tesla de la zona de tu cámara a la vez.", "Você descarrega todas as portas tesla da zona da sua câmera de uma vez.");
		Habilidades.Asignar(p, "Sobrecarga tesla", 90f, delegate(Player s)
		{
			ZoneType zoneType = ZonaDeCamara(s);
			int num = 0;
			foreach (Exiled.API.Features.TeslaGate item in Exiled.API.Features.TeslaGate.List)
			{
				if (zoneType == ZoneType.Unspecified || !(item.Room != null) || item.Room.Zone == zoneType)
				{
					item.Trigger();
					num++;
				}
			}
			if (num == 0)
			{
				Falla(s, "No hay teslas en esta zona.");
				return false;
			}
			Ok(s, $"{num} teslas sobrecargadas.");
			return true;
		}, linterna: false);
	}

	private static ZoneType ZonaDeCamara(Player scp079)
	{
		return (scp079.Role is Scp079Role scp079Role && scp079Role.Camera?.Room != null) ? scp079Role.Camera.Room.Zone : ZoneType.Unspecified;
	}

	public static void ColericoSpawn(Player p)
	{
		Presentar(p, "Colérico", "#e32433", "Te recompones: curas 250 de vida y recibes 35% menos daño durante 8 s.", "Você se recompõe: cura 250 de vida e recebe 35% menos dano por 8 s.");
		Habilidades.Asignar(p, "Furia contenida", 70f, delegate(Player s)
		{
			Util.Curar(s, 250f);
			Var.Coraza096[s] = Time.time + 8f;
			Ok(s, "Furia contenida: +250 de vida.");
			return true;
		}, linterna: false);
	}

	public static void EscurridizoSpawn(Player p)
	{
		Presentar(p, "Escurridizo", "#B0B0B0", "Estando calmado, corres muy rápido durante 10 s.", "Estando calmo, você corre muito rápido por 10 s.");
		Habilidades.Asignar(p, "Huida", 100f, delegate(Player s)
		{
			if (s.Role is Exiled.API.Features.Roles.Scp096Role { RageState: not Scp096RageState.Docile })
			{
				Falla(s, "Solo puedes huir estando calmado.");
				return false;
			}
			s.EnableEffect(EffectType.MovementBoost, 80, 10f);
			Ok(s, "¡Huida! Corres muy rápido durante 10 s.");
			return true;
		}, linterna: false);
	}

	public static void CorrosivoSpawn(Player p)
	{
		Presentar(p, "Corrosivo", "#3B2A1A", "Durante 8 s los humanos a 3,5 m reciben 6 de daño por segundo y se ralentizan.", "Por 8 s os humanos a 3,5 m recebem 6 de dano por segundo e ficam lentos.");
		Habilidades.Asignar(p, "Manto corrosivo", 60f, delegate(Player s)
		{
			Var.Coroutines.Add(Timing.RunCoroutine(MantoCorrosivo(s, s.Role.Type)));
			Ok(s, "Manto corrosivo activo.");
			return true;
		}, linterna: false);
	}

	private static IEnumerator<float> MantoCorrosivo(Player s, RoleTypeId rol)
	{
		for (int i = 0; i < 8; i++)
		{
			if (!Util.Vivo(s))
			{
				break;
			}
			if (s.Role.Type != rol)
			{
				break;
			}
			foreach (Player h in Util.HumanosCerca(s, 3.5f))
			{
				h.Hurt(s, 6f, DamageType.Scp106);
				h.EnableEffect(EffectType.Slowness, 30, 1.5f);
			}
			yield return Timing.WaitForSeconds(1f);
		}
	}

	public static void HambrientoSpawn(Player p)
	{
		Presentar(p, "Hambriento", "#8B0000", "Durante 10 s eres más rápido y tus ataques hacen +50% de daño.", "Por 10 s você fica mais rápido e seus ataques causam +50% de dano.");
		Habilidades.Asignar(p, "Hambre", 75f, delegate(Player s)
		{
			s.EnableEffect(EffectType.MovementBoost, 20, 10f);
			Var.Hambre106[s] = Time.time + 10f;
			Ok(s, "¡Hambre!");
			return true;
		}, linterna: false);
	}

	public static void AcorazadoSpawn(Player p)
	{
		Presentar(p, "Acorazado", "#7A7A7A", "Te cubres de piedra: ganas 300 de escudo que se va gastando.", "Você se cobre de pedra: ganha 300 de escudo que vai se esgotando.");
		Habilidades.Asignar(p, "Coraza de piedra", 80f, delegate(Player s)
		{
			s.AddAhp(300f, 300f, 10f, 0.7f, 5f);
			Ok(s, "Coraza de piedra: +300 de escudo.");
			return true;
		}, linterna: false);
	}

	public static void GargolaSpawn(Player p)
	{
		Presentar(p, "Gárgola", "#9E9E9E", "Petrificas a los humanos a 8 m: no se pueden mover durante 2,5 s.", "Você petrifica os humanos a 8 m: não conseguem se mover por 2,5 s.");
		Habilidades.Asignar(p, "Petrificar", 50f, delegate(Player s)
		{
			List<Player> list = Util.HumanosCerca(s, 8f);
			if (list.Count == 0)
			{
				Falla(s, "No hay nadie cerca.");
				return false;
			}
			foreach (Player item in list)
			{
				item.EnableEffect(EffectType.Ensnared, 2.5f);
				item.ShowHint("<color=#9E9E9E>Tus piernas se vuelven de piedra...</color>", 2.5f);
			}
			Ok(s, $"{list.Count} petrificados.");
			return true;
		}, linterna: false);
	}

	public static void AulladorSpawn(Player p)
	{
		Presentar(p, "Aullador", "#B23D37", "Tu aullido deja sordos 6 s y sin aire 5 s a los humanos a 12 m.", "Seu uivo deixa surdos 6 s e sem fôlego 5 s os humanos a 12 m.");
		Habilidades.Asignar(p, "Aullido", 50f, delegate(Player s)
		{
			List<Player> list = Util.HumanosCerca(s, 12f);
			foreach (Player item in list)
			{
				item.EnableEffect(EffectType.Deafened, 6f);
				item.EnableEffect(EffectType.Exhausted, 5f);
				item.ShowHint("<color=#B23D37>Un aullido te atraviesa los oídos.</color>");
			}
			Ok(s, (list.Count == 0) ? "Aullido (nadie lo oyó)." : $"Aullido: {list.Count} aturdidos.");
			return true;
		}, linterna: false);
	}

	public static void RastreadorSpawn(Player p)
	{
		Presentar(p, "Rastreador", "#C9A227", "Olfateas durante 6 s: ves dirección y distancia de los humanos a 35 m.", "Você fareja por 6 s: vê direção e distância dos humanos a 35 m.");
		Habilidades.Asignar(p, "Olfato", 60f, delegate(Player s)
		{
			Var.Coroutines.Add(Timing.RunCoroutine(Olfato(s, s.Role.Type)));
			return true;
		}, linterna: false);
	}

	private static IEnumerator<float> Olfato(Player s, RoleTypeId rol)
	{
		StringBuilder sb = new StringBuilder(256);
		for (int i = 0; i < 6; i++)
		{
			if (!Util.Vivo(s) || s.Role.Type != rol)
			{
				yield break;
			}
			sb.Clear();
			List<Player> presas = Util.HumanosCerca(s, 35f);
			presas.Sort((Player a, Player b) => (a.Position - s.Position).sqrMagnitude.CompareTo((b.Position - s.Position).sqrMagnitude));
			foreach (Player h in presas.Take(6))
			{
				sb.Append("<color=#C9A227>").Append(Util.Flecha(s, h.Position)).Append(' ')
					.Append((int)Vector3.Distance(h.Position, s.Position))
					.Append(" m</color>\n");
			}
			EffectsHUD.Panel(s, "olfato", "<color=#B23D37><b>OLFATO</b></color>\n" + ((sb.Length == 0) ? "<color=#AAAAAA>Ningún rastro.</color>" : sb.ToString().TrimEnd(new char[1] { '\n' })), 1.5f);
			yield return Timing.WaitForSeconds(1f);
		}
		EffectsHUD.QuitarPanel(s, "olfato");
	}

	public static void TitiriteroSpawn(Player p)
	{
		Presentar(p, "Titiritero", "#E8E0C8", "Los humanos a 5 m quedan inmovilizados 1 s y torpes 4 s.", "Os humanos a 5 m ficam imobilizados 1 s e desajeitados 4 s.");
		Habilidades.Asignar(p, "Hueso roto", 45f, delegate(Player s)
		{
			List<Player> list = Util.HumanosCerca(s, 5f);
			if (list.Count == 0)
			{
				Falla(s, "No hay nadie a tu alcance.");
				return false;
			}
			foreach (Player item in list)
			{
				item.EnableEffect(EffectType.Ensnared, 1f);
				item.EnableEffect(EffectType.Disabled, 4f);
				item.ShowHint("<color=#E8E0C8>*crack*</color>", 2f);
			}
			Ok(s, $"{list.Count} con huesos rotos.");
			return true;
		}, linterna: false);
	}

	public static void EspectroSpawn(Player p)
	{
		Presentar(p, "Espectro", "#57e5f7", "Te desvaneces: 5 s invisible y en silencio.", "Você desaparece: 5 s invisível e em silêncio.");
		Habilidades.Asignar(p, "Desvanecer", 70f, delegate(Player s)
		{
			s.EnableEffect(EffectType.Invisible, 5f);
			s.EnableEffect(EffectType.SilentWalk, byte.MaxValue, 5f);
			Ok(s, "Te desvaneces...");
			return true;
		}, linterna: false);
	}

	private static Room SalaVacia(ZoneType zona, Room actual)
	{
		List<Room> list = new List<Room>();
		foreach (Room item in Room.List)
		{
			if (!(item == null) && !(item == actual) && item.Zone == zona && EsPasillo(item) && !item.Players.Any((Player x) => x.IsHuman))
			{
				list.Add(item);
			}
		}
		return (list.Count == 0) ? null : list[Var.random.Next(list.Count)];
	}

	private static bool EsPasillo(Room r)
	{
		string text = r.Type.ToString();
		return text.Contains("Straight") || text.Contains("Curve") || text.Contains("Crossing") || text.Contains("TCross") || text.Contains("Intersection") || text.Contains("Corner");
	}

	public static void Asignar(Player p)
	{
		int num = Var.random.Next(100);
		bool flag = num < 20;
		bool flag2 = num >= 20 && num < 40;
		if (p.Role.Type == RoleTypeId.Scp173 && num >= 60 && num < 75)
		{
			Extra.ManiExplosivoSpawn(p);
		}
		else
		{
			if (num >= 60)
			{
				return;
			}
			if (num >= 40)
			{
				switch (p.Role.Type)
				{
				case RoleTypeId.Scp049:
					Extra.ResucitadorSpawn(p);
					break;
				case RoleTypeId.Scp079:
					Extra.InterferenciaSpawn(p);
					break;
				case RoleTypeId.Scp096:
					Extra.HipnotizadorSpawn(p);
					break;
				case RoleTypeId.Scp106:
					Extra.HundidorSpawn(p);
					break;
				case RoleTypeId.Scp173:
					Extra.AcechadorSpawn(p);
					break;
				case RoleTypeId.Scp939:
					Extra.SaltadorSpawn(p);
					break;
				case RoleTypeId.Scp3114:
					Extra.CarroneroSpawn(p);
					break;
				}
				return;
			}
			switch (p.Role.Type)
			{
			case RoleTypeId.Scp049:
				if (flag)
				{
					CirujanoSpawn(p);
				}
				else
				{
					PastorSpawn(p);
				}
				break;
			case RoleTypeId.Scp079:
				if (flag)
				{
					ApagonSpawn(p);
				}
				else
				{
					SobrecargaSpawn(p);
				}
				break;
			case RoleTypeId.Scp096:
				if (flag)
				{
					ColericoSpawn(p);
				}
				else
				{
					EscurridizoSpawn(p);
				}
				break;
			case RoleTypeId.Scp106:
				if (flag)
				{
					CorrosivoSpawn(p);
				}
				else
				{
					HambrientoSpawn(p);
				}
				break;
			case RoleTypeId.Scp173:
				if (flag)
				{
					AcorazadoSpawn(p);
				}
				else
				{
					GargolaSpawn(p);
				}
				break;
			case RoleTypeId.Scp939:
				if (flag)
				{
					AulladorSpawn(p);
				}
				else
				{
					RastreadorSpawn(p);
				}
				break;
			case RoleTypeId.Scp3114:
				if (flag)
				{
					TitiriteroSpawn(p);
				}
				else
				{
					EspectroSpawn(p);
				}
				break;
			}
		}
	}
}
