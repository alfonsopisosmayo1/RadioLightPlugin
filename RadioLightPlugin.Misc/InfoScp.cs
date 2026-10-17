using System;
using System.Collections.Generic;
using System.Text;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class InfoScp
{
	private static CoroutineHandle _bucle;

	public static void Iniciar()
	{
		Timing.KillCoroutines(_bucle);
		_bucle = Timing.RunCoroutine(Bucle());
	}

	public static void Detener()
	{
		Timing.KillCoroutines(_bucle);
	}

	private static IEnumerator<float> Bucle()
	{
		StringBuilder sb = new StringBuilder(128);
		while (true)
		{
			try
			{
				ArmarEquipo();
			}
			catch (Exception e0)
			{
				Log.Debug("[InfoScp] equipo: " + e0.Message);
			}
			foreach (Player p in Player.List)
			{
				try
				{
					if (!p.IsAlive || p.IsNPC)
					{
						continue;
					}
					string texto = null;
					sb.Clear();
					Role role = p.Role;
					Role role2 = role;
					if (!(role2 is Scp079Role r))
					{
						if (!(role2 is Scp106Role r2))
						{
							if (role2 is Exiled.API.Features.Roles.Scp096Role r3)
							{
								texto = r3.RageState switch
								{
									Scp096RageState.Enraged => "<color=#e32433><b>\ud83d\ude21 FURIOSO</b></color>", 
									Scp096RageState.Distressed => "<color=#FF8E00><b>\ud83d\ude23 ENFURECIÉNDOSE...</b></color>", 
									Scp096RageState.Calming => "<color=#FFD400><b>\ud83d\ude2e\u200d\ud83d\udca8 CALMÁNDOSE</b></color>", 
									_ => "<color=#AAAAAA>\ud83d\ude22 Calmado</color>", 
								};
							}
						}
						else
						{
							float vigor = ((r2.Vigor <= 1.01f) ? (r2.Vigor * 100f) : r2.Vigor);
							sb.Append("<color=#8D6E63><b>Energía:</b> ").Append(Mathf.FloorToInt(vigor)).Append("%</color> ")
								.Append(Barra(vigor / 100f, "#8D6E63"));
							texto = sb.ToString();
						}
					}
					else
					{
						sb.Append("<color=#6A00FF><b>SCP-079</b></color>\n").Append("<color=#B39DDB>Energía: ").Append(Mathf.FloorToInt(r.Energy))
							.Append('/')
							.Append(Mathf.FloorToInt(r.MaxEnergy))
							.Append("</color>\n")
							.Append("<color=#B39DDB>Nivel: ")
							.Append(r.Level)
							.Append("</color>\n")
							.Append(Generadores());
						texto = sb.ToString();
					}
					// [cambio] ya no hay panel aparte del 079 (cámara), 106 (energía) ni 096 (carita): todo eso está en la lista de SCPs
					EffectsHUD.QuitarPanel(p, "infoscp");
					// lo que el resto del equipo SCP tiene que saber (energía del 079 y su cámara, energía del 106, estado del 096)
					string equipo = (p.Role.Team == Team.SCPs) ? TextoEquipo(p) : null;
					if (equipo != null)
					{
						EffectsHUD.Panel(p, "equiposcp", equipo, 3f);
					}
					else
					{
						EffectsHUD.QuitarPanel(p, "equiposcp");
					}
					string hab = Habilidades.Nombre(p);
					string extra = Util.TextoCooldowns(p);
					string panelHab = null;
					if (hab != null)
					{
						float falta = Habilidades.Restante(p);
						string tecla = ((p.Role.Type == RoleTypeId.Scp079) ? "PING" : (Habilidades.UsaTecla(p) ? "TU TECLA" : "ALT"));
						string estado = ((falta <= 0f) ? "<color=#4CAF50>lista</color>" : $"<color=#AAAAAA>{Mathf.CeilToInt(falta)} s</color>");
						panelHab = "<size=20><color=#FFD400>[" + tecla + "]</color> <color=#C2D0E6>" + hab + ":</color> " + estado + "</size>";
					}
					if (extra != null)
					{
						panelHab = ((panelHab == null) ? extra : (panelHab + "\n" + extra));
					}
					if (panelHab != null)
					{
						EffectsHUD.Panel(p, "habilidad", panelHab, 3f);
					}
					else
					{
						EffectsHUD.QuitarPanel(p, "habilidad");
					}
				}
				catch (Exception e)
				{
					Log.Debug("[InfoScp] " + p?.Nickname + ": " + e.Message);
				}
			}
			yield return Timing.WaitForSeconds(1f);
		}
	}

	private sealed class EntradaScp
	{
		public Player P;

		public string Texto;

		public bool Es079;

		public Vector3 Pos;
	}

	private static readonly List<EntradaScp> _lista = new List<EntradaScp>();

	private const string Titulo = "<color=#2C378A>L</color><color=#34408F>i</color><color=#3C4995>s</color><color=#44529A>t</color><color=#4C5BA0>a</color> <color=#5C6EAB>S</color><color=#6477B0>C</color><color=#6C80B6>P</color><color=#7489BB>s</color><color=#7C92C1>:</color>";

	/// <summary>
	/// Lista de SCPs aliados hecha por RadioLight (ya no depende de ScpOverlay/RueI): vida, escudo, ubicación,
	/// y lo importante de cada uno (energía y cámara del 079, energía del 106, estado del 096, zombis del 049).
	/// </summary>
	private static void ArmarEquipo()
	{
		_lista.Clear();
		int num = 0;
		foreach (Player item in Player.List)
		{
			if (!(item == null) && item.IsAlive && item.Role.Type == RoleTypeId.Scp0492)
			{
				num++;
			}
		}
		StringBuilder stringBuilder = new StringBuilder(96);
		foreach (Player item2 in Player.List)
		{
			if (item2 == null || item2.IsNPC || !item2.IsAlive || item2.Role.Team != Team.SCPs || item2.Role.Type == RoleTypeId.Scp0492)
			{
				continue;
			}
			stringBuilder.Clear();
			Role role = item2.Role;
			stringBuilder.Append("<color=#EC2121>").Append(item2.Role.Type.ToString().Replace("Scp", "SCP-")).Append("</color> ");
			bool es = false;
			if (role is Scp079Role scp079Role)
			{
				es = true;
				stringBuilder.Append("<color=#B39DDB>⚡").Append(Mathf.FloorToInt(scp079Role.Energy)).Append('/').Append(Mathf.FloorToInt(scp079Role.MaxEnergy))
					.Append(" Nv")
					.Append(scp079Role.Level)
					.Append(" \ud83d\udcf7")
					.Append(Sala(scp079Role.Camera?.Room))
					.Append(" ⚙")
					.Append(GeneradoresListos())
					.Append("</color>");
			}
			else
			{
				stringBuilder.Append("<color=#0bfc03>\ud83d\udc9a").Append(Mathf.CeilToInt(item2.Health)).Append('/').Append(Mathf.CeilToInt(item2.MaxHealth))
					.Append("</color>");
				if (item2.MaxHumeShield > 0f)
				{
					stringBuilder.Append(" <color=#00c2f2>\ud83d\udc99").Append(Mathf.CeilToInt(item2.HumeShield)).Append('/').Append(Mathf.CeilToInt(item2.MaxHumeShield))
						.Append("</color>");
				}
				if (role is Scp106Role scp106Role)
				{
					float num2 = ((scp106Role.Vigor <= 1.01f) ? (scp106Role.Vigor * 100f) : scp106Role.Vigor);
					stringBuilder.Append(" <color=#A1887F>\ud83c\udf11").Append(Mathf.FloorToInt(num2)).Append("%</color>");
				}
				else if (role is Exiled.API.Features.Roles.Scp096Role scp096Role)
				{
					stringBuilder.Append(scp096Role.RageState switch
					{
						Scp096RageState.Enraged => " <color=#e32433><b>\ud83d\ude21FURIA</b></color>", 
						Scp096RageState.Distressed => " <color=#FF8E00>\ud83d\ude23</color>", 
						Scp096RageState.Calming => " <color=#FFD400>\ud83d\ude2e</color>", 
						_ => " <color=#AAAAAA>\ud83d\ude22</color>", 
					});
				}
				else if (item2.Role.Type == RoleTypeId.Scp049)
				{
					stringBuilder.Append(" <color=#ff0000>\ud83d\udc65").Append(num).Append("</color>");
				}
			}
			_lista.Add(new EntradaScp
			{
				P = item2,
				Texto = stringBuilder.ToString(),
				Es079 = es,
				Pos = item2.Position
			});
		}
	}

	/// <summary>
	/// La lista que ve <paramref name="quien"/>: primero él mismo (marcado con ▸, así la lista se ve aunque sea el único SCP)
	/// y después el resto, con la distancia o zona de cada uno.
	/// </summary>
	private static string TextoEquipo(Player quien)
	{
		if (_lista.Count == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(400);
		stringBuilder.Append(Titulo);
		Vector3 position = quien.Position;
		foreach (EntradaScp item in _lista)
		{
			if (item.P == quien)
			{
				stringBuilder.Append("\n<color=#FFFFFF>▸</color>").Append(item.Texto);
			}
		}
		foreach (EntradaScp item in _lista)
		{
			if (item.P == quien)
			{
				continue;
			}
			stringBuilder.Append('\n').Append(item.Texto);
			if (!item.Es079)
			{
				float magnitude = (item.Pos - position).magnitude;
				stringBuilder.Append(" <color=#8800ff>\ud83d\udccf").Append((magnitude <= 30f) ? (Mathf.RoundToInt(magnitude) + "m") : Zona(item.P)).Append("</color>");
			}
		}
		return stringBuilder.ToString();
	}

	private static string Zona(Player p)
	{
		return p.Zone switch
		{
			ZoneType.LightContainment => "LCZ", 
			ZoneType.HeavyContainment => "HCZ", 
			ZoneType.Entrance => "EZ", 
			ZoneType.Surface => "Surf", 
			ZoneType.Pocket => "Dim.", 
			_ => "?", 
		};
	}

	private static string Sala(Room sala)
	{
		if (sala == null)
		{
			return "?";
		}
		string text = sala.Type.ToString();
		if (text.StartsWith("Hcz"))
		{
			return "HCZ " + text.Substring(3);
		}
		if (text.StartsWith("Lcz"))
		{
			return "LCZ " + text.Substring(3);
		}
		if (text.StartsWith("Ez"))
		{
			return "EZ " + text.Substring(2);
		}
		if (text.StartsWith("Surface"))
		{
			return "Surf";
		}
		return text;
	}

	private static string BarraChica(float valor, string color)
	{
		int num = Mathf.Clamp(Mathf.RoundToInt(valor * 6f), 0, 6);
		return "<color=" + color + ">" + new string('▮', num) + "</color><color=#444444>" + new string('▮', 6 - num) + "</color>";
	}

	private static string GeneradoresListos()
	{
		int num = 0;
		int num2 = 0;
		foreach (Generator item in Generator.List)
		{
			num2++;
			if (item.IsEngaged)
			{
				num++;
			}
		}
		return num + "/" + ((num2 == 0) ? 3 : num2);
	}

	private static string Generadores()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (Generator item in Generator.List)
		{
			num3++;
			if (item.IsEngaged)
			{
				num++;
			}
			else if (item.IsActivating)
			{
				num2++;
			}
		}
		if (num3 == 0)
		{
			num3 = 3;
		}
		string arg = ((num2 > 0) ? $" <color=#e32433>+{num2}</color>" : string.Empty);
		return $"<color=#B39DDB>Generadores: {num}/{num3}</color>{arg}";
	}

	private static string Barra(float valor, string color)
	{
		int num = Mathf.Clamp(Mathf.RoundToInt(valor * 10f), 0, 10);
		return "<color=" + color + ">" + new string('█', num) + "</color><color=#444444>" + new string('█', 10 - num) + "</color>";
	}
}
