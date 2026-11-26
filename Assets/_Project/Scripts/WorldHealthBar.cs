using UnityEngine;
using UnityEngine.UI;

// Barra de vida que flota encima de cada personaje y siempre mira a la cámara.
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] Color allyColor = new Color(0.3f, 0.85f, 0.3f);
    [SerializeField] Color enemyColor = new Color(0.9f, 0.25f, 0.25f);

    Health health;
    Camera cam;
    Transform owner;

    void Start()
    {
        health = GetComponentInParent<Health>();
        cam = Camera.main;

        Unit unit = GetComponentInParent<Unit>();
        if (unit != null) owner = unit.transform;
        Unit player = Unit.FindPlayer();
        if (fill != null && unit != null && player != null)
            fill.color = unit.IsEnemyOf(player) ? enemyColor : allyColor;

        if (health != null)
        {
            health.OnChanged += UpdateBar;
            UpdateBar(health.Current, health.Max);
        }
    }

    void OnDestroy()
    {
        if (health != null) health.OnChanged -= UpdateBar;
    }

    void UpdateBar(float current, float max)
    {
        if (fill != null) fill.fillAmount = max > 0f ? current / max : 0f;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;

        // En la vista 2D la barra va justo encima de la cabeza del sprite
        if (cam.orthographic && owner != null)
            transform.position = owner.position + new Vector3(0f, 1.5f, 2.15f);
    }
}
