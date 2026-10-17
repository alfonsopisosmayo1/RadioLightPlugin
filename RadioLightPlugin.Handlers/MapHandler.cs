using System;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Handlers;

public class MapHandler
{
	private static CoroutineHandle _anuncioScps;

	public void OnIDC(AnnouncingChaosEntranceEventArgs ev)
	{
		ev.IsAllowed = false;
	}

	public void OnMTF(AnnouncingNtfEntranceEventArgs ev)
	{
		// si entró un escuadrón especial (Nu-7 / Beta-7), su propio mensaje de CASSIE reemplaza al normal
		if (RadioLightPlugin.Subclases.Escuadrones.SilenciarAnuncioNtf())
		{
			ev.IsAllowed = false;
			return;
		}
		DateTime now = DateTime.Now;
		if (now.Month == 4 && now.Day == 1)
		{
			ev.IsAllowed = false;
			Otros.AprilFools();
			return;
		}
		ev.UnitName = "PAPA";
		ev.UnitNumber = Var.papanum++;
		if (now.Month == 12)
		{
			Otros.Navidad(ev);
		}
		else if (Var.random.Next(100) < 20)
		{
			Otros.MtfRandom(ev);
		}
	}

	public void OnScpCassie(AnnouncingScpTerminationEventArgs ev)
	{
		Timing.KillCoroutines(_anuncioScps);
		_anuncioScps = Timing.CallDelayed(15f, delegate
		{
			if (!Exiled.API.Features.Round.IsEnded && !Exiled.API.Features.Player.Get(Team.SCPs).Any((Exiled.API.Features.Player p) => p.IsAlive))
			{
				Sonido.Mapa("AnnouncAllContain.ogg");
				Announcer.Message(". . . . . . . . . . . . . . . . . . . . . . .", "Elementos del Destacamento Móvil <color=#0096FF>Nine Tailed Fox</color> y fuerzas de seguridad secundaria han logrado recontener a todos los <color=#EC2121>SCP</color> hostiles y vagantes dentro de la instalación.<split>Se le recomienda a todo el personal mantenerse en un lugar seguro hasta que todas las amenazas hayan sido neutralizadas, incluyendo posibles <color=#e32433>infiltrados</color> y personal fugado de <color=#FF8E00>Clase-D</color>.<split>Algunas zonas de la instalación puede que sigan bajo efectos anómalos los cuales serán abordados por el Destacamento Móvil Delta-7.");
			}
		});
		Var.Coroutines.Add(_anuncioScps);
	}

	public void OnDecontaminating(DecontaminatingEventArgs ev)
	{
		Log.Info("Descontaminación iniciada");
		Exiled.API.Features.Server.ExecuteCommand("/cleanup bulletholes");
		Exiled.API.Features.Server.ExecuteCommand("/cleanup blood");
		ServerHandler.BorrarLucesDeZona(ZoneType.LightContainment);
	}
}
