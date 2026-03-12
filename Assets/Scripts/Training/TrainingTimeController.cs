using UnityEngine;

public class TrainingTimeController : MonoBehaviour
{
    [Header("Training speed")]
    public float timeScale = 1f;

    float _baseFixedDeltaTime;

    void Awake()
    {
        _baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    void OnEnable()
    {
        Apply();
    }

    void OnDisable()
    {
        // Restore normal so stopping play doesn't leave editor in weird state
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _baseFixedDeltaTime;
    }

    void Apply()
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = _baseFixedDeltaTime * Time.timeScale;
    }
}
