using System;
using System.Collections.Generic;
using CommandSystem;
using Exiled.API.Features;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class ChatProximidad : ICommand
{
	private const int MaxLargo = 60;

	private const float Duracion = 5f;

	private const float Alcance = 18f;

	private static readonly Dictionary<Exiled.API.Features.Player, (TextToy Texto, CoroutineHandle Bucle)> _activos = new Dictionary<Exiled.API.Features.Player, (TextToy, CoroutineHandle)>();

	private static readonly Dictionary<Exiled.API.Features.Player, float> _ultimo = new Dictionary<Exiled.API.Features.Player, float>();

	public string Command { get; } = "chat";


	public string[] Aliases { get; } = new string[4] { "c", "say", "hablar", "texto" };


	public string Description { get; } = "Mensaje de proximidad encima de tu cabeza: .c <mensaje>";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		Exiled.API.Features.Player player = Exiled.API.Features.Player.Get(sender);
		if (player == null)
		{
			response = "Este comando se usa desde el juego.";
			return false;
		}
		if (!player.IsAlive)
		{
			response = "Solo puedes usar esto estando vivo.";
			return false;
		}
		if (arguments.Count == 0)
		{
			response = $"Uso: .c <mensaje> (hasta {60} caracteres)";
			return false;
		}
		if (_ultimo.TryGetValue(player, out var value) && Time.time - value < 1.5f)
		{
			response = "Espera un momento antes de mandar otro mensaje.";
			return false;
		}
		string text = string.Join(" ", arguments).Trim();
		if (text.Length == 0)
		{
			response = "No puedes enviar un mensaje vacío.";
			return false;
		}
		if (text.Length > 60)
		{
			response = $"El mensaje es muy largo: máximo {60} caracteres (el tuyo tiene {text.Length}).";
			return false;
		}
		if (Vip.EsPalabraRacista(text.ToLower()) || text.IndexOf('<') >= 0)
		{
			response = "No puedes mandar ese mensaje.";
			return false;
		}
		text = text.Replace('{', '(').Replace('}', ')');
		_ultimo[player] = Time.time;
		try
		{
			Quitar(player);
			TextToy textToy = TextToy.Create(player.Position + Vector3.up * 1.15f, Quaternion.identity);
			textToy.DisplaySize = new Vector2(70f, 15f);
			textToy.TextFormat = "<color=#2C378A><size=1>\ud83d\udd09:</size></color><color=#859CC7><size=1>" + text + "</size></color>";
			CoroutineHandle item = Timing.RunCoroutine(Seguir(player, textToy));
			_activos[player] = (textToy, item);
		}
		catch (Exception ex)
		{
			Log.Warn("[Chat] " + ex.Message);
			response = "No se pudo mostrar el mensaje.";
			return false;
		}
		int num = 0;
		foreach (Exiled.API.Features.Player item2 in Exiled.API.Features.Player.List)
		{
			if (item2 != null && item2 != player && item2.IsAlive && (item2.Position - player.Position).sqrMagnitude <= 324f)
			{
				num++;
			}
		}
		response = $"\ud83d\udcac Tu mensaje se envió a {num} jugadores.";
		return true;
	}

	private static IEnumerator<float> Seguir(Exiled.API.Features.Player p, TextToy toy)
	{
		float fin = Time.time + 5f;
		while (Time.time < fin && p != null && p.IsConnected && p.IsAlive && toy?.Base != null)
		{
			Vector3 mira = p.CameraTransform.forward;
			mira.y = 0f;
			toy.Transform.position = p.Position + Vector3.up * 1.15f;
			if (mira.sqrMagnitude > 0.01f)
			{
				toy.Transform.rotation = Quaternion.LookRotation(-mira.normalized);
			}
			yield return Timing.WaitForSeconds(0.1f);
		}
		if (p != null && _activos.TryGetValue(p, out var a) && a.Texto == toy)
		{
			_activos.Remove(p);
		}
		try
		{
			toy?.Destroy();
		}
		catch
		{
		}
	}

	private static void Quitar(Exiled.API.Features.Player p)
	{
		if (!_activos.TryGetValue(p, out (TextToy, CoroutineHandle) value))
		{
			return;
		}
		_activos.Remove(p);
		Timing.KillCoroutines(value.Item2);
		try
		{
			value.Item1?.Destroy();
		}
		catch
		{
		}
	}
}
