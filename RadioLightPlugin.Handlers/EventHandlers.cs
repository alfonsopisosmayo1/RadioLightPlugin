using System.Collections.Generic;
using System.Text;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Scp049;
using Exiled.Events.EventArgs.Scp0492;
using Exiled.Events.EventArgs.Scp096;
using Exiled.Events.EventArgs.Scp330;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Handlers;

public class EventHandlers
{
	private static readonly Color ColorLuzNormal = new Color(0.286f, 0.651f, 0.431f);

	private static readonly Color ColorLuzOmega = new Color(0f, 0.2f, 0.4f);

	private static readonly string Candy1 = Degradado("Agarraste 1 caramelo en total.", new Color32(byte.MaxValue, 181, 77, byte.MaxValue), new Color32(byte.MaxValue, 199, 120, byte.MaxValue));

	private static readonly string Candy2 = Degradado("Agarraste 2 caramelos en total.", new Color32(byte.MaxValue, 162, 31, byte.MaxValue), new Color32(byte.MaxValue, 175, 64, byte.MaxValue));

	private static readonly string Candy3 = Degradado("¡Eso te pasa por agarrar más de 2, ladrón!", new Color32(byte.MaxValue, 0, 0, byte.MaxValue), new Color32(byte.MaxValue, 40, 40, byte.MaxValue));

	public static void StopOmegaWarhead()
	{
		Var.BetterOmegaWarheadActivated = false;
		foreach (CoroutineHandle omegaCoroutine in Var.OmegaCoroutines)
		{
			Timing.KillCoroutines(omegaCoroutine);
		}
		Var.OmegaCoroutines.Clear();
		foreach (Room room in Var.RoomList)
		{
			room.ResetColor();
		}
		foreach (Exiled.API.Features.Toys.Light lucesPuerta in Var.lucesPuertas)
		{
			lucesPuerta.Color = ColorLuzNormal;
		}
	}

	public static void StartOmegaWarhead()
	{
		if (Var.BetterOmegaWarheadActivated || Round.IsEnded)
		{
			return;
		}
		Warhead.Stop();
		Warhead.IsLocked = true;
		switch (Var.random.Next(3))
		{
		case 0:
			Var.OmegaTimer = 186f;
			Var.OmegaCoroutines.Add(Timing.CallDelayed(13f, delegate
			{
				Sonido.Mapa("Countdown.ogg");
			}));
			break;
		case 1:
			Var.OmegaTimer = 194f;
			Sonido.Mapa("Sudno.ogg");
			break;
		default:
			Var.OmegaTimer = 196f;
			Sonido.Mapa("Kino.ogg");
			break;
		}
		Var.OmegaEndTime = Time.time + Var.OmegaTimer;
		Var.BetterOmegaWarheadActivated = true;
		Var.OmegaCoroutines.Add(Timing.RunCoroutine(Coroutines.OmegaWarheadTimer()));
		Var.OmegaCoroutines.Add(Timing.RunCoroutine(Otros.Transition(new Color(1f, 0.9098f, 0.7686f), new Color(0.2745f, 0.5882f, 0.8824f), 5f)));
		Map.Broadcast(5, "<b><color=#e32433>Detonación de la <color=#4696E1>Omega Warhead</color> automática iniciada.</color></b>");
		foreach (Door item in Door.List)
		{
			if (!item.IsElevator)
			{
				item.IsOpen = true;
				item.Lock(69420f, DoorLockType.Warhead);
			}
		}
		foreach (Exiled.API.Features.Toys.Light lucesPuerta in Var.lucesPuertas)
		{
			lucesPuerta.Color = ColorLuzOmega;
		}
		Var.OmegaCoroutines.Add(Timing.CallDelayed(Var.OmegaTimer, delegate
		{
			if (!Var.BetterOmegaWarheadActivated)
			{
				return;
			}
			foreach (Player item2 in new List<Player>(Player.List))
			{
				if (item2.IsAlive)
				{
					item2.Kill("Vaporizado por la Omega Warhead.");
				}
			}
			Warhead.Detonate();
			Warhead.Shake();
			foreach (Lift item3 in Lift.List)
			{
				item3.ChangeLock(DoorLockReason.Warhead);
			}
		}));
	}

	public void On049touch(AttackingEventArgs ev)
	{
		if (ev.IsAllowed && !(ev.Target == null) && !ev.Target.IsGodModeEnabled && Var.random.Next(100) < 5)
		{
			ev.Target.ClearInventory();
			Util.ConvertirEnZombie(ev.Target, null, usarSpawnpoint: true);
		}
	}

	public void On0492eat(ConsumedCorpseEventArgs ev)
	{
		if (ev.Player == null || ev.Ragdoll == null)
		{
			return;
		}
		Player owner = ev.Ragdoll.Owner;
		if (owner == null || !owner.IsConnected || owner.IsAlive || owner.IsOverwatchEnabled || Var.random.Next(100) >= 10)
		{
			return;
		}
		Vector3 position = ev.Player.Position;
		ev.Ragdoll.Destroy();
		Util.ConvertirEnZombie(owner, position, usarSpawnpoint: true);
	}

	public void OnChargingJailbird(ChargingJailbirdEventArgs ev)
	{
		if (Var.Spy.Contains(ev.Player))
		{
			ev.IsAllowed = true;
		}
	}

	public void OnEatingCandy(EatingScp330EventArgs ev)
	{
		string[] array = Plugin.Singleton?.Config?.TextosCarameloRosa;
		if (ev.Candy.Kind == CandyKindID.Pink && array != null && array.Length != 0 && Var.random.Next(2) == 0)
		{
			ev.Player.ShowHint(array[Var.random.Next(array.Length)]);
		}
	}

	public static string Degradado(string texto, Color32 desde, Color32 hasta)
	{
		StringBuilder stringBuilder = new StringBuilder(texto.Length * 24);
		int num = Mathf.Max(1, texto.Length - 1);
		for (int i = 0; i < texto.Length; i++)
		{
			char c = texto[i];
			if (c == ' ')
			{
				stringBuilder.Append(' ');
				continue;
			}
			Color32 color = Color32.Lerp(desde, hasta, (float)i / (float)num);
			stringBuilder.Append("<color=#").Append(color.r.ToString("X2")).Append(color.g.ToString("X2"))
				.Append(color.b.ToString("X2"))
				.Append('>')
				.Append(c)
				.Append("</color>");
		}
		return stringBuilder.ToString();
	}

	public void OnCandy(InteractingScp330EventArgs ev)
	{
		Plugin singleton = Plugin.Singleton;
		bool flag = singleton != null && singleton.Config?.Brazuca == true;
		string message = ev.UsageCount switch
		{
			0 => flag ? "<color=#a1ffeb>Você pegou 1 doce no total.</color>" : Candy1, 
			1 => flag ? "<color=#a1ffeb>Você pegou 2 doces no total.</color>" : Candy2, 
			_ => flag ? "<color=#f00010>Isso é o que você ganha por pegar mais de 2, ladrão!</color>" : Candy3, 
		};
		ev.Player.ShowHint(message, 5f);
		if (Var.random.Next(100) < 10)
		{
			ev.Candy = CandyKindID.Pink;
		}
	}

	public void OnSee096(AddingTargetEventArgs ev)
	{
		ev.Target?.EnableEffect(EffectType.Traumatized, 20f);
	}
}
