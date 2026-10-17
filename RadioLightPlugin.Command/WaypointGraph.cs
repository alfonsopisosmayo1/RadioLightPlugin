using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using UnityEngine;

namespace RadioLightPlugin.Command;

public static class WaypointGraph
{
	public class WpNode
	{
		public int Id;

		public Vector3 Position;

		public Room Room;

		public List<WpEdge> Edges = new List<WpEdge>();
	}

	public class WpEdge
	{
		public WpNode To;

		public float Cost;
	}

	private class AStarEntry
	{
		public WpNode Node;

		public float G;

		public float F;

		public AStarEntry Parent;
	}

	private class DupComparer : IComparer<float>
	{
		public int Compare(float x, float y)
		{
			int num = x.CompareTo(y);
			return (num == 0) ? 1 : num;
		}
	}

	public static readonly int WallMask = 536952833;

	private static readonly int FloorMask = 671170561;

	private const float GridStep = 3f;

	private const float AgentHeight = 1.8f;

	private const float AgentRadius = 0.3f;

	private const float FloorProbe = 3f;

	private const float CeilingClear = 0.4f;

	private const float MaxEdgeDist = 4.5f;

	private static List<WpNode> _allNodes = new List<WpNode>();

	private static int _nextId = 0;

	public static bool IsBuilt { get; private set; }

	public static void Build()
	{
		IsBuilt = false;
		_allNodes.Clear();
		_nextId = 0;
		foreach (Room item in Room.List)
		{
			SeedRoomWaypoints(item);
		}
		Log.Info($"[WaypointGraph] Waypoints sembrados: {_allNodes.Count}");
		ConnectIntraRoom();
		ConnectViaDoors();
		int count = _allNodes.Count;
		_allNodes = _allNodes.Where((WpNode n) => n.Edges.Count > 0).ToList();
		Log.Info($"[WaypointGraph] Nodos aislados eliminados: {count - _allNodes.Count}");
		IsBuilt = true;
		Log.Info($"[WaypointGraph] Listo: {_allNodes.Count} nodos, " + $"{_allNodes.Sum((WpNode n) => n.Edges.Count) / 2} aristas.");
	}

	private static void SeedRoomWaypoints(Room room)
	{
		for (float num = -12f; num <= 12f; num += 3f)
		{
			for (float num2 = -12f; num2 <= 12f; num2 += 3f)
			{
				Vector3 origin = room.Position + new Vector3(num, 2f, num2);
				if (!Physics.Raycast(origin, Vector3.down, out var hitInfo, 3f, FloorMask))
				{
					continue;
				}
				Vector3 floorPos = hitInfo.point + Vector3.up * 0.4f;
				if (!Physics.Raycast(floorPos, Vector3.up, 1.8f, WallMask) && !Physics.CheckSphere(floorPos + Vector3.up * 0.3f, 0.3f, WallMask))
				{
					Room room2 = Room.List.OrderBy((Room r) => Vector3.Distance(floorPos, r.Position)).FirstOrDefault();
					if (!(room2 != room))
					{
						_allNodes.Add(new WpNode
						{
							Id = _nextId++,
							Position = floorPos,
							Room = room
						});
					}
				}
			}
		}
	}

	private static void ConnectIntraRoom()
	{
		Dictionary<Room, List<WpNode>> dictionary = (from n in _allNodes
			group n by n.Room).ToDictionary((IGrouping<Room, WpNode> g) => g.Key, (IGrouping<Room, WpNode> g) => g.ToList());
		foreach (KeyValuePair<Room, List<WpNode>> item in dictionary)
		{
			List<WpNode> value = item.Value;
			for (int i = 0; i < value.Count; i++)
			{
				for (int j = i + 1; j < value.Count; j++)
				{
					WpNode wpNode = value[i];
					WpNode wpNode2 = value[j];
					float num = Vector3.Distance(wpNode.Position, wpNode2.Position);
					if (num > 4.5f)
					{
						continue;
					}
					Vector3 vector = wpNode.Position + Vector3.up * 0.9f;
					Vector3 vector2 = wpNode2.Position + Vector3.up * 0.9f;
					if (!Physics.Linecast(vector, vector2, WallMask))
					{
						Vector3 normalized = (vector2 - vector).normalized;
						if (!Physics.CapsuleCast(vector, vector + Vector3.up * 0.9f, 0.3f, normalized, num, WallMask))
						{
							AddEdge(wpNode, wpNode2, num);
						}
					}
				}
			}
		}
	}

	private static void ConnectViaDoors()
	{
		foreach (Door door in Door.List)
		{
			Room[] array = Room.List.OrderBy((Room r) => Vector3.Distance(door.Position, r.Position)).Take(5).ToArray();
			Room roomA = array[0];
			Room room = array.FirstOrDefault((Room r) => r != roomA);
			if (!(room == null) && !(roomA == room))
			{
				Vector3 normalized = (room.Position - roomA.Position).normalized;
				Vector3 vector = SnapToFloor(door.Position - normalized * 1.2f);
				Vector3 vector2 = SnapToFloor(door.Position + normalized * 1.2f);
				if (!(vector == Vector3.zero) && !(vector2 == Vector3.zero))
				{
					WpNode wpNode = new WpNode
					{
						Id = _nextId++,
						Position = vector,
						Room = roomA
					};
					WpNode wpNode2 = new WpNode
					{
						Id = _nextId++,
						Position = vector2,
						Room = room
					};
					float cost = Vector3.Distance(vector, vector2);
					AddEdge(wpNode, wpNode2, cost);
					_allNodes.Add(wpNode);
					_allNodes.Add(wpNode2);
					ConnectPortalToRoom(wpNode, roomA);
					ConnectPortalToRoom(wpNode2, room);
				}
			}
		}
	}

