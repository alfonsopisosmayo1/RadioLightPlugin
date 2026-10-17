using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using UnityEngine;

namespace RadioLightPlugin.Patches;

internal static class Perfilador
{
	internal sealed class Dato
	{
		public string Nombre;

		public long Ticks;

		public long Max;

		public int Llamadas;

		public bool Externo;
	}

	/// <summary>Envuelve corrutinas MEC de OTROS plugins (y del juego) para medir cuánto tarda cada paso.</summary>
	private sealed class Envoltura : IEnumerator<float>, System.Collections.IEnumerator, IDisposable
	{
		internal IEnumerator<float> Interno;

		internal Dato D;

		public float Current => Interno.Current;

		object System.Collections.IEnumerator.Current => Interno.Current;

		public bool MoveNext()
		{
			long timestamp = Stopwatch.GetTimestamp();
			try
			{
				return Interno.MoveNext();
			}
			finally
			{
				Registrar(D, Stopwatch.GetTimestamp() - timestamp);
			}
		}

		public void Reset()
		{
			Interno.Reset();
		}

		public void Dispose()
		{
			Interno.Dispose();
		}
	}

	private static readonly Dictionary<object, Dato> _datos = new Dictionary<object, Dato>();

	private static readonly Dictionary<Type, FieldInfo> _campoAccion = new Dictionary<Type, FieldInfo>();

	private static int _hiloPrincipal = -1;

	private static int _frameActual;

	private static long _ticksFrame;

	private static Dato _peorFrame;

	private static long _peorFrameTicks;

	private static long _ticksFrameAnterior;

	private static Dato _peorFrameAnterior;

	private static long _peorFrameAnteriorTicks;

	private static float _ultimoTiron;

	private static CoroutineHandle _vigia;

	private static readonly object _candado = new object();

	private static readonly double MsPorTick = 1000.0 / (double)Stopwatch.Frequency;

	private static readonly long AvisoTicks = (long)(30.0 / MsPorTick);

	private static DateTime _desde = DateTime.UtcNow;

	private static CoroutineHandle _bucle;

	private static readonly Dictionary<string, float> _ultimoAviso = new Dictionary<string, float>();

