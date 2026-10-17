using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exiled.API.Features;
using MEC;

namespace RadioLightPlugin.Misc;

public static class Logros
{
	public enum Tier
	{
		Ninguno,
		Epico,
		Legendario,
		Leyenda
	}

	private static readonly Dictionary<string, (string Nombre, string Descripcion, string Color, string Hex)> Lista = new Dictionary<string, (string, string, string, string)>
	{
		["fugitivo"] = ("Fugitivo", "Escapó como Clase-D sin ningún arma", "orange", "#FF8C1A"),
		["cerebrito"] = ("Cerebrito", "Escapó como Científico", "yellow", "#FFE040"),
		["cazascp"] = ("Cazador de SCPs", "Mató a un SCP siendo humano", "red", "#E32433"),
		["exterminador"] = ("Exterminador", "8 bajas en una ronda como SCP", "crimson", "#B0103A"),
		["inmortal"] = ("Inmortal", "Ganó 2 gulags en la misma ronda", "cyan", "#3FE0E0"),
		["invicto"] = ("Invicto", "Ganó 5 prácticas seguidas", "aqua", "#40C0FF"),
		["ultimo"] = ("Último en pie", "Ganó un todos contra todos de .practica", "pumpkin", "#D4AF37"),
		["veterano"] = ("Veterano", "Llegó a 100 bajas en total", "silver", "#C0C0C0"),
		["fiel"] = ("Fiel", "Entró 50 veces al servidor", "pink", "#FF77BB"),
		["primerasangre"] = ("Primera sangre", "Hizo la primera baja de la ronda", "red", "#FF3B3B"),
		["masacre"] = ("Masacre", "5 bajas en una ronda siendo humano", "tomato", "#FF6347"),
		["matagigantes"] = ("Matagigantes", "Mató a 2 SCPs en la misma ronda", "deep_pink", "#FF1493"),
		["zombi"] = ("Zombi hambriento", "3 bajas como SCP-049-2 en una ronda", "army_green", "#6D704F"),
		["expres"] = ("Escape exprés", "Escapó en los primeros 4 minutos de la ronda", "lime", "#7CFC00"),
		["pacifista"] = ("Pacifista", "Escapó sin haberle hecho daño a nadie", "mint", "#98FF98"),
		["superviviente"] = ("Superviviente", "Seguía vivo como Clase-D o Científico al terminar la ronda", "light_green", "#8BC34A"),
		["gladiador"] = ("Gladiador", "Ganó 10 gulags en total", "carmine", "#D3003F"),
		["campeon"] = ("Campeón de práctica", "Ganó 25 peleas de .practica en total", "emerald", "#50C878"),
		["leyenda"] = ("Leyenda", "Llegó a 500 bajas en total", "magenta", "#FF00FF"),
		["adicto"] = ("Adicto", "Entró 200 veces al servidor", "deep_pink", "#FF4FA0")
	};

	private static readonly HashSet<Player> _conInsignia = new HashSet<Player>();

	private static readonly HashSet<(Player, string)> _yaEnRonda = new HashSet<(Player, string)>();

	private static readonly Dictionary<string, Tier> Tiers = new Dictionary<string, Tier>
	{
		["fiel"] = Tier.Epico,
		["veterano"] = Tier.Epico,
		["gladiador"] = Tier.Legendario,
		["campeon"] = Tier.Legendario,
		["leyenda"] = Tier.Leyenda,
		["adicto"] = Tier.Leyenda
	};

	private static readonly HashSet<Player> _conTitulo = new HashSet<Player>();

	public static int Total => Lista.Count;

	private static bool Activo
	{
		get
		{
			Plugin singleton = Plugin.Singleton;
			return (singleton == null || singleton.Config?.Logros != false) && Estadisticas.Activo;
		}
	}

	public static bool Activos => Activo;

	public static string Nombre(string id)
	{
		string result;
		if (Lista.TryGetValue(id, out (string, string, string, string) value))
		{
			(result, _, _, _) = value;
		}
		else
		{
			result = id;
		}
		return result;
	}

