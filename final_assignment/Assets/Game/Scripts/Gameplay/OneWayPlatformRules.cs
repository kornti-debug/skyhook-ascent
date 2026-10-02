using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    public static class OneWayPlatformRules
    {
        public const float SurfaceClearance = 0.02f;
        private const float MinimumUndersideNormalY = 0.5f;

        public static bool ShouldIgnoreUndersideCollision(
            Bounds playerBounds,
            Bounds platformBounds,
            Vector3 platformToPlayerNormal)
        {
            return platformToPlayerNormal.y <= -MinimumUndersideNormalY &&
                playerBounds.min.y <
                    platformBounds.max.y - SurfaceClearance;
        }

        public static bool ShouldRestoreCollision(
            Bounds playerBounds,
            Bounds platformBounds,
            float playerVerticalVelocity = 0f)
        {
            bool clearedTop = playerBounds.min.y >=
                platformBounds.max.y + SurfaceClearance;
            bool fellBackBelow = playerVerticalVelocity < 0f &&
                playerBounds.max.y <=
                    platformBounds.min.y - SurfaceClearance;
            bool leftFootprint = playerBounds.max.x < platformBounds.min.x ||
                playerBounds.min.x > platformBounds.max.x ||
                playerBounds.max.z < platformBounds.min.z ||
                playerBounds.min.z > platformBounds.max.z;

            return clearedTop || fellBackBelow || leftFootprint;
        }
    }
}
