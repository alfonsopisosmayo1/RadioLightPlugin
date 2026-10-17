using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class Extra
{
	public const string NombreFuria = "Furia sanguinaria";

	public const string NombreDetonacion = "Detonación";

	private static readonly List<(Player Dueño, Vector3 Centro, float Hasta)> _explosionesScp = new List<(Player, Vector3, float)>();

	private static readonly Dictionary<Player, (Side Lado, float Cuando)> _ladoAlMorir = new Dictionary<Player, (Side, float)>();

	private static void Presentar(Player p, string color, string nombre, string descEs, string descPt)
	{
		p.ClearBroadcasts();
		Util.Bc(p, 6, "[ Eres un: <color=" + color + ">" + nombre + "</color> ]", "[ Você é um: <color=" + color + ">" + nombre + "</color> ]");
		Util.Bc(p, 10, "<color=" + color + ">[ " + descEs + " ]</color>", "<color=" + color + ">[ " + descPt + " ]</color>");
	}

	private static void Ok(Player p, string texto)
	{
		EffectsHUD.Notify(p, "<b>" + texto + "</b>");
	}

	private static void Falla(Player p, string texto)
	{
		EffectsHUD.Notify(p, "<color=#AAAAAA>" + texto + "</color>", 2f);
	}

	private static Player MasCercano(List<Player> lista, Vector3 desde)
	{
		return lista.OrderBy((Player x) => (x.Position - desde).sqrMagnitude).FirstOrDefault();
	}

	public static void RateroSpawn(Player p)
	{
		Presentar(p, "#C9A227", "Ratero", "Con tu habilidad le robas un objeto al azar al enemigo que tengas a menos de 2,5 m.", "Com sua habilidade você rouba um item aleatório do inimigo a menos de 2,5 m.");
		Habilidades.Asignar(p, "Mano rápida", 60f, delegate(Player s)
		{
			if (s.IsInventoryFull)
			{
				Falla(s, "Tu inventario está lleno.");
				return false;
			}
			Player player = MasCercano(Util.Cercanos(s.Position, 2.5f, (Player x) => x.IsHuman && Util.EsEnemigo(s, x) && x.Items.Count > 0), s.Position);
			if (player == null)
			{
				Falla(s, "No hay nadie con objetos a tu alcance.");
				return false;
			}
			Item item = player.Items.ElementAt(Var.random.Next(player.Items.Count));
			ItemType type = item.Type;
			player.RemoveItem(item);
			s.AddItem(type);
			player.ShowHint("<color=#e32433>Sentís que te falta algo en los bolsillos...</color>");
			Ok(s, $"<color=#C9A227>Robaste: {type}</color>");
			return true;
		});
	}

	public static void EscapistaSpawn(Player p)
	{
		Presentar(p, "#FF8E00", "Escapista", "Con tu habilidad te quitas las esposas (si las tienes) y corres más rápido y en silencio durante 6 s.", "Com sua habilidade você tira as algemas (se tiver) e corre mais rápido e em silêncio por 6 s.");
		Habilidades.Asignar(p, "Fuga", 90f, delegate(Player s)
		{
			bool isCuffed = s.IsCuffed;
			if (isCuffed)
			{
				s.RemoveHandcuffs();
			}
			s.EnableEffect(EffectType.MovementBoost, 30, 6f);
			s.EnableEffect(EffectType.SilentWalk, byte.MaxValue, 6f);
			Ok(s, isCuffed ? "<color=#FF8E00>¡Te zafaste de las esposas!</color>" : "<color=#FF8E00>¡A correr!</color>");
			return true;
		}, linterna: false);
	}

	public static void QuimicoSpawn(Player p)
	{
		Presentar(p, "#4CAF50", "Químico", "Con tu habilidad liberas un compuesto: tus aliados a 8 m se curan 25 de vida y los enemigos reciben 35 de daño y quedan envenenados 8 s.", "Com sua habilidade você libera um composto: aliados a 8 m curam 25 de vida e inimigos recebem 35 de dano e ficam envenenados por 8 s.");
		Habilidades.Asignar(p, "Compuesto volátil", 60f, delegate(Player s)
		{
			int num = 0;
			int num2 = 0;
			foreach (Player item in Util.Cercanos(s.Position, 8f, (Player x) => x.IsHuman || x.IsScp))
			{
				if (item.IsScp == s.IsScp && item.Role.Side == s.Role.Side)
				{
					Util.Curar(item, 25f);
					if (item != s)
					{
						num++;
					}
				}
				else if (item.IsScp != s.IsScp || Util.EsEnemigo(s, item))
				{
					item.Hurt(s, item.IsScp ? 80f : 35f, DamageType.Poison);
					if (item.IsAlive)
					{
						item.EnableEffect(EffectType.Poisoned, 8f);
					}
					num2++;
				}
			}
			Ok(s, $"<color=#4CAF50>Compuesto liberado: {num} curados, {num2} envenenados.</color>");
			return true;
		});
	}

	public static void HackerSpawn(Player p)
	{
		Presentar(p, "#00E5FF", "Hacker", "Con tu habilidad abres todas las puertas a 6 m (aunque no tengas tarjeta) y quedan trabadas abiertas 6 s.", "Com sua habilidade você abre todas as portas a 6 m (mesmo sem cartão) e elas ficam travadas abertas por 6 s.");
		Habilidades.Asignar(p, "Bypass", 70f, delegate(Player s)
		{
			if (Warhead.IsDetonated || Var.BetterOmegaWarheadActivated)
			{
				Falla(s, "El sistema de puertas está caído.");
				return false;
			}
			int num = 0;
			foreach (Door item in Door.List)
			{
				if (!item.IsElevator && !item.IsLocked && !((item.Position - s.Position).sqrMagnitude > 36f) && !MTF.EsPuertaReforzada(item) && !(item is BreakableDoor { IsDestroyed: not false }))
				{
					item.IsOpen = true;
					item.Lock(6f, DoorLockType.AdminCommand);
					num++;
				}
			}
			if (num == 0)
			{
				Falla(s, "No hay puertas cerca.");
				return false;
			}
			Ok(s, $"<color=#00E5FF>Acceso concedido: {num} puertas.</color>");
			return true;
		});
	}

	public static void AntidisturbiosSpawn(Player p)
	{
		Presentar(p, "#5B6370", "Antidisturbios", "Con tu habilidad levantas el escudo: 6 s recibiendo mucho menos daño y los enemigos a 4 m quedan aturdidos.", "Com sua habilidade você levanta o escudo: 6 s recebendo muito menos dano e inimigos a 4 m ficam atordoados.");
		Habilidades.Asignar(p, "Escudo antidisturbios", 60f, delegate(Player s)
		{
			s.EnableEffect(EffectType.DamageReduction, 60, 6f);
			foreach (Player item in Util.Cercanos(s.Position, 4f, (Player x) => x.IsHuman && Util.EsEnemigo(s, x)))
			{
				item.EnableEffect(EffectType.Concussed, 4f);
				item.EnableEffect(EffectType.Slowness, 30, 3f);
			}
			Ok(s, "<color=#5B6370>¡Escudo arriba!</color>");
			return true;
		});
	}

	public static void GranaderoSpawn(Player p)
	{
		if (!p.IsInventoryFull)
		{
			p.AddItem(ItemType.GrenadeHE);
		}
		Presentar(p, "#0096FF", "Granadero", "Empiezas con una granada extra y con tu habilidad fabricas otra cada minuto.", "Você começa com uma granada extra e com sua habilidade fabrica outra a cada minuto.");
		Habilidades.Asignar(p, "Reabastecer granada", 60f, delegate(Player s)
		{
			if (s.IsInventoryFull)
			{
				Falla(s, "No tienes lugar en el inventario.");
				return false;
			}
			s.AddItem(ItemType.GrenadeHE);
			Ok(s, "<color=#0096FF>Granada lista.</color>");
			return true;
		});
	}

	public static void ExploradorSpawn(Player p)
	{
		Presentar(p, "#0096FF", "Explorador", "Con tu habilidad lanzas un dron: todo tu equipo ve durante 8 s en qué sala está cada SCP.", "Com sua habilidade você lança um drone: toda sua equipe vê por 8 s em que sala está cada SCP.");
		Habilidades.Asignar(p, "Dron de reconocimiento", 90f, delegate(Player s)
		{
			StringBuilder stringBuilder = new StringBuilder("<color=#0096FF><b>DRON</b></color>");
			int num = 0;
			foreach (Player item in Player.List)
			{
				if (item.IsAlive && item.Role.Team == Team.SCPs)
				{
					stringBuilder.Append("\n<color=#EC2121>").Append(Util.NombreRol(item.Role.Type)).Append("</color> · ")
						.Append(Util.Sala(item));
					num++;
				}
			}
			if (num == 0)
			{
				Falla(s, "El dron no encontró ningún SCP.");
				return true;
			}
			string texto = stringBuilder.ToString();
			foreach (Player item2 in Player.List)
			{
				if (item2.IsAlive && item2.Role.Side == s.Role.Side)
				{
					EffectsHUD.Panel(item2, "dron", texto, 8f);
				}
			}
			return true;
		});
	}

	public static void ComandanteSpawn(Player p)
	{
		Presentar(p, "#003ECA", "Comandante de asalto", "Con tu habilidad ordenas un asalto: tú y tus aliados a 15 m ganan velocidad 8 s y 30 de escudo.", "Com sua habilidade você ordena um ataque: você e seus aliados a 15 m ganham velocidade por 8 s e 30 de escudo.");
		Habilidades.Asignar(p, "¡Al asalto!", 100f, delegate(Player s)
		{
			int num = 0;
			foreach (Player item in Util.Cercanos(s.Position, 15f, (Player x) => x.IsHuman && x.Role.Side == s.Role.Side))
			{
				item.EnableEffect(EffectType.MovementBoost, 15, 8f);
				item.AddAhp(30f, 30f, 1f, 0.7f, 5f);
				if (item != s)
				{
					EffectsHUD.Notify(item, "<color=#003ECA><b>¡" + s.Nickname + " ordena el asalto!</b></color>");
				}
				num++;
			}
			Ok(s, $"<color=#003ECA>¡Al asalto! ({num} soldados)</color>");
			return true;
		});
	}

	public static void SaboteadorSpawn(Player p)
	{
		Presentar(p, "#008F1E", "Saboteador", "Con tu habilidad cortas la luz de la sala donde estás durante 15 s (tú ves con visión nocturna) y bloqueas sus puertas 4 s.", "Com sua habilidade você corta a luz da sala onde está por 15 s (você enxerga com visão noturna) e tranca as portas por 4 s.");
		Habilidades.Asignar(p, "Sabotaje", 75f, delegate(Player s)
		{
			Room currentRoom = s.CurrentRoom;
			if (currentRoom == null || currentRoom.Type == RoomType.Pocket)
			{
				Falla(s, "Acá no hay nada que sabotear.");
				return false;
			}
			currentRoom.TurnOffLights(15f);
			s.EnableEffect(EffectType.NightVision, 15f);
			foreach (Door door in currentRoom.Doors)
			{
				if (!door.IsElevator && !door.IsLocked)
				{
					door.Lock(4f, DoorLockType.AdminCommand);
				}
			}
			Ok(s, "<color=#008F1E>Sala saboteada.</color>");
			return true;
		});
	}

	public static void SanguinarioSpawn(Player p)
	{
		Presentar(p, "#8B0000", "Sanguinario", "Te curas MATANDO: cada enemigo que matas te da 35 de vida (botiquines, analgésicos y SCP-500 solo te curan un 25%). Con tu habilidad entras en furia 6 s (más rápido) a cambio de 15 de vida.", "Você se cura MATANDO: cada inimigo morto dá 35 de vida (kits médicos, analgésicos e SCP-500 só curam 25%). Com sua habilidade entra em fúria por 6 s (mais rápido) em troca de 15 de vida.");
		Habilidades.Asignar(p, "Furia sanguinaria", 45f, delegate(Player s)
		{
			if (s.Health <= 20f)
			{
				Falla(s, "Estás demasiado herido.");
				return false;
			}
			s.Health -= 15f;
			s.EnableEffect(EffectType.MovementBoost, 25, 6f);
			s.EnableEffect(EffectType.Invigorated, 6f);
			Ok(s, "<color=#8B0000>¡FURIA!</color>");
			return true;
		});
	}

	public static void ResucitadorSpawn(Player p)
	{
		PresentarScp(p, "Resucitador", "#6D704F", "Reanimas el cadáver humano más cercano (4 m) como zombie, si su dueño está de espectador.", "Reanima o cadáver humano mais próximo (4 m) como zumbi.", "Reanimación", 120f, delegate(Player s)
		{
			Ragdoll ragdoll = (from r in Ragdoll.List
				where r != null && r.Owner != null && r.Owner.IsConnected && !r.Owner.IsAlive && !r.Owner.IsOverwatchEnabled && r.Role.GetTeam() != 0 && (r.Position - s.Position).sqrMagnitude <= 16f
				orderby (r.Position - s.Position).sqrMagnitude
				select r).FirstOrDefault();
			if (ragdoll == null)
			{
				Falla(s, "No hay ningún cadáver que reanimar.");
				return false;
			}
			Player dueño = ragdoll.Owner;
			Vector3 pos = ragdoll.Position + Vector3.up * 0.3f;
			if (Coroutines.EnZonaGulag(pos))
			{
				Falla(s, "Ese cadáver no se puede reanimar.");
				return false;
			}
			ragdoll.Destroy();
			Util.ConvertirEnZombie(dueño, pos);
			Ok(s, "<color=#6D704F>" + dueño.Nickname + " se levanta.</color>");
			return true;
		});
	}

	public static void InterferenciaSpawn(Player p)
	{
		PresentarScp(p, "Interferencia", "#6A00FF", "Los humanos en la sala de tu cámara quedan cegados 4 s y sordos 6 s.", "Os humanos na sala da sua câmera ficam cegos 4 s e surdos 6 s.", "Interferencia", 70f, delegate(Player s)
		{
			if (!(s.Role is Scp079Role { Camera: not null } scp079Role))
			{
				Falla(s, "No estás mirando por ninguna cámara.");
				return false;
			}
			Room sala = scp079Role.Camera.Room;
			Vector3 cam = scp079Role.Camera.Position;
			List<Player> list = Player.List.Where((Player x) => x.IsHuman && x.IsAlive && ((sala != null && x.CurrentRoom == sala) || (x.Position - cam).sqrMagnitude <= 225f)).ToList();
			if (list.Count == 0)
			{
				Falla(s, "No hay humanos cerca de esta cámara.");
				return false;
			}
			foreach (Player item in list)
			{
				item.EnableEffect(EffectType.Flashed, 2f);
				item.EnableEffect(EffectType.Blinded, byte.MaxValue, 4f);
				item.EnableEffect(EffectType.Deafened, byte.MaxValue, 6f);
				item.ShowHint("<color=#6A00FF>Las pantallas chillan y todo se vuelve blanco...</color>");
			}
			Ok(s, $"<color=#6A00FF>Interferencia: {list.Count} afectados.</color>");
			return true;
		});
	}

	public static void HipnotizadorSpawn(Player p)
	{
		PresentarScp(p, "Hipnotizador", "#C9C9C9", "Obligas al humano más cercano (hasta 25 m) a mirarte la cara.", "Obriga o humano mais próximo (até 25 m) a olhar para o seu rosto.", "Mirada forzada", 300f, delegate(Player s)
		{
			Player victima = HumanoParaMirar(s);
			if (victima == null)
			{
				Falla(s, "No hay ningún humano cerca.");
				return false;
			}
			for (int i = 0; i < 4; i++)
			{
				Timing.CallDelayed((float)i * 0.12f, delegate
				{
					if (Util.Vivo(victima) && Util.Vivo(s))
					{
						Vector3 position = s.CameraTransform.position;
						Vector3 vector = position - victima.CameraTransform.position;
						if (vector.sqrMagnitude > 0.01f)
						{
							victima.Rotation = Quaternion.LookRotation(vector.normalized);
						}
					}
				});
			}
			victima.ShowHint("<color=#C9C9C9>Algo te obliga a levantar la vista...</color>");
			Ok(s, "<color=#C9C9C9>" + victima.Nickname + " te miró.</color>");
			return true;
		});
	}

	private static Player HumanoParaMirar(Player scp)
	{
		Vector3 position = scp.CameraTransform.position;
		Player player = null;
		Player player2 = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		foreach (Player item in Player.List)
		{
			if (item == null || item.IsNPC || !item.IsAlive || !item.IsHuman || item.Role.Type == RoleTypeId.Tutorial)
			{
				continue;
			}
			float sqrMagnitude = (item.Position - scp.Position).sqrMagnitude;
			if (!(sqrMagnitude > 625f))
			{
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					player = item;
				}
				Vector3 position2 = item.CameraTransform.position;
				if ((!Physics.Linecast(position, position2, out var hitInfo, LayerMask.GetMask("Default", "Door", "Glass")) || !(hitInfo.collider != null) || !(Player.Get(hitInfo.collider) != item)) && sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					player2 = item;
				}
			}
		}
		return player2 ?? player;
	}

	public static void HundidorSpawn(Player p)
	{
		PresentarScp(p, "Hundidor", "#3B2A1A", "Los humanos a 6 m se hunden en el suelo: muy lentos durante 4 s.", "Os humanos a 6 m afundam no chão: muito lentos por 4 s.", "Arenas movedizas", 50f, delegate(Player s)
		{
			List<Player> list = Util.HumanosCerca(s, 6f);
			if (list.Count == 0)
			{
				Falla(s, "No hay nadie cerca.");
				return false;
			}
			foreach (Player item in list)
			{
				item.EnableEffect(EffectType.SinkHole, 4f);
				item.EnableEffect(EffectType.Slowness, 40, 4f);
			}
			Ok(s, $"<color=#3B2A1A>{list.Count} atrapados.</color>");
			return true;
		});
	}

	public static void AcechadorSpawn(Player p)
	{
		PresentarScp(p, "Acechador", "#7A7A7A", "Apareces justo detrás del humano más cercano (15 m).", "Aparece logo atrás do humano mais próximo (15 m).", "Acecho", 90f, delegate(Player s)
		{
			Player player = MasCercano(Util.HumanosCerca(s, 15f), s.Position);
			if (player == null)
			{
				Falla(s, "No hay presas cerca.");
				return false;
			}
			Vector3 forward = player.CameraTransform.forward;
			forward.y = 0f;
			forward = ((forward.sqrMagnitude < 0.01f) ? Vector3.forward : forward.normalized);
			Vector3 origin = player.Position + Vector3.up * 0.5f;
			if (Physics.Raycast(origin, -forward, 1.6f))
			{
				Falla(s, "No hay lugar detrás de tu presa.");
				return false;
			}
			s.Position = player.Position - forward * 1.3f;
			Ok(s, "<color=#7A7A7A>...</color>");
			return true;
		});
	}

	public static void SaltadorSpawn(Player p)
	{
		PresentarScp(p, "Saltador", "#C9A227", "Das un salto hacia adelante de hasta 7 m.", "Dá um salto para a frente de até 7 m.", "Salto", 25f, delegate(Player s)
		{
			Vector3 forward = s.CameraTransform.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.01f)
			{
				return false;
			}
			forward.Normalize();
			Vector3 origin = s.Position + Vector3.up * 0.5f;
			RaycastHit hitInfo;
			float num = (Physics.Raycast(origin, forward, out hitInfo, 7f) ? (hitInfo.distance - 0.8f) : 7f);
			if (num < 1f)
			{
				Falla(s, "No hay espacio para saltar.");
				return false;
			}
			s.Position += forward * num;
			return true;
		});
	}

	public static void CarroneroSpawn(Player p)
	{
		PresentarScp(p, "Carroñero", "#E8E0C8", "Devoras el cadáver más cercano (3 m) y te curas 150 de vida.", "Devora o cadáver mais próximo (3 m) e cura 150 de vida.", "Festín", 30f, delegate(Player s)
		{
			Ragdoll ragdoll = (from r in Ragdoll.List
				where r != null && (r.Position - s.Position).sqrMagnitude <= 9f
				orderby (r.Position - s.Position).sqrMagnitude
				select r).FirstOrDefault();
			if (ragdoll == null)
			{
				Falla(s, "No hay ningún cadáver cerca.");
				return false;
			}
			ragdoll.Destroy();
			Util.Curar(s, 150f);
			Ok(s, "<color=#E8E0C8>Festín: +150 de vida.</color>");
			return true;
		});
	}

	private static void PresentarScp(Player p, string nombre, string color, string descEs, string descPt, string habilidad, float cooldown, Func<Player, bool> accion)
	{
		Util.Bc(p, 6, "<color=#EC2121>[ " + Util.NombreRol(p.Role.Type) + ": <color=" + color + ">" + nombre + "</color> ]</color>", "<color=#EC2121>[ " + Util.NombreRol(p.Role.Type) + ": <color=" + color + ">" + nombre + "</color> ]</color>");
		bool flag = p.Role.Type == RoleTypeId.Scp079;
		Util.Bc(p, 12, "<color=#EC2121>[ " + descEs + " Se usa con <color=#FFD400>" + (flag ? "el PING (si está lista)" : "[ALT]") + "</color>. ]</color>", "<color=#EC2121>[ " + descPt + " Use com <color=#FFD400>" + (flag ? "o PING (se estiver pronta)" : "[ALT]") + "</color>. ]</color>");
		Habilidades.Asignar(p, habilidad, cooldown, accion, linterna: false);
	}

	public static void ExplosivoSpawn(Player p)
	{
		PresentarScp(p, "Explosivo", "#FF5722", "Explotas al morir. También puedes detonarte a propósito.", "Você explode ao morrer. Também pode se detonar de propósito.", "Detonación", 5f, delegate(Player s)
		{
			s.Kill("Se hizo estallar.");
			return true;
		});
	}

	public static void PutrefactoSpawn(Player p)
	{
		PresentarScp(p, "Putrefacto", "#8BC34A", "Vomitas sobre los humanos a 4 m: quedan envenenados 6 s y ven borroso.", "Vomita nos humanos a 4 m: ficam envenenados 6 s e com a visão embaçada.", "Vómito", 35f, delegate(Player s)
		{
			List<Player> list = Util.HumanosCerca(s, 4f);
			if (list.Count == 0)
			{
				Falla(s, "No hay nadie cerca.");
				return false;
			}
			foreach (Player item in list)
			{
				item.EnableEffect(EffectType.Poisoned, 6f);
				item.EnableEffect(EffectType.Blurred, 6f);
			}
			Ok(s, $"<color=#8BC34A>{list.Count} vomitados.</color>");
			return true;
		});
	}

	public static void ManiExplosivoSpawn(Player p)
	{
		Var.ManiExplosivo.Add(p);
		Util.Bc(p, 6, "<color=#EC2121>[ SCP-173: <color=#FF5722>Maní explosivo</color> ]</color>", "<color=#EC2121>[ SCP-173: <color=#FF5722>Amendoim explosivo</color> ]</color>");
		Util.Bc(p, 10, "<color=#EC2121>[ Si te matan, explotas con fuerza y te llevas a los que estén cerca. ]</color>", "<color=#EC2121>[ Se te matarem, você explode com força e leva junto quem estiver perto. ]</color>");
	}

	public static void AlCurarse(HealingEventArgs ev)
	{
		if (!(ev?.Player == null) && !(ev.Amount <= 0f) && !(Habilidades.Nombre(ev.Player) != "Furia sanguinaria"))
		{
			ev.Amount *= 0.25f;
		}
	}

	public static void AlCambiarItem(ChangingItemEventArgs ev)
	{
		Player player = ev?.Player;
		if (!(player == null) && ev.IsAllowed && ev.Item != null && player.IsAlive && ev.Item.Type != ItemType.SCP268 && player.IsEffectActive<Invisible>())
		{
			player.DisableEffect(EffectType.Invisible);
			EffectsHUD.Notify(player, "<color=#AAAAAA>Sacaste algo en la mano: ya no estás invisible.</color>");
		}
	}

	public static bool InvisibleSinAtacar(Player p)
	{
		if (p == null || !p.IsAlive || !p.IsEffectActive<Invisible>())
		{
			return false;
		}
		Item currentItem = p.CurrentItem;
		if (currentItem != null && currentItem.Type == ItemType.SCP268)
		{
			return false;
		}
		EffectsHUD.Notify(p, "<color=#57e5f7>No podés atacar mientras sos invisible.</color>", 2f);
		return true;
	}

	private static void MarcarExplosionScp(Player dueño, Vector3 centro)
	{
		float ahora = Time.time;
		_explosionesScp.RemoveAll(((Player Dueño, Vector3 Centro, float Hasta) e) => e.Hasta < ahora);
		_explosionesScp.Add((dueño, centro, ahora + 1.5f));
	}

	private static bool EsExplosionScpContraScp(HurtingEventArgs ev)
	{
		if (_explosionesScp.Count != 0 && !(ev.Player == null) && ev.Player.IsScp)
		{
			CustomDamageHandler damageHandler = ev.DamageHandler;
			if (damageHandler != null && damageHandler.Type == DamageType.Explosion)
			{
				float time = Time.time;
				foreach (var item in _explosionesScp)
				{
					if (item.Hasta < time || !((ev.Attacker != null) ? (ev.Attacker == item.Dueño) : ((ev.Player.Position - item.Centro).sqrMagnitude < 100f)))
					{
						continue;
					}
					return true;
				}
				return false;
			}
		}
		return false;
	}

	private static bool EsExplosionContraCompañero(HurtingEventArgs ev)
	{
		Player attacker = ev.Attacker;
		Player player = ev.Player;
		if (!(attacker == null) && !(player == null) && !(attacker == player))
		{
			CustomDamageHandler damageHandler = ev.DamageHandler;
			if (damageHandler != null && damageHandler.Type == DamageType.Explosion)
			{
				Side side = Side.None;
				(Side, float) value;
				if (attacker.IsAlive)
				{
					side = attacker.Role.Side;
				}
				else if (_ladoAlMorir.TryGetValue(attacker, out value) && Time.time - value.Item2 < 15f)
				{
					(side, _) = value;
				}
				return side != Side.None && side != Side.Tutorial && side == player.Role.Side;
			}
		}
		return false;
	}

	public static void AlHerir(HurtingEventArgs ev)
	{
		if (ev != null && !Round.IsEnded && !AntiTeamKill.GanadorElegido && (EsExplosionScpContraScp(ev) || EsExplosionContraCompañero(ev)))
		{
			ev.IsAllowed = false;
		}
		else if (ev?.Attacker != null && ev.Attacker != ev.Player && InvisibleSinAtacar(ev.Attacker))
		{
			ev.IsAllowed = false;
		}
	}

	public static void AlDisparar(ShootingEventArgs ev)
	{
		Player player = ev?.Player;
		if (!(player == null) && player.IsAlive && player.IsEffectActive<Invisible>())
		{
			Item currentItem = player.CurrentItem;
			if (currentItem == null || currentItem.Type != ItemType.SCP268)
			{
				player.DisableEffect(EffectType.Invisible);
				EffectsHUD.Notify(player, "<color=#AAAAAA>Disparaste: ya no estás invisible.</color>");
			}
		}
	}

	public static void AlMatar(Player atacante, Player victima)
	{
		if (!(atacante == null) && !(victima == null) && !(atacante == victima) && atacante.IsAlive && Habilidades.Nombre(atacante) == "Furia sanguinaria" && Util.EsEnemigo(atacante, victima))
		{
			Util.Curar(atacante, 35f);
			EffectsHUD.Notify(atacante, "<color=#8B0000>+35 de vida</color>", 2f);
		}
	}

	public static void AlMorir(Player victima)
	{
		if (victima == null)
		{
			return;
		}
		if (victima.IsAlive)
		{
			_ladoAlMorir[victima] = (victima.Role.Side, Time.time);
		}
		if (victima.Role.Type == RoleTypeId.Scp173 && Var.ManiExplosivo.Remove(victima))
		{
			Vector3 centro = victima.Position;
			MarcarExplosionScp(victima, centro);
			Timing.CallDelayed(0.1f, delegate
			{
				Map.Explode(centro, ProjectileType.FragGrenade, victima);
				Map.Explode(centro + Vector3.up * 0.5f, ProjectileType.FragGrenade, victima);
			});
		}
		else if (victima.Role.Type == RoleTypeId.Scp0492 && !(Habilidades.Nombre(victima) != "Detonación"))
		{
			Vector3 pos = victima.Position;
			Habilidades.Quitar(victima);
			MarcarExplosionScp(victima, pos);
			Timing.CallDelayed(0.1f, delegate
			{
				Map.Explode(pos, ProjectileType.FragGrenade, victima);
			});
		}
	}
}
