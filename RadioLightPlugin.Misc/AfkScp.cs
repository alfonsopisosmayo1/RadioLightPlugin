using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class AfkScp
{
	private sealed class Foto
	{
		public Player Jugador;

		public RoleTypeId Rol;

		public Vector3 Posicion;

		public Quaternion Camara;
	}

	private sealed class Oferta
	{
		public Player Afk;

		public RoleTypeId Rol;

		public string NickAfk;

		public float Hasta;
	}

	private const float SegundosParaTomar = 20f;

	private static readonly List<Foto> _fotos = new List<Foto>();

	private static readonly List<Oferta> _ofertas = new List<Oferta>();

	private static string NombreRol(RoleTypeId r)
	{
		return r.ToString().Replace("Scp", "SCP-").Replace("SCP-0492", "SCP-049-2");
	}

	public static void AlEmpezarRonda()
	{
		_fotos.Clear();
		_ofertas.Clear();
		int valueOrDefault = (Plugin.Singleton?.Config?.AfkScpSegundos).GetValueOrDefault();
		if (valueOrDefault <= 0)
		{
			return;
		}
		Var.Coroutines.Add(Timing.CallDelayed(3f, delegate
		{
			if (Var.EventoEnCurso)
			{
				return;
			}
			foreach (Player item in Player.List)
			{
				if (!(item == null) && !item.IsNPC && item.IsAlive && item.Role.Side == Side.Scp)
				{
					RoleTypeId type = item.Role.Type;
					if ((type != RoleTypeId.Scp079 && type != RoleTypeId.Scp0492) || 1 == 0)
					{
						_fotos.Add(new Foto
						{
							Jugador = item,
							Rol = item.Role.Type,
							Posicion = item.Position,
							Camara = item.CameraTransform.rotation
						});
					}
				}
			}
		}));
		Var.Coroutines.Add(Timing.CallDelayed(3f + (float)valueOrDefault, Revisar));
	}

	private static void Revisar()
	{
		try
		{
			if (Var.EventoEnCurso || Round.IsEnded)
			{
				return;
			}
			foreach (Foto foto in _fotos)
			{
				Player jugador = foto.Jugador;
				if (!(jugador == null) && jugador.IsConnected && jugador.IsAlive && jugador.Role.Type == foto.Rol && Vector3.Distance(jugador.Position, foto.Posicion) < 1.5f && Quaternion.Angle(jugador.CameraTransform.rotation, foto.Camara) < 3f)
				{
					Ofrecer(jugador, foto.Rol);
				}
			}
			_fotos.Clear();
		}
		catch (Exception arg)
		{
			Log.Error($"[SCP AFK] {arg}");
		}
	}

	private static IEnumerable<Player> Candidatos()
	{
		return Player.List.Where((Player x) => x != null && !x.IsNPC && x.IsConnected && x.Role.Type == RoleTypeId.Spectator && !Var.GenteGulag.Contains(x) && !Var.ColaPractica.Contains(x));
	}

	private static void Ofrecer(Player afk, RoleTypeId rol)
	{
		if (!Candidatos().Any())
		{
			Log.Info($"[SCP AFK] {afk.Nickname} está AFK como {rol}, pero no hay espectadores para reemplazarlo.");
			return;
		}
		Oferta o = new Oferta
		{
			Afk = afk,
			Rol = rol,
			NickAfk = afk.Nickname,
			Hasta = Time.time + 20f
		};
		_ofertas.Add(o);
		Log.Info($"[SCP AFK] {afk.Nickname} está AFK como {rol}: se ofrece a los espectadores.");
		foreach (Player item in Candidatos())
		{
			item.Broadcast(20, "<size=30>\ud83d\udca4 <color=#e32433>" + NombreRol(rol) + "</color> <color=#859CC7>está AFK.</color></size>\n<size=24><color=#AAAAAA>Abre la consola (Ñ) y escribe</color> <b><color=#FFD400>.tomarscp</color></b> <color=#AAAAAA>para jugarlo.</color></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
		}
		afk.Broadcast(10, "<size=30><color=#e32433>Parece que estás AFK.</color> <color=#859CC7>Si no te mueves, un espectador va a tomar tu SCP.</color></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
		Var.Coroutines.Add(Timing.CallDelayed(20.5f, delegate
		{
			if (_ofertas.Remove(o))
			{
				DiscordLog.ScpAfk(NombreRol(o.Rol), o.NickAfk, null);
			}
		}));
	}

	public static string Tomar(Player quien)
	{
		if (quien == null)
		{
			return "Este comando se usa desde el juego.";
		}
		_ofertas.RemoveAll((Oferta o) => Time.time > o.Hasta || o.Afk == null || !o.Afk.IsConnected || !o.Afk.IsAlive || o.Afk.Role.Type != o.Rol);
		if (_ofertas.Count == 0)
		{
			return "No hay ningún SCP AFK para tomar ahora.";
		}
		if (quien.Role.Type != RoleTypeId.Spectator || Var.GenteGulag.Contains(quien) || Var.ColaPractica.Contains(quien))
		{
			return "Tienes que estar de espectador (fuera del gulag y de la práctica) para tomar un SCP.";
		}
		Oferta oferta = _ofertas[0];
		_ofertas.RemoveAt(0);
		Player afk = oferta.Afk;
		Vector3 pos = afk.Position;
		float vida = afk.Health;
		float humeShield = 0f;
		try
		{
			humeShield = afk.HumeShield;
		}
		catch
		{
		}
		afk.Role.Set(RoleTypeId.Spectator);
		afk.Broadcast(8, "<color=#e32433>Estabas AFK: un espectador tomó tu SCP.</color>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
		quien.Role.Set(oferta.Rol, SpawnReason.ForceClass, RoleSpawnFlags.None);
		Timing.CallDelayed(0.3f, delegate
		{
			if (!quien.IsConnected || quien.Role.Type != oferta.Rol)
			{
				return;
			}
			quien.Position = pos;
			quien.Health = Mathf.Clamp(vida, 1f, quien.MaxHealth);
			try
			{
				if (humeShield > 0f)
				{
					quien.HumeShield = humeShield;
				}
			}
			catch
			{
			}
		});
		foreach (Player item in Candidatos())
		{
			if (item != quien)
			{
				item.Broadcast(4, "<color=#859CC7>" + quien.Nickname + " tomó a " + NombreRol(oferta.Rol) + ".</color>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			}
		}
		Log.Info($"[SCP AFK] {quien.Nickname} tomó a {oferta.Rol} (antes {oferta.NickAfk}, AFK).");
		DiscordLog.ScpAfk(NombreRol(oferta.Rol), oferta.NickAfk, quien.Nickname);
		return "¡Ahora eres " + NombreRol(oferta.Rol) + "!";
	}
}