	private static void ConnectPortalToRoom(WpNode portal, Room room)
	{
		IEnumerable<WpNode> enumerable = _allNodes.Where((WpNode n) => n.Room == room && n != portal);
		foreach (WpNode item in enumerable)
		{
			float num = Vector3.Distance(portal.Position, item.Position);
			if (!(num > 9f))
			{
				Vector3 start = portal.Position + Vector3.up * 0.9f;
				Vector3 end = item.Position + Vector3.up * 0.9f;
				if (!Physics.Linecast(start, end, WallMask))
				{
					AddEdge(portal, item, num);
				}
			}
		}
	}

	private static void AddEdge(WpNode a, WpNode b, float cost)
	{
		a.Edges.Add(new WpEdge
		{
			To = b,
			Cost = cost
		});
		b.Edges.Add(new WpEdge
		{
			To = a,
			Cost = cost
		});
	}

	private static Vector3 SnapToFloor(Vector3 point)
	{
		Vector3 origin = point + Vector3.up * 1f;
		if (Physics.Raycast(origin, Vector3.down, out var hitInfo, 3f, FloorMask))
		{
			return hitInfo.point + Vector3.up * 0.4f;
		}
		return Vector3.zero;
	}

	public static List<Vector3> GetPath(Vector3 from, Vector3 to)
	{
		if (!IsBuilt || _allNodes.Count == 0)
		{
			return new List<Vector3> { to };
		}
		WpNode wpNode = ClosestNode(from);
		WpNode wpNode2 = ClosestNode(to);
		if (wpNode == null || wpNode2 == null)
		{
			return new List<Vector3> { to };
		}
		if (wpNode == wpNode2)
		{
			return new List<Vector3> { to };
		}
		SortedList<float, AStarEntry> sortedList = new SortedList<float, AStarEntry>(new DupComparer());
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		Dictionary<int, AStarEntry> dictionary2 = new Dictionary<int, AStarEntry>();
		float f = Heuristic(wpNode.Position, wpNode2.Position);
		AStarEntry aStarEntry = new AStarEntry
		{
			Node = wpNode,
			G = 0f,
			F = f
		};
		sortedList.Add(aStarEntry.F, aStarEntry);
		dictionary[wpNode.Id] = 0f;
		HashSet<int> hashSet = new HashSet<int>();
		AStarEntry aStarEntry2 = null;
		while (sortedList.Count > 0)
		{
			AStarEntry aStarEntry3 = sortedList.Values[0];
			sortedList.RemoveAt(0);
			if (hashSet.Contains(aStarEntry3.Node.Id))
			{
				continue;
			}
			hashSet.Add(aStarEntry3.Node.Id);
			if (aStarEntry3.Node == wpNode2)
			{
				aStarEntry2 = aStarEntry3;
				break;
			}
			foreach (WpEdge edge in aStarEntry3.Node.Edges)
			{
				if (!hashSet.Contains(edge.To.Id))
				{
					float num = aStarEntry3.G + edge.Cost;
					if (!dictionary.TryGetValue(edge.To.Id, out var value) || !(value <= num))
					{
						dictionary[edge.To.Id] = num;
						float num2 = num + Heuristic(edge.To.Position, wpNode2.Position);
						AStarEntry value2 = new AStarEntry
						{
							Node = edge.To,
							G = num,
							F = num2,
							Parent = aStarEntry3
						};
						dictionary2[edge.To.Id] = value2;
						sortedList.Add(num2, value2);
					}
				}
			}
		}
		if (aStarEntry2 == null)
		{
			Log.Warn("[WaypointGraph][A*] Sin camino. Fallback directo.");
			return new List<Vector3> { to };
		}
		List<Vector3> list = new List<Vector3> { to };
		AStarEntry aStarEntry4 = aStarEntry2;
		while (aStarEntry4.Parent != null)
		{
			list.Add(aStarEntry4.Node.Position);
			aStarEntry4 = aStarEntry4.Parent;
		}
		list.Reverse();
		return SmoothPath(list);
	}

	private static List<Vector3> SmoothPath(List<Vector3> raw)
	{
		if (raw.Count <= 2)
		{
			return raw;
		}
		List<Vector3> list = new List<Vector3> { raw[0] };
		int num = 0;
		while (num < raw.Count - 1)
		{
			int num2 = num + 1;
			for (int i = num + 2; i < raw.Count; i++)
			{
				Vector3 start = raw[num] + Vector3.up * 1.8f * 0.5f;
				Vector3 end = raw[i] + Vector3.up * 1.8f * 0.5f;
				if (!Physics.Linecast(start, end, WallMask))
				{
					num2 = i;
					continue;
				}
				break;
			}
			list.Add(raw[num2]);
			num = num2;
		}
		return list;
	}

	private static WpNode ClosestNode(Vector3 pos)
	{
		if (_allNodes.Count == 0)
		{
			return null;
		}
		WpNode result = null;
		float num = float.MaxValue;
		foreach (WpNode allNode in _allNodes)
		{
			float sqrMagnitude = (allNode.Position - pos).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = allNode;
			}
		}
		return result;
	}

	private static float Heuristic(Vector3 a, Vector3 b)
	{
		return Vector3.Distance(a, b);
	}
}