	public static void Dar(Player p, string id, bool anunciar = true)
	{
		if (!Activo || p == null || p.IsNPC || !Lista.TryGetValue(id, out var logro) || !_yaEnRonda.Add((p, id)))
		{
			return;
		}
		bool flag = Estadisticas.Ficha(p)?.Logros.Add(id) ?? false;
		if (anunciar)
		{
			Map.ShowHint("<size=28>\ud83c\udfc5 <color=#D4AF37>" + p.Nickname + "</color> <color=#859CC7>consiguió el logro</color> <b><color=" + logro.Hex + ">" + logro.Nombre + "</color></b>" + (flag ? " <color=#4CAF50>¡nuevo!</color>" : "") + "</size>\n<size=20><color=#AAAAAA>" + logro.Descripcion + "</color></size>", 5f);
		}
		PonerInsignia(p, logro.Nombre, logro.Color);
		if (!flag || TierDe(id) == Tier.Ninguno)
		{
			return;
		}
		(string Nombre, string ColorJuego, string Hex) t = InfoTier(TierDe(id));
		Timing.CallDelayed(5.5f, delegate
		{
			if (p.IsConnected)
			{
				p.Broadcast(7, "<size=26><color=" + t.Hex + "><b>[" + t.Nombre + "]</b></color> <color=#859CC7>Desbloqueaste el título <b>" + logro.Nombre + "</b>. Equípalo con</color> <color=#FFD400>.logro " + logro.Nombre.Split(new char[1] { ' ' })[0].ToLowerInvariant() + "</color></size>");
			}
		});
	}

	public static void AlVerificar(Player p, FichaJugador f)
	{
		if (f != null && f.Visitas >= 50 && !f.Logros.Contains("fiel"))
		{
			Timing.CallDelayed(10f, delegate
			{
				if (p.IsConnected)
				{
					Dar(p, "fiel");
				}
			});
		}
		if (f == null || f.Visitas < 200 || f.Logros.Contains("adicto"))
		{
			return;
		}
		Timing.CallDelayed(14f, delegate
		{
			if (p.IsConnected)
			{
				Dar(p, "adicto");
			}
		});
	}

	private static void PonerInsignia(Player p, string nombre, string color)
	{
		try
		{
			bool flag = !string.IsNullOrEmpty(p.RankName) && !_conInsignia.Contains(p) && !_conTitulo.Contains(p);
			if (!(p.Group != null || flag))
			{
				p.RankName = "\ud83c\udfc5 " + nombre;
				p.RankColor = color;
				_conInsignia.Add(p);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Logros] Insignia: " + ex.Message);
		}
	}

	public static void QuitarInsignias()
	{
		foreach (Player conInsignium in _conInsignia)
		{
			try
			{
				if (!(conInsignium == null) && conInsignium.IsConnected && conInsignium.Group == null)
				{
					_conTitulo.Remove(conInsignium);
					conInsignium.RankName = null;
					AplicarTitulo(conInsignium);
				}
			}
			catch
			{
			}
		}
		_conInsignia.Clear();
		_yaEnRonda.Clear();
	}

	private static (string Nombre, string ColorJuego, string Hex) InfoTier(Tier t)
	{
		return t switch
		{
			Tier.Epico => (Nombre: "ÉPICO", ColorJuego: "magenta", Hex: "#B45CFF"), 
			Tier.Legendario => (Nombre: "LEGENDARIO", ColorJuego: "orange", Hex: "#FF9F1A"), 
			Tier.Leyenda => (Nombre: "LEYENDA", ColorJuego: "crimson", Hex: "#FF2E4D"), 
			_ => (Nombre: "", ColorJuego: "default", Hex: "#FFFFFF"), 
		};
	}

	public static Tier TierDe(string id)
	{
		Tier value;
		return (id != null && Tiers.TryGetValue(id, out value)) ? value : Tier.Ninguno;
	}

