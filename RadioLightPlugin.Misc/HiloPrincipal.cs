using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Exiled.API.Features;
using MEC;

namespace RadioLightPlugin.Misc;

public static class HiloPrincipal
{
	private static readonly ConcurrentQueue<Action> _cola = new ConcurrentQueue<Action>();

	private static CoroutineHandle _handle;

	public static void Iniciar()
	{
		Timing.KillCoroutines(_handle);
		_handle = Timing.RunCoroutine(Bucle());
	}

	public static void Detener()
	{
		Timing.KillCoroutines(_handle);
	}

	public static void Ejecutar(Action accion)
	{
		if (accion != null)
		{
			_cola.Enqueue(accion);
		}
	}

	private static IEnumerator<float> Bucle()
	{
		while (true)
		{
			if (_cola.TryDequeue(out var accion))
			{
				try
				{
					accion();
				}
				catch (Exception ex)
				{
					Exception e = ex;
					Log.Error($"[HiloPrincipal] {e}");
				}
			}
			else
			{
				yield return float.NegativeInfinity;
			}
		}
	}
}
