using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using MEC;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;
using RadioLightPlugin.Patches;
using UnityEngine;

namespace RadioLightPlugin.Misc;

public static class Scp0871
{
	public const string Nombre = "SCP-087-1";

	private const float Duracion = 10f;

	private const int MaximoVictimas = 3;

	private const float DistanciaToque = 1.4f;

	private const float Velocidad = 5f;

	private const float RadioPersecucion = 20f;

	private const float Gracia = 1f;

	private static Npc _npc;

	/// <summary>Máximo de apariciones solas por partida (las del comando de admin no cuentan para el límite).</summary>
	public const int MaxPorRonda = 3;

	private static int _veces;

	private static Room _sala;

	private static CoroutineHandle _vida;

	private static Vector3 _origen;

	private static readonly HashSet<RoomType> SalasProhibidas = new HashSet<RoomType>
	{
		RoomType.HczElevatorA,
		RoomType.HczElevatorB,
		RoomType.Hcz079,
		RoomType.EzCollapsedTunnel,
		RoomType.EzShelter,
		RoomType.HczNuke,
		RoomType.EzGateA,
		RoomType.EzGateB
	};

	public static bool Activo => _npc != null;

	public static bool Es(Player p)
	{
		return p != null && (p == _npc || (_npc != null && p.ReferenceHub == _npc.ReferenceHub) || (p.IsNPC && p.Nickname == "SCP-087-1"));
	}

	public static IEnumerator<float> Bucle()
	{
		if (!OpcionBool("Scp0871Activado", porDefecto: true))
		{
			yield break;
		}
		_veces = 0;
		do
		{
			float min = OpcionNum("Scp0871MinutosMin", 9f);
			float max = OpcionNum("Scp0871MinutosMax", 9f);
			if (max < min)
			{
				max = min;
			}
			yield return Timing.WaitForSeconds(UnityEngine.Random.Range(min, max) * 60f);
			while (!Round.IsEnded && _veces < MaxPorRonda && !Aparecer(null, forzado: false))
			{
				yield return Timing.WaitForSeconds(30f);
			}
		}
		while (!Round.IsEnded && _veces < MaxPorRonda);
	}

	public static void Limpiar()
	{
		Timing.KillCoroutines(_vida);
		Desaparecer();
	}

	public static bool Aparecer(Room salaForzada, bool forzado)
	{
		try
		{
			if (_npc != null || !Round.IsStarted || Round.IsEnded || Var.EventoEnCurso || Warhead.IsDetonated || (!forzado && _veces >= MaxPorRonda))
			{
				return false;
			}
			Room room = salaForzada ?? SalaConMasGente();
			if (room == null || !SalaValida(room))
			{
				return false;
			}
			List<Player> list = HumanosEn(room);
			int val = (int)OpcionNum("Scp0871MinimoGente", 2f);
			if (!forzado && list.Count < Math.Max(1, val))
			{
				return false;
			}
			Vector3? vector = LugarDeAparicion(room, list);
			if (!vector.HasValue)
			{
				return false;
			}
			_sala = room;
			_origen = vector.Value;
			_npc = Npc.Spawn("SCP-087-1", RoleTypeId.Scp106, ignored: true, vector.Value);
			Vector3 pos = vector.Value;
			Timing.CallDelayed(0.6f, delegate
			{
				Empezar(pos);
			});
			if (!forzado)
			{
				_veces++;
			}
			Log.Info($"[SCP-087-1] Apareció en {room.Type} ({list.Count} humanos). Apariciones esta partida: {_veces}/{MaxPorRonda}.");
			return true;
		}
		catch (Exception arg)
		{
			Log.Error($"[SCP-087-1] Aparecer: {arg}");
			Desaparecer();
			return false;
		}
	}

