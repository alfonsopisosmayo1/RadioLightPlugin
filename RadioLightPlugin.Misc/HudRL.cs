using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Exiled.API.Features;
using Hints;
using UnityEngine;

namespace RadioLightPlugin.Misc;

/// <summary>
/// HUD RadioLight: compositor propio de TODO lo que ve en pantalla un jugador vivo.
/// Un solo hint por jugador, con 4 zonas fijas que nunca se pisan ni se salen de la pantalla:
///   · arriba-izquierda: paneles (habilidad, cooldowns, cámaras, radar, info SCP...)
///   · arriba-derecha:   efectos activos + kill feed
///   · centro:           avisos/hints generales (de este plugin y de otros plugins)
///   · abajo-izquierda:  notificaciones (carteles de subclase, avisos cortos)
/// Todas las líneas se cortan a mano antes de enviarse (el juego nunca las corta solo, que era lo que
/// hacía que los textos se encimaran) y cada zona tiene un máximo de líneas.
/// No depende de RueI: funciona aunque RueI no esté o falle.
/// </summary>
internal static class HudRL
{
	private sealed class Estado
	{
		public string Hint;

		public float HintHasta;

		public string Kill;

		public float KillHasta;

		public string Enviado;

		public float Cuando;
	}

	// Posiciones verticales (voffset) relativas a la línea base natural de los hints del juego (~35 % desde abajo).
	// La pantalla va más o menos de +1250 (arriba) a -750 (abajo). Cada zona tiene una FRANJA fija y nunca sale de ella:
	//   franja de arriba:  +840 .. +480   (nunca más alto que +840: queda lejos del borde aunque el jugador use escala de UI grande)
	//   franja del centro: +400 .. -60
	//   franja de abajo:   -120 .. -420
	// Entre franjas siempre queda espacio libre, así que es imposible que dos zonas se encimen.
	private const float ArribaTope = 840f;

	private const float ArribaPiso = 480f;

	private const float PasoArriba = 26f;

	private const float CentroTope = 400f;

	private const float CentroPiso = -60f;

	private const float PasoCentro = 31f;

	// avisos / carteles de subclase: subidos un 20 % de la pantalla (antes -120 .. -420) para que queden lejos de las barras de vida
	private const float AbajoTope = 310f;

	private const float AbajoPiso = 10f;

	private const float PasoAbajo = 25f;

	// lista de SCPs (ScpOverlay): la línea de más abajo queda justo encima de las barras de HP/HS/estamina
	// lista de SCPs: a la izquierda, justo debajo de los paneles y ENCIMA de los carteles de subclase (crece hacia abajo)
	private const float ListaTope = 460f;

	private const float PasoLista = 22f;

	private const int MaxListaScp = 9;

	private static readonly int MaxPaneles = Capacidad(ArribaTope, ArribaPiso, PasoArriba);

	private static readonly int MaxDerecha = Capacidad(ArribaTope, ArribaPiso, PasoArriba);

	private static readonly int MaxCentro = Math.Min(6, Capacidad(CentroTope, CentroPiso, PasoCentro));

	private static readonly int MaxAvisos = Capacidad(AbajoTope, AbajoPiso, PasoAbajo);

	private static int Capacidad(float tope, float piso, float paso)
	{
		return Mathf.Max(1, Mathf.FloorToInt((tope - piso) / paso) + 1);
	}

	private const float Renovar = 1.2f;

	private const float DuracionHint = 2.6f;

	private static readonly Dictionary<string, Estado> Estados = new Dictionary<string, Estado>();

	private static readonly StringBuilder Sb = new StringBuilder(2048);

