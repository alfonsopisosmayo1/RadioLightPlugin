using System;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;

namespace RadioLightPlugin.Misc;

public static class Sonido
{
	private static bool _buscado;

	private static MethodInfo _para;

	private static MethodInfo _desde;

	private static MethodInfo _mapa;

	private static MethodInfo _cargar;

	private static void Buscar()
	{
		if (_buscado)
		{
			return;
		}
		_buscado = true;
		try
		{
			Type type = (from a in AppDomain.CurrentDomain.GetAssemblies()
				select a.GetType("AudioPlayer.Features.Extensions.PlayerExtension", throwOnError: false)).FirstOrDefault((Type x) => x != null);
			_para = type?.GetMethod("PlayForPlayer", BindingFlags.Static | BindingFlags.Public);
			_desde = type?.GetMethod("PlayFromPlayer", BindingFlags.Static | BindingFlags.Public);
			_mapa = (from a in AppDomain.CurrentDomain.GetAssemblies()
				select a.GetType("AudioPlayer.Features.AudioSoundPlayer", throwOnError: false)).FirstOrDefault((Type x) => x != null)?.GetMethod("VPlayOnMap", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(string) }, null);
			_cargar = (from a in AppDomain.CurrentDomain.GetAssemblies()
				select a.GetType("AudioPlayer.Models.AudioClipStorage", throwOnError: false)).FirstOrDefault((Type x) => x != null)?.GetMethod("TryGetOrLoadClip", BindingFlags.Static | BindingFlags.Public);
			if (_para == null)
			{
				Log.Debug("[Sonido] No se encontró AudioPlayer: los sonidos van por comando.");
			}
		}
		catch
		{
		}
	}

	public static void Para(int id, string archivo)
	{
		Tocar(_para, "pfor", id, archivo);
	}

	public static void Desde(int id, string archivo)
	{
		Tocar(_desde, "pfrom", id, archivo);
	}

	/// <summary>Deja los sonidos ya cargados en memoria (así la primera vez que suenan no se pierden mientras se decodifican).</summary>
	public static void Precargar(params string[] archivos)
	{
		Buscar();
		if (_cargar == null || archivos == null)
		{
			return;
		}
		foreach (string text in archivos)
		{
			try
			{
				_cargar.Invoke(null, new object[2] { text, null });
			}
			catch (Exception ex)
			{
				Log.Debug("[Sonido] Precargar " + text + ": " + (ex.InnerException?.Message ?? ex.Message));
			}
		}
	}

	public static void Mapa(string archivo)
	{
		Buscar();
		try
		{
			if (_mapa != null)
			{
				_mapa.Invoke(null, new object[1] { archivo });
				return;
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Sonido] " + archivo + ": " + (ex.InnerException?.Message ?? ex.Message));
			return;
		}
		Server.ExecuteCommand("/absolute map " + archivo);
	}

	private static void Tocar(MethodInfo metodo, string modo, int id, string archivo)
	{
		Buscar();
		try
		{
			Player player = Player.Get(id);
			if (player == null)
			{
				return;
			}
			if (metodo != null)
			{
				metodo.Invoke(null, new object[2] { player, archivo });
				return;
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Sonido] " + archivo + ": " + (ex.InnerException?.Message ?? ex.Message));
			return;
		}
		Server.ExecuteCommand($"/absolute {modo} {id} {archivo}");
	}
}
