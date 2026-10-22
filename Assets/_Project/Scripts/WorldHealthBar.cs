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

    void Start()
    {
        health = GetComponentInParent<Health>();
        cam = Camera.main;

        Unit unit = GetComponentInParent<Unit>();
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
        if (cam != null) transform.rotation = cam.transform.rotation;
    }
}
