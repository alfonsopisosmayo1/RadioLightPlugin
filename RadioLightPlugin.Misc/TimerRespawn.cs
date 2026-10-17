using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using MapGeneration.Distributors;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using RueI.API;
using RueI.API.Elements;
using RueI.API.Elements.Enums;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class TimerRespawn
{
	private static class Pantalla
	{
		private static readonly Tag TagArriba = new Tag("radiolight_timer_arriba");

		private static readonly Tag TagPrincipal = new Tag("radiolight_timer_respawn");

		private const float Posicion = 120f;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Mostrar(Player p)
		{
			RueDisplay rueDisplay = RueDisplay.Get(p.ReferenceHub);
			rueDisplay.Show(TagPrincipal, new DynamicElement(120f, () => TextoPrincipal())
			{
				UpdateInterval = TimeSpan.FromSeconds(1.0),
				VerticalAlign = VerticalAlign.Down,
				ZIndex = 1000
			});
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Quitar(Player p)
		{
			RueDisplay rueDisplay = RueDisplay.Get(p.ReferenceHub);
			rueDisplay.Remove(TagArriba);
			rueDisplay.Remove(TagPrincipal);
			rueDisplay.Update();
		}
	}

	private const float SegundosLlegadaCaos = 14f;

	private const float SegundosLlegadaMtf = 18f;

	private static CoroutineHandle _bucle;

	private static float _llegada = -1f;

	private static WaveQueueState _estadoAnterior = WaveQueueState.Idle;

	private static bool _avisado;

	private static bool _primeraVez;

	private static readonly StringBuilder _sb = new StringBuilder(1024);

	private static string _textoCache = string.Empty;

	private static int _cuadroTexto = -1;

	private static readonly List<(int Tam, string Texto)> _lineas = new List<(int, string)>(8);

	private static int _cuadro = -1;

	private const string ColorFondo = "#0A0A0ACC";

	private const float AnchoCuadro = 820f;

	private static string _fondoCache = string.Empty;

	private static int _cuadroFondo = -1;

	public static void Iniciar()
	{
		Timing.KillCoroutines(_bucle);
		_bucle = Timing.RunCoroutine(Bucle());
	}

	public static void Detener()
	{
		Timing.KillCoroutines(_bucle);
		foreach (Player item in Player.List)
		{
			Quitar(item);
		}
	}

	public static void AlCambiarRol(ChangingRoleEventArgs ev)
	{
		if (ev?.Player == null || ev.Player.IsNPC)
		{
			return;
		}
		Player p = ev.Player;
		RoleTypeId newRole = ev.NewRole;
		if ((newRole == RoleTypeId.Spectator || newRole == RoleTypeId.Overwatch) ? true : false)
		{
			Timing.CallDelayed(0.2f, delegate
			{
				Refrescar(p);
			});
		}
		else
		{
			Quitar(p);
		}
	}

	public static void AlTerminarRonda()
	{
		foreach (Player item in Player.List)
		{
			Quitar(item);
		}
	}

	private static void Refrescar(Player p)
	{
		if (p == null || !p.IsConnected)
		{
			return;
		}
		bool flag = Round.IsStarted && !Round.IsEnded && !Var.EventoEnCurso;
		bool flag2 = flag;
		if (flag2)
		{
			RoleTypeId type = p.Role.Type;
			bool flag3 = ((type == RoleTypeId.Spectator || type == RoleTypeId.Overwatch) ? true : false);
			flag2 = flag3;
		}
		bool flag4 = flag2;
		try
		{
			if (flag4 && HudRueI.Disponible)
			{
				Pantalla.Mostrar(p);
				if (!_primeraVez)
				{
					_primeraVez = true;
					Log.Info("[TimerRespawn] Timer de oleadas mostrado a " + p.Nickname + ". Texto: " + DiscordLog.SinEtiquetas(TextoPrincipal()).Replace("\n", " / "));
				}
			}
			else
			{
				Quitar(p);
			}
		}
		catch (Exception arg)
		{
			if (!_avisado)
			{
				_avisado = true;
				Log.Warn($"[TimerRespawn] No se pudo mostrar el timer: {arg}");
			}
		}
	}

	private static void Quitar(Player p)
	{
		if (p?.ReferenceHub == null || !HudRueI.Disponible)
		{
			return;
		}
		try
		{
			Pantalla.Quitar(p);
		}
		catch
		{
		}
	}

	private static IEnumerator<float> Bucle()
	{
		int vuelta = 0;
		while (true)
		{
			yield return Timing.WaitForSeconds(1f);
			try
			{
				WaveQueueState estado = WaveManager.State;
				bool flag = (uint)(estado - 1) <= 1u;
				bool llegando = flag;
				bool flag2 = llegando;
				bool flag3 = flag2;
				if (flag3)
				{
					WaveQueueState estadoAnterior = _estadoAnterior;
					flag = (uint)(estadoAnterior - 1) <= 1u;
					flag3 = !flag;
				}
				if (flag3)
				{
					SpawnableWaveBase nextWave = WaveManager._nextWave;
					_llegada = ((nextWave != null && nextWave.TargetFaction == Faction.FoundationEnemy) ? 14f : 18f);
				}
				else if (llegando && _llegada > 0f)
				{
					_llegada -= 1f;
				}
				else if (!llegando)
				{
					_llegada = -1f;
				}
				_estadoAnterior = estado;
				int num = vuelta + 1;
				vuelta = num;
				if (num % 5 != 0)
				{
					continue;
				}
				foreach (Player p in Player.List)
				{
					bool flag4 = p != null && !p.IsNPC && p.IsConnected;
					bool flag5 = flag4;
					if (flag5)
					{
						RoleTypeId type = p.Role.Type;
						flag = ((type == RoleTypeId.Spectator || type == RoleTypeId.Overwatch) ? true : false);
						flag5 = flag;
					}
					if (flag5)
					{
						Refrescar(p);
					}
				}
			}
			catch (Exception ex)
			{
				Exception e = ex;
				if (!_avisado)
				{
					_avisado = true;
					Log.Warn($"[TimerRespawn] {e}");
				}
			}
		}
	}

	internal static string TextoArriba()
	{
		try
		{
			TimeSpan elapsedTime = Round.ElapsedTime;
			int num = Player.List.Count((Player x) => x != null && !x.IsNPC && x.Role.Type == RoleTypeId.Spectator);
			return $"<size=18><color=#888888>Ronda: <b>{(int)elapsedTime.TotalMinutes:00}:{elapsedTime.Seconds:00}</b>   ·   Espectadores: <b>{num}</b></color></size>";
		}
		catch
		{
			return string.Empty;
		}
	}

	internal static string TextoPrincipal()
	{
		try
		{
			int num = (int)(Time.realtimeSinceStartup * 2f);
			if (num == _cuadroTexto)
			{
				return _textoCache;
			}
			_cuadroTexto = num;
			_textoCache = HudRueI.SinLlaves(Armar());
			return _textoCache;
		}
		catch (Exception arg)
		{
			if (!_avisado)
			{
				_avisado = true;
				Log.Warn($"[TimerRespawn] Armar texto: {arg}");
			}
			return string.Empty;
		}
	}

	private static string Armar()
	{
		ArmarLineas();
		if (_lineas.Count == 0)
		{
			return string.Empty;
		}
		_sb.Clear();
		_sb.Append("<align=center>");
		for (int i = 0; i < _lineas.Count; i++)
		{
			if (i > 0)
			{
				_sb.Append('\n');
			}
			_sb.Append("<size=").Append(_lineas[i].Tam).Append('>')
				.Append(_lineas[i].Texto)
				.Append("</size>");
		}
		_sb.Append("</align>");
		return _sb.ToString();
	}

	internal static string TextoFondo()
	{
		int num = (int)(Time.realtimeSinceStartup * 2f);
		if (num == _cuadroFondo)
		{
			return _fondoCache;
		}
		_cuadroFondo = num;
		_fondoCache = ArmarFondo();
		return _fondoCache;
	}

	private static string ArmarFondo()
	{
		try
		{
			if (Time.frameCount != _cuadro)
			{
				ArmarLineas();
			}
			if (_lineas.Count == 0)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder(512);
			stringBuilder.Append("<align=center>");
			for (int i = 0; i < _lineas.Count; i++)
			{
				int item = _lineas[i].Tam;
				int repeatCount = Mathf.Max(1, Mathf.RoundToInt(820f / (float)item));
				if (i > 0)
				{
					stringBuilder.Append('\n');
				}
				stringBuilder.Append("<size=").Append(item).Append("><mark=")
					.Append("#0A0A0ACC")
					.Append(" padding=\"0,0,")
					.Append((i == 0) ? 14 : 4)
					.Append(',')
					.Append((i == _lineas.Count - 1) ? 14 : 4)
					.Append("\">")
					.Append("<mspace=1em><color=#00000000>")
					.Append('_', repeatCount)
					.Append("</color></mspace></mark></size>");
			}
			stringBuilder.Append("</align>");
			return stringBuilder.ToString();
		}
		catch
		{
			return string.Empty;
		}
	}

	private static void ArmarLineas()
	{
		_lineas.Clear();
		_cuadro = Time.frameCount;
		if (!Round.IsStarted || Round.IsEnded || Var.EventoEnCurso)
		{
			return;
		}
		List<TimeBasedWave> source = WaveManager.Waves.OfType<TimeBasedWave>().ToList();
		TimeBasedWave w2 = source.FirstOrDefault((TimeBasedWave w) => w is NtfSpawnWave);
		TimeBasedWave w3 = source.FirstOrDefault((TimeBasedWave w) => w is ChaosSpawnWave);
		TimeBasedWave miniMtf = source.FirstOrDefault((TimeBasedWave w) => w is NtfMiniWave);
		TimeBasedWave miniCaos = source.FirstOrDefault((TimeBasedWave w) => w is ChaosMiniWave);
		WaveQueueState state = WaveManager.State;
		bool flag = (uint)(state - 1) <= 1u;
		if (flag && WaveManager._nextWave != null)
		{
			int num = Mathf.Max(0, Mathf.CeilToInt(_llegada));
			string text = "<color=#FFD400>Aparecés como</color> <b>" + Nombre(WaveManager._nextWave) + "</b>";
			if (_llegada >= 0f)
			{
				text += $"  <b><color={ColorTiempo(num)}>{num}s</color></b>";
			}
			_lineas.Add((30, text));
		}
		else
		{
			TimeBasedWave timeBasedWave = (from w in source.Where(PuedeSalir)
				orderby w.Timer.TimeLeft
				select w).FirstOrDefault();
			_lineas.Add((30, (timeBasedWave != null) ? ("<color=#FFD400>Próxima oleada</color>  <b>" + Nombre(timeBasedWave) + "</b>  <b><color=" + ColorTiempo(timeBasedWave.Timer.TimeLeft) + ">" + Reloj(timeBasedWave.Timer.TimeLeft) + "</color></b>") : "<color=#FFD400>Sin oleadas por ahora</color>"));
		}
		_lineas.Add((23, "<color=#4F8DFF>MTF</color> <b>" + TiempoColor(w2) + "</b><color=#666666>    |    </color><color=#35C46A>Caos</color> <b>" + TiempoColor(w3) + "</b>"));
		string text2 = Refuerzos(miniMtf, miniCaos);
		if (text2 != null)
		{
			_lineas.Add((18, text2));
		}
		int num2 = 0;
		int num3 = 0;
		foreach (Player item in Player.List)
		{
			if (!(item == null) && item.IsAlive && !item.IsNPC && item.Role.Type != RoleTypeId.Tutorial)
			{
				if (item.IsScp)
				{
					num3++;
				}
				else
				{
					num2++;
				}
			}
		}
		int num4 = 0;
		try
		{
			num4 = Scp079Recontainer.AllGenerators.Count((Scp079Generator g) => g != null && g.Engaged);
		}
		catch
		{
		}
		TimeSpan elapsedTime = Round.ElapsedTime;
		int num5 = Player.List.Count((Player x) => x != null && !x.IsNPC && x.Role.Type == RoleTypeId.Spectator);
		_lineas.Add((18, "<color=#C08CFF>Nuke</color> " + EstadoNuke() + "<color=#666666>  ·  </color><color=#FF9F1A>Generadores " + num4 + "/3</color><color=#666666>  ·  </color><color=#7FD3FF>" + num2 + " humanos</color><color=#666666>  ·  </color><color=#FF4D4D>" + num3 + " SCPs</color>"));
		_lineas.Add((16, $"<color=#5FE0C8>Ronda {(int)elapsedTime.TotalMinutes:00}:{elapsedTime.Seconds:00}</color><color=#666666>  ·  </color><color=#B0B8C8>{num5} espectadores</color>"));
	}

	private static string ColorTiempo(float segundos)
	{
		return (segundos > 120f) ? "#7CFC9A" : ((segundos > 30f) ? "#FFD54F" : "#FF5C5C");
	}

	private static string TiempoColor(TimeBasedWave w)
	{
		if (w?.Timer == null || !PuedeSalir(w))
		{
			return Tiempo(w);
		}
		return "<color=" + ColorTiempo(w.Timer.TimeLeft) + ">" + Reloj(w.Timer.TimeLeft) + "</color>";
	}

	private static bool PuedeSalir(TimeBasedWave w)
	{
		return w?.Timer != null && w.Configuration.IsEnabled && !w.Timer.IsPaused && !w.Timer.IsForcefullyPaused && (!(w is ILimitedWave limitedWave) || limitedWave.RespawnTokens > 0);
	}

	private static string Tiempo(TimeBasedWave w)
	{
		if (w?.Timer == null)
		{
			return "<color=#666666>—</color>";
		}
		if (!PuedeSalir(w))
		{
			return "<color=#666666>pausada</color>";
		}
		return Reloj(w.Timer.TimeLeft);
	}

	private static string Refuerzos(TimeBasedWave miniMtf, TimeBasedWave miniCaos)
	{
		List<string> list = new List<string>(2);
		if (miniMtf != null && PuedeSalir(miniMtf))
		{
			list.Add("<color=#6E9BFF>Refuerzo MTF:</color> <color=" + ColorTiempo(miniMtf.Timer.TimeLeft) + ">" + Reloj(miniMtf.Timer.TimeLeft) + "</color> <color=#9FB4FF>(" + Fichas(miniMtf) + ")</color>");
		}
		if (miniCaos != null && PuedeSalir(miniCaos))
		{
			list.Add("<color=#5FD38D>Refuerzo Caos:</color> <color=" + ColorTiempo(miniCaos.Timer.TimeLeft) + ">" + Reloj(miniCaos.Timer.TimeLeft) + "</color> <color=#9FE3B8>(" + Fichas(miniCaos) + ")</color>");
		}
		return (list.Count == 0) ? null : string.Join("      ", list);
	}

	private static string Fichas(TimeBasedWave w)
	{
		int num = ((w is ILimitedWave limitedWave) ? limitedWave.RespawnTokens : 0);
		return (num == 1) ? "1 ficha" : $"{num} fichas";
	}

	private static string Reloj(float segundos)
	{
		int num = Mathf.Max(0, Mathf.CeilToInt(segundos));
		return $"{num / 60}:{num % 60:00}";
	}

	private static string Nombre(SpawnableWaveBase w)
	{
		if (!(w is NtfMiniWave))
		{
			if (!(w is ChaosMiniWave))
			{
				if (!(w is NtfSpawnWave))
				{
					if (w is ChaosSpawnWave)
					{
						return "<color=#2E8B57>Insurgencia del Caos</color>";
					}
					return (w.TargetFaction == Faction.FoundationStaff) ? "<color=#3B82F6>Fundación</color>" : "<color=#2E8B57>Caos</color>";
				}
				return "<color=#3B82F6>MTF</color>";
			}
			return "<color=#5FD38D>refuerzo del Caos</color>";
		}
		return "<color=#6E9BFF>refuerzo MTF</color>";
	}

	private static string EstadoNuke()
	{
		try
		{
			if (Warhead.IsDetonated)
			{
				return "<color=#8B0000>detonada</color>";
			}
			if (Warhead.IsInProgress)
			{
				return $"<color=#e32433>¡en curso! {Mathf.CeilToInt(Warhead.DetonationTimer)} s</color>";
			}
			return Warhead.LeverStatus ? "<color=#FF8C1A>armada</color>" : "<color=#4CAF50>desarmada</color>";
		}
		catch
		{
			return "—";
		}
	}
}