	private static void Empezar(Vector3 pos)
	{
		try
		{
			if (_npc == null || _npc.GameObject == null || _npc.Role.Type != RoleTypeId.Scp106)
			{
				Desaparecer();
				return;
			}
			_npc.Position = pos;
			_npc.IsGodModeEnabled = true;
			try
			{
				_npc.CustomInfo = "<color=#960018>SCP-087-1</color>";
				_npc.InfoArea = PlayerInfoArea.CustomInfo;
			}
			catch
			{
			}
			_sala.TurnOffLights(11f);
			foreach (Door door in _sala.Doors)
			{
				if (door != null && !door.IsElevator && !door.IsLocked && !(door is BreakableDoor { IsDestroyed: not false }))
				{
					door.IsOpen = false;
					door.Lock(2f, DoorLockType.AdminCommand);
				}
			}
			foreach (Player item in HumanosEn(_sala))
			{
				item.Broadcast(4, "<size=34><b><color=#8B0000>Algo se mueve en la oscuridad...</color></b></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			}
			// [audio] sonido de aparición para todos los humanos cerca (no solo los que el juego cuenta "dentro" de la sala)
			Cerca(pos, "miedito.ogg");
			Timing.KillCoroutines(_vida);
			_vida = Timing.RunCoroutine(Cazar());
		}
		catch (Exception arg)
		{
			Log.Error($"[SCP-087-1] Empezar: {arg}");
			Desaparecer();
		}
	}

	private static IEnumerator<float> Cazar()
	{
		float inicio = Time.time;
		int llevados = 0;
		Player objetivo = null;
		bool sonoMitad = false;
		while (_npc != null && _npc.GameObject != null && Time.time - inicio < 10f && llevados < 3 && !Round.IsEnded)
		{
			try
			{
				Vector3 pos = _npc.Position;
				// [audio] a mitad de la cacería vuelve a sonar para los que siguen en la sala
				if (!sonoMitad && Time.time - inicio >= 5f)
				{
					sonoMitad = true;
					Cerca(pos, "miedito1.ogg");
				}
				if (Time.time - inicio >= 1f)
				{
					foreach (Player h2 in Humanos())
					{
						if (llevados >= 3)
						{
							break;
						}
						if (!((h2.Position - pos).sqrMagnitude > 1.9599999f))
						{
							Llevar(h2);
							llevados++;
							if (objetivo == h2)
							{
								objetivo = null;
							}
						}
					}
				}
				// [fix] si se sale de su sala o se hunde (quedaba fuera del mapa), vuelve al punto de aparición
				Room room = Room.Get(pos);
				if (pos.y < _origen.y - 1.5f || pos.y > _origen.y + 4f || (room != null && room != _sala))
				{
					_npc.FollowedPlayer = null;
					objetivo = null;
					_npc.Position = _origen;
					pos = _origen;
				}
				Player cercano = (from h in Humanos()
					where h.CurrentRoom == _sala && (h.Position - pos).sqrMagnitude <= 400f && Mathf.Abs(h.Position.y - _origen.y) < 3f
					orderby (h.Position - pos).sqrMagnitude
					select h).FirstOrDefault();
				if (cercano != objetivo)
				{
					objetivo = cercano;
					if (objetivo != null)
					{
						_npc.Follow(objetivo, 1000f, 0.3f, 5f);
					}
					else
					{
						_npc.FollowedPlayer = null;
					}
				}
			}
			catch (Exception e)
			{
				Log.Debug("[SCP-087-1] Cazar: " + e.Message);
			}
			yield return Timing.WaitForSeconds(0.1f);
		}
		Desaparecer();
	}

	/// <summary>Le hace sonar <paramref name="archivo"/> a cada humano a 20 m o menos de <paramref name="pos"/>.</summary>
	private static void Cerca(Vector3 pos, string archivo)
	{
		try
		{
			foreach (Player item in Humanos())
			{
				if ((item.Position - pos).sqrMagnitude <= 400f)
				{
					Sonido.Para(item.Id, archivo);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[SCP-087-1] sonido: " + ex.Message);
		}
	}

	private static void Llevar(Player h)
	{
		try
		{
			h.EnableEffect(EffectType.PocketCorroding);
			Sonido.Para(h.Id, "miedito2.ogg");
			h.Broadcast(5, "<size=30><color=#8B0000><b>SCP-087-1</b> te arrastró a la dimensión de bolsillo.</color></size>", Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			Log.Info("[SCP-087-1] Se llevó a " + h.Nickname + ".");
		}
		catch (Exception ex)
		{
			Log.Debug("[SCP-087-1] Llevar: " + ex.Message);
		}
	}

	private static void Desaparecer()
	{
		Npc npc = _npc;
		Room sala = _sala;
		_npc = null;
		_sala = null;
		try
		{
			if (sala != null)
			{
				sala.AreLightsOff = false;
			}
		}
		catch
		{
		}
		if (npc == null)
		{
			return;
		}
		try
		{
			npc.FollowedPlayer = null;
			npc.Destroy();
		}
		catch (Exception ex)
		{
			Log.Debug("[SCP-087-1] Desaparecer: " + ex.Message);
		}
	}

	private static bool EsVictimaValida(Player p)
	{
		return p != null && p.IsConnected && p.IsAlive && !p.IsNPC && p.IsHuman && p.Role.Type != RoleTypeId.Tutorial && !p.IsGodModeEnabled && !Dimension106.EstaAdentro(p);
	}

	private static IEnumerable<Player> Humanos()
	{
		return Player.List.Where(EsVictimaValida);
	}

	private static List<Player> HumanosEn(Room sala)
	{
		return (from p in Humanos()
			where p.CurrentRoom == sala
			select p).ToList();
	}

	private static bool SalaValida(Room sala)
	{
		if (sala == null || SalasProhibidas.Contains(sala.Type) || sala.Type == RoomType.Pocket)
		{
			return false;
		}
		switch (sala.Zone)
		{
		case ZoneType.HeavyContainment:
		case ZoneType.Entrance:
			return true;
		case ZoneType.LightContainment:
			return !Map.IsLczDecontaminated;
		default:
			return false;
		}
	}

	private static Room SalaConMasGente()
	{
		return (from p in Humanos()
			where p.CurrentRoom != null && SalaValida(p.CurrentRoom)
			group p by p.CurrentRoom into g
			orderby g.Count() descending
			select g.Key).FirstOrDefault();
	}

	/// <summary>
	/// [fix] Dónde aparece: 1) en el centro de la sala si hay piso y lugar; 2) si no, en una salida de la dimensión
	/// de bolsillo de esa sala (como el salto de SCP-507); 3) si no, en la salida de bolsillo más cercana a la gente
	/// en esa zona. Si nada sirve, no aparece (antes caía en cualquier lado y quedaba fuera del mapa).
	/// </summary>
	private static Vector3? LugarDeAparicion(Room sala, List<Player> presentes)
	{
		Vector3? vector = PisoLibre(sala.Position);
		if (vector.HasValue && Room.Get(vector.Value) == sala)
		{
			return vector;
		}
		try
		{
			FacilityZone facilityZone = ((sala.Identifier != null) ? sala.Identifier.Zone : FacilityZone.None);
			Pose[] array = ((facilityZone != FacilityZone.None) ? Scp106PocketExitFinder.GetPosesForZone(facilityZone) : null);
			if (array != null && array.Length != 0)
			{
				Player player = presentes.FirstOrDefault((Player p) => p.ReferenceHub.roleManager.CurrentRole is IFpcRole) ?? Player.List.FirstOrDefault((Player p) => p.IsAlive && !p.IsNPC && p.ReferenceHub.roleManager.CurrentRole is IFpcRole);
				Vector3 centroSala = sala.Position;
				Vector3 centroGente = ((presentes.Count > 0) ? (presentes.Aggregate(Vector3.zero, (Vector3 acc, Player p) => acc + p.Position) / presentes.Count) : centroSala);
				List<Pose> list = array.Where((Pose x) => Room.Get(x.position) == sala).OrderBy((Pose x) => (x.position - centroSala).sqrMagnitude).ToList();
				if (list.Count == 0)
				{
					list = array.OrderBy((Pose x) => (x.position - centroGente).sqrMagnitude).Take(1).ToList();
				}
				foreach (Pose item in list)
				{
					Vector3? vector2 = null;
					if (player != null && player.ReferenceHub.roleManager.CurrentRole is IFpcRole fpcRole)
					{
						vector2 = SafeLocationFinder.GetSafePositionForPose(item, Scp106PocketExitFinder.GetRaycastRange(facilityZone), fpcRole.FpcModule.CharController);
					}
					Vector3? vector3 = PisoLibre(vector2 ?? item.position);
					if (vector3.HasValue)
					{
						return vector3;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[SCP-087-1] Punto de aparición: " + ex.Message);
		}
		return null;
	}

	/// <summary>Busca piso debajo del punto y que haya lugar para un cuerpo; devuelve la posición del jugador (centro).</summary>
	private static Vector3? PisoLibre(Vector3 punto)
	{
		try
		{
			int mask = ~LayerMask.GetMask("Player", "Hitbox", "Ragdoll", "Pickup", "InvisibleCollider", "Viewmodel");
			if (!Physics.Raycast(punto + Vector3.up * 1.5f, Vector3.down, out var hitInfo, 4f, mask, QueryTriggerInteraction.Ignore))
			{
				return null;
			}
			Vector3 point = hitInfo.point;
			if (Physics.CheckCapsule(point + Vector3.up * 0.6f, point + Vector3.up * 1.6f, 0.3f, mask, QueryTriggerInteraction.Ignore))
			{
				return null;
			}
			return point + Vector3.up * 1f;
		}
		catch
		{
			return null;
		}
	}

	private static bool OpcionBool(string nombre, bool porDefecto)
	{
		return ConsolaLimpia.Opcion(nombre, porDefecto);
	}

	private static float OpcionNum(string nombre, float porDefecto)
	{
		try
		{
			object obj = Plugin.Singleton?.Config;
			object obj2 = obj?.GetType().GetProperty(nombre)?.GetValue(obj);
			return (obj2 is float num) ? num : ((obj2 is int num2) ? ((float)num2) : ((!(obj2 is double num3)) ? porDefecto : ((float)num3)));
		}
		catch
		{
			return porDefecto;
		}
	}
}
