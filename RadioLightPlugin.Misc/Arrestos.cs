using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace RadioLightPlugin.Misc;

public static class Arrestos
{
	private static readonly Dictionary<Player, Player> _captor = new Dictionary<Player, Player>();

	private static CoroutineHandle _bucle;

	private static string Nombre(Player p)
	{
		return p?.DisplayNickname ?? p?.Nickname ?? "?";
	}

	public static void Iniciar()
	{
		Timing.KillCoroutines(_bucle);
		_bucle = Timing.RunCoroutine(Bucle());
	}

	public static void Detener()
	{
		Timing.KillCoroutines(_bucle);
		_captor.Clear();
	}

	public static void AlEsposar(HandcuffingEventArgs ev)
	{
		if (!ev.IsAllowed || ev.Player == null || ev.Target == null)
		{
			return;
		}
		Player captor = ev.Player;
		Player preso = ev.Target;
		Timing.CallDelayed(0.2f, delegate
		{
			if (!(preso == null) && preso.IsConnected && preso.IsCuffed)
			{
				_captor[preso] = captor;
				EffectsHUD.Notify(captor, "<color=#FFD400>Has arrestado a <b>" + Nombre(preso) + "</b>.</color>", 4f);
				EffectsHUD.Notify(preso, "<color=#e32433>Fuiste arrestado por <b>" + Nombre(captor) + "</b>.</color>", 4f);
			}
		});
	}

	public static void AlDesesposar(RemovedHandcuffsEventArgs ev)
	{
		Player target = ev.Target;
		if (target == null || !_captor.TryGetValue(target, out var value))
		{
			return;
		}
		_captor.Remove(target);
		if (!(value == null) && value.IsConnected && value.IsAlive)
		{
			Player player = ev.Player;
			if (player == null || player == value)
			{
				EffectsHUD.Notify(value, "<color=#859CC7>Liberaste a <b>" + Nombre(target) + "</b>.</color>", 4f);
				return;
			}
			EffectsHUD.Notify(value, "<color=#e32433><b>" + Nombre(target) + "</b> fue desarrestado por <b>" + Nombre(player) + "</b>.</color>", 5f);
		}
	}

	private static IEnumerator<float> Bucle()
	{
		StringBuilder sb = new StringBuilder(128);
		List<Player> presos = new List<Player>();
		while (true)
		{
			foreach (Player p3 in _captor.Keys.ToList())
			{
				if (p3 == null || !p3.IsConnected || !p3.IsCuffed)
				{
					_captor.Remove(p3);
				}
			}
			foreach (Player captor in Player.List)
			{
				if (!captor.IsAlive || captor.IsNPC)
				{
					continue;
				}
				presos.Clear();
				foreach (Player p2 in Player.List)
				{
					if (p2.IsAlive && p2.IsCuffed && p2.Cuffer == captor)
					{
						presos.Add(p2);
					}
				}
				if (presos.Count == 0)
				{
					continue;
				}
				sb.Clear();
				sb.Append("<color=#FFD400><b>Prisioneros (").Append(presos.Count).Append(")</b></color>");
				foreach (Player p in presos)
				{
					sb.Append("\n<color=#FF8E00>• ").Append(Nombre(p)).Append("</color>");
				}
				EffectsHUD.Panel(captor, "presos", sb.ToString(), 1.5f);
			}
			yield return Timing.WaitForSeconds(1f);
		}
	}
}