	public static string NombreEquipado(FichaJugador f)
	{
		if (f != null && f.Equipado != null && f.Equipado.StartsWith("t:") && int.TryParse(f.Equipado.Substring(2), out var result) && result >= 0 && result < f.Temporadas.Count)
		{
			return f.Temporadas[result] + " [TEMPORADA]";
		}
		return NombreConTier(f?.Equipado);
	}

	public static string NombreConTier(string id)
	{
		Tier tier = TierDe(id);
		if (tier == Tier.Ninguno)
		{
			return Nombre(id);
		}
		return Nombre(id) + " [" + InfoTier(tier).Nombre + "]";
	}

	public static void AplicarTitulo(Player p)
	{
		try
		{
			if (p == null || !p.IsConnected || p.IsNPC || p.Group != null || _conInsignia.Contains(p))
			{
				return;
			}
			FichaJugador fichaJugador = Estadisticas.Ficha(p);
			if (fichaJugador == null || string.IsNullOrEmpty(fichaJugador.Equipado))
			{
				return;
			}
			if (fichaJugador.Equipado.StartsWith("t:") && int.TryParse(fichaJugador.Equipado.Substring(2), out var result) && result >= 0 && result < fichaJugador.Temporadas.Count)
			{
				p.RankName = "\ud83c\udfc6 " + fichaJugador.Temporadas[result];
				p.RankColor = "yellow";
				_conTitulo.Add(p);
			}
			else if (fichaJugador.Logros.Contains(fichaJugador.Equipado))
			{
				Tier tier = TierDe(fichaJugador.Equipado);
				if (tier != 0)
				{
					(string, string, string) tuple = InfoTier(tier);
					p.RankName = tuple.Item1 + " · " + Nombre(fichaJugador.Equipado);
					p.RankColor = tuple.Item2;
					_conTitulo.Add(p);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Logros] Título: " + ex.Message);
		}
	}

	public static void AlVerificarTitulo(Player p)
	{
		Timing.CallDelayed(3f, delegate
		{
			AplicarTitulo(p);
			AnunciarEntrada(p);
		});
	}

	private static void AnunciarEntrada(Player p)
	{
		try
		{
			Plugin singleton = Plugin.Singleton;
			if ((singleton != null && singleton.Config?.BienvenidaConTitulo == false) || p == null || !p.IsConnected)
			{
				return;
			}
			FichaJugador fichaJugador = Estadisticas.Ficha(p);
			if (fichaJugador == null || string.IsNullOrEmpty(fichaJugador.Equipado))
			{
				return;
			}
			string text;
			string text2;
			if (fichaJugador.Equipado.StartsWith("t:") && int.TryParse(fichaJugador.Equipado.Substring(2), out var result) && result >= 0 && result < fichaJugador.Temporadas.Count)
			{
				text = "\ud83c\udfc6 " + fichaJugador.Temporadas[result];
				text2 = "#FFD700";
			}
			else
			{
				if (!fichaJugador.Logros.Contains(fichaJugador.Equipado) || TierDe(fichaJugador.Equipado) == Tier.Ninguno)
				{
					return;
				}
				(string, string, string) tuple = InfoTier(TierDe(fichaJugador.Equipado));
				text = tuple.Item1 + " · " + Nombre(fichaJugador.Equipado);
				text2 = tuple.Item3;
			}
			Map.Broadcast(5, "<size=28><color=#859CC7>Entró</color> <b>" + p.Nickname + "</b>  <color=" + text2 + "><b>" + text + "</b></color></size>");
		}
		catch (Exception ex)
		{
			Log.Debug("[Logros] Bienvenida: " + ex.Message);
		}
	}

	public static string Comando(Player p, string argumento)
	{
		if (p == null)
		{
			return "Este comando se usa desde el juego.";
		}
		if (!Activo)
		{
			return "Los logros están desactivados.";
		}
		FichaJugador fichaJugador = Estadisticas.Ficha(p);
		if (fichaJugador == null)
		{
			return "No se pudieron cargar tus logros.";
		}
		string text = (argumento ?? "").Trim().ToLowerInvariant();
		if (((text != null && text.Length == 0) || text == "lista" || text == "list") ? true : false)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("\n\ud83c\udfc6 Títulos de colección (se equipan con .logro <nombre>)");
			Tier[] array = new Tier[3]
			{
				Tier.Leyenda,
				Tier.Legendario,
				Tier.Epico
			};
			foreach (Tier tier in array)
			{
				stringBuilder.AppendLine("  [" + InfoTier(tier).Nombre + "]");
				foreach (KeyValuePair<string, Tier> item in Tiers.Where((KeyValuePair<string, Tier> x) => x.Value == tier))
				{
					bool flag = fichaJugador.Logros.Contains(item.Key);
					string text2 = ((fichaJugador.Equipado == item.Key) ? "  ← equipado" : "");
					stringBuilder.AppendLine("    " + (flag ? "✔" : "✖") + " " + Nombre(item.Key) + " — " + Lista[item.Key].Descripcion + text2);
				}
			}
			stringBuilder.AppendLine("  [TEMPORADA]  (el primero de cada .top del mes)");
			if (fichaJugador.Temporadas.Count == 0)
			{
				stringBuilder.AppendLine("    — Todavía no ganaste ninguna temporada.");
			}
			for (int j = 0; j < fichaJugador.Temporadas.Count; j++)
			{
				stringBuilder.AppendLine(string.Format("    ✔ {0}  → .logro t{1}{2}", fichaJugador.Temporadas[j], j + 1, (fichaJugador.Equipado == "t:" + j) ? "  ← equipado" : ""));
			}
			stringBuilder.Append("  Quitar el título: .logro quitar");
			if (p.Group != null)
			{
				stringBuilder.Append("\n  (Tienes insignia de staff/VIP: el título se guarda pero no se ve mientras la tengas.)");
			}
			return stringBuilder.ToString();
		}
		bool flag2;
		switch (text)
		{
		case "quitar":
		case "ninguno":
		case "off":
			flag2 = true;
			break;
		default:
			flag2 = false;
			break;
		}
		if (flag2)
		{
			fichaJugador.Equipado = "";
			if (_conTitulo.Remove(p) && p.Group == null && !_conInsignia.Contains(p))
			{
				p.RankName = null;
			}
			return "Título quitado.";
		}
		if (text.Length >= 2 && text[0] == 't' && int.TryParse(text.Substring(1), out var result))
		{
			if (result < 1 || result > fichaJugador.Temporadas.Count)
			{
				return "No tienes ese título de temporada. Escribe .logro para ver la lista.";
			}
			fichaJugador.Equipado = "t:" + (result - 1);
			_conTitulo.Remove(p);
			AplicarTitulo(p);
			return "Título de temporada «" + fichaJugador.Temporadas[result - 1] + "» equipado.";
		}
		string buscado = Normal(text);
		string text3 = Tiers.Keys.FirstOrDefault((string k) => k == buscado || Normal(Nombre(k)) == buscado || Normal(Nombre(k)).StartsWith(buscado));
		if (text3 == null)
		{
			return "Ese título no existe. Escribe .logro para ver la lista.";
		}
		if (!fichaJugador.Logros.Contains(text3))
		{
			return "Todavía no tienes «" + Nombre(text3) + "». Para conseguirlo: " + Lista[text3].Descripcion + ".";
		}
		fichaJugador.Equipado = text3;
		_conTitulo.Remove(p);
		AplicarTitulo(p);
		(string, string, string) tuple = InfoTier(TierDe(text3));
		return (p.Group != null) ? ("Título «" + Nombre(text3) + "» [" + tuple.Item1 + "] equipado. No se ve mientras tengas insignia de staff/VIP.") : ("Título «" + Nombre(text3) + "» [" + tuple.Item1 + "] equipado.");
		static string Normal(string x)
		{
			return x.ToLowerInvariant().Replace("á", "a").Replace("é", "e")
				.Replace("í", "i")
				.Replace("ó", "o")
				.Replace("ú", "u");
		}
	}
}
