using System;
using System.Collections.Generic;
using System.Text;
using CustomPlayerEffects;
using Exiled.API.Features;
using MEC;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class EffectsHUD
{
	private sealed class Notification
	{
		public string Text;

		public int TicksLeft;

		public bool Fijo;
	}

	private sealed class PanelHud
	{
		public string Text;

		public float Hasta;
	}

	private enum EffectCategory
	{
		Good,
		Neutral,
		Bad
	}

	private const float TickRate = 1f;

	private const int MaxEffectLines = 5;

	private const int MaxAvisos = 3;

	private const string ColorGood = "#4CAF50";

	private const string ColorNeutral = "#FFD400";

	private const string ColorBad = "#e32433";

	private static CoroutineHandle _coroutine;

	private static CoroutineHandle _coroutineSucios;

	// jugadores cuya HUD hay que redibujar pronto (se agrupan: como mucho un envío cada 0.3 s por jugador)
	private static readonly HashSet<Player> _sucios = new HashSet<Player>();

	private static readonly List<Player> _listos = new List<Player>();

	private static readonly Dictionary<string, List<Notification>> _notifications = new Dictionary<string, List<Notification>>();

	private static readonly Dictionary<string, float> _pausa = new Dictionary<string, float>();

	private static bool _mostrandoHud;

	private static readonly StringBuilder _sb = new StringBuilder(512);

	private static readonly StringBuilder _effects = new StringBuilder(256);

	private static readonly Dictionary<string, Dictionary<string, PanelHud>> _paneles = new Dictionary<string, Dictionary<string, PanelHud>>();

	private static readonly List<string> _clavesVencidas = new List<string>();

	private const int EfectosArriba = 1260;

	private const int AltoLineaEfectos = 22;

	private const int PanelesAbajo = -290;

	private const int PanelesArriba = 1260;

	private const int AltoLineaPaneles = 24;

	private static readonly StringBuilder _final = new StringBuilder(1024);

	private static readonly StringBuilder _sbArriba = new StringBuilder(512);

	private static readonly List<string> _lineas = new List<string>();

	private const string Separador = "\n<size=8> </size>\n";

	private static readonly string[] OrdenPaneles = new string[12]
	{
		"infoscp", "habilidad", "mtf", "presos", "camaras", "radar", "olfato", "dron", "disfraz", "cura049",
		"bomba", "killfeed"
	};

	private static readonly List<KeyValuePair<string, PanelHud>> _ordenados = new List<KeyValuePair<string, PanelHud>>();

	private static readonly HashSet<string> Ocultos = new HashSet<string> { "FogControl", "SoundtrackMute", "InsufficientLighting", "Scp1344Detected", "PitDeath" };

	private static readonly Dictionary<string, (string Name, EffectCategory Category)> EffectInfo = new Dictionary<string, (string, EffectCategory)>
	{
		["MovementBoost"] = ("Movement Boost", EffectCategory.Good),
		["Invisible"] = ("Invisible", EffectCategory.Good),
		["Invigorated"] = ("Invigorated", EffectCategory.Good),
		["Vitality"] = ("Vitality", EffectCategory.Good),
		["SilentWalk"] = ("Silent Walk", EffectCategory.Good),
		["SpawnProtected"] = ("Spawn Protected", EffectCategory.Good),
		["BodyshotReduction"] = ("Bodyshot Reduction", EffectCategory.Good),
		["DamageReduction"] = ("Damage Reduction", EffectCategory.Good),
		["RainbowTaste"] = ("Rainbow Taste", EffectCategory.Good),
		["Scp1853"] = ("SCP-1853", EffectCategory.Good),
		["AntiScp207"] = ("Anti-Cola", EffectCategory.Good),
		["Scp207"] = ("SCP-207", EffectCategory.Neutral),
		["Scanned"] = ("Scanned", EffectCategory.Neutral),
		["Ghostly"] = ("Ghostly", EffectCategory.Neutral),
		["Scp1576"] = ("SCP-1576", EffectCategory.Neutral),
		["Scp559"] = ("SCP-559", EffectCategory.Neutral),
		["Slowness"] = ("Slowness", EffectCategory.Bad),
		["Exhausted"] = ("Exhausted", EffectCategory.Bad),
		["Asphyxiated"] = ("Asphyxiated", EffectCategory.Bad),
		["AmnesiaVision"] = ("Amnesia", EffectCategory.Bad),
		["AmnesiaItems"] = ("Amnesia", EffectCategory.Bad),
		["Blinded"] = ("Blinded", EffectCategory.Bad),
		["Burned"] = ("Burned", EffectCategory.Bad),
		["CardiacArrest"] = ("Cardiac Arrest", EffectCategory.Bad),
		["Concussed"] = ("Concussed", EffectCategory.Bad),
		["Corroding"] = ("Corroding", EffectCategory.Bad),
		["Deafened"] = ("Deafened", EffectCategory.Bad),
		["Decontaminating"] = ("Decontaminating", EffectCategory.Bad),
		["Disabled"] = ("Disabled", EffectCategory.Bad),
		["Ensnared"] = ("Ensnared", EffectCategory.Bad),
		["Flashed"] = ("Flashed", EffectCategory.Bad),
		["Bleeding"] = ("Bleeding", EffectCategory.Bad),
		["Hypothermia"] = ("Hypothermia", EffectCategory.Bad),
		["Poisoned"] = ("Poisoned", EffectCategory.Bad),
		["PocketCorroding"] = ("Pocket Corroding", EffectCategory.Bad),
		["SeveredHands"] = ("Severed Hands", EffectCategory.Bad),
		["Sinkhole"] = ("Sinkhole", EffectCategory.Bad),
		["SinkHole"] = ("Sinkhole", EffectCategory.Bad),
		["Stained"] = ("Stained", EffectCategory.Bad),
		["Strangled"] = ("Strangled", EffectCategory.Bad),
		["Traumatized"] = ("Traumatized", EffectCategory.Bad),
		["Scp1344"] = ("Visión SCP-1344", EffectCategory.Bad)
	};

	private static bool HintOcupadoGlobal => false;

	public static void Start()
	{
		Stop();
		_coroutine = Timing.RunCoroutine(HudLoop());
		_coroutineSucios = Timing.RunCoroutine(BucleSucios());
		HudRueI.IniciarRefresco();
	}

	public static void Stop()
	{
		Timing.KillCoroutines(_coroutine);
		Timing.KillCoroutines(_coroutineSucios);
		_sucios.Clear();
		HudRL.Limpiar();
		_notifications.Clear();
		_pausa.Clear();
		_paneles.Clear();
	}

	public static void Panel(Player player, string clave, string texto, float duracion)
	{
		if (player == null || string.IsNullOrEmpty(player.UserId) || string.IsNullOrEmpty(clave))
		{
			return;
		}
		if (string.IsNullOrEmpty(texto))
		{
			QuitarPanel(player, clave);
			return;
		}
		if (!_paneles.TryGetValue(player.UserId, out var value))
		{
			value = (_paneles[player.UserId] = new Dictionary<string, PanelHud>(2));
		}
		value[clave] = new PanelHud
		{
			Text = texto,
			Hasta = Time.time + Math.Max(0.1f, duracion)
		};
	}

	public static void QuitarPanel(Player player, string clave)
	{
		if (!(player == null) && !string.IsNullOrEmpty(player.UserId) && _paneles.TryGetValue(player.UserId, out var value) && value.Remove(clave) && value.Count == 0)
		{
			_paneles.Remove(player.UserId);
		}
	}

	public static void Limpiar(Player player)
	{
		if (!(player == null) && !string.IsNullOrEmpty(player.UserId))
		{
			_paneles.Remove(player.UserId);
			_notifications.Remove(player.UserId);
			_pausa.Remove(player.UserId);
		}
	}

	public static void Pausar(Player player, float segundos)
	{
		// [HUD RadioLight] ya no hace falta pausar: la HUD propia junta todos los textos
		return;
		if (!(player == null) && !string.IsNullOrEmpty(player.UserId) && !HudRueI.Disponible)
		{
			float num = Time.time + segundos;
			if (!_pausa.TryGetValue(player.UserId, out var value) || value < num)
			{
				_pausa[player.UserId] = num;
			}
		}
	}

	internal static void OtroHint(Player player, float duracion)
	{
		// hint especial de otro plugin (con efectos) que la HUD no puede dibujar: se lo deja ver un momento
		if (!_mostrandoHud && !(player == null) && !string.IsNullOrEmpty(player.UserId))
		{
			_pausa[player.UserId] = Time.time + Mathf.Clamp(duracion, 0f, 5f);
		}
	}

	public static void Hint(Player player, string text, float duration)
	{
		if (!(player == null))
		{
			Pausar(player, duration);
			player.ShowHint(text, duration);
		}
	}

	public static void Notify(Player player, string text, float duration = 3f)
	{
		if (player == null || string.IsNullOrEmpty(text))
		{
			return;
		}
		if (!_coroutine.IsRunning || HintOcupadoGlobal)
		{
			player.ShowHint(text, duration);
			return;
		}
		if (!_notifications.TryGetValue(player.UserId, out var value))
		{
			value = (_notifications[player.UserId] = new List<Notification>(2));
		}
		int val = Math.Max(1, (int)Math.Ceiling(duration / 1f));
		Notification notification = value.Find((Notification n) => n.Text == text);
		if (notification != null)
		{
			notification.TicksLeft = Math.Max(notification.TicksLeft, val);
			return;
		}
		// [fix] los carteles de subclase ("[ Eres un ... ]" y su descripción) no se pierden cuando llegan otros avisos
		bool fijo = EsCartelSubclase(text);
		if (value.Count >= 5)
		{
			int num = value.FindIndex((Notification n) => !n.Fijo);
			value.RemoveAt((num >= 0) ? num : 0);
		}
		value.Add(new Notification
		{
			Text = text,
			TicksLeft = Math.Max(1, (int)Math.Ceiling(duration / 1f)),
			Fijo = fijo
		});
	}

	private static bool EsCartelSubclase(string text)
	{
		try
		{
			string text2 = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty).TrimStart();
			return text2.StartsWith("[");
		}
		catch
		{
			return false;
		}
	}

	private static int _porFrame;

	private static readonly List<Player> _ronda = new List<Player>();

	private static IEnumerator<float> HudLoop()
	{
		while (true)
		{
			if (HintOcupadoGlobal)
			{
				yield return Timing.WaitForSeconds(1f);
				continue;
			}
			float ahora = Time.time;
			_ronda.Clear();
			_ronda.AddRange(Player.List);
			foreach (Player player in _ronda)
			{
				try
				{
					if (!(player == null) && player.IsConnected && !player.IsNPC && player.IsAlive)
					{
						if (!_pausa.TryGetValue(player.UserId, out var hasta))
						{
							goto IL_011c;
						}
						if (!(ahora < hasta))
						{
							_pausa.Remove(player.UserId);
							goto IL_011c;
						}
					}
					goto end_IL_0096;
					IL_011c:
					string hint = BuildHint(player);
					if (hint != null)
					{
						_mostrandoHud = true;
						try
						{
							player.ShowHint(hint, 1.25f);
						}
						finally
						{
							_mostrandoHud = false;
						}
					}
					end_IL_0096:;
				}
				catch (Exception e)
				{
					Log.Error($"[EffectsHUD] Error mostrando HUD a {player?.Nickname}: {e}");
				}
				_sucios.Remove(player);
				// [rendimiento] no armar la HUD de todos en el mismo frame: de a 3 jugadores por frame
				if (++_porFrame >= 3)
				{
					_porFrame = 0;
					yield return Timing.WaitForOneFrame;
				}
			}
			_porFrame = 0;
			yield return Timing.WaitForSeconds(1f);
		}
	}

	/// <summary>[nuevo] Redibuja el HUD de un jugador ya mismo (sin esperar al ciclo de 1 s). No consume la duración de los avisos.</summary>
	public static void RefrescarAhora(Player player)
	{
		if (player != null)
		{
			_sucios.Add(player);
		}
	}

	private static IEnumerator<float> BucleSucios()
	{
		while (true)
		{
			yield return Timing.WaitForSeconds(0.1f);
			if (_sucios.Count == 0)
			{
				continue;
			}
			_listos.Clear();
			foreach (Player suc in _sucios)
			{
				if (suc == null || !suc.IsConnected || HudRL.PuedeEnviar(suc))
				{
					_listos.Add(suc);
				}
			}
			int hechos = 0;
			foreach (Player p in _listos)
			{
				_sucios.Remove(p);
				if (p != null && p.IsConnected && hechos < 4)
				{
					hechos++;
					RedibujarYa(p);
				}
				else if (p != null && p.IsConnected)
				{
					_sucios.Add(p);
				}
			}
		}
	}

	private static void RedibujarYa(Player player)
	{
		try
		{
			if (player == null || player.IsNPC || !player.IsAlive || HintOcupadoGlobal || (_pausa.TryGetValue(player.UserId, out var value) && Time.time < value))
			{
				return;
			}
			string text = BuildHint(player, contar: false);
			if (text != null)
			{
				_mostrandoHud = true;
				try
				{
					player.ShowHint(text, 1.25f);
				}
				finally
				{
					_mostrandoHud = false;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[EffectsHUD] RefrescarAhora: " + ex.Message);
		}
	}

	/// <summary>Deja como mucho <paramref name="max"/> líneas (las primeras o las últimas).</summary>
	private static string Recortar(string texto, int max, bool quedarseConUltimas)
	{
		if (string.IsNullOrEmpty(texto))
		{
			return texto;
		}
		string[] array = texto.Split('\n');
		if (array.Length <= max)
		{
			return texto;
		}
		return quedarseConUltimas ? string.Join("\n", array, array.Length - max, max) : string.Join("\n", array, 0, max);
	}

	private static string BuildHint(Player player, bool contar = true)
	{
		_sb.Clear();
		_sbArriba.Clear();
		bool flag = false;
		bool flag2 = false;
		if (_notifications.TryGetValue(player.UserId, out var value))
		{
			for (int i = 0; i < value.Count; i++)
			{
				if (flag)
				{
					_sb.Append("\n<size=8> </size>\n");
				}
				_sb.Append(value[i].Text);
				flag = true;
			}
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (contar && --value[num].TicksLeft <= 0)
				{
					value.RemoveAt(num);
				}
			}
			if (value.Count == 0)
			{
				_notifications.Remove(player.UserId);
			}
		}
		string listaScp = null;
		string equipoScp = null;
		if (_paneles.TryGetValue(player.UserId, out var value2))
		{
			float time = Time.time;
			_clavesVencidas.Clear();
			foreach (KeyValuePair<string, PanelHud> item in Ordenados(value2))
			{
				if (time >= item.Value.Hasta)
				{
					_clavesVencidas.Add(item.Key);
					continue;
				}
				bool flag3 = true;
				// la lista de SCPs aliados (ScpOverlay) va en su propia zona, abajo a la izquierda
				if (item.Key == "externo")
				{
					listaScp = item.Value.Text;
					continue;
				}
				if (item.Key == "equiposcp")
				{
					equipoScp = item.Value.Text;
					continue;
				}
				if (flag2)
				{
					_sbArriba.Append("\n<size=8> </size>\n");
				}
				_sbArriba.Append(item.Value.Text);
				flag2 = true;
			}
			foreach (string clavesVencida in _clavesVencidas)
			{
				value2.Remove(clavesVencida);
			}
			if (value2.Count == 0)
			{
				_paneles.Remove(player.UserId);
			}
		}
		if (equipoScp != null)
		{
			// la lista propia (InfoScp) ya trae título y todo: reemplaza a la de ScpOverlay
			listaScp = equipoScp;
		}
		bool flag4 = BuildEffects(player);
		// [HUD RadioLight] todo se dibuja con la HUD propia (zonas fijas, líneas cortadas, sin RueI)
		string efectos = (flag4 ? ("<color=#C2D0E6><b>EFECTOS</b></color>\n" + _effects.ToString()) : null);
		string paneles = (flag2 ? _sbArriba.ToString() : null);
		string avisos = (flag ? _sb.ToString() : null);
		HudRL.Render(player, efectos, paneles, avisos, listaScp);
		return null;
	}

	internal static string Envolver(string texto, int max)
	{
		if (string.IsNullOrEmpty(texto))
		{
			return texto;
		}
		StringBuilder stringBuilder = new StringBuilder(texto.Length + 16);
		string[] array = texto.Split(new char[1] { '\n' });
		foreach (string text in array)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			int num = 0;
			int num2 = -1;
			int num3 = stringBuilder.Length;
			bool flag = false;
			string text2 = text;
			foreach (char c in text2)
			{
				stringBuilder.Append(c);
				if (c == '<')
				{
					flag = true;
					continue;
				}
				if (flag)
				{
					if (c == '>')
					{
						flag = false;
					}
					continue;
				}
				num++;
				if (c == ' ')
				{
					num2 = stringBuilder.Length - 1;
				}
				if (num > max && num2 > num3)
				{
					stringBuilder[num2] = '\n';
					num3 = num2 + 1;
					num = stringBuilder.Length - num3 - ContarEtiquetas(stringBuilder, num3);
					num2 = -1;
				}
			}
		}
		return stringBuilder.ToString();
	}

	private static int ContarEtiquetas(StringBuilder sb, int desde)
	{
		int num = 0;
		bool flag = false;
		for (int i = desde; i < sb.Length; i++)
		{
			char c = sb[i];
			if (c == '<')
			{
				flag = true;
			}
			if (flag)
			{
				num++;
			}
			if (c == '>')
			{
				flag = false;
			}
		}
		return num;
	}

	private static List<KeyValuePair<string, PanelHud>> Ordenados(Dictionary<string, PanelHud> paneles)
	{
		_ordenados.Clear();
		_ordenados.AddRange(paneles);
		_ordenados.Sort((KeyValuePair<string, PanelHud> a, KeyValuePair<string, PanelHud> b) => Indice(a.Key).CompareTo(Indice(b.Key)));
		return _ordenados;
	}

	private static int Indice(string clave)
	{
		int num = Array.IndexOf(OrdenPaneles, clave);
		return (num < 0) ? OrdenPaneles.Length : num;
	}

	private static bool BuildEffects(Player player)
	{
		_effects.Clear();
		int num = 0;
		foreach (StatusEffectBase activeEffect in player.ActiveEffects)
		{
			if (activeEffect == null || !activeEffect.IsEnabled)
			{
				continue;
			}
			string name = activeEffect.GetType().Name;
			if (Ocultos.Contains(name))
			{
				continue;
			}
			num++;
			if (num <= 5)
			{
				if (!EffectInfo.TryGetValue(name, out (string, EffectCategory) value))
				{
					value = (name, EffectCategory.Neutral);
				}
				string value2 = value.Item2 switch
				{
					EffectCategory.Good => "#4CAF50", 
					EffectCategory.Bad => "#e32433", 
					_ => "#FFD400", 
				};
				if (_effects.Length > 0)
				{
					_effects.Append('\n');
				}
				_effects.Append("<color=").Append(value2).Append('>')
					.Append(value.Item1);
				if (activeEffect.Intensity > 1)
				{
					_effects.Append(' ').Append(activeEffect.Intensity);
				}
				if (activeEffect.TimeLeft > 0f)
				{
					_effects.Append(" (").Append((int)Math.Ceiling(activeEffect.TimeLeft)).Append("s)");
				}
				_effects.Append("</color>");
			}
		}
		if (num > 5)
		{
			_effects.Append("\n<color=#AAAAAA>...</color>");
		}
		return num > 0;
	}
}
