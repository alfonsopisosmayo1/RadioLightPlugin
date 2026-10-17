using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using CentralAuth;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace RadioLightPlugin.Subclases;

public static class Habilidades
{
	private sealed class Habilidad
	{
		public string Nombre;

		public float Cooldown;

		public Func<Player, bool> Accion;

		public bool Linterna;

		public float ListaEn;
	}

	private enum Modo
	{
		Apagado,
		Pendiente,
		Normal,
		SSMenu
	}

	public const int IdTecla = 47310;

	public const int IdCabecera = 47311;

	private const float AntiRebote = 0.35f;

	private static readonly Dictionary<Player, Habilidad> _habilidades = new Dictionary<Player, Habilidad>();

	private static readonly Dictionary<Player, float> _ultimaPulsacion = new Dictionary<Player, float>();

	private static readonly Dictionary<Player, float> _ultimoEncendido = new Dictionary<Player, float>();

	private static readonly HashSet<string> _usaTecla = new HashSet<string>();

	private static bool _escuchando;

	private static readonly HashSet<string> _diagnosticados = new HashSet<string>();

	private const float VentanaDobleLinterna = 1.5f;

	private const string Titulo = "RadioLight · Subclases";

	private const string Descripcion = "Habilidades de las subclases del servidor.";

	private const string Etiqueta = "Usar habilidad de subclase";

	private const string Pista = "Activa la habilidad de tu subclase (humanos y SCPs, también SCP-079). Si no responde, asigná aquí una tecla.";

	private static Modo _modo = Modo.Apagado;

	private static CoroutineHandle _registroPendiente;

	private static KeybindSetting _tecla;

	private static HeaderSetting _cabecera;

	private static readonly Dictionary<Player, float> _ultimaTecla = new Dictionary<Player, float>();

	private static HashSet<string> _guardados;

	private static readonly object _candado = new object();

	private static readonly Dictionary<Player, List<float>> _altIgnorado = new Dictionary<Player, List<float>>();

	private static readonly Dictionary<Player, float> _ultimoPing = new Dictionary<Player, float>();

	private static int HashMenu => Math.Abs("RadioLight · Subclases".GetHashCode() % 100000);

	private static string ArchivoTeclas => Path.Combine(Paths.Configs, "RadioLight", "habilidad-tecla.txt");

	public static void Registrar()
	{
		if (_modo == Modo.Apagado)
		{
			_modo = Modo.Pendiente;
			_registroPendiente = Timing.CallDelayed(1f, RegistrarAhora);
		}
	}

