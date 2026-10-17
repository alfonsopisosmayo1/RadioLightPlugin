using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Exiled.API.Features;
using Hints;
using MEC;
using RueI.API;
using RueI.API.Elements;
using RueI.API.Elements.Enums;
using UnityEngine;

namespace RadioLightPlugin.Misc;

internal static class HudRueI
{
	private sealed class UltimoHud
	{
		public string Efectos;

		public string Arriba;

		public string Abajo;

		public float Enviado;
	}

	private static class Impl
	{
		private static readonly Tag TagEfectos = new Tag("radiolight_efectos");

		private static readonly Tag TagArriba = new Tag("radiolight_paneles_scp");

		private static readonly Tag TagAbajo = new Tag("radiolight_avisos");

		private static readonly Tag TagHint = new Tag("radiolight_hint_general");

		private static readonly Tag TagRespawn = new Tag("radiolight_timer_respawn");

		private static readonly Tag TagKillFeed = new Tag("radiolight_killfeed");

		private static readonly Dictionary<Player, float> _vence = new Dictionary<Player, float>();

		private static readonly List<Player> _vencidos = new List<Player>();

		internal static readonly Dictionary<Player, string> UltimoHint = new Dictionary<Player, string>();

		private const float PosRespawn = 785f;

		private const float PosHintNormal = 352.8f;

		private static readonly Tag TagPanelLobby = new Tag("radiolight_panel_lobby");

		private const float VidaPanelLobby = 2.5f;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void KillFeed(Player player, string texto, float duracion)
		{
			RueDisplay rueDisplay = RueDisplay.Get(player.ReferenceHub);
			if (string.IsNullOrEmpty(texto) || duracion <= 0f)
			{
				rueDisplay.Remove(TagKillFeed);
				rueDisplay.Update();
				return;
			}
			rueDisplay.Show(TagKillFeed, new BasicElement(730f, "<align=right><size=19>" + SinLlaves(texto) + "</size></align>")
			{
				VerticalAlign = VerticalAlign.Down,
				ZIndex = 1000
			}, duracion);
			Programar(player, duracion);
		}

