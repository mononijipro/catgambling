using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Flashes target renderers when the cat takes damage.
/// Attach to any object and assign renderers.
/// Supports SpriteRenderer and regular Renderer materials.
/// </summary>
public class CatHurtObjectFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CatHealthSystem healthSystem;
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private Renderer[] renderers;

    [Header("Material Flash")]
    [SerializeField] private string primaryColorProperty = "_BaseColor";
    [SerializeField] private string fallbackColorProperty = "_Color";

    [Header("Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private int flashCount = 3;

    private Color[] baseColors;
    private Color[] baseRendererColors;
    private int[] rendererColorPropertyIds;
    private int previousHealth = -1;
    private float flashPhaseTimer;
    private bool flashLit;
    private int flashesRemaining;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = FindObjectOfType<CatHealthSystem>();
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (renderers == null || renderers.Length == 0)
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            List<Renderer> nonSpriteRenderers = new List<Renderer>(allRenderers.Length);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && !(allRenderers[i] is SpriteRenderer))
                {
                    nonSpriteRenderers.Add(allRenderers[i]);
                }
            }
            renderers = nonSpriteRenderers.ToArray();
        }

        baseColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            baseColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }

        propertyBlock = new MaterialPropertyBlock();
        baseRendererColors = new Color[renderers.Length];
        rendererColorPropertyIds = new int[renderers.Length];

        int primaryId = Shader.PropertyToID(primaryColorProperty);
        int fallbackId = Shader.PropertyToID(fallbackColorProperty);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                rendererColorPropertyIds[i] = -1;
                baseRendererColors[i] = Color.white;
                continue;
            }

            if (targetRenderer.sharedMaterial.HasProperty(primaryId))
            {
                rendererColorPropertyIds[i] = primaryId;
                baseRendererColors[i] = targetRenderer.sharedMaterial.GetColor(primaryId);
            }
            else if (targetRenderer.sharedMaterial.HasProperty(fallbackId))
            {
                rendererColorPropertyIds[i] = fallbackId;
                baseRendererColors[i] = targetRenderer.sharedMaterial.GetColor(fallbackId);
            }
            else
            {
                rendererColorPropertyIds[i] = -1;
                baseRendererColors[i] = Color.white;
            }
        }
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            previousHealth = healthSystem.CurrentHealth;
            healthSystem.HealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.HealthChanged -= OnHealthChanged;
        }

        RestoreBaseColors();
    }

    private void Update()
    {
        if (flashesRemaining <= 0)
        {
            return;
        }

        flashPhaseTimer -= Time.deltaTime;
        if (flashPhaseTimer > 0f)
        {
            return;
        }

        float halfPhase = Mathf.Max(0.01f, flashDuration * 0.5f);
        flashPhaseTimer = halfPhase;

        if (flashLit)
        {
            flashLit = false;
            RestoreBaseColors();
            flashesRemaining--;
            return;
        }

        flashLit = true;
        ApplyFlashColors();
    }

    private void OnHealthChanged(int current, int max)
    {
        if (previousHealth >= 0 && current < previousHealth)
        {
            flashesRemaining = Mathf.Max(1, flashCount);
            flashLit = true;
            flashPhaseTimer = Mathf.Max(0.01f, flashDuration * 0.5f);
            ApplyFlashColors();
        }

        previousHealth = current;
    }

    private void RestoreBaseColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            spriteRenderers[i].color = baseColors[i];
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || rendererColorPropertyIds[i] < 0)
            {
                continue;
            }

            ApplyRendererColor(renderers[i], rendererColorPropertyIds[i], baseRendererColors[i]);
        }
    }

    private void ApplyFlashColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            spriteRenderers[i].color = flashColor;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || rendererColorPropertyIds[i] < 0)
            {
                continue;
            }

            ApplyRendererColor(renderers[i], rendererColorPropertyIds[i], flashColor);
        }
    }

    private void ApplyRendererColor(Renderer targetRenderer, int colorPropertyId, Color color)
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyId, color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
