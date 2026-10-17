using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Mer
{
	private const BindingFlags Estatico = BindingFlags.Static | BindingFlags.Public;

	private const BindingFlags Instancia = BindingFlags.Instance | BindingFlags.Public;

	private static Assembly _asm;

	private static MethodInfo _spawn;

	private static bool _avisado;

	private static readonly Dictionary<string, bool> _existe = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

	public static bool Disponible => MetodoSpawn() != null;

	private static bool EstaLista(Assembly a)
	{
		try
		{
			Type type = a.GetType("ProjectMER.Features.PrefabManager");
			Type type2 = a.GetType("ProjectMER.ProjectMER");
			if (type == null || type2 == null)
			{
				return false;
			}
			object obj = type.GetProperty("PrimitiveObject", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
			object obj2 = type2.GetProperty("SchematicsDir", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
			return obj is UnityEngine.Object @object && @object != null && obj2 != null;
		}
		catch
		{
			return false;
		}
	}

	private static MethodInfo MetodoSpawn()
	{
		if (_spawn != null && EstaLista(_asm))
		{
			return _spawn;
		}
		_spawn = null;
		List<Assembly> list = AppDomain.CurrentDomain.GetAssemblies().Where(delegate(Assembly a)
		{
			try
			{
				return a.GetName().Name == "ProjectMER";
			}
			catch
			{
				return false;
			}
		}).ToList();
		foreach (Assembly item in list)
		{
			if (!EstaLista(item))
			{
				continue;
			}
			MethodInfo methodInfo = item.GetType("ProjectMER.Features.ObjectSpawner")?.GetMethod("SpawnSchematic", BindingFlags.Static | BindingFlags.Public, null, new Type[4]
			{
				typeof(string),
				typeof(Vector3),
				typeof(Quaternion),
				typeof(Vector3)
			}, null);
			if (methodInfo == null)
			{
				continue;
			}
			_asm = item;
			_spawn = methodInfo;
			if (list.Count > 1 && !_avisado)
			{
				_avisado = true;
				Log.Info($"[ProjectMER] Hay {list.Count} copias de ProjectMER cargadas; se usa la que está lista.");
			}
			return methodInfo;
		}
		return null;
	}

	public static bool SchematicExiste(string nombre)
	{
		if (string.IsNullOrWhiteSpace(nombre))
		{
			return false;
		}
		if (_existe.TryGetValue(nombre, out var value))
		{
			return value;
		}
		string text = null;
		try
		{
			text = _asm?.GetType("ProjectMER.ProjectMER")?.GetProperty("SchematicsDir", BindingFlags.Static | BindingFlags.Public)?.GetValue(null)?.ToString();
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text))
		{
			return true;
		}
		bool flag = Directory.Exists(Path.Combine(text, nombre));
		_existe[nombre] = flag;
		if (!flag)
		{
			Log.Warn("[ProjectMER] El schematic '" + nombre + "' no existe en " + text + ": no se pondrá (se avisa una sola vez).");
		}
		return flag;
	}

	public static object Spawnear(string nombre, Vector3 posicion, Quaternion rotacion, Vector3 escala)
	{
		MethodInfo methodInfo = MetodoSpawn();
		if (methodInfo == null)
		{
			throw new InvalidOperationException("ProjectMER no está listo (no hay ninguna copia con los prefabs cargados).");
		}
		if (!SchematicExiste(nombre))
		{
			return null;
		}
		object obj;
		try
		{
			obj = methodInfo.Invoke(null, new object[4] { nombre, posicion, rotacion, escala });
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			throw ex.InnerException;
		}
		return (obj is UnityEngine.Object @object && @object == null) ? null : obj;
	}

	public static object Spawnear(string nombre, Vector3 posicion)
	{
		return Spawnear(nombre, posicion, Quaternion.identity, Vector3.one);
	}

	public static object Spawnear(string nombre, Vector3 posicion, Quaternion rotacion)
	{
		return Spawnear(nombre, posicion, rotacion, Vector3.one);
	}

	public static bool Existe(object schematic)
	{
		return schematic is Component component && component != null;
	}

	public static void Mover(object schematic, Vector3 posicion, Quaternion rotacion)
	{
		if (schematic is Component component && !(component == null))
		{
			Type type = schematic.GetType();
			PropertyInfo property = type.GetProperty("Position", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo property2 = type.GetProperty("Rotation", BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property2 != null)
			{
				property.SetValue(schematic, posicion);
				property2.SetValue(schematic, rotacion);
			}
			else
			{
				component.transform.SetPositionAndRotation(posicion, rotacion);
			}
		}
	}

	public static void Destruir(object schematic)
	{
		if (schematic is Component component && !(component == null))
		{
			MethodInfo method = schematic.GetType().GetMethod("Destroy", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
			if (method != null)
			{
				method.Invoke(schematic, null);
			}
			else
			{
				UnityEngine.Object.Destroy(component.gameObject);
			}
		}
	}

	public static T[] Componentes<T>(object schematic) where T : Component
	{
		return (schematic is Component component && component != null) ? component.GetComponentsInChildren<T>(includeInactive: true) : Array.Empty<T>();
	}
}
