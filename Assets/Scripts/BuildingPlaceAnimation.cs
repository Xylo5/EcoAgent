using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Rise-from-ground placement animation with dust burst and sound hook.
/// Add to a building after instantiation — plays once and removes itself.
/// </summary>
public class BuildingPlaceAnimation : MonoBehaviour
{
    [Header("Animation")]
    public float duration = 0.4f;
    public float dropDepth = 2f;
    public float bounceOvershoot = 0.08f; // how far above final pos the bounce peaks

    [Header("Dust")]
    public bool spawnDust = true;
    public Color dustColor = new Color(0.8f, 0.7f, 0.5f, 0.6f);
    public int dustCount = 20;

    /// <summary>
    /// Fired when the animation finishes — wire up your AudioSource.PlayOneShot here.
    /// </summary>
    public event Action OnLanded;

    private Vector3 finalPos;
    private Vector3 finalScale;

    public void Play(Vector3 targetPos, Vector3 targetScale)
    {
        finalPos = targetPos;
        finalScale = targetScale;
        StartCoroutine(AnimateRise());
    }

    private IEnumerator AnimateRise()
    {
        Vector3 startPos = finalPos + Vector3.down * dropDepth;
        Vector3 startScale = new Vector3(finalScale.x, finalScale.y * 0.1f, finalScale.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBounce(t);

            // Position: rise from below with bounce overshoot
            float y = Mathf.Lerp(startPos.y, finalPos.y, eased);
            if (eased > 1f)
                y += bounceOvershoot * (eased - 1f);
            transform.position = new Vector3(finalPos.x, y, finalPos.z);

            // Scale Y: squashed -> full (clamped so it never goes negative)
            float scaleY = Mathf.Lerp(startScale.y, finalScale.y, Mathf.Clamp01(eased));
            transform.localScale = new Vector3(finalScale.x, scaleY, finalScale.z);

            yield return null;
        }

        // Snap to exact final values
        transform.position = finalPos;
        transform.localScale = finalScale;

        if (spawnDust)
            SpawnDustBurst();

        OnLanded?.Invoke();

        Destroy(this);
    }

    /// <summary>
    /// Smooth ease-out with a single gentle bounce. Always returns [0, ~1.05] — no negatives.
    /// </summary>
    private static float EaseOutBounce(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        // Fast rise (ease-out cubic) then a small sine bounce at the end
        float rise = 1f - (1f - t) * (1f - t) * (1f - t);

        // Add a gentle single bounce in the last 30% of the animation
        float bounce = 0f;
        if (t > 0.7f)
        {
            float bounceT = (t - 0.7f) / 0.3f; // 0..1 within bounce phase
            bounce = Mathf.Sin(bounceT * Mathf.PI) * 0.06f; // peaks at ~0.06 above target
        }

        return rise + bounce;
    }

    private void SpawnDustBurst()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }

        GameObject dustGO = new GameObject("PlacementDust");
        dustGO.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        ParticleSystem ps = dustGO.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stop auto-play so we can configure
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startColor = dustColor;
        main.gravityModifier = 0.3f;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = dustCount;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, dustCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(bounds.size.x * 0.8f, 0.1f, bounds.size.z * 0.8f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(dustColor, 0f), new GradientColorKey(dustColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(dustColor.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var particleRenderer = dustGO.GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        particleRenderer.material.SetColor("_BaseColor", dustColor);

        ps.Play();
    }
}
