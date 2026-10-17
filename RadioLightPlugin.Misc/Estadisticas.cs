using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;

namespace RadioLightPlugin.Misc;

public static class Estadisticas
{
	private static readonly Dictionary<string, FichaJugador> _fichas = new Dictionary<string, FichaJugador>();

	private static bool _cargado;

	private static bool _cambios;

	private static readonly Dictionary<string, int> _bajasRonda = new Dictionary<string, int>();

	private static readonly Dictionary<string, int> _bajasScpRonda = new Dictionary<string, int>();

	private static readonly Dictionary<string, int> _gulagsRonda = new Dictionary<string, int>();

	private static readonly Dictionary<string, int> _rachaPractica = new Dictionary<string, int>();

	private static readonly Dictionary<Player, bool> _escapaSinArmas = new Dictionary<Player, bool>();

	private static readonly Dictionary<string, int> _bajasHumanoRonda = new Dictionary<string, int>();

	private static readonly Dictionary<string, int> _scpsRonda = new Dictionary<string, int>();

	private static readonly Dictionary<string, int> _bajasZombieRonda = new Dictionary<string, int>();

	private static readonly HashSet<string> _hizoDanio = new HashSet<string>();

	private static readonly Dictionary<string, float> _danioRonda = new Dictionary<string, float>();

	private static string _primerEscape;

	private static bool _primeraSangre;

	private static readonly object _candadoArchivo = new object();

	private static readonly string[] Meses = new string[12]
	{
		"Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre",
		"Noviembre", "Diciembre"
	};

	public static int EscapesRonda { get; private set; }

	public static int GulagsRonda { get; private set; }

	private static Config Cfg => Plugin.Singleton?.Config;

	public static bool Activo => Cfg?.Estadisticas ?? true;

	private static string Carpeta => Path.Combine(Paths.Configs, "RadioLight");

	private static string Archivo => Path.Combine(Carpeta, "estadisticas.txt");

	public static int SemanaActual
	{
		get
		{
			DateTime utcNow = DateTime.UtcNow;
			int weekOfYear = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(utcNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
			return utcNow.Year * 100 + weekOfYear;
		}
	}

	public static IEnumerable<FichaJugador> Todas
	{
		get
		{
			Cargar();
			return _fichas.Values;
		}
	}

	public static int TemporadaActual => DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;

	public static int DiasRestantes
	{
		get
		{
			DateTime utcNow = DateTime.UtcNow;
			DateTime dateTime = new DateTime(utcNow.Year, utcNow.Month, 1).AddMonths(1);
			return Math.Max(0, (int)Math.Ceiling((dateTime - utcNow).TotalDays));
		}
	}

	private static bool Contable(Player p)
	{
		return p != null && !p.IsNPC && !string.IsNullOrEmpty(p.UserId);
	}

	public static FichaJugador Ficha(Player p)
	{
		if (!Contable(p))
		{
			return null;
		}
		Cargar();
		if (!_fichas.TryGetValue(p.UserId, out var value))
		{
			value = new FichaJugador
			{
				UserId = p.UserId,
				Primera = DateTime.UtcNow.Ticks,
				Ultima = DateTime.UtcNow.Ticks
			};
			_fichas[p.UserId] = value;
		}
		value.Nick = Limpiar(p.Nickname);
		_cambios = true;
		return value;
	}

	public static FichaJugador Buscar(string nickOId)
	{
		Cargar();
		if (string.IsNullOrWhiteSpace(nickOId))
		{
			return null;
		}
		if (_fichas.TryGetValue(nickOId, out var value))
		{
			return value;
		}
		string q = nickOId.ToLowerInvariant();
		return _fichas.Values.FirstOrDefault((FichaJugador f) => f.Nick.ToLowerInvariant() == q) ?? (from f in _fichas.Values
			where f.Nick.ToLowerInvariant().Contains(q)
			orderby f.Ultima descending
			select f).FirstOrDefault();
	}

	private static string Limpiar(string s)
	{
		return (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
	}

	public static void Cargar()
	{
		if (_cargado)
		{
			return;
		}
		_cargado = true;
		try
		{
			if (!File.Exists(Archivo))
			{
				return;
			}
			string[] array = File.ReadAllLines(Archivo, Encoding.UTF8);
			foreach (string text in array)
			{
				string[] c;
				if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("#"))
				{
					c = text.Split(new char[1] { '\t' });
					if (c.Length >= 17)
					{
						FichaJugador fichaJugador = new FichaJugador();
						fichaJugador.UserId = c[0];
						fichaJugador.Nick = c[1];
						fichaJugador.Bajas = I(2);
						fichaJugador.Muertes = I(3);
						fichaJugador.Escapes = I(4);
						fichaJugador.RondasScp = I(5);
						fichaJugador.ScpsMatados = I(6);
						fichaJugador.Gulags = I(7);
						fichaJugador.Practicas = I(8);
						fichaJugador.UltimoEnPie = I(9);
						fichaJugador.MejorRachaPractica = I(10);
						fichaJugador.PracticasSemana = I(11);
						fichaJugador.Semana = I(12);
						fichaJugador.Primera = L(13);
						fichaJugador.Ultima = L(14);
						fichaJugador.Visitas = I(15);
						fichaJugador.Logros = new HashSet<string>(c[16].Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries));
						fichaJugador.Equipado = ((c.Length > 17) ? c[17].Trim() : "");
						fichaJugador.Mes = ((c.Length > 18) ? I(18) : 0);
						fichaJugador.PracticasMes = ((c.Length > 19) ? I(19) : 0);
						fichaJugador.BajasMes = ((c.Length > 20) ? I(20) : 0);
						fichaJugador.Temporadas = ((c.Length > 21) ? c[21].Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>());
						FichaJugador fichaJugador2 = fichaJugador;
						_fichas[fichaJugador2.UserId] = fichaJugador2;
					}
				}
				int I(int i)
				{
					int result2;
					return int.TryParse(c[i], out result2) ? result2 : 0;
				}
				long L(int i)
				{
					long result;
					return long.TryParse(c[i], out result) ? result : 0;
				}
			}
			Log.Info($"[Estadísticas] {_fichas.Count} jugadores cargados.");
		}
		catch (Exception ex)
		{
			Log.Error("[Estadísticas] No se pudo leer " + Archivo + ": " + ex.Message);
		}
	}

