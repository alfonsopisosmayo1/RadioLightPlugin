using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using MEC;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(ClientCommandHandler))]
public class HolaCommand : ICommand
{
	private const string Charset = "abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%&*";

	private static readonly Random Rng = new Random();

	public string Command => "hola";

	public string[] Aliases => Array.Empty<string>();

	public string Description => "efecto y hetero el que use";

	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (arguments.Count < 1)
		{
			response = "Uso: .hola texto aca";
			return false;
		}
		string text = string.Join(" ", arguments);
		Player player = Player.Get(sender);
		if (player == null)
		{
			response = "Solo in-game";
			return false;
		}
		Timing.RunCoroutine(DecodeEffect(player, text));
		response = "Hint enviado";
		return true;
	}

	private static IEnumerator<float> DecodeEffect(Player player, string text, string color = "859CC7")
	{
		int len = text.Length;
		int revealed = 0;
		float tickTime = 0.01f;
		int ticksPerChar = 1;
		int tick = 0;
		while (revealed < len)
		{
			StringBuilder sb = new StringBuilder(len);
			for (int i = 0; i < len; i++)
			{
				if (text[i] == ' ')
				{
					sb.Append(' ');
				}
				else if (i < revealed)
				{
					sb.Append(text[i]);
				}
				else
				{
					sb.Append("abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%&*"[Rng.Next("abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%&*".Length)]);
				}
			}
			player.ShowHint($"<color=#{color}>{sb}</color>", tickTime + 0.1f);
			yield return Timing.WaitForSeconds(tickTime);
			tick++;
			if (tick >= ticksPerChar)
			{
				tick = 0;
				revealed++;
			}
		}
		player.ShowHint("<color=#" + color + ">" + text + "</color>");
	}
}