		internal static void Programar(Player player, float duracion)
		{
			if (!(player == null) && !(duracion <= 0f))
			{
				float num = Time.timeSinceLevelLoad + duracion + 0.05f;
				if (!_vence.TryGetValue(player, out var value) || num > value)
				{
					_vence[player] = num;
				}
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void Refrescar()
		{
			if (_vence.Count == 0)
			{
				return;
			}
			float timeSinceLevelLoad = Time.timeSinceLevelLoad;
			_vencidos.Clear();
			foreach (KeyValuePair<Player, float> item in _vence)
			{
				if (item.Value <= timeSinceLevelLoad)
				{
					_vencidos.Add(item.Key);
				}
			}
			foreach (Player vencido in _vencidos)
			{
				_vence.Remove(vencido);
				try
				{
					if (vencido?.ReferenceHub != null && vencido.IsConnected)
					{
						RueDisplay.Get(vencido.ReferenceHub).Update();
					}
				}
				catch
				{
				}
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Respawn(Player player, string texto, float duracion)
		{
			RueDisplay rueDisplay = RueDisplay.Get(player.ReferenceHub);
			rueDisplay.Show(TagRespawn, new BasicElement(785f, "<align=center>" + SinLlaves(texto) + "</align>")
			{
				VerticalAlign = VerticalAlign.Down,
				ZIndex = 1000
			}, duracion);
			Programar(player, duracion);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void PanelLobby(Player player, string texto, float duracion)
		{
			RueDisplay rueDisplay = RueDisplay.Get(player.ReferenceHub);
			rueDisplay.Show(TagPanelLobby, new BasicElement(352.8f, SinLlaves(texto))
			{
				VerticalAlign = VerticalAlign.Center,
				ZIndex = 999
			}, Math.Max(duracion, 2.5f));
			Programar(player, Math.Max(duracion, 2.5f));
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Hint(Player player, string texto, float duracion)
		{
			RueDisplay rueDisplay = RueDisplay.Get(player.ReferenceHub);
			if (string.IsNullOrWhiteSpace(texto) || duracion <= 0f)
			{
				UltimoHint.Remove(player);
				rueDisplay.Remove(TagHint);
				rueDisplay.Update();
				return;
			}
			UltimoHint[player] = texto;
			texto = Ordenar(texto);
			if (texto.Length == 0)
			{
				rueDisplay.Remove(TagHint);
				rueDisplay.Update();
				return;
			}
			rueDisplay.Show(TagHint, new BasicElement(352.8f, SinLlaves(texto))
			{
				VerticalAlign = VerticalAlign.Center,
				ZIndex = 999
			}, duracion);
			Programar(player, duracion);
		}

		internal static string Ordenar(string texto)
		{
			texto = texto.Replace("\r", "").Trim('\n', ' ');
			while (texto.Contains("\n\n\n"))
			{
				texto = texto.Replace("\n\n\n", "\n\n");
			}
			return EffectsHUD.Envolver(texto, 80);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Mostrar(Player player, string efectos, string arriba, string abajo, float duracion, bool quitarEfectos, bool quitarArriba, bool quitarAbajo)
		{
			RueDisplay rueDisplay = RueDisplay.Get(player.ReferenceHub);
			bool flag = false;
			flag |= Poner(rueDisplay, TagEfectos, efectos, 880f, VerticalAlign.Down, duracion, player, quitarEfectos);
			flag |= Poner(rueDisplay, TagArriba, arriba, 880f, VerticalAlign.Down, duracion, player, quitarArriba);
			// [cambio] avisos anclados por ARRIBA: el primero queda fijo y los nuevos aparecen debajo
			// (antes estaban anclados por abajo y cada aviso nuevo empujaba los anteriores hacia arriba)
			if (flag | Poner(rueDisplay, TagAbajo, abajo, 290f, VerticalAlign.Down, duracion, player, quitarAbajo))
			{
				rueDisplay.Update();
			}
		}

		private static bool Poner(RueDisplay d, Tag tag, string texto, float pos, VerticalAlign alineacion, float duracion, Player player, bool quitar)
		{
			if (string.IsNullOrEmpty(texto) || duracion <= 0f)
			{
				if (!quitar)
				{
					return false;
				}
				d.Remove(tag);
				return true;
			}
			d.Show(tag, new BasicElement(pos, SinLlaves(texto))
			{
				VerticalAlign = alineacion,
				ZIndex = 1000
			}, duracion);
			Programar(player, duracion);
			return false;
		}
	}

	internal const float PosArriba = 880f;

	internal const float PosAbajo = 230f;

	private const int Prioridad = 1000;

	private static bool? _disponible;

	private static readonly Dictionary<int, UltimoHud> _ultimoHud = new Dictionary<int, UltimoHud>();

	private const float RenovarCada = 2.5f;

	private const float VidaMinima = 3.2f;

	private static bool _avisoRespawn;

	internal static readonly HashSet<Player> VieronPanelLobby = new HashSet<Player>();

	private static CoroutineHandle _bucle;

	public static bool Disponible
	{
		get
		{
			if (!_disponible.HasValue)
			{
				try
				{
					_disponible = AppDomain.CurrentDomain.GetAssemblies().Any((Assembly a) => a.GetName().Name == "RueI");
					if (_disponible == true)
					{
						Log.Info("[EffectsHUD] RueI detectado: el HUD se dibuja con RueI (no se pisa con otros plugins).");
					}
				}
				catch
				{
					_disponible = false;
				}
			}
			return _disponible.Value;
		}
	}

	public static bool Mostrar(Player player, string efectos, string arriba, string abajo, float duracion)
	{
		try
		{
			float time = Time.time;
			if (!_ultimoHud.TryGetValue(player.Id, out var value))
			{
				Dictionary<int, UltimoHud> ultimoHud = _ultimoHud;
				int id = player.Id;
				UltimoHud obj = new UltimoHud
				{
					Enviado = -100f
				};
				value = obj;
				ultimoHud[id] = obj;
			}
			if (time - value.Enviado < 2.5f && value.Efectos == efectos && value.Arriba == arriba && value.Abajo == abajo)
			{
				return true;
			}
			bool quitarEfectos = efectos == null && value.Efectos != null;
			bool quitarArriba = arriba == null && value.Arriba != null;
			bool quitarAbajo = abajo == null && value.Abajo != null;
			value.Efectos = efectos;
			value.Arriba = arriba;
			value.Abajo = abajo;
			value.Enviado = time;
			Impl.Mostrar(player, efectos, arriba, abajo, Math.Max(duracion, 3.2f), quitarEfectos, quitarArriba, quitarAbajo);
			return true;
		}
		catch (Exception ex)
		{
			_disponible = false;
			Log.Warn("[EffectsHUD] RueI falló, se vuelve al hint normal: " + ex.Message);
			return false;
		}
	}

	public static bool MostrarHint(Player player, string texto, float duracion)
	{
		try
		{
			Impl.Hint(player, texto, duracion);
			return true;
		}
		catch (Exception ex)
		{
			_disponible = false;
			Log.Warn("[EffectsHUD] RueI falló al mostrar un hint, se vuelve al hint normal: " + ex.Message);
			return false;
		}
	}

	public static bool MostrarPanelLobby(Player player, string texto, float duracion)
	{
		try
		{
			Impl.PanelLobby(player, texto, duracion);
			return true;
		}
		catch (Exception ex)
		{
			Log.Debug("[EffectsHUD] Panel del lobby por RueI: " + ex.Message);
			return false;
		}
	}

	public static void MostrarKillFeed(Player player, string texto, float duracion)
	{
		if (player != null && player.IsAlive)
		{
			HudRL.KillFeed(player, texto, duracion);
			return;
		}
		if (_disponible != true && !Disponible)
		{
			return;
		}
		try
		{
			Impl.KillFeed(player, texto, duracion);
		}
		catch
		{
		}
	}

	public static void MostrarRespawn(Player player, string texto, float duracion)
	{
		try
		{
			Impl.Respawn(player, texto, duracion);
		}
		catch (Exception arg)
		{
			if (!_avisoRespawn)
			{
				_avisoRespawn = true;
				Log.Warn($"[EffectsHUD] Timer de respawn (RueI): {arg}");
			}
		}
	}

	public static void QuitarHintATodos()
	{
		QuitarHintATodos(tambienCrudo: false);
	}

	public static void QuitarHintATodos(bool tambienCrudo)
	{
		foreach (Player item in Player.List)
		{
			if (item?.ReferenceHub == null || item.IsNPC || !VieronPanelLobby.Contains(item))
			{
				continue;
			}
			try
			{
				if (_disponible == true && Impl.UltimoHint.TryGetValue(item, out var value) && EsPanelMvp(value))
				{
					Impl.Hint(item, null, 0f);
				}
				if (tambienCrudo)
				{
					item.HintDisplay.Show(new TextHint(string.Empty, new HintParameter[1]
					{
						new StringHintParameter(string.Empty)
					}, null, 0.1f));
				}
			}
			catch
			{
			}
		}
	}

	internal static string SinLlaves(string texto)
	{
		return (string.IsNullOrEmpty(texto) || (texto.IndexOf('{') < 0 && texto.IndexOf('}') < 0)) ? texto : texto.Replace('{', '(').Replace('}', ')');
	}

	internal static bool EsPanelMvp(string texto)
	{
		return texto != null && (texto.Contains("Rondas jugadas") || texto.Contains("Tiempo jugado") || texto.Contains("Rounds played"));
	}

	public static void IniciarRefresco()
	{
		Timing.KillCoroutines(_bucle);
		_bucle = Timing.RunCoroutine(BucleRefresco());
	}

	private static IEnumerator<float> BucleRefresco()
	{
		while (true)
		{
			yield return Timing.WaitForSeconds(0.2f);
			if (_disponible == true)
			{
				try
				{
					Impl.Refrescar();
				}
				catch (Exception ex)
				{
					Exception e = ex;
					Log.Debug("[EffectsHUD] Refresco RueI: " + e.Message);
				}
			}
		}
	}

	public static void Quitar(Player player)
	{
		if (_disponible != true || player?.ReferenceHub == null || !_ultimoHud.TryGetValue(player.Id, out var value) || (value.Efectos == null && value.Arriba == null && value.Abajo == null))
		{
			return;
		}
		bool quitarEfectos = value.Efectos != null;
		bool quitarArriba = value.Arriba != null;
		bool quitarAbajo = value.Abajo != null;
		value.Efectos = (value.Arriba = (value.Abajo = null));
		value.Enviado = Time.time;
		try
		{
			Impl.Mostrar(player, null, null, null, 0f, quitarEfectos, quitarArriba, quitarAbajo);
		}
		catch
		{
		}
	}
}