	private static void RegistrarAhora()
	{
		if (_modo != Modo.Pendiente)
		{
			return;
		}
		try
		{
			if (RegistrarEnSSMenuSystem())
			{
				_modo = Modo.SSMenu;
				Escuchar(si: true);
				return;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("[Habilidades] SSMenuSystem está instalado pero no se pudo usar (" + ex.Message + "); se registra la tecla normal.");
		}
		try
		{
			_cabecera = new HeaderSetting(47311, "RadioLight · Subclases", "Habilidades de las subclases del servidor.");
			_tecla = new KeybindSetting(47310, "Usar habilidad de subclase", KeyCode.LeftAlt, preventInteractionOnGUI: true, allowSpectatorTrigger: false, "Activa la habilidad de tu subclase (humanos y SCPs, también SCP-079). Si no responde, asigná aquí una tecla.", byte.MaxValue, _cabecera);
			SettingBase.Register(new SettingBase[1] { _tecla });
			Escuchar(si: true);
			_modo = Modo.Normal;
			Log.Info("[Habilidades] Tecla de subclase registrada en Ajustes → Server-specific.");
		}
		catch (Exception arg)
		{
			_modo = Modo.Apagado;
			Log.Error($"[Habilidades] No se pudo registrar la tecla: {arg}");
		}
	}

	private static bool RegistrarEnSSMenuSystem()
	{
		Type type = BuscarTipo("SSMenuSystem.Plugin");
		Type type2 = BuscarTipo("SSMenuSystem.Features.Wrappers.Keybind");
		if (type == null || type2 == null)
		{
			return false;
		}
		object obj = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
		if (obj == null)
		{
			return false;
		}
		Action<ReferenceHub, bool> action = delegate(ReferenceHub hub, bool presionada)
		{
			if (presionada)
			{
				PorTecla(Player.Get(hub));
			}
		};
		object obj2 = Activator.CreateInstance(type2, 47310, "Usar habilidad de subclase", action, KeyCode.LeftAlt, true, false, "Activa la habilidad de tu subclase (humanos y SCPs, también SCP-079). Si no responde, asigná aquí una tecla.", true);
		ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[2]
		{
			new SSGroupHeader("RadioLight · Subclases", reducedPadding: false, "Habilidades de las subclases del servidor."),
			(ServerSpecificSettingBase)obj2
		};
		bool flag = false;
		if (BuscarTipo("SSMenuSystem.Features.Menu")?.GetProperty("Menus", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) is IEnumerable enumerable)
		{
			foreach (object item in enumerable)
			{
				if (item?.GetType().GetProperty("Settings")?.GetValue(item) is ServerSpecificSettingBase[] array)
				{
					object[] array2 = array;
					if (Array.IndexOf(array2, obj2) >= 0)
					{
						flag = true;
						break;
					}
				}
			}
		}
		if (!flag)
		{
			flag = RegistrarMenuPropio(obj2);
		}
		if (flag)
		{
			Log.Info("[Habilidades] SSMenuSystem detectado: la tecla de subclase quedó como tecla GLOBAL (Ajustes → Server-specific → \"Global Keybinding\"). Funciona en cualquier menú.");
		}
		else
		{
			Log.Warn("[Habilidades] SSMenuSystem no aceptó la tecla. Las habilidades siguen funcionando con ALT, la linterna y .habilidad.");
		}
		return true;
	}

	private static bool RegistrarMenuPropio(object tecla)
	{
		try
		{
			Type type = BuscarTipo("SSMenuSystem.Features.Menu");
			Type type2 = BuscarTipo("SSMenuSystem.Features.AssemblyMenu");
			if (type == null || type2 == null)
			{
				return false;
			}
			object obj = null;
			if (type.GetProperty("Menus", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) is IEnumerable enumerable)
			{
				foreach (object item in enumerable)
				{
					if (item != null && item.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(item) as string == "RadioLight · Subclases")
					{
						obj = item;
					}
				}
			}
			ServerSpecificSettingBase[] value = new ServerSpecificSettingBase[1] { (ServerSpecificSettingBase)tecla };
			if (obj != null && type2.IsInstanceOfType(obj))
			{
				type2.GetProperty("OverrideSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(obj, value);
			}
			else
			{
				if (obj != null)
				{
					return false;
				}
				object obj2 = Activator.CreateInstance(type2, nonPublic: true);
				type2.GetProperty("Assembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(obj2, typeof(Habilidades).Assembly);
				type2.GetProperty("OverrideSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(obj2, value);
				type2.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(obj2, "RadioLight · Subclases");
				type2.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(obj2, -Math.Abs("RadioLight · Subclases".GetHashCode() % 100000 + 4731));
				type.GetMethod("Register", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { type }, null)?.Invoke(null, new object[1] { obj2 });
			}
			MethodInfo method = type.GetMethod("LoadForPlayer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
				{
					if (allHub != null && !allHub.isLocalPlayer && allHub.Mode == ClientInstanceMode.ReadyClient)
					{
						try
						{
							method.Invoke(null, new object[2] { allHub, null });
						}
						catch
						{
						}
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("[Habilidades] No se pudo crear el menú propio en SSMenuSystem: " + (ex.InnerException?.Message ?? ex.Message));
			return false;
		}
	}

	private static Type BuscarTipo(string nombre)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				Type type = assembly.GetType(nombre, throwOnError: false);
				if (type != null)
				{
					return type;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	public static void Desregistrar()
	{
		Timing.KillCoroutines(_registroPendiente);
		try
		{
			Escuchar(si: false);
			if (_modo == Modo.Normal)
			{
				SettingBase.Unregister((Func<Player, bool>)null, (IEnumerable<SettingBase>)new SettingBase[1] { _tecla });
			}
			else if (_modo == Modo.SSMenu)
			{
				ServerSpecificSettingsSync.DefinedSettings = Array.Empty<ServerSpecificSettingBase>();
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Habilidades] Desregistrar: " + ex.Message);
		}
		_habilidades.Clear();
		_ultimaPulsacion.Clear();
		_ultimoEncendido.Clear();
		_ultimoPing.Clear();
		_usaTecla.Clear();
		_diagnosticados.Clear();
		_modo = Modo.Apagado;
	}

	public static void Asegurar()
	{
		if (_modo != Modo.Normal || _tecla?.Base == null)
		{
			return;
		}
		ServerSpecificSettingBase[] array = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
		ServerSpecificSettingBase[] array2 = array;
		foreach (ServerSpecificSettingBase serverSpecificSettingBase in array2)
		{
			if (serverSpecificSettingBase != null && serverSpecificSettingBase.SettingId == 47310 && serverSpecificSettingBase is SSKeybindSetting)
			{
				return;
			}
		}
		List<ServerSpecificSettingBase> list = new List<ServerSpecificSettingBase>(array);
		if (_cabecera?.Base != null)
		{
			list.Add(_cabecera.Base);
		}
		list.Add(_tecla.Base);
		ServerSpecificSettingsSync.DefinedSettings = list.ToArray();
		ServerSpecificSettingsSync.SendToAll();
		Log.Warn("[Habilidades] Otro plugin había borrado la tecla de subclase; se volvió a registrar.");
	}

	public static void EnviarA(Player player)
	{
		if (_modo == Modo.Normal && !(player?.ReferenceHub == null) && player.IsConnected && !player.IsNPC)
		{
			Asegurar();
			ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub);
		}
	}

	private static void Escuchar(bool si)
	{
		if (si != _escuchando)
		{
			if (si)
			{
				ServerSpecificSettingsSync.ServerOnSettingValueReceived += AlRecibirAjuste;
			}
			else
			{
				ServerSpecificSettingsSync.ServerOnSettingValueReceived -= AlRecibirAjuste;
			}
			_escuchando = si;
		}
	}

	private static void AlRecibirAjuste(ReferenceHub hub, ServerSpecificSettingBase ajuste)
	{
		if (!(ajuste is SSKeybindSetting sSKeybindSetting) || (sSKeybindSetting.SettingId != 47310 && sSKeybindSetting.SettingId != 47310 + HashMenu && (sSKeybindSetting.SettingId <= 47310 || sSKeybindSetting.SettingId > 147310 || !(sSKeybindSetting.Label == "Usar habilidad de subclase")) && !(sSKeybindSetting.Label == "Usar habilidad de subclase") && !(sSKeybindSetting.OriginalDefinition?.Label == "Usar habilidad de subclase") && (sSKeybindSetting.Label == null || sSKeybindSetting.Label.IndexOf("habilidad de subclase", StringComparison.OrdinalIgnoreCase) < 0)))
		{
			return;
		}
		Player player = Player.Get(hub);
		if (!(player == null))
		{
			if (_diagnosticados.Add(player.UserId))
			{
				Log.Info($"[Habilidades] Tecla recibida de {player.Nickname}: id={sSKeybindSetting.SettingId}, pulsada={sSKeybindSetting.SyncIsPressed}, modo={_modo}.");
			}
			if (sSKeybindSetting.SyncIsPressed)
			{
				PorTecla(player);
			}
		}
	}

	private static void PorTecla(Player player)
	{
		if (!(player == null))
		{
			float time = Time.time;
			if (!_ultimaTecla.TryGetValue(player, out var value) || !(time - value < 0.3f))
			{
				_ultimaTecla[player] = time;
				MarcarTecla(player);
				Usar(player);
			}
		}
	}

	private static void MarcarTecla(Player player)
	{
		if (_usaTecla.Add(player.UserId))
		{
			Log.Debug("[Habilidades] " + player.Nickname + " tiene la tecla de Server-specific: se le ignora el ALT del noclip.");
		}
		if (CargarGuardados().Add(player.UserId))
		{
			GuardarGuardados();
		}
	}

	private static HashSet<string> CargarGuardados()
	{
		if (_guardados != null)
		{
			return _guardados;
		}
		_guardados = new HashSet<string>();
		try
		{
			if (File.Exists(ArchivoTeclas))
			{
				string[] array = File.ReadAllLines(ArchivoTeclas);
				foreach (string text in array)
				{
					if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("#"))
					{
						_guardados.Add(text.Trim());
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("[Habilidades] No se pudo leer " + ArchivoTeclas + ": " + ex.Message);
		}
		return _guardados;
	}

	private static void GuardarGuardados()
	{
		string contenido = "# UserIds que usan su propia tecla de habilidad (Ajustes → Server-specific). El ALT del noclip se les ignora.\n" + string.Join("\n", CargarGuardados());
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				lock (_candado)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(ArchivoTeclas));
					File.WriteAllText(ArchivoTeclas, contenido);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("[Habilidades] No se pudo guardar " + ArchivoTeclas + ": " + ex.Message);
			}
		});
	}

	public static bool AltIgnorado(Player player)
	{
		if (player == null)
		{
			return false;
		}
		if (!_altIgnorado.TryGetValue(player, out var value))
		{
			value = (_altIgnorado[player] = new List<float>(3));
		}
		float ahora = Time.time;
		value.RemoveAll((float t) => ahora - t > 6f);
		value.Add(ahora);
		if (value.Count < 3)
		{
			return false;
		}
		_altIgnorado.Remove(player);
		_usaTecla.Remove(player.UserId);
		if (CargarGuardados().Remove(player.UserId))
		{
			GuardarGuardados();
		}
		EffectsHUD.Notify(player, "<color=#859CC7>Tu tecla de habilidad no está llegando: <b>ALT</b> vuelve a funcionar.</color>", 4f);
		Usar(player);
		return true;
	}

	public static bool UsaTecla(Player player)
	{
		return player != null && (_usaTecla.Contains(player.UserId) || CargarGuardados().Contains(player.UserId));
	}

	public static void Asignar(Player player, string nombre, float cooldown, Func<Player, bool> accion, bool linterna = true)
	{
		if (player == null)
		{
			return;
		}
		_usaTecla.Remove(player.UserId);
		_habilidades[player] = new Habilidad
		{
			Nombre = nombre,
			Cooldown = cooldown,
			Accion = accion,
			Linterna = (linterna && player.IsHuman)
		};
		if (player.IsHuman && linterna && !player.HasItem(ItemType.Flashlight) && !player.IsInventoryFull)
		{
			player.AddItem(ItemType.Flashlight);
		}
		string como = ((player.Role.Type == RoleTypeId.Scp079) ? "<color=#FFD400>el PING</color> (cuando está lista, tu ping la activa; también sirve .habilidad en la consola)" : ((player.IsHuman && linterna) ? "<color=#FFD400>[ALT]</color> o encendiendo la <color=#FFD400>linterna dos veces seguidas</color>" : "<color=#FFD400>[ALT]</color>"));
		Timing.CallDelayed(0.5f, delegate
		{
			if (player.IsConnected)
			{
				EffectsHUD.Notify(player, "<color=#859CC7>Habilidad: <b>" + nombre + "</b> — usala con " + como + ".</color>\n<size=18><color=#AAAAAA>Podés cambiar la tecla en Ajustes → Server-specific → «Usar habilidad de subclase».</color></size>", 8f);
			}
		});
	}

	public static object Exportar(Player player)
	{
		Habilidad value;
		return (player != null && _habilidades.TryGetValue(player, out value)) ? value : null;
	}

	public static void Importar(Player player, object datos)
	{
		if (!(player == null) && datos is Habilidad value)
		{
			_habilidades[player] = value;
			_ultimaPulsacion.Remove(player);
			_ultimoEncendido.Remove(player);
		}
	}

	public static void Quitar(Player player)
	{
		if (!(player == null))
		{
			_habilidades.Remove(player);
			_ultimaTecla.Remove(player);
			_altIgnorado.Remove(player);
			_ultimaPulsacion.Remove(player);
			_ultimoEncendido.Remove(player);
			_usaTecla.Remove(player.UserId);
		}
	}

	public static void Limpiar()
	{
		_habilidades.Clear();
		_ultimaTecla.Clear();
		_altIgnorado.Clear();
		_ultimaPulsacion.Clear();
		_ultimoEncendido.Clear();
		_ultimoPing.Clear();
		_usaTecla.Clear();
	}

	public static bool DobleLinterna(Player player)
	{
		float time = Time.time;
		if (_ultimoEncendido.TryGetValue(player, out var value) && time - value <= 1.5f)
		{
			_ultimoEncendido.Remove(player);
			return true;
		}
		_ultimoEncendido[player] = time;
		return false;
	}

	public static bool DoblePing(Player player)
	{
		float time = Time.time;
		if (_ultimoPing.TryGetValue(player, out var value) && time - value <= 1.5f)
		{
			_ultimoPing.Remove(player);
			return true;
		}
		_ultimoPing[player] = time;
		return false;
	}

	public static bool Tiene(Player player)
	{
		return player != null && _habilidades.ContainsKey(player);
	}

	public static bool UsaLinterna(Player player)
	{
		Habilidad value;
		return player != null && _habilidades.TryGetValue(player, out value) && value.Linterna;
	}

	public static string Nombre(Player player)
	{
		Habilidad value;
		return (player != null && _habilidades.TryGetValue(player, out value)) ? value.Nombre : null;
	}

	public static float Restante(Player player)
	{
		Habilidad value;
		return (player != null && _habilidades.TryGetValue(player, out value)) ? Mathf.Max(0f, value.ListaEn - Time.time) : 0f;
	}

	public static void Usar(Player player)
	{
		if (player == null || !player.IsConnected || !player.IsAlive)
		{
			return;
		}
		float time = Time.time;
		if (_ultimaPulsacion.TryGetValue(player, out var value) && time - value < 0.35f)
		{
			return;
		}
		_ultimaPulsacion[player] = time;
		if (!_habilidades.TryGetValue(player, out var h))
		{
			return;
		}
		if (time < h.ListaEn)
		{
			EffectsHUD.Notify(player, $"<color=#e32433>{h.Nombre}</color> <color=#AAAAAA>disponible en {Mathf.CeilToInt(h.ListaEn - time)}s.</color>", 2f);
			return;
		}
		bool flag;
		try
		{
			flag = h.Accion(player);
		}
		catch (Exception arg)
		{
			Log.Error($"[Habilidades] {h.Nombre} ({player.Nickname}): {arg}");
			flag = false;
		}
		if (!flag || h.Cooldown <= 0f)
		{
			return;
		}
		h.ListaEn = time + h.Cooldown;
		Var.Coroutines.Add(Timing.CallDelayed(h.Cooldown, delegate
		{
			if (player.IsConnected && player.IsAlive && _habilidades.TryGetValue(player, out var value2) && value2 == h)
			{
				EffectsHUD.Notify(player, "<color=#57e5f7>" + h.Nombre + " está lista.</color>");
			}
		}));
	}
}
