using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class ArenaPractica
{
	private const float Ancho = 36f;

	private const float Largo = 48f;

	private const float AltoPared = 10f;

	private const float Grosor = 1f;

	private const float AlturaSobrePiso = 1f;

	private static readonly List<AdminToy> _piezas = new List<AdminToy>();

	public static bool Construida => _piezas.Count > 0;

	public static void Destruir()
	{
		foreach (AdminToy pieza in _piezas)
		{
			try
			{
				pieza?.Destroy();
			}
			catch
			{
			}
		}
		_piezas.Clear();
	}

	public static bool Construir(Vector3 centro, out Vector3 spawnA, out Vector3 spawnB, out Vector3 espera)
	{
		Destruir();
		spawnA = centro + new Vector3(0f, 0f, 14f);
		spawnB = centro + new Vector3(0f, 0f, -14f);
		espera = centro + new Vector3(0f, 4f, 31f);
		try
		{
			float num = centro.y - 1f;
			Color color = new Color(0.22f, 0.24f, 0.28f);
			Color color2 = new Color(0.32f, 0.35f, 0.42f);
			Color color3 = new Color(0.45f, 0.3f, 0.22f);
			Caja(new Vector3(centro.x, num - 0.5f, centro.z), new Vector3(38f, 1f, 50f), color);
			float y = num + 5f;
			Caja(new Vector3(centro.x + 18f + 0.5f, y, centro.z), new Vector3(1f, 10f, 50f), color2);
			Caja(new Vector3(centro.x - 18f - 0.5f, y, centro.z), new Vector3(1f, 10f, 50f), color2);
			Caja(new Vector3(centro.x, y, centro.z + 24f + 0.5f), new Vector3(38f, 10f, 1f), color2);
			Caja(new Vector3(centro.x, y, centro.z - 24f - 0.5f), new Vector3(38f, 10f, 1f), color2);
			float y2 = num + 1.5f;
			(float, float, float, float)[] array = new(float, float, float, float)[6]
			{
				(0f, 0f, 6f, 2f),
				(-9f, 7f, 3f, 3f),
				(9f, 7f, 3f, 3f),
				(-9f, -7f, 3f, 3f),
				(9f, -7f, 3f, 3f),
				(0f, 0f, 2f, 10f)
			};
			(float, float, float, float)[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				(float, float, float, float) tuple = array2[i];
				Caja(new Vector3(centro.x + tuple.Item1, y2, centro.z + tuple.Item2), new Vector3(tuple.Item3, 3f, tuple.Item4), color3);
			}
			float num2 = espera.y - 1f;
			Caja(new Vector3(espera.x, num2 - 0.5f, espera.z), new Vector3(10f, 1f, 10f), color);
			float y3 = num2 + 2f;
			Caja(new Vector3(espera.x + 5.5f, y3, espera.z), new Vector3(1f, 4f, 11f), color2);
			Caja(new Vector3(espera.x - 5.5f, y3, espera.z), new Vector3(1f, 4f, 11f), color2);
			Caja(new Vector3(espera.x, y3, espera.z + 5.5f), new Vector3(11f, 4f, 1f), color2);
			Caja(new Vector3(espera.x, y3, espera.z - 5.5f), new Vector3(11f, 4f, 1f), color2);
			Luz(centro + new Vector3(-10f, 7f, 12f));
			Luz(centro + new Vector3(10f, 7f, 12f));
			Luz(centro + new Vector3(-10f, 7f, -12f));
			Luz(centro + new Vector3(10f, 7f, -12f));
			Luz(espera + new Vector3(0f, 3.5f, 0f), 14f);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("[Práctica] No se pudo construir la arena propia: " + ex.Message);
			Destruir();
			return false;
		}
	}

	private static void Caja(Vector3 posicion, Vector3 escala, Color color)
	{
		Primitive primitive = Primitive.Create(PrimitiveType.Cube, posicion, Vector3.zero, escala, spawn: true, color);
		if (primitive != null)
		{
			_piezas.Add(primitive);
		}
	}

	private static void Luz(Vector3 posicion, float rango = 30f)
	{
		Exiled.API.Features.Toys.Light light = Exiled.API.Features.Toys.Light.Create(posicion, null, Vector3.one, spawn: true, new Color(1f, 0.97f, 0.9f));
		if (light != null)
		{
			light.Range = rango;
			light.Intensity = 1.2f;
			_piezas.Add(light);
		}
	}
}
