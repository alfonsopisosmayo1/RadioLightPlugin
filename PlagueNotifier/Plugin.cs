using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Exiled.API.Features;
using MEC;
using PlagueNotifier.Handlers;
using RadioLightPlugin;

namespace PlagueNotifier;

public class Plugin
{
	private PlagueDeathHandler _handler;

	public static Plugin Instance { get; private set; }

	public Config Config => RadioLightPlugin.Plugin.Singleton?.Config?.Plaga;

	public static void Activar()
	{
		Timing.CallDelayed(1f, delegate
		{
			if (Instance == null)
			{
				if (!AppDomain.CurrentDomain.GetAssemblies().Any((Assembly a) => a.GetName().Name == "PlagueNotifier"))
				{
					try
					{
						Encender();
						Log.Info("[PlagueNotifier] Módulo activado dentro de RadioLightPlugin.");
						return;
					}
					catch (Exception ex)
					{
						Instance = null;
						Log.Warn("[PlagueNotifier] No se pudo activar el log de muertes de zombies (¿está instalado RueI?): " + ex.Message);
						return;
					}
				}
				Log.Info("[PlagueNotifier] Ya está instalado como plugin aparte; el módulo de RadioLight queda apagado.");
			}
		});
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Encender()
	{
		PlagueDeathHandler plagueDeathHandler = new PlagueDeathHandler();
		plagueDeathHandler.Register();
		Instance = new Plugin
		{
			_handler = plagueDeathHandler
		};
	}

	public static void Desactivar()
	{
		if (Instance != null)
		{
			Instance._handler?.Unregister();
			Instance._handler = null;
			Instance = null;
		}
	}
}