	private static readonly Regex TagsDeLayout = new Regex("</?(size|align|line-height|voffset|pos|indent|margin|mspace|cspace|width|nobr|br)(=[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly HintParameter[] SinParametros = new HintParameter[1]
	{
		new StringHintParameter(string.Empty)
	};

	public static bool Avisado;

	private static Estado Get(Player p)
	{
		string key = p.UserId ?? p.Id.ToString();
		if (!Estados.TryGetValue(key, out var value))
		{
			value = (Estados[key] = new Estado());
		}
		return value;
	}

	/// <summary>Hint general (centro de la pantalla).</summary>
	public static void Hint(Player p, string texto, float duracion)
	{
		if (p == null)
		{
			return;
		}
		Estado estado = Get(p);
		if (string.IsNullOrWhiteSpace(texto) || duracion <= 0f)
		{
			estado.Hint = null;
		}
		else
		{
			estado.Hint = texto;
			estado.HintHasta = Time.time + Mathf.Clamp(duracion, 0.5f, 30f);
		}
		EffectsHUD.RefrescarAhora(p);
	}

	/// <summary>Kill feed (arriba a la derecha, debajo de los efectos).</summary>
	public static void KillFeed(Player p, string texto, float duracion)
	{
		if (p == null)
		{
			return;
		}
		Estado estado = Get(p);
		if (string.IsNullOrEmpty(texto) || duracion <= 0f)
		{
			estado.Kill = null;
		}
		else
		{
			estado.Kill = texto;
			estado.KillHasta = Time.time + Mathf.Clamp(duracion, 0.5f, 30f);
		}
		EffectsHUD.RefrescarAhora(p);
	}

	/// <summary>True si ya pasó el mínimo entre envíos (0.3 s) y se puede redibujar sin saturar la red.</summary>
	public static bool PuedeEnviar(Player p)
	{
		if (p == null)
		{
			return false;
		}
		return !Estados.TryGetValue(p.UserId ?? p.Id.ToString(), out var value) || Time.time - value.Cuando >= 0.5f;
	}

	public static void Olvidar(Player p)
	{
		if (p != null)
		{
			Estados.Remove(p.UserId ?? p.Id.ToString());
		}
	}

	public static void Limpiar()
	{
		Estados.Clear();
	}

	/// <summary>Arma y envía la pantalla completa del jugador. Los textos vienen sin cortar.</summary>
	public static void Render(Player p, string efectos, string paneles, string avisos, string listaScp = null)
	{
		if (p == null || !p.IsConnected)
		{
			return;
		}
		if (!Avisado)
		{
			Avisado = true;
			Log.Info("[HUD RadioLight] HUD propia activa (zonas fijas, sin RueI).");
		}
		Estado estado = Get(p);
		float time = Time.time;
		string texto = ((estado.Hint != null && time < estado.HintHasta) ? estado.Hint : null);
		string texto2 = ((estado.Kill != null && time < estado.KillHasta) ? estado.Kill : null);
		if (texto == null)
		{
			estado.Hint = null;
		}
		if (texto2 == null)
		{
			estado.Kill = null;
		}
		List<string> list = Lineas(paneles, 44, MaxPaneles, quedarseConUltimas: false);
		List<string> list2 = Lineas(efectos, 30, 7, quedarseConUltimas: false);
		if (texto2 != null)
		{
			List<string> list3 = Lineas(texto2, 36, 5, quedarseConUltimas: true);
			if (list2.Count > 0 && list3.Count > 0)
			{
				list2.Add(string.Empty);
			}
			list2.AddRange(list3);
			if (list2.Count > MaxDerecha)
			{
				list2.RemoveRange(0, list2.Count - MaxDerecha);
			}
		}
		List<string> list4 = Lineas(texto, 56, MaxCentro, quedarseConUltimas: false);
		List<string> list6 = Lineas(listaScp, 46, MaxListaScp, quedarseConUltimas: false);
		float num2 = AbajoPiso;
		float topeAvisos = AbajoTope;
		if (list6.Count > 0)
		{
			// los carteles de subclase empiezan debajo de la lista, sin pisarla
			topeAvisos = Mathf.Min(AbajoTope, ListaTope - (float)(list6.Count - 1) * PasoLista - 30f);
		}
		int num3 = Mathf.Max(1, Mathf.FloorToInt((topeAvisos - num2) / PasoAbajo) + 1);
		List<string> list5 = Lineas(avisos, 42, Mathf.Min(MaxAvisos, num3), quedarseConUltimas: true);
		if (list.Count + list2.Count + list4.Count + list5.Count + list6.Count == 0)
		{
			Enviar(p, estado, null);
			return;
		}
		Sb.Clear();
		Sb.Append("<line-height=0>");
		for (int i = 0; i < list.Count; i++)
		{
			Linea(Mathf.Max(ArribaPiso, ArribaTope - (float)i * PasoArriba), "left", 20, list[i]);
		}
		Sb.Append("</b></i></u></color></color></color>");
		for (int j = 0; j < list2.Count; j++)
		{
			Linea(Mathf.Max(ArribaPiso, ArribaTope - (float)j * PasoArriba), "right", 19, list2[j]);
		}
		Sb.Append("</b></i></u></color></color></color>");
		float num = (CentroTope + CentroPiso) / 2f + (float)(list4.Count - 1) * PasoCentro / 2f;
		for (int k = 0; k < list4.Count; k++)
		{
			Linea(Mathf.Clamp(num - (float)k * PasoCentro, CentroPiso, CentroTope), "center", 23, list4[k]);
		}
		Sb.Append("</b></i></u></color></color></color>");
		for (int l = 0; l < list5.Count; l++)
		{
			Linea(Mathf.Max(num2, topeAvisos - (float)l * PasoAbajo), "left", 21, list5[l]);
		}
		Sb.Append("</b></i></u></color></color></color>");
		for (int m = 0; m < list6.Count; m++)
		{
			Linea(ListaTope - (float)m * PasoLista, "left", 17, list6[m]);
		}
		Enviar(p, estado, Sb.ToString());
	}

	// abajo a la izquierda el texto empieza corrido a la derecha, para no quedar encima/debajo de las barras de vida, escudo y estamina
	private const string SangriaAbajo = "20%";

	private static void Linea(float y, string alineacion, int tam, string texto, string sangria = null)
	{
		if (!string.IsNullOrEmpty(texto))
		{
			Sb.Append("<voffset=").Append(y.ToString("0", CultureInfo.InvariantCulture)).Append("><align=")
				.Append(alineacion)
				.Append("><size=")
				.Append(tam)
				.Append('>');
			if (sangria != null)
			{
				Sb.Append("<indent=").Append(sangria).Append('>');
			}
			Sb.Append(texto);
			if (sangria != null)
			{
				Sb.Append("</indent>");
			}
			Sb.Append("</size></align></voffset>\n");
		}
	}

	private static void Enviar(Player p, Estado e, string texto)
	{
		float time = Time.time;
		if (texto == null)
		{
			if (e.Enviado != null)
			{
				e.Enviado = null;
				Mostrar(p, string.Empty, 0.1f);
			}
			return;
		}
		if (texto == e.Enviado && time - e.Cuando < Renovar)
		{
			return;
		}
		e.Enviado = texto;
		e.Cuando = time;
		Mostrar(p, texto, DuracionHint);
	}

	private static void Mostrar(Player p, string texto, float duracion)
	{
		try
		{
			p.HintDisplay?.Show(new TextHint(HudRueI.SinLlaves(texto), SinParametros, null, duracion));
		}
		catch (Exception ex)
		{
			Log.Debug("[HUD RadioLight] " + ex.Message);
		}
	}

	/// <summary>
	/// Limpia etiquetas de posición/tamaño (las pone la HUD), corta cada línea a <paramref name="max"/> letras visibles
	/// y deja como mucho <paramref name="maxLineas"/> líneas.
	/// </summary>
	internal static List<string> Lineas(string texto, int max, int maxLineas, bool quedarseConUltimas)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(texto))
		{
			return list;
		}
		texto = TagsDeLayout.Replace(texto.Replace("\r", string.Empty), string.Empty);
		string text = EffectsHUD.Envolver(texto, max);
		bool flag = false;
		string[] array = text.Split('\n');
		foreach (string text2 in array)
		{
			string text3 = text2.Trim();
			bool flag2 = Visible(text3).Length == 0;
			if (flag2)
			{
				if (list.Count == 0 || flag)
				{
					continue;
				}
				flag = true;
				list.Add(string.Empty);
			}
			else
			{
				flag = false;
				list.Add(Cortar(text3, max + 6));
			}
		}
		while (list.Count > 0 && list[list.Count - 1].Length == 0)
		{
			list.RemoveAt(list.Count - 1);
		}
		if (list.Count > maxLineas)
		{
			if (quedarseConUltimas)
			{
				list.RemoveRange(0, list.Count - maxLineas);
			}
			else
			{
				list.RemoveRange(maxLineas, list.Count - maxLineas);
			}
		}
		return list;
	}

	/// <summary>Si una línea sigue siendo más larga que el máximo (palabras sin espacios), la corta a la fuerza con "…".</summary>
	private static string Cortar(string linea, int max)
	{
		if (Visible(linea).Length <= max)
		{
			return linea;
		}
		StringBuilder stringBuilder = new StringBuilder(linea.Length);
		int num = 0;
		bool flag = false;
		foreach (char c in linea)
		{
			if (c == '<')
			{
				flag = true;
			}
			if (flag)
			{
				stringBuilder.Append(c);
				if (c == '>')
				{
					flag = false;
				}
			}
			else if (num < max - 1)
			{
				stringBuilder.Append(c);
				num++;
			}
		}
		return stringBuilder.Append('…').ToString();
	}

	private static string Visible(string linea)
	{
		if (linea.IndexOf('<') < 0)
		{
			return linea.Trim();
		}
		StringBuilder stringBuilder = new StringBuilder(linea.Length);
		bool flag = false;
		foreach (char c in linea)
		{
			if (c == '<')
			{
				flag = true;
			}
			else if (c == '>')
			{
				flag = false;
			}
			else if (!flag)
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString().Trim();
	}
}