	public static void Guardar(bool enSegundoPlano = false)
	{
		if (!_cargado || !_cambios)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(Carpeta);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# userid\tnick\tbajas\tmuertes\tescapes\trondas_scp\tscps_matados\tgulags\tpracticas\tultimo_en_pie\tmejor_racha\tpracticas_semana\tsemana\tprimera\tultima\tvisitas\tlogros\tequipado\tmes\tpracticas_mes\tbajas_mes\ttemporadas");
			foreach (FichaJugador value in _fichas.Values)
			{
				stringBuilder.Append(value.UserId).Append('\t').Append(Limpiar(value.Nick))
					.Append('\t')
					.Append(value.Bajas)
					.Append('\t')
					.Append(value.Muertes)
					.Append('\t')
					.Append(value.Escapes)
					.Append('\t')
					.Append(value.RondasScp)
					.Append('\t')
					.Append(value.ScpsMatados)
					.Append('\t')
					.Append(value.Gulags)
					.Append('\t')
					.Append(value.Practicas)
					.Append('\t')
					.Append(value.UltimoEnPie)
					.Append('\t')
					.Append(value.MejorRachaPractica)
					.Append('\t')
					.Append(value.PracticasSemana)
					.Append('\t')
					.Append(value.Semana)
					.Append('\t')
					.Append(value.Primera)
					.Append('\t')
					.Append(value.Ultima)
					.Append('\t')
					.Append(value.Visitas)
					.Append('\t')
					.Append(string.Join(",", value.Logros))
					.Append('\t')
					.Append(value.Equipado ?? "")
					.Append('\t')
					.Append(value.Mes)
					.Append('\t')
					.Append(value.PracticasMes)
					.Append('\t')
					.Append(value.BajasMes)
					.Append('\t')
					.Append(string.Join("|", value.Temporadas.Select(Limpiar)))
					.AppendLine();
			}
			string contenido = stringBuilder.ToString();
			_cambios = false;
			if (enSegundoPlano)
			{
				ThreadPool.QueueUserWorkItem(delegate
				{
					Escribir(contenido);
				});
			}
			else
			{
				Escribir(contenido);
			}
		}
		catch (Exception ex)
		{
			Log.Error("[Estadísticas] No se pudo guardar: " + ex.Message);
		}
	}

	private static void Escribir(string contenido)
	{
		try
		{
			lock (_candadoArchivo)
			{
				string text = Archivo + ".tmp";
				File.WriteAllText(text, contenido, Encoding.UTF8);
				if (File.Exists(Archivo))
				{
					File.Delete(Archivo);
				}
				File.Move(text, Archivo);
			}
		}
		catch (Exception ex)
		{
			_cambios = true;
			Log.Error("[Estadísticas] No se pudo guardar: " + ex.Message);
		}
	}

	public static void AlVerificar(VerifiedEventArgs ev)
	{
		Player p = ev?.Player;
		if (!Activo || !Contable(p))
		{
			return;
		}
		Cargar();
		bool flag = !_fichas.ContainsKey(p.UserId);
		FichaJugador fichaJugador = Ficha(p);
		long ultima = fichaJugador.Ultima;
		fichaJugador.Visitas++;
		fichaJugador.Ultima = DateTime.UtcNow.Ticks;
		Config cfg = Cfg;
		if (cfg == null || cfg.MensajeBienvenida)
		{
			string msg;
			if (flag)
			{
				msg = "<size=32><color=#859CC7>¡Bienvenido por primera vez a <b>RadioLight</b>, <color=#D4AF37>" + p.Nickname + "</color>!</color></size>\n<size=22><color=#AAAAAA>Escribe <b>.stats</b> en la consola (Ñ) para ver tus estadísticas.</color></size>";
			}
			else
			{
				msg = $"<size=32><color=#859CC7>Bienvenido de vuelta, <color=#D4AF37>{p.Nickname}</color>.</color></size>\n<size=22><color=#AAAAAA>Última vez: {Hace(ultima)} · {fichaJugador.Bajas} bajas · {fichaJugador.Escapes} escapes · {fichaJugador.Practicas} prácticas ganadas</color></size>";
			}
			Timing.CallDelayed(1.5f, delegate
			{
				if (p.IsConnected)
				{
					p.Broadcast(7, msg);
				}
			});
		}
		Logros.AlVerificar(p, fichaJugador);
		Logros.AlVerificarTitulo(p);
	}

	public static string Hace(long ticks)
	{
		if (ticks <= 0)
		{
			return "nunca";
		}
		TimeSpan timeSpan = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
		if (timeSpan.TotalMinutes < 2.0)
		{
			return "hace un momento";
		}
		if (timeSpan.TotalHours < 1.0)
		{
			return $"hace {(int)timeSpan.TotalMinutes} min";
		}
		if (timeSpan.TotalDays < 1.0)
		{
			return $"hace {(int)timeSpan.TotalHours} h";
		}
		if (timeSpan.TotalDays < 2.0)
		{
			return "ayer";
		}
		return $"hace {(int)timeSpan.TotalDays} días";
	}

	public static void AlEmpezarRonda()
	{
		_bajasRonda.Clear();
		_bajasScpRonda.Clear();
		_gulagsRonda.Clear();
		_rachaPractica.Clear();
		_escapaSinArmas.Clear();
		_bajasHumanoRonda.Clear();
		_scpsRonda.Clear();
		_bajasZombieRonda.Clear();
		_hizoDanio.Clear();
		_danioRonda.Clear();
		_primerEscape = null;
		_primeraSangre = false;
		EscapesRonda = 0;
		GulagsRonda = 0;
		if (!Activo)
		{
			return;
		}
		CerrarTemporadaSiCambio();
		Timing.CallDelayed(3f, delegate
		{
			foreach (Player item in Player.List)
			{
				if (Contable(item) && item.Role.Side == Side.Scp && item.Role.Type != RoleTypeId.Scp0492)
				{
					Ficha(item).RondasScp++;
				}
			}
		});
	}

	public static void AlTerminarRonda()
	{
		try
		{
			List<Player> list = Player.List.Where(delegate(Player p)
			{
				bool flag = Contable(p) && p.IsAlive;
				bool flag2 = flag;
				if (flag2)
				{
					RoleTypeId type = p.Role.Type;
					bool flag3 = ((type == RoleTypeId.ClassD || type == RoleTypeId.Scientist) ? true : false);
					flag2 = flag3;
				}
				return flag2;
			}).ToList();
			foreach (Player item in list)
			{
				Logros.Dar(item, "superviviente", anunciar: false);
			}
			if (list.Count > 0 && Logros.Activos)
			{
				Map.ShowHint("<size=26>\ud83c\udfc5 <color=#859CC7>Logro</color> <b><color=#8BC34A>Superviviente</color></b><color=#859CC7>: " + string.Join(", ", list.Select((Player v) => v.Nickname)) + "</color></size>", 5f);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Logros] Superviviente: " + ex.Message);
		}
		Guardar(enSegundoPlano: true);
	}

	public static void AlHacerDanio(HurtEventArgs ev)
	{
		Player player = ev?.Attacker;
		if (player != null && player != ev.Player && Contable(player))
		{
			_hizoDanio.Add(player.UserId);
			if (ev.Player != null && ev.Player.Role.Side != player.Role.Side && player.Role.Type != RoleTypeId.Tutorial)
			{
				_danioRonda.TryGetValue(player.UserId, out var value);
				_danioRonda[player.UserId] = value + Math.Max(0f, ev.Amount);
			}
		}
	}

	public static void AlMorir(DiedEventArgs ev)
	{
		if (!Activo || ev?.Player == null)
		{
			return;
		}
		RoleTypeId targetOldRole = ev.TargetOldRole;
		if (targetOldRole == RoleTypeId.Tutorial || targetOldRole == RoleTypeId.Spectator || targetOldRole == RoleTypeId.None || Var.EventoEnCurso || !Round.IsStarted || Round.IsEnded)
		{
			return;
		}
		FichaJugador fichaJugador = Ficha(ev.Player);
		if (fichaJugador != null)
		{
			fichaJugador.Muertes++;
		}
		Player attacker = ev.Attacker;
		if (!Contable(attacker) || attacker == ev.Player)
		{
			return;
		}
		Side side = PlayerRolesUtils.GetTeam(targetOldRole).GetSide();
		Side side2 = attacker.Role.Side;
		if (side == side2 || side2 == Side.Tutorial)
		{
			return;
		}
		FichaJugador fichaJugador2 = Ficha(attacker);
		fichaJugador2.Bajas++;
		AlDia(fichaJugador2);
		fichaJugador2.BajasMes++;
		Sumar(_bajasRonda, attacker.UserId);
		if (!_primeraSangre && attacker.IsAlive)
		{
			_primeraSangre = true;
			Logros.Dar(attacker, "primerasangre");
		}
		if (side2 == Side.Scp)
		{
			int num = Sumar(_bajasScpRonda, attacker.UserId);
			if (num == 8)
			{
				Logros.Dar(attacker, "exterminador");
			}
			if (attacker.Role.Type == RoleTypeId.Scp0492 && Sumar(_bajasZombieRonda, attacker.UserId) == 3)
			{
				Logros.Dar(attacker, "zombi");
			}
		}
		else if (attacker.IsAlive)
		{
			if (Sumar(_bajasHumanoRonda, attacker.UserId) == 5)
			{
				Logros.Dar(attacker, "masacre");
			}
			if (side == Side.Scp && targetOldRole != RoleTypeId.Scp0492)
			{
				fichaJugador2.ScpsMatados++;
				Logros.Dar(attacker, "cazascp");
				if (Sumar(_scpsRonda, attacker.UserId) == 2)
				{
					Logros.Dar(attacker, "matagigantes");
				}
			}
		}
		if (fichaJugador2.Bajas == 100)
		{
			Logros.Dar(attacker, "veterano");
		}
		else if (fichaJugador2.Bajas == 500)
		{
			Logros.Dar(attacker, "leyenda");
		}
	}

	public static void AlEscapar(EscapingEventArgs ev)
	{
		if (!(ev?.Player == null))
		{
			_escapaSinArmas[ev.Player] = !ev.Player.Items.Any((Item i) => i is Firearm);
		}
	}

	public static void AlEscapado(EscapedEventArgs ev)
	{
		Player player = ev?.Player;
		if (Activo && Contable(player) && !Var.EventoEnCurso)
		{
			EscapesRonda++;
			Ficha(player).Escapes++;
			if (_primerEscape == null)
			{
				_primerEscape = $"{player.Nickname} ({(int)Round.ElapsedTime.TotalMinutes}:{Round.ElapsedTime.Seconds:00})";
			}
			RoleTypeId roleTypeId = ev.OldRole?.Type ?? RoleTypeId.None;
			bool value;
			bool flag = _escapaSinArmas.TryGetValue(player, out value) && value;
			_escapaSinArmas.Remove(player);
			if (roleTypeId == RoleTypeId.ClassD && flag)
			{
				Logros.Dar(player, "fugitivo");
			}
			else if (roleTypeId == RoleTypeId.Scientist)
			{
				Logros.Dar(player, "cerebrito");
			}
			if (Round.ElapsedTime.TotalMinutes < 4.0)
			{
				Logros.Dar(player, "expres");
			}
			if (!_hizoDanio.Contains(player.UserId))
			{
				Logros.Dar(player, "pacifista");
			}
		}
	}

	public static void GanoGulag(Player p)
	{
		if (Activo && Contable(p))
		{
			GulagsRonda++;
			FichaJugador fichaJugador = Ficha(p);
			fichaJugador.Gulags++;
			if (Sumar(_gulagsRonda, p.UserId) == 2)
			{
				Logros.Dar(p, "inmortal");
			}
			if (fichaJugador.Gulags == 10)
			{
				Logros.Dar(p, "gladiador");
			}
		}
	}

	public static void GanoPractica(Player p, bool ultimoEnPie)
	{
		if (Activo && Contable(p))
		{
			FichaJugador fichaJugador = Ficha(p);
			fichaJugador.Practicas++;
			int semanaActual = SemanaActual;
			if (fichaJugador.Semana != semanaActual)
			{
				fichaJugador.Semana = semanaActual;
				fichaJugador.PracticasSemana = 0;
			}
			fichaJugador.PracticasSemana++;
			AlDia(fichaJugador);
			fichaJugador.PracticasMes++;
			int num = Sumar(_rachaPractica, p.UserId);
			if (num > fichaJugador.MejorRachaPractica)
			{
				fichaJugador.MejorRachaPractica = num;
			}
			if (num == 5)
			{
				Logros.Dar(p, "invicto");
			}
			if (fichaJugador.Practicas == 25)
			{
				Logros.Dar(p, "campeon");
			}
			if (ultimoEnPie)
			{
				fichaJugador.UltimoEnPie++;
				Logros.Dar(p, "ultimo");
			}
		}
	}

	public static void PerdioPractica(Player p)
	{
		if (Contable(p))
		{
			_rachaPractica.Remove(p.UserId);
		}
	}

	private static int Sumar(Dictionary<string, int> d, string id)
	{
		d.TryGetValue(id, out var value);
		return d[id] = value + 1;
	}

	public static (string Nick, int Bajas) MejorDeLaRonda()
	{
		if (_bajasRonda.Count == 0)
		{
			return (Nick: null, Bajas: 0);
		}
		KeyValuePair<string, int> keyValuePair = _bajasRonda.OrderByDescending((KeyValuePair<string, int> x) => x.Value).First();
		FichaJugador value;
		string item = (_fichas.TryGetValue(keyValuePair.Key, out value) ? value.Nick : keyValuePair.Key);
		return (Nick: item, Bajas: keyValuePair.Value);
	}

	private static int Puesto(FichaJugador f, Func<FichaJugador, int> valor)
	{
		int v = valor(f);
		if (v <= 0)
		{
			return 0;
		}
		return _fichas.Values.Count((FichaJugador x) => valor(x) > v) + 1;
	}

	public static string TarjetaPerfil(FichaJugador f)
	{
		int temp = TemporadaActual;
		int num = ((f.Mes == temp) ? f.PracticasMes : 0);
		int num2 = ((f.Mes == temp) ? f.BajasMes : 0);
		int num3 = Puesto(f, (FichaJugador x) => (x.Mes == temp) ? x.PracticasMes : 0);
		int num4 = Puesto(f, (FichaJugador x) => (x.Mes == temp) ? x.BajasMes : 0);
		double num5 = ((f.Muertes == 0) ? ((double)f.Bajas) : ((double)f.Bajas / (double)f.Muertes));
		string text = (string.IsNullOrEmpty(f.Equipado) ? "<color=#777777>sin título</color>" : ("<color=#FFD700>" + Logros.NombreEquipado(f) + "</color>"));
		int num6 = f.Logros.Count((string id) => Logros.TierDe(id) != Logros.Tier.Ninguno);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<size=30><b><color=#859CC7>\ud83d\udc64 " + f.Nick + "</color></b></size>\n");
		stringBuilder.Append("<size=22>" + text + "</size>\n");
		stringBuilder.Append($"<size=20><color=#C2D0E6>Bajas <b>{f.Bajas}</b> · Muertes <b>{f.Muertes}</b> · K/D <b>{num5:0.00}</b> · Escapes <b>{f.Escapes}</b></color></size>\n");
		stringBuilder.Append($"<size=20><color=#C2D0E6>Gulags <b>{f.Gulags}</b> · Prácticas <b>{f.Practicas}</b> · Último en pie <b>{f.UltimoEnPie}</b> · Mejor racha <b>{f.MejorRachaPractica}</b></color></size>\n");
		stringBuilder.Append(string.Format("<size=20><color=#FFD400>Temporada {0}:</color> <color=#C2D0E6>{1} prácticas{2} · {3} bajas{4}</color></size>\n", NombreTemporada(temp), num, (num3 > 0) ? $" (#{num3})" : "", num2, (num4 > 0) ? $" (#{num4})" : ""));
		stringBuilder.Append($"<size=20><color=#C2D0E6>Logros <b>{f.Logros.Count}/{Logros.Total}</b> · Títulos de colección <b>{num6}</b> · Temporadas ganadas <b>{f.Temporadas.Count}</b></color></size>\n");
		stringBuilder.Append($"<size=18><color=#888888>{f.Visitas} visitas · desde {new DateTime(f.Primera, DateTimeKind.Utc):dd/MM/yyyy} · visto {Hace(f.Ultima)}</color></size>");
		return stringBuilder.ToString();
	}

	private static string NickDe(string id)
	{
		FichaJugador value;
		return _fichas.TryGetValue(id, out value) ? value.Nick : id;
	}

	private static (string Nick, float Valor) Mejor<T>(Dictionary<string, T> d, Func<T, float> num)
	{
		if (d.Count == 0)
		{
			return (Nick: null, Valor: 0f);
		}
		KeyValuePair<string, T> keyValuePair = d.OrderByDescending((KeyValuePair<string, T> x) => num(x.Value)).First();
		return (num(keyValuePair.Value) <= 0f) ? (Nick: null, Valor: 0f) : (Nick: NickDe(keyValuePair.Key), Valor: num(keyValuePair.Value));
	}

	public static void AnunciarMvp()
	{
		Plugin singleton = Plugin.Singleton;
		if ((singleton != null && singleton.Config?.AnuncioMvp == false) || Var.EventoEnCurso)
		{
			return;
		}
		try
		{
			List<string> list = new List<string>();
			(string, float) tuple = Mejor(_bajasHumanoRonda, (int v) => v);
			if (tuple.Item1 != null)
			{
				list.Add($"<color=#4FC3F7>\ud83d\udd2b Más bajas (humano):</color> <b>{tuple.Item1}</b> <color=#AAAAAA>({tuple.Item2:0})</color>");
			}
			(string, float) tuple2 = Mejor(_bajasScpRonda, (int v) => v);
			if (tuple2.Item1 != null)
			{
				list.Add($"<color=#EC2121>⚔ SCP más letal:</color> <b>{tuple2.Item1}</b> <color=#AAAAAA>({tuple2.Item2:0})</color>");
			}
			(string, float) tuple3 = Mejor(_danioRonda, (float v) => v);
			if (tuple3.Item1 != null)
			{
				list.Add($"<color=#FF9F1A>\ud83d\udca5 Más daño:</color> <b>{tuple3.Item1}</b> <color=#AAAAAA>({tuple3.Item2:0})</color>");
			}
			if (_primerEscape != null)
			{
				list.Add("<color=#8BC34A>\ud83c\udfc3 Primer escape:</color> <b>" + _primerEscape + "</b>");
			}
			(string, float) tuple4 = Mejor(_gulagsRonda, (int v) => v);
			if (tuple4.Item1 != null)
			{
				list.Add($"<color=#D4AF37>⚔ Rey del gulag:</color> <b>{tuple4.Item1}</b> <color=#AAAAAA>({tuple4.Item2:0})</color>");
			}
			if (list.Count != 0)
			{
				string message = "<size=30><color=#FFD700><b>⭐ MVP DE LA RONDA ⭐</b></color></size>\n<size=22>" + string.Join("\n", list) + "</size>";
				Map.ClearBroadcasts();
				Map.Broadcast(12, message);
				DiscordLog.Enviar("⭐ MVP de la ronda", DiscordLog.SinEtiquetas(string.Join("\n", list)), 13938487);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[MVP] " + ex.Message);
		}
	}

	public static string NombreTemporada(int yyyymm)
	{
		return (yyyymm <= 0) ? "?" : $"{Meses[Math.Max(1, Math.Min(12, yyyymm % 100)) - 1]} {yyyymm / 100}";
	}

	private static void AlDia(FichaJugador f)
	{
		int temporadaActual = TemporadaActual;
		if (f.Mes != temporadaActual)
		{
			if (f.Mes != 0 && (f.PracticasMes > 0 || f.BajasMes > 0))
			{
				CerrarTemporadaSiCambio();
			}
			f.Mes = temporadaActual;
			f.PracticasMes = 0;
			f.BajasMes = 0;
		}
	}

	public static void CerrarTemporadaSiCambio()
	{
		try
		{
			Cargar();
			int actual = TemporadaActual;
			List<int> list = (from m in (from f in _fichas.Values
					where f.Mes != 0 && f.Mes != actual && (f.PracticasMes > 0 || f.BajasMes > 0)
					select f.Mes).Distinct()
				orderby m
				select m).ToList();
			foreach (int mes in list)
			{
				List<FichaJugador> list2 = _fichas.Values.Where((FichaJugador f) => f.Mes == mes).ToList();
				string nombre = NombreTemporada(mes);
				List<string> list3 = new List<string>();
				FichaJugador fichaJugador = (from f in list2
					where f.PracticasMes > 0
					orderby f.PracticasMes descending
					select f).FirstOrDefault();
				if (fichaJugador != null)
				{
					fichaJugador.Temporadas.Add("Campeón de práctica · " + nombre);
					list3.Add($"\ud83e\udd4a {fichaJugador.Nick} ({fichaJugador.PracticasMes} prácticas)");
				}
				FichaJugador fichaJugador2 = (from f in list2
					where f.BajasMes > 0
					orderby f.BajasMes descending
					select f).FirstOrDefault();
				if (fichaJugador2 != null)
				{
					fichaJugador2.Temporadas.Add("Cazador · " + nombre);
					list3.Add($"\ud83d\udd2b {fichaJugador2.Nick} ({fichaJugador2.BajasMes} bajas)");
				}
				foreach (FichaJugador item in list2)
				{
					item.Mes = actual;
					item.PracticasMes = 0;
					item.BajasMes = 0;
				}
				_cambios = true;
				if (list3.Count > 0)
				{
					string texto = string.Join(" · ", list3);
					Log.Info("[Temporadas] Terminó la temporada " + nombre + ". Ganadores: " + texto);
					Timing.CallDelayed(8f, delegate
					{
						Map.Broadcast(10, "<size=30><color=#FFD700>\ud83c\udfc6 Terminó la temporada " + nombre + "</color></size>\n<size=24><color=#859CC7>" + texto + "</color></size>\n<size=20><color=#AAAAAA>Sus títulos se equipan con .logro</color></size>");
					});
					DiscordLog.Enviar("\ud83c\udfc6 Fin de la temporada " + nombre, texto, 13938487);
				}
			}
			if (list.Count > 0)
			{
				Guardar(enSegundoPlano: true);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[Temporadas] {arg}");
		}
	}

	public static string TextoStats(FichaJugador f)
	{
		if (f == null)
		{
			return "No hay estadísticas de ese jugador.";
		}
		double num = ((f.Muertes == 0) ? ((double)f.Bajas) : ((double)f.Bajas / (double)f.Muertes));
		int num2 = ((f.Semana == SemanaActual) ? f.PracticasSemana : 0);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("\n\ud83d\udcca Estadísticas de " + f.Nick);
		stringBuilder.AppendLine($"  Bajas: {f.Bajas}   Muertes: {f.Muertes}   K/D: {num:0.00}");
		stringBuilder.AppendLine($"  Escapes: {f.Escapes}   Rondas como SCP: {f.RondasScp}   SCPs matados: {f.ScpsMatados}");
		stringBuilder.AppendLine($"  Gulags ganados: {f.Gulags}");
		stringBuilder.AppendLine($"  Prácticas ganadas: {f.Practicas} (esta semana: {num2})   Último en pie: {f.UltimoEnPie}   Mejor racha: {f.MejorRachaPractica}");
		stringBuilder.AppendLine($"  Visitas: {f.Visitas}   Primera vez: {new DateTime(f.Primera, DateTimeKind.Utc):dd/MM/yyyy}   Última: {Hace(f.Ultima)}");
		stringBuilder.Append($"  Logros ({f.Logros.Count}/{Logros.Total}): ");
		stringBuilder.Append((f.Logros.Count == 0) ? "ninguno todavía" : string.Join(", ", f.Logros.Select(Logros.NombreConTier)));
		int temporadaActual = TemporadaActual;
		stringBuilder.Append($"\n  Temporada {NombreTemporada(temporadaActual)}: {((f.Mes == temporadaActual) ? f.PracticasMes : 0)} prácticas · {((f.Mes == temporadaActual) ? f.BajasMes : 0)} bajas");
		if (f.Temporadas.Count > 0)
		{
			stringBuilder.Append("\n  Títulos de temporada: " + string.Join(", ", f.Temporadas));
		}
		if (!string.IsNullOrEmpty(f.Equipado))
		{
			stringBuilder.Append("\n  Título equipado: " + Logros.NombreEquipado(f));
		}
		return stringBuilder.ToString();
	}

	public static string TextoTop(string tipo, Player quien)
	{
		Cargar();
		int semana = SemanaActual;
		int temporada = TemporadaActual;
		string value = $"\n  Temporada {NombreTemporada(temporada)} · termina en {DiasRestantes} días · el primero se lleva un título";
		string text;
		Func<FichaJugador, int> valor;
		switch ((tipo ?? "").ToLowerInvariant())
		{
		case "semana":
			text = "\ud83e\udd4a Top práctica de la semana";
			valor = (FichaJugador f) => (f.Semana == semana) ? f.PracticasSemana : 0;
			value = "";
			break;
		case "bajas":
		case "kills":
			text = "\ud83d\udd2b Top bajas · Temporada " + NombreTemporada(temporada);
			valor = (FichaJugador f) => (f.Mes == temporada) ? f.BajasMes : 0;
			break;
		case "bajastotal":
		case "bajas-total":
			text = "\ud83d\udd2b Top bajas (de siempre)";
			valor = (FichaJugador f) => f.Bajas;
			value = "";
			break;
		case "total":
		case "siempre":
			text = "\ud83e\udd4a Top práctica (de siempre)";
			valor = (FichaJugador f) => f.Practicas;
			value = "";
			break;
		case "uep":
		case "ultimo":
			text = "\ud83d\udc51 Top último en pie (de siempre)";
			valor = (FichaJugador f) => f.UltimoEnPie;
			value = "";
			break;
		default:
			text = "\ud83e\udd4a Top práctica · Temporada " + NombreTemporada(temporada);
			valor = (FichaJugador f) => (f.Mes == temporada) ? f.PracticasMes : 0;
			break;
		}
		List<FichaJugador> list = _fichas.Values.Where((FichaJugador f) => valor(f) > 0).OrderByDescending(valor).ThenBy((FichaJugador f) => f.Nick)
			.ToList();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("\n" + text);
		if (list.Count == 0)
		{
			stringBuilder.AppendLine("  Nadie todavía. ¡Entra con .practica!");
		}
		for (int i = 0; i < Math.Min(10, list.Count); i++)
		{
			stringBuilder.AppendLine(string.Format("  {0} {1} — {2}", i switch
			{
				0 => "\ud83e\udd47", 
				1 => "\ud83e\udd48", 
				2 => "\ud83e\udd49", 
				_ => $" {i + 1}.", 
			}, list[i].Nick, valor(list[i])));
		}
		if (Contable(quien))
		{
			int num = list.FindIndex((FichaJugador f) => f.UserId == quien.UserId);
			stringBuilder.AppendLine((num >= 0) ? $"  Tu puesto: {num + 1}º con {valor(list[num])}" : "  Todavía no estás en esta tabla.");
		}
		stringBuilder.Append(value);
		stringBuilder.Append("\n  Otras tablas: .top bajas · .top semana · .top total · .top bajastotal · .top uep");
		return stringBuilder.ToString();
	}
}