	public static void Aplicar(Harmony harmony)
	{
		if (!ConsolaLimpia.Opcion("Perfilador", porDefecto: true))
		{
			return;
		}
		HarmonyMethod prefix = new HarmonyMethod(typeof(Perfilador), "Antes");
		HarmonyMethod postfix = new HarmonyMethod(typeof(Perfilador), "Despues");
		int num = 0;
		foreach (MethodInfo item in Objetivos())
		{
			try
			{
				harmony.Patch(item, prefix, postfix);
				num++;
			}
			catch
			{
			}
		}
		_hiloPrincipal = System.Threading.Thread.CurrentThread.ManagedThreadId;
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(Timing), "RunCoroutineInternal");
			if (methodInfo != null)
			{
				harmony.Patch(methodInfo, new HarmonyMethod(typeof(Perfilador), "AntesDeCorrutina"));
				num++;
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Rendimiento] No se pudo medir corrutinas de otros plugins: " + ex.Message);
		}
		Timing.KillCoroutines(_bucle);
		_bucle = Timing.RunCoroutine(Informe());
		Timing.KillCoroutines(_vigia);
		_vigia = Timing.RunCoroutine(VigiaTirones());
		Log.Info($"[Rendimiento] Midiendo {num} partes (RadioLight + corrutinas de todos los plugins). Comando: rlrendimiento");
	}

	public static void Detener()
	{
		Timing.KillCoroutines(_bucle);
		Timing.KillCoroutines(_vigia);
	}

	private static void AntesDeCorrutina(ref IEnumerator<float> coroutine)
	{
		if (coroutine == null || coroutine is Envoltura)
		{
			return;
		}
		try
		{
			Type type = coroutine.GetType();
			if (type.Assembly == typeof(Perfilador).Assembly)
			{
				return;
			}
			object obj = type;
			if (type.DeclaringType == typeof(Timing))
			{
				if (!_campoAccion.TryGetValue(type, out var value))
				{
					value = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((FieldInfo f) => typeof(Delegate).IsAssignableFrom(f.FieldType));
					_campoAccion[type] = value;
				}
				if (value?.GetValue(coroutine) is Delegate obj2 && obj2.Method != null)
				{
					obj = obj2.Method;
				}
			}
			Dato value2;
			lock (_candado)
			{
				if (!_datos.TryGetValue(obj, out value2))
				{
					value2 = new Dato
					{
						Nombre = NombreExterno(obj),
						Externo = true
					};
					_datos[obj] = value2;
				}
			}
			coroutine = new Envoltura
			{
				Interno = coroutine,
				D = value2
			};
		}
		catch
		{
		}
	}

	private static string NombreExterno(object clave)
	{
		if (clave is MethodInfo methodInfo)
		{
			Type type = methodInfo.DeclaringType;
			string text = methodInfo.Name;
			if (text.StartsWith("<"))
			{
				int num = text.IndexOf('>');
				text = ((num > 1) ? text.Substring(1, num - 1) : text);
			}
			while (type != null && type.Name.StartsWith("<") && type.DeclaringType != null)
			{
				type = type.DeclaringType;
			}
			return (type?.Assembly.GetName().Name ?? "?") + ": " + type?.Name + "." + text + " (CallDelayed)";
		}
		Type type2 = clave as Type;
		if (type2 == null)
		{
			return clave?.ToString() ?? "?";
		}
		string text2 = type2.Name;
		Type type3 = type2;
		if (text2.StartsWith("<"))
		{
			int num2 = text2.IndexOf('>');
			text2 = ((num2 > 1) ? text2.Substring(1, num2 - 1) : text2);
			type3 = type2.DeclaringType ?? type2;
		}
		return type2.Assembly.GetName().Name + ": " + type3.Name + "." + text2 + " (bucle)";
	}

	private static void Registrar(Dato d, long t)
	{
		lock (_candado)
		{
			d.Ticks += t;
			d.Llamadas++;
			if (t > d.Max)
			{
				d.Max = t;
			}
		}
		if (System.Threading.Thread.CurrentThread.ManagedThreadId != _hiloPrincipal)
		{
			return;
		}
		int frameCount = Time.frameCount;
		if (frameCount != _frameActual)
		{
			_ticksFrameAnterior = ((frameCount == _frameActual + 1) ? _ticksFrame : 0);
			_peorFrameAnterior = ((frameCount == _frameActual + 1) ? _peorFrame : null);
			_peorFrameAnteriorTicks = ((frameCount == _frameActual + 1) ? _peorFrameTicks : 0);
			_frameActual = frameCount;
			_ticksFrame = 0L;
			_peorFrame = null;
			_peorFrameTicks = 0L;
		}
		_ticksFrame += t;
		if (t > _peorFrameTicks)
		{
			_peorFrameTicks = t;
			_peorFrame = d;
		}
		if (t > AvisoTicks)
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (!_ultimoAviso.TryGetValue(d.Nombre, out var value) || !(realtimeSinceStartup - value < 30f))
			{
				_ultimoAviso[d.Nombre] = realtimeSinceStartup;
				Log.Warn($"[Rendimiento] {d.Nombre} tardó {(double)t * MsPorTick:0.0} ms en una sola llamada.");
			}
		}
	}

	internal static string Estado()
	{
		try
		{
			return $"jugadores {Player.List.Count} · objetos en el suelo {Exiled.API.Features.Pickups.Pickup.List.Count()} · cuerpos {Ragdoll.List.Count()} · objetos de red {Mirror.NetworkServer.spawned.Count} · TPS {Server.Tps:0}";
		}
		catch (Exception ex)
		{
			return "estado no disponible (" + ex.Message + ")";
		}
	}

	/// <summary>Avisa en la consola cada vez que el servidor se congela (un frame de más de 150 ms) y qué código corrió en ese frame.</summary>
	private static IEnumerator<float> VigiaTirones()
	{
		while (true)
		{
			yield return Timing.WaitForOneFrame;
			float dt = Time.unscaledDeltaTime;
			if (dt < 0.15f || Time.realtimeSinceStartup - _ultimoTiron < 10f || Round.IsLobby)
			{
				continue;
			}
			_ultimoTiron = Time.realtimeSinceStartup;
			try
			{
				bool flag = _frameActual == Time.frameCount;
				long num = (flag ? _ticksFrameAnterior : _ticksFrame);
				Dato dato = (flag ? _peorFrameAnterior : _peorFrame);
				long num2 = (flag ? _peorFrameAnteriorTicks : _peorFrameTicks);
				string text = ((dato != null) ? $" Lo más pesado medido: {dato.Nombre} ({(double)num2 * MsPorTick:0.0} ms)." : string.Empty);
				Log.Warn($"[Rendimiento] TIRÓN: el servidor se trabó {dt * 1000f:0} ms. Corrutinas medidas en ese frame: {(double)num * MsPorTick:0.0} ms.{text} Estado: {Estado()}");
			}
			catch (Exception ex)
			{
				Log.Debug("[Rendimiento] vigía: " + ex.Message);
			}
		}
	}

	private static IEnumerable<MethodInfo> Objetivos()
	{
		Assembly asm = typeof(Perfilador).Assembly;
		Type[] tipos;
		try
		{
			tipos = asm.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			ReflectionTypeLoadException e = ex;
			tipos = e.Types.Where((Type t) => t != null).ToArray();
		}
		Type[] array = tipos;
		foreach (Type t2 in array)
		{
			if (t2 == typeof(Perfilador) || t2 == typeof(Envoltura) || t2.ContainsGenericParameters || t2.Namespace == null || !t2.Namespace.StartsWith("RadioLightPlugin"))
			{
				continue;
			}
			bool esCoroutine = typeof(IEnumerator<float>).IsAssignableFrom(t2);
			MethodInfo[] methods = t2.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo i in methods)
			{
				if (i.IsAbstract || i.ContainsGenericParameters || i.GetMethodBody() == null)
				{
					continue;
				}
				if (esCoroutine)
				{
					if (i.Name == "MoveNext")
					{
						yield return i;
					}
					continue;
				}
				ParameterInfo[] ps = i.GetParameters();
				if (ps.Length == 1 && ps[0].ParameterType.Name.EndsWith("EventArgs") && !i.IsSpecialName)
				{
					yield return i;
					continue;
				}
				bool flag = ps.Length == 0 && i.ReturnType == typeof(string);
				bool flag2 = flag;
				if (flag2)
				{
					bool flag3;
					switch (i.Name)
					{
					case "TextoPrincipal":
					case "TextoFondo":
					case "ArmarPanel":
						flag3 = true;
						break;
					default:
						flag3 = false;
						break;
					}
					flag2 = flag3;
				}
				if (flag2)
				{
					yield return i;
				}
			}
		}
	}

	private static void Antes(out long __state)
	{
		__state = Stopwatch.GetTimestamp();
	}

	private static void Despues(MethodBase __originalMethod, long __state)
	{
		long t = Stopwatch.GetTimestamp() - __state;
		Dato value;
		lock (_candado)
		{
			if (!_datos.TryGetValue(__originalMethod, out value))
			{
				value = new Dato
				{
					Nombre = Nombre(__originalMethod)
				};
				_datos[__originalMethod] = value;
			}
		}
		Registrar(value, t);
	}

	private static string Nombre(MethodBase m)
	{
		Type declaringType = m.DeclaringType;
		if (declaringType != null && m.Name == "MoveNext" && declaringType.Name.StartsWith("<"))
		{
			int num = declaringType.Name.IndexOf('>');
			string text = ((num > 1) ? declaringType.Name.Substring(1, num - 1) : declaringType.Name);
			return declaringType.DeclaringType?.Name + "." + text + " (bucle)";
		}
		return declaringType?.Name + "." + m.Name;
	}

	internal static List<Dato> Ranking()
	{
		lock (_candado)
		{
			return (from x in _datos.Values
				where x.Llamadas > 0
				orderby x.Ticks descending
				select new Dato
				{
					Nombre = x.Nombre,
					Ticks = x.Ticks,
					Max = x.Max,
					Llamadas = x.Llamadas,
					Externo = x.Externo
				}).ToList();
		}
	}

	internal static void Reiniciar()
	{
		lock (_candado)
		{
			// se ponen a cero (no se borran): las corrutinas que ya corren siguen apuntando a su dato
			foreach (Dato value in _datos.Values)
			{
				value.Ticks = 0L;
				value.Max = 0L;
				value.Llamadas = 0;
			}
		}
		_desde = DateTime.UtcNow;
	}

	internal static string Texto(int cuantos)
	{
		List<Dato> list = Ranking();
		List<Dato> list2 = list.Where((Dato x) => !x.Externo).ToList();
		List<Dato> list3 = list.Where((Dato x) => x.Externo).ToList();
		double num = Math.Max(0.1, (DateTime.UtcNow - _desde).TotalMinutes);
		double num2 = (double)list2.Sum((Dato x) => x.Ticks) * MsPorTick;
		double num3 = (double)list3.Sum((Dato x) => x.Ticks) * MsPorTick;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"[Rendimiento] Últimos {num:0.0} min · {Estado()}");
		stringBuilder.AppendLine($"RadioLightPlugin: {num2:0} ms en total ({num2 / num / 600.0:0.00}% del servidor).");
		foreach (Dato item in list2.Take(cuantos))
		{
			stringBuilder.AppendLine($"  {(double)item.Ticks * MsPorTick,8:0.0} ms  ·  {item.Llamadas,7} llamadas  ·  máx {(double)item.Max * MsPorTick,6:0.0} ms  ·  {item.Nombre}");
		}
		stringBuilder.AppendLine($"Corrutinas de otros plugins y del juego: {num3:0} ms en total ({num3 / num / 600.0:0.00}% del servidor).");
		foreach (Dato item2 in list3.Take(cuantos))
		{
			stringBuilder.AppendLine($"  {(double)item2.Ticks * MsPorTick,8:0.0} ms  ·  {item2.Llamadas,7} llamadas  ·  máx {(double)item2.Max * MsPorTick,6:0.0} ms  ·  {item2.Nombre}");
		}
		return stringBuilder.ToString();
	}

	private static IEnumerator<float> Informe()
	{
		while (true)
		{
			yield return Timing.WaitForSeconds(300f);
			try
			{
				List<Dato> r = Ranking();
				if (r.Count > 0 && (double)r[0].Ticks * MsPorTick > 20.0)
				{
					Log.Info(Texto(8));
				}
				Reiniciar();
			}
			catch (Exception ex)
			{
				Exception e = ex;
				Log.Debug("[Rendimiento] " + e.Message);
			}
		}
	}
}
