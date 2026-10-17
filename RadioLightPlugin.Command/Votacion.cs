using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandSystem;
using Exiled.API.Features;
using MEC;
using RadioLightPlugin.Patches;

namespace RadioLightPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class Votacion : ICommand
{
	public static readonly HttpClient _client = new HttpClient();

	private const int DURACION_SEGUNDOS = 30;

	public static bool VotacionActiva = false;

	public static string Pregunta = string.Empty;

	public static List<string> Opciones = new List<string>();

	public static Dictionary<int, int> Votos = new Dictionary<int, int>();

	public static HashSet<string> JugadoresQueVotaron = new HashSet<string>();

	public static string NombreIniciador = string.Empty;

	internal static DateTime _inicio = DateTime.MinValue;

	private static string _mensajeInicio = string.Empty;

	public bool SanitizeResponse => false;

	public string Command { get; } = "votacion";


	public string[] Aliases { get; } = null;


	public string Description { get; } = "Inicia una votación. Uso: votacion \"pregunta\" \"op1\" \"op2\" ...";


	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (VotacionActiva && (DateTime.UtcNow - _inicio).TotalSeconds > 40.0)
		{
			VotacionActiva = false;
		}
		if (VotacionActiva)
		{
			response = "Ya hay una votación en curso.";
			return false;
		}
		string input = string.Join(" ", arguments);
		List<string> list = ParsearComillas(input);
		if (list.Count < 3)
		{
			response = "Uso: votacion \"pregunta\" \"op1\" \"op2\" [\"op3\" ...]\nNecesitas al menos 2 opciones.";
			return false;
		}
		if (list.Count - 1 > 9)
		{
			response = "Máximo 9 opciones.";
			return false;
		}
		string text = "Servidor";
		if (sender is CommandSender commandSender)
		{
			text = commandSender.Nickname ?? "Servidor";
		}
		NombreIniciador = text;
		Pregunta = list[0];
		Opciones = new List<string>();
		Votos = new Dictionary<int, int>();
		JugadoresQueVotaron = new HashSet<string>();
		for (int i = 1; i < list.Count; i++)
		{
			Opciones.Add(list[i]);
			Votos[i] = 0;
		}
		VotacionActiva = true;
		_inicio = DateTime.UtcNow;
		string text2 = string.Join("  ", Opciones.Select((string op, int idx) => $"{op}({idx + 1})"));
		string text3 = "<color=#2C378A>[VOTACIÓN INICIADA POR " + text + "]</color>\n<color=#596AA9>'" + Pregunta + "'</color>\n<color=#859CC7>" + text2 + "</color>\n<color=#5A6FAF>Para votar, abre la consola (Ñ) y escribe \".votar número\"</color>";
		AnunciarATodos(text3, 30);
		_mensajeInicio = text3;
		Timing.RunCoroutine(FinalizarVotacion());
		response = $"Votación iniciada: \"{Pregunta}\" con {Opciones.Count} opciones.";
		return true;
	}

	internal static void AnunciarATodos(string mensaje, ushort segundos)
	{
		try
		{
			Broadcast broadcast = Server.Broadcast;
			broadcast.RpcClearElements();
			broadcast.RpcAddElement(mensaje, segundos, Broadcast.BroadcastFlags.Normal);
		}
		catch (Exception ex)
		{
			Log.Warn("[Votación] No se pudo mandar el anuncio: " + ex.Message);
			ConsolaLimpia.SinHud(delegate
			{
				Map.Broadcast(segundos, mensaje, Broadcast.BroadcastFlags.Normal, shouldClearPrevious: true);
			});
		}
	}

	private static List<string> ParsearComillas(string input)
	{
		List<string> list = new List<string>();
		int i = 0;
		while (i < input.Length)
		{
			for (; i < input.Length && input[i] == ' '; i++)
			{
			}
			if (i >= input.Length)
			{
				break;
			}
			if (input[i] == '"')
			{
				i++;
				int num = i;
				for (; i < input.Length && input[i] != '"'; i++)
				{
				}
				list.Add(input.Substring(num, i - num));
				if (i < input.Length)
				{
					i++;
				}
			}
			else
			{
				int num2 = i;
				for (; i < input.Length && input[i] != ' '; i++)
				{
				}
				list.Add(input.Substring(num2, i - num2));
			}
		}
		return list;
	}

	private static IEnumerator<float> FinalizarVotacion()
	{
		for (int pasado = 10; pasado < 30; pasado += 10)
		{
			yield return Timing.WaitForSeconds(10f);
			if (!VotacionActiva)
			{
				yield break;
			}
			int quedan = 30 - pasado;
			AnunciarATodos(_mensajeInicio + $"\n<size=22><color=#AAAAAA>Quedan {quedan} s · {JugadoresQueVotaron.Count} votos</color></size>", (ushort)quedan);
		}
		yield return Timing.WaitForSeconds(10f);
		VotacionActiva = false;
		int totalUsuarios = Player.List.Count();
		int totalStaffs = Player.List.Count((Player p) => p.RemoteAdminAccess);
		string resultados = string.Join(" | ", Opciones.Select(delegate(string op, int idx)
		{
			int num = (Votos.ContainsKey(idx + 1) ? Votos[idx + 1] : 0);
			return string.Format("{0} {1} {2}", op, num, (num == 1) ? "voto" : "votos");
		}));
		string broadcastMsg = "<color=#2C378A>[RESULTADOS DE LA VOTACIÓN]</color>\n<color=#859CC7>" + resultados + "</color>";
		AnunciarATodos(broadcastMsg, 10);
		string webhookMsg = "RESULTADOS DE LA VOTACIÓN\nNombre: " + NombreIniciador + "\n" + $"Usuarios: {totalUsuarios} | Staffs: {totalStaffs}\n" + "Pregunta: " + Pregunta + "\n" + resultados + "\n_ _\n";
		Task.Run(() => Logs(webhookMsg, Secretos.WebhookVotacion));
		Opciones.Clear();
		Votos.Clear();
		JugadoresQueVotaron.Clear();
		Pregunta = string.Empty;
		NombreIniciador = string.Empty;
	}

	public static async Task Logs(string payload, string whurl)
	{
		if (string.IsNullOrWhiteSpace(whurl))
		{
			return;
		}
		try
		{
			string payloadEscaped = payload.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")
				.Replace("\r", "");
			StringContent msg = new StringContent("{\"content\": \"" + payloadEscaped + "\"}", Encoding.UTF8, "application/json");
			await _client.PostAsync(whurl, msg);
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Error("Error en webhook pe " + ex.Message);
		}
	}
}
