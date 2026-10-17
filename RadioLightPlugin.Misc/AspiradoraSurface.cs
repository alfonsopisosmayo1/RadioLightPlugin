using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class AspiradoraSurface
{
	private static class Modelo
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static object Spawnear(Vector3 posicion, Quaternion rotacion)
		{
			return Mer.Spawnear("Aspiradora", posicion, rotacion, new Vector3(0.3f, 0.3f, 0.3f));
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Destruir(object schematic)
		{
			Mer.Destruir(schematic);
		}
	}

	private static object _actual;

	public static void AlEmpezarRonda()
	{
		_actual = null;
		Plugin singleton = Plugin.Singleton;
		if (singleton != null && singleton.Config?.AspiradoraGateB == true)
		{
			Var.Coroutines.Add(Timing.CallDelayed(3f, Poner));
		}
	}

	private static void Poner()
	{
		try
		{
			Config config = Plugin.Singleton.Config;
			if (TryLeerPosicion(config.AspiradoraPosicion, out var pos))
			{
				Colocar(pos, Quaternion.Euler(0f, config.AspiradoraRotacionY, 0f));
				return;
			}
			Vector3? vector = Util.SalidaAscensor(ElevatorType.GateB, 6f, 3f);
			if (!vector.HasValue)
			{
				Log.Debug("[Aspiradora] No se encontró el ascensor de Gate B.");
			}
			else
			{
				Colocar(vector.Value + Vector3.up * 0.05f, Quaternion.Euler(0f, Var.random.Next(0, 360), 0f));
			}
		}
		catch (Exception ex)
		{
			Log.Warn("[Aspiradora] No se pudo poner la aspiradora (¿está ProjectMER y el schematic 'Aspiradora'?): " + ex.Message);
		}
	}

	public static void Colocar(Vector3 pos, Quaternion rot)
	{
		if (_actual != null)
		{
			try
			{
				Modelo.Destruir(_actual);
			}
			catch
			{
			}
			_actual = null;
		}
		_actual = Modelo.Spawnear(pos, rot);
	}

	public static bool TryLeerPosicion(string texto, out Vector3 pos)
	{
		pos = default(Vector3);
		if (string.IsNullOrWhiteSpace(texto))
		{
			return false;
		}
		string[] array = texto.Split(new char[1] { ',' });
		if (array.Length != 3)
		{
			return false;
		}
		if (!float.TryParse(array[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !float.TryParse(array[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) || !float.TryParse(array[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
		{
			return false;
		}
		pos = new Vector3(result, result2, result3);
		return true;
	}
}
