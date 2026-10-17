using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using MEC;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Scp3114;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class Cientifico
{
	private const float VidenteRangoSqr = 225f;

	private const float BombaSegundos = 300f;

	private const float Scp507Cooldown = 60f;

	private const float Scp990RadioSqr = 36f;

	private static readonly RoomType[] SalasMalas507 = new RoomType[10]
	{
		RoomType.Pocket,
		RoomType.HczTesla,
		RoomType.Hcz049,
		RoomType.Hcz106,
		RoomType.HczTestRoom,
		RoomType.Lcz914,
		RoomType.Hcz079,
		RoomType.EzCollapsedTunnel,
		RoomType.HczElevatorA,
		RoomType.HczElevatorB
	};

	public static void CientEjecSpawn(Player player)
	{
		player.ClearInventory();
		player.AddItem(ItemType.ArmorLight);
		player.AddItem(ItemType.KeycardZoneManager);
		player.AddItem(ItemType.GunCOM18);
		player.AddItem(ItemType.Medkit);
		player.AddAmmo(AmmoType.Nato9, 5);
		Var.CientEjec = player.UserId;
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Científico ejecutivo.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Cientista Executivo.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes un chaleco ligero, una tarjeta distinta, COM-18 y un botiquín. Al recibir daño corres más rápido. ]</color>", "<color=#FF8E00>[ Você tem um colete leve, um cartão diferente, uma COM-18 e um kit médico. ]</color>");
	}

	public static void Scp181Spawn(Player player)
	{
		Util.SetVida(player, 200f);
		player.ClearInventory();
		player.AddItem(ItemType.KeycardZoneManager);
		player.AddItem(ItemType.Medkit);
		Util.Pasiva(player, EffectType.DamageReduction, 80);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres el <color=#fc5e03>SCP-181.</color> ]</color>", "<color=#FF8E00>[ Você é o <color=#fc5e03>SCP-181.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 200 de vida, recibes menos daño y serás avisado si hay un SCP cerca de ti. ]</color>", "<color=#FF8E00>[ Você tem 200 de vida, recebe menos dano e será avisado se houver um SCP perto. ]</color>");
		Var.Scp181.Add(player);
		Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.ScpCheck(player)));
	}

	public static void Scp073Spawn(Player player)
	{
		Util.SetVida(player, 250f);
		Util.Pasiva(player, EffectType.MovementBoost, 15);
		Util.Pasiva(player, EffectType.DamageReduction, 40);
		Util.Bc(player, 5, "<color=#57e5f7>[<color=#ad6a1d>Eres el SCP-073</color>]</color>", "<color=#57e5f7>[<color=#ad6a1d>Você é o SCP-073</color>]</color>");
		Util.Bc(player, 5, "<color=#57e5f7>[<color=#ad6a1d>Tienes 250 de vida, eres un 15% más rápido y eres más resistente al daño, pero no puedes agarrar armas de fuego.</color>]</color>", "<color=#57e5f7>[<color=#ad6a1d>Você tem 250 de vida, é 15% mais rápido e mais resistente, mas não pode pegar armas de fogo.</color>]</color>");
		Var.SCP073.Add(player);
	}

	public static void FantasmaSpawn(Player player)
	{
		Util.SetVida(player, 100f);
		Var.CooldownFantasma.Remove(player);
		Util.Bc(player, 5, "<color=#57e5f7>[<color=#ad6a1d>Eres un Fantasma</color>]</color>", "<color=#57e5f7>[<color=#ad6a1d>Você é um Fantasma</color>]</color>");
		Util.Bc(player, 7, "<color=#57e5f7>[<color=#ad6a1d>Tienes 100 de vida y puedes volverte invisible durante 10 segundos con tu habilidad (ALT o doble toque de linterna).</color>]</color>", "<color=#57e5f7>[<color=#ad6a1d>Você tem 100 de vida e pode ficar invisível por 10 segundos com sua habilidade (ALT ou dois toques na lanterna).</color>]</color>");
		Var.Fantasma.Add(player);
		Habilidades.Asignar(player, "Invisibilidad", 0f, delegate(Player p)
		{
			if (Var.CooldownFantasma.Contains(p))
			{
				EffectsHUD.Notify(p, "<color=#e32433>Tu invisibilidad todavía se está recargando.</color>", 2f);
				return false;
			}
			Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.FantasmaInvisible(p)));
			return true;
		});
	}

	public static void ExpertoSpawn(Player player)
	{
		player.ClearInventory();
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.GunAK);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.KeycardMTFPrivate);
		player.AddAmmo(AmmoType.Nato762, 30);
		Var.Militar = player.UserId;
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Experto en contención de SCP.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Especialista em contenção de SCP.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes un chaleco de combate, un AK y un botiquín. ]</color>", "<color=#FF8E00>[ Você tem um colete de combate, um AK e um kit médico. ]</color>");
	}

	public static void VidenteSpawn(Player player)
	{
		Util.SetVida(player, 100f);
		Util.Bc(player, 5, "<color=#57e5f7>[<color=#ad6a1d>Eres un Vidente</color>]</color>", "<color=#57e5f7>[<color=#ad6a1d>Você é um Vidente</color>]</color>");
		Util.Bc(player, 7, "<color=#57e5f7>[<color=#ad6a1d>Tienes 100 de vida y puedes detectar a los SCPs a menos de 15 m a través de las paredes.</color>]</color>", "<color=#57e5f7>[<color=#ad6a1d>Você tem 100 de vida e pode detectar SCPs a menos de 15 m através das paredes.</color>]</color>");
		Var.Vidente.Add(player);
		Var.Coroutines.Add(Timing.RunCoroutine(VidenteRadar(player)));
	}

	internal static IEnumerator<float> VidenteRadar(Player vidente)
	{
		StringBuilder sb = new StringBuilder(256);
		while (Util.Vivo(vidente) && Var.Vidente.Contains(vidente))
		{
			sb.Clear();
			Vector3 pos = vidente.Position;
			foreach (Player scp in Player.List)
			{
				if (!scp.IsAlive || scp.Role.Team != 0 || scp.Role.Type == RoleTypeId.Scp079)
				{
					continue;
				}
				Role role = scp.Role;
				if (!(role is Exiled.API.Features.Roles.Scp3114Role esqueleto) || esqueleto.DisguiseStatus == Scp3114Identity.DisguiseStatus.None)
				{
					float dist2 = (scp.Position - pos).sqrMagnitude;
					if (!(dist2 > 225f))
					{
						sb.Append("<color=#e32433>").Append(Flecha(vidente, scp.Position)).Append(' ')
							.Append(scp.Role.Type)
							.Append(" - ")
							.Append((int)Mathf.Sqrt(dist2))
							.Append("m</color>\n");
					}
				}
			}
			if (sb.Length > 0)
			{
				EffectsHUD.Panel(vidente, "radar", "<color=#57e5f7><b>Presencias detectadas</b></color>\n" + sb.ToString().TrimEnd(new char[1] { '\n' }), 1.5f);
			}
			else
			{
				EffectsHUD.QuitarPanel(vidente, "radar");
			}
			yield return Timing.WaitForSeconds(1f);
		}
		EffectsHUD.QuitarPanel(vidente, "radar");
	}

	private static string Flecha(Player desde, Vector3 objetivo)
	{
		Vector3 to = objetivo - desde.Position;
		to.y = 0f;
		Vector3 forward = desde.CameraTransform.forward;
		forward.y = 0f;
		if (to.sqrMagnitude < 0.01f || forward.sqrMagnitude < 0.01f)
		{
			return "•";
		}
		float num = Vector3.SignedAngle(forward, to, Vector3.up);
		if (num >= -45f && num <= 45f)
		{
			return "↑";
		}
		if (num > 45f && num < 135f)
		{
			return "→";
		}
		if (num < -45f && num > -135f)
		{
			return "←";
		}
		return "↓";
	}

	public static void MacheteroSpawn(Player player)
	{
		Util.SetVida(player, 200f);
		player.ClearInventory();
		player.AddItem(ItemType.ArmorLight);
		player.AddItem(ItemType.SCP1509);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.KeycardMTFPrivate);
		Var.Machetero = player.UserId;
		Timing.CallDelayed(0.3f, delegate
		{
			if (!Util.Vivo(player) || Var.Machetero != player.UserId)
			{
				return;
			}
			try
			{
				player.Position = RoleTypeId.FacilityGuard.GetRandomSpawnLocation().Position;
			}
			catch (Exception ex)
			{
				Log.Debug("[Machetero] No se pudo llevar a Entrance: " + ex.Message);
			}
		});
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Machetero.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Machetero.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 200 de vida, un chaleco ligero, una tarjeta distinta, un machete y un botiquín, pero no puedes escapar. ]</color>", "<color=#FF8E00>[ Você tem 200 de vida, um colete leve, um cartão diferente, um facão e um kit médico, mas não pode escapar. ]</color>");
	}

	public static void BombaSpawn(Player player)
	{
		Var.Bomba = player.UserId;
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres una: <color=#fc5e03>Bomba.</color> ]</color>", "<color=#FF8E00>[ Você é uma: <color=#fc5e03>Bomba.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes que escapar antes de los 5 minutos o explotarás. ]</color>", "<color=#FF8E00>[ Você precisa escapar antes de 5 minutos ou vai explodir. ]</color>");
		Var.Coroutines.Add(Timing.RunCoroutine(BombaCuentaAtras(player, player.UserId, player.Role.Type)));
	}

	private static bool BombaSigueActiva(Player player, string userId, RoleTypeId rolInicial)
	{
		return Util.Vivo(player) && player.Role.Type == rolInicial && Var.Bomba == userId;
	}

	private static IEnumerator<float> BombaCuentaAtras(Player player, string userId, RoleTypeId rolInicial)
	{
		for (float restante = 300f; restante > 0f; restante -= 1f)
		{
			if (!BombaSigueActiva(player, userId, rolInicial))
			{
				yield break;
			}
			if (restante <= 30f)
			{
				EffectsHUD.Panel(player, "bomba", $"<color=#e32433><b>Explotas en {(int)restante} segundos</b></color>", 1.5f);
			}
			yield return Timing.WaitForSeconds(1f);
		}
		if (!BombaSigueActiva(player, userId, rolInicial))
		{
			yield break;
		}
		Var.Bomba = string.Empty;
		player.DisableEffect(EffectType.SpawnProtected);
		player.ArtificialHealth = 0f;
		player.HumeShield = 0f;
		player.Health = 1f;
		Item item = Item.Create(ItemType.GrenadeHE);
		if (item is ExplosiveGrenade granada)
		{
			granada.FuseTime = 0.1f;
			granada.SpawnActive(player.Position, player);
		}
		Timing.CallDelayed(0.5f, delegate
		{
			if (Util.Vivo(player))
			{
				player.Kill("Explotaste.");
			}
		});
	}

	public static void SCP990Spawn(Player player)
	{
		Util.SetVida(player, 120f);
		player.ClearInventory();
		player.AddItem(ItemType.KeycardZoneManager);
		player.AddItem(ItemType.Medkit);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres el <color=#fc5e03>SCP-990.</color> ]</color>", "<color=#FF8E00>[ Você é o <color=#fc5e03>SCP-990.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 120 de vida y los enemigos que se acerquen a ti se sentirán adormecidos y lentos. ]</color>", "<color=#FF8E00>[ Você tem 120 de vida e os inimigos que se aproximarem ficarão sonolentos e lentos. ]</color>");
		Var.Scp990.Add(player);
		Var.Coroutines.Add(Timing.RunCoroutine(Scp990Aura(player)));
	}

	internal static IEnumerator<float> Scp990Aura(Player dueño)
	{
		while (Util.Vivo(dueño) && Var.Scp990.Contains(dueño))
		{
			yield return Timing.WaitForSeconds(1f);
			if (!Util.Vivo(dueño) || !Var.Scp990.Contains(dueño))
			{
				break;
			}
			Vector3 pos = dueño.Position;
			Side lado = dueño.Role.Side;
			foreach (Player enemigo in Player.List)
			{
				if (!(enemigo == dueño) && enemigo.IsAlive && enemigo.Role.Side != lado && enemigo.Role.Side != Side.Tutorial && !((enemigo.Position - pos).sqrMagnitude > 36f))
				{
					enemigo.EnableEffect(EffectType.Slowness, 25, 2f);
				}
			}
		}
	}

	public static void SCP507Spawn(Player player)
	{
		Util.SetVida(player, 100f);
		player.ClearInventory();
		player.AddItem(ItemType.KeycardZoneManager);
		player.AddItem(ItemType.Medkit);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres el <color=#fc5e03>SCP-507.</color> ]</color>", "<color=#FF8E00>[ Você é o <color=#fc5e03>SCP-507.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Puedes saltar a una sala aleatoria de la zona en la que estás con tu habilidad: ALT o doble toque de linterna (cooldown de 60 segundos). ]</color>", "<color=#FF8E00>[ Você pode saltar para uma sala aleatória da zona onde você está com sua habilidade: ALT ou dois toques na lanterna (cooldown de 60 segundos). ]</color>");
		Var.CooldownScp507.Remove(player);
		Var.Scp507.Add(player);
		Habilidades.Asignar(player, "Salto dimensional", 0f, delegate(Player p)
		{
			Scp507Teleport(p);
			return true;
		});
	}

	public static void Scp507Teleport(Player player)
	{
		if (!Util.Vivo(player) || !Var.Scp507.Contains(player))
		{
			return;
		}
		if (Var.CooldownScp507.Contains(player))
		{
			player.ShowHint("<size=24><color=#e32433>El SCP-507 está en cooldown.</color></size>");
			return;
		}
		ZoneType zona = player.Zone;
		ZoneType zoneType = zona;
		bool flag = (((uint)(zoneType - 1) <= 1u || zoneType == ZoneType.Entrance) ? true : false);
		if (!flag || player.CurrentRoom == null || player.CurrentRoom.Type == RoomType.Pocket)
		{
			player.ShowHint("<size=24><color=#e32433>Desde aquí no puedes hacer el salto dimensional.</color></size>");
			return;
		}
		List<Room> list = Room.List.Where((Room r) => r != null && r.Zone == zona && r != player.CurrentRoom && r.Type != RoomType.Pocket).ToList();
		if (list.Count == 0)
		{
			player.ShowHint("<size=24><color=#e32433>No hay ninguna sala segura a la que saltar.</color></size>");
			return;
		}
		Vector3? vector = PuntoSeguroDelJuego(player, list);
		if (!vector.HasValue)
		{
			player.ShowHint("<size=24><color=#e32433>No hay ninguna sala segura a la que saltar.</color></size>");
			return;
		}
		player.Position = vector.Value;
		player.ShowHint("<size=24><color=#57e5f7><b>Has saltado a otra dimensión.</b></color></size>");
		Var.CooldownScp507.Add(player);
		Util.MarcarCooldown(player, "Salto dimensional", 60f);
		Var.Coroutines.Add(Timing.CallDelayed(60f, delegate
		{
			if (!(player == null))
			{
				Var.CooldownScp507.Remove(player);
				if (Util.Vivo(player) && Var.Scp507.Contains(player))
				{
					player.ShowHint("<size=24><color=#57e5f7>Tu salto dimensional está disponible otra vez.</color></size>");
				}
			}
		}));
	}

	private static Vector3? PuntoSeguroDelJuego(Player player, List<Room> salasZona)
	{
		try
		{
			if (!(player.ReferenceHub.roleManager.CurrentRole is IFpcRole fpcRole))
			{
				return null;
			}
			FacilityZone facilityZone = ((player.CurrentRoom?.Identifier != null) ? player.CurrentRoom.Identifier.Zone : FacilityZone.None);
			if (facilityZone == FacilityZone.None)
			{
				return null;
			}
			Pose[] posesForZone = Scp106PocketExitFinder.GetPosesForZone(facilityZone);
			if (posesForZone == null || posesForZone.Length == 0)
			{
				return null;
			}
			Vector3 actual = player.Position;
			List<Pose> list = posesForZone.Where((Pose x) => (x.position - actual).sqrMagnitude > 225f).ToList();
			Pose pose = ((list.Count > 0) ? list : posesForZone.ToList())[Var.random.Next((list.Count > 0) ? list.Count : posesForZone.Length)];
			float raycastRange = Scp106PocketExitFinder.GetRaycastRange(facilityZone);
			return SafeLocationFinder.GetSafePositionForPose(pose, raycastRange, fpcRole.FpcModule.CharController);
		}
		catch (Exception ex)
		{
			Log.Debug("[SCP-507] Punto seguro: " + ex.Message);
			return null;
		}
	}

	private static Vector3? PuntoSeguro507(Room sala)
	{
		if (SalasMalas507.Contains(sala.Type))
		{
			return null;
		}
		foreach (Door item in sala.Doors.OrderBy((Door _) => Var.random.Next()))
		{
			if (item == null || item.IsElevator || item.Type == DoorType.Scp914Gate)
			{
				continue;
			}
			Vector3 vector = item.Rotation * Vector3.forward;
			vector.y = 0f;
			if (!(vector.sqrMagnitude < 0.01f))
			{
				vector.Normalize();
				Vector3 vector2 = item.Position + vector * 1.6f;
				Vector3 vector3 = item.Position - vector * 1.6f;
				Vector3 vector4 = ((Vector3.Distance(vector2, sala.Position) <= Vector3.Distance(vector3, sala.Position)) ? vector2 : vector3);
				Vector3 vector5 = vector4 + Vector3.up * 1.2f;
				if (Physics.Raycast(vector5, Vector3.down, out var hitInfo, 3f) && !Physics.CheckSphere(vector5 + Vector3.up * 0.3f, 0.3f, -1, QueryTriggerInteraction.Ignore))
				{
					return hitInfo.point + Vector3.up * 1.1f;
				}
			}
		}
		return null;
	}

	public static void SCP1915Spawn(Player player)
	{
		Util.SetVida(player, 120f);
		player.ClearInventory();
		player.AddItem(ItemType.KeycardZoneManager);
		player.AddItem(ItemType.Medkit);
		player.AddAhp(75f, 75f, 0f, 0.9f);
		Util.Pasiva(player, EffectType.MovementBoost, 10);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres el <color=#fc5e03>SCP-1915.</color> ]</color>", "<color=#FF8E00>[ Você é o <color=#fc5e03>SCP-1915.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 120 de vida, 75 de escudo y eres algo más rápido. ]</color>", "<color=#FF8E00>[ Você tem 120 de vida, 75 de escudo e é um pouco mais rápido. ]</color>");
		Var.Scp1915.Add(player);
	}

	public static void ObservadorSpawn(Player player)
	{
		Util.SetVida(player, 100f);
		player.ClearInventory();
		player.AddItem(ItemType.ArmorLight);
		player.AddItem(ItemType.KeycardZoneManager);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.SCP1344);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Observador.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Observador.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes un chaleco ligero, un botiquín y las gafas del SCP-1344. ]</color>", "<color=#FF8E00>[ Você tem um colete leve, um kit médico e os óculos do SCP-1344. ]</color>");
		Var.Observador.Add(player);
	}
}
