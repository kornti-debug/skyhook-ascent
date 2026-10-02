using System.Collections;
using NUnit.Framework;
using SkyhookAscent.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace SkyhookAscent.Tests
{
    public sealed class OneWayPlatformPhysicsTests
    {
        [UnityTest]
        public IEnumerator PlayerPassesUpThroughChunkPlatformThenLandsOnTop()
        {
            GameObject chunkObject = new GameObject("One-way test chunk");
            GameObject playerObject = new GameObject("One-way test player");

            try
            {
                TowerChunk chunk = chunkObject.AddComponent<TowerChunk>();
                GameObject platformObject = new GameObject("Platform");
                platformObject.transform.SetParent(chunk.transform, false);
                platformObject.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                BoxCollider platform = platformObject.AddComponent<BoxCollider>();

                CapsuleCollider capsule = playerObject.AddComponent<CapsuleCollider>();
                Rigidbody body = playerObject.AddComponent<Rigidbody>();
                PlayerController controller = playerObject.AddComponent<PlayerController>();
                playerObject.transform.position = new Vector3(0f, 0.4f, 0f);
                body.linearVelocity = Vector3.up * 14f;
                Physics.SyncTransforms();

                bool clearedTop = false;
                for (int i = 0; i < 180; i++)
                {
                    yield return new WaitForFixedUpdate();
                    if (capsule.bounds.min.y >=
                        platform.bounds.max.y + OneWayPlatformRules.SurfaceClearance)
                    {
                        clearedTop = true;
                        break;
                    }
                }

                Assert.That(clearedTop, Is.True,
                    $"The player should pass through the platform and clear its top. " +
                    $"Position={playerObject.transform.position}, " +
                    $"velocity={body.linearVelocity}, " +
                    $"collisionIgnored={Physics.GetIgnoreCollision(capsule, platform)}.");

                bool landedOnTop = false;
                for (int i = 0; i < 240; i++)
                {
                    yield return new WaitForFixedUpdate();
                    float bottomGap = capsule.bounds.min.y - platform.bounds.max.y;
                    if (Mathf.Abs(bottomGap) <= 0.05f &&
                        Mathf.Abs(body.linearVelocity.y) <= 0.1f &&
                        controller.IsGrounded)
                    {
                        landedOnTop = true;
                        break;
                    }
                }

                Assert.That(landedOnTop, Is.True,
                    "The collision should be restored so the falling player lands on top.");
            }
            finally
            {
                Object.Destroy(chunkObject);
                Object.Destroy(playerObject);
            }
        }

        [UnityTest]
        public IEnumerator TrampolineLaunchCarriesPlayerThroughOverheadPlatform()
        {
            GameObject chunkObject = new GameObject("Trampoline test chunk");
            GameObject playerObject = new GameObject("Trampoline test player");

            try
            {
                TowerChunk chunk = chunkObject.AddComponent<TowerChunk>();
                GameObject trampolineObject = new GameObject("Trampoline");
                trampolineObject.transform.SetParent(chunk.transform, false);
                trampolineObject.transform.localScale = new Vector3(2f, 0.6f, 2f);
                trampolineObject.AddComponent<BoxCollider>();
                trampolineObject.AddComponent<TrampolinePlatform>();

                GameObject overheadObject = new GameObject("Overhead platform");
                overheadObject.transform.SetParent(chunk.transform, false);
                overheadObject.transform.localPosition = new Vector3(0f, 3.65f, 0f);
                overheadObject.transform.localScale = new Vector3(1.5f, 0.6f, 1.5f);
                BoxCollider overhead = overheadObject.AddComponent<BoxCollider>();

                CapsuleCollider capsule = playerObject.AddComponent<CapsuleCollider>();
                Rigidbody body = playerObject.AddComponent<Rigidbody>();
                PlayerController controller = playerObject.AddComponent<PlayerController>();
                playerObject.transform.position = new Vector3(0f, 2.5f, 0f);
                Physics.SyncTransforms();

                bool passedThroughOverhead = false;
                for (int i = 0; i < 240; i++)
                {
                    yield return new WaitForFixedUpdate();
                    if (capsule.bounds.min.y >=
                        overhead.bounds.max.y + OneWayPlatformRules.SurfaceClearance)
                    {
                        passedThroughOverhead = true;
                        break;
                    }
                }

                Assert.That(passedThroughOverhead, Is.True,
                    $"The trampoline should launch the player through the overhead platform. " +
                    $"Position={playerObject.transform.position}, " +
                    $"velocity={body.linearVelocity}.");

                bool landedOnOverhead = false;
                for (int i = 0; i < 240; i++)
                {
                    yield return new WaitForFixedUpdate();
                    float bottomGap = capsule.bounds.min.y - overhead.bounds.max.y;
                    if (Mathf.Abs(bottomGap) <= 0.05f &&
                        Mathf.Abs(body.linearVelocity.y) <= 0.1f &&
                        controller.IsGrounded)
                    {
                        landedOnOverhead = true;
                        break;
                    }
                }

                Assert.That(landedOnOverhead, Is.True,
                    "The player should be caught by the overhead platform while falling.");
            }
            finally
            {
                Object.Destroy(chunkObject);
                Object.Destroy(playerObject);
            }
        }
    }
}
