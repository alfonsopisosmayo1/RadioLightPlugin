using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;
using HarmonyLib;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Patches;

internal static class ConsolaLimpia
{
	[ThreadStatic]
	internal static bool Directo;

	internal static void SinHud(Action accion)
	{
		bool directo = Directo;
		Directo = true;
		try
		{
			accion();
		}
		finally
		{
			Directo = directo;
		}
	}

	public static void Aplicar(Harmony harmony)
	{
		int num = 0;
		foreach (MethodInfo item in typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(EsBroadcastDeTexto))
		{
			harmony.Patch(item, new HarmonyMethod(typeof(ConsolaLimpia), "PrefixJugador"));
			num++;
		}
		foreach (MethodInfo item2 in typeof(Map).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(EsBroadcastDeTexto))
		{
			harmony.Patch(item2, new HarmonyMethod(typeof(ConsolaLimpia), "PrefixMapa"));
			num++;
		}
		if (num == 0)
		{
			Log.Warn("[ConsolaLimpia] No se encontró Broadcast(ushort, string) para el parche; los anuncios seguirán en la consola.");
		}
	}

	private static bool EsBroadcastDeTexto(MethodInfo m)
	{
		if (m.Name != "Broadcast")
		{
			return false;
		}
		ParameterInfo[] parameters = m.GetParameters();
		return parameters.Length >= 2 && parameters[0].ParameterType == typeof(ushort) && parameters[1].ParameterType == typeof(string);
	}

	private static bool PrefixJugador(Player __instance, object[] __args)
	{
		try
		{
			if (Directo || !Activo() || __instance == null || __instance.IsNPC || !__instance.IsConnected || !__instance.IsAlive || !LoLlamaEstePlugin())
			{
				return true;
			}
			string text = Limpiar(__args[1] as string);
			if (text == null)
			{
				return true;
			}
			EffectsHUD.Notify(__instance, text, Convert.ToSingle(__args[0]));
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool PrefixMapa(object[] __args)
	{
		try
		{
			if (Directo || !Activo() || !LoLlamaEstePlugin())
			{
				return true;
			}
			string text = Limpiar(__args[1] as string);
			if (text == null)
			{
				return true;
			}
			string original = __args[1] as string;
			ushort segundos = Convert.ToUInt16(__args[0]);
			Broadcast.BroadcastFlags tipo = ((__args.Length > 2 && __args[2] is Broadcast.BroadcastFlags broadcastFlags) ? broadcastFlags : Broadcast.BroadcastFlags.Normal);
			bool flag = default(bool);
			int num;
			if (__args.Length > 3)
			{
				object obj = __args[3];
				if (obj is bool)
				{
					flag = (bool)obj;
					num = 1;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 0;
			}
			bool limpiar = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
			foreach (Player p in Player.List.ToList())
			{
				if (p == null || p.IsNPC || !p.IsConnected)
				{
					continue;
				}
				if (p.IsAlive)
				{
					EffectsHUD.Notify(p, text, (int)segundos);
					continue;
				}
				SinHud(delegate
				{
					p.Broadcast(segundos, original, tipo, limpiar);
				});
			}
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool Activo()
	{
		return Opcion("ConsolaLimpia", porDefecto: true);
	}

	internal static bool Opcion(string nombre, bool porDefecto)
	{
		object obj = Plugin.Singleton?.Config;
		return (obj?.GetType().GetProperty(nombre)?.GetValue(obj) is bool flag) ? flag : porDefecto;
	}

	private static string Limpiar(string texto)
	{
		if (string.IsNullOrWhiteSpace(texto))
		{
			return null;
		}
		texto = texto.Trim('\n', '\r', ' ');
		return (texto.Length == 0) ? null : texto;
	}

	private static bool LoLlamaEstePlugin()
	{
		Assembly assembly = typeof(ConsolaLimpia).Assembly;
		StackFrame[] frames = new StackTrace(1, fNeedFileInfo: false).GetFrames();
		if (frames == null)
		{
			return false;
		}
		for (int i = 0; i < frames.Length && i < 10; i++)
		{
			Type type = frames[i].GetMethod()?.DeclaringType;
			if (!(type == null) && !(type == typeof(ConsolaLimpia)) && type.Assembly == assembly)
			{
				return true;
			}
		}
		return false;
	}
}
