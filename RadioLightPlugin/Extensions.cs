using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace RadioLightPlugin;

public static class Extensions
{
	public static bool GetRandomSpawnPoint(this RoleTypeId roleType, out Vector3 pos, out float rot)
	{
		pos = Vector3.zero;
		rot = 0f;
		if (!roleType.TryGetRoleTemplate<PlayerRoleBase>(out var result))
		{
			return false;
		}
		if (!(result is IFpcRole { SpawnpointHandler: var spawnpointHandler }))
		{
			return false;
		}
		return spawnpointHandler?.TryGetSpawnpoint(out pos, out rot) ?? false;
	}

	public static (ushort horizontal, ushort vertical) ToClientUShorts(this Quaternion rotation)
	{
		float num = (rotation.eulerAngles.x + 180f) % 360f - 180f;
		return (horizontal: (ushort)(rotation.eulerAngles.y * 65535f / 360f), vertical: (ushort)((num + 88f) * 65535f / 176f));
	}
}
