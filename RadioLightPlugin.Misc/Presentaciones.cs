using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Presentaciones
{
	private sealed class Datos
	{
		public string Titulo;

		public string Descripcion;

		public float Cuando;
	}

	private static readonly Dictionary<Player, Datos> _datos = new Dictionary<Player, Datos>();

	private static readonly Regex EsTitulo = new Regex("Eres (un|una|el|la)\\b|\\[\\s*SCP-[0-9]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static void Registrar(Player p, string texto)
	{
		if (!(p == null) && !string.IsNullOrEmpty(texto))
		{
			string input = Regex.Replace(texto, "<.*?>", "");
			string text = Limpiar(texto);
			Datos value;
			if (EsTitulo.IsMatch(input))
			{
				_datos[p] = new Datos
				{
					Titulo = text,
					Cuando = Time.time
				};
			}
			else if (_datos.TryGetValue(p, out value) && value.Descripcion == null && Time.time - value.Cuando < 1.5f)
			{
				value.Descripcion = text;
			}
		}
	}

	public static void AlCambiarRol(ChangingRoleEventArgs ev)
	{
		if (ev?.Player != null)
		{
			_datos.Remove(ev.Player);
		}
	}

	public static void Limpiar()
	{
		_datos.Clear();
	}

	private static string Limpiar(string texto)
	{
		string text = Regex.Replace(texto, "<.*?>", "").Trim();
		return text.Trim('[', ']', ' ');
	}

	public static string Texto(Player p)
	{
		if (p == null)
		{
			return "Este comando se usa desde el juego.";
		}
		string text = Habilidades.Nombre(p);
		Datos value;
		bool flag = _datos.TryGetValue(p, out value);
		if (!flag && text == null)
		{
			return "No tienes ninguna subclase en esta vida.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("\ud83c\udfad " + (flag ? value.Titulo : "Tu subclase"));
		if (flag && !string.IsNullOrEmpty(value.Descripcion))
		{
			stringBuilder.AppendLine("   " + value.Descripcion);
		}
		if (text != null)
		{
			float num = Habilidades.Restante(p);
			string text2 = ((p.Role.Type == RoleTypeId.Scp079) ? "el PING (cuando está lista)" : (Habilidades.UsaLinterna(p) ? "tu tecla de Ajustes → Server-specific (ALT izquierdo por defecto) o la linterna dos veces" : "tu tecla de Ajustes → Server-specific (ALT izquierdo por defecto)"));
			stringBuilder.AppendLine("   Habilidad: " + text + " — " + ((num <= 0f) ? "lista" : $"faltan {Mathf.CeilToInt(num)} s"));
			stringBuilder.AppendLine("   Se usa con " + text2 + ". También: .habilidad");
		}
		return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}
}
