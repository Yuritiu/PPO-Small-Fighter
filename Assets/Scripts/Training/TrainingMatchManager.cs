using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TrainingMatchManager : MonoBehaviour
{
    [Header("Fighters (NewFighter)")]
    public NewFighter fighterA;
    public NewFighter fighterB;

    [Header("Spawn points")]
    public Transform spawnA;
    public Transform spawnB;

    [Header("Round settings")]
    public float roundTimeSeconds = 20f;
    public TextMeshProUGUI timerText;

    [Tooltip("If true, timer ignores Time.timeScale. If false, timer speeds up with timeScale (better for fast training).")]
    public bool useUnscaledTime = false;

    [Tooltip("Automatically reset immediately after KO/timeout.")]
    public bool autoReset = true;

    [Tooltip("Small delay so KO/timeout can be observed (set 0 for instant).")]
    public float resetDelaySeconds = 0f;

    public event Action<int> OnRoundEnded; // 0=draw, 1=A wins, 2=B wins

    float timeRemaining;
    bool roundActive;
    bool resetting;

    [Header("Agents")]
    public FighterAgent agentA;
    public FighterAgent agentB;

    [Header("Terminal rewards")]
    public float winReward = 1f;
    public float lossPenalty = -1f;
    public float drawReward = 0f;
    public float timeoutScale = 0.5f;

    void Start()
    {
        BeginRound();
    }

    void Update()
    {
        if (!roundActive || resetting) return;
        if (fighterA == null || fighterB == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        timeRemaining -= dt;

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining)).ToString();

        // KO checks
        bool aDead = fighterA.currentHealth <= 0;
        bool bDead = fighterB.currentHealth <= 0;

        if (aDead || bDead) { EndRound(); return; }

        // Timeout
        if (timeRemaining <= 0f)
        {
            EndRound();
        }
    }

    public void BeginRound()
    {
        if (agentA == null && fighterA != null) agentA = fighterA.GetComponent<FighterAgent>();
        if (agentB == null && fighterB != null) agentB = fighterB.GetComponent<FighterAgent>();

        roundActive = true;
        resetting = false;
        timeRemaining = roundTimeSeconds;

        CleanupSceneArtifacts();

        ResetFighter(fighterA, spawnA, startOnLeftSide: true);
        ResetFighter(fighterB, spawnB, startOnLeftSide: false);

        // If you added SyncHealthBars in FightManager, this keeps UI correct after reset:
        if (FightManager.instance != null)
        {
            // Safe even if you haven't added it — just comment this out if needed.
            FightManager.instance.SyncHealthBars();
        }
    }

    void EndRound()
    {
        if (!roundActive) return;

        roundActive = false;

        // Terminal rewards + EndEpisode
        if (agentA != null && agentB != null)
        {
            bool aDead = fighterA.currentHealth <= 0;
            bool bDead = fighterB.currentHealth <= 0;

            // KO CASE
            if (aDead && !bDead)
            {
                agentA.AddReward(lossPenalty);
                agentB.AddReward(winReward);
            }
            else if (bDead && !aDead)
            {
                agentA.AddReward(winReward);
                agentB.AddReward(lossPenalty);
            }
            // TIMEOUT CASE
            else
            { 
                float aHP = (float)fighterA.currentHealth / fighterA.maxHealth;
                float bHP = (float)fighterB.currentHealth / fighterB.maxHealth;
                float diff = Mathf.Clamp(aHP - bHP, -1f, 1f);   // + if a ahead

                // Give both agents symmetric rewards:
                agentA.AddReward(diff * timeoutScale);
                agentB.AddReward(-diff * timeoutScale);

                agentA.EndEpisode();
                agentB.EndEpisode();
            }
        }

        if (autoReset)
            StartCoroutine(ResetAfterDelay());
    }

    IEnumerator ResetAfterDelay()
    {
        resetting = true;

        if (resetDelaySeconds > 0f)
            yield return new WaitForSeconds(useUnscaledTime ? 0f : resetDelaySeconds);
        // Note: WaitForSeconds uses scaled time. If you want unscaled delay:
        if (resetDelaySeconds > 0f && useUnscaledTime)
            yield return new WaitForSecondsRealtime(resetDelaySeconds);

        BeginRound();
    }

    void ResetFighter(NewFighter fighter, Transform spawn, bool startOnLeftSide)
    {
        if (fighter == null || spawn == null) return;

        // IMPORTANT: ResetFighter mirrors x internally for the right-side fighter.
        // So for right side we store NEGATED x so -(-x) = +x.
        var p = spawn.position;
        fighter.startingPosition = startOnLeftSide
            ? new Vector3(p.x, p.y, p.z)
            : new Vector3(-p.x, p.y, p.z);

        fighter.UnpauseFighter();
        fighter.ResetFighter(startOnLeftSide);
    }

    void CleanupSceneArtifacts()
    {
        foreach (var projectile in GameObject.FindGameObjectsWithTag("ProjectileBox"))
            Destroy(projectile);

        foreach (var particle in GameObject.FindGameObjectsWithTag("Particles"))
            Destroy(particle);
    }

    // Optional: quick on-screen debug of timer without relying on game UI
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 250, 30), $"Time: {Mathf.Max(0f, timeRemaining):0.0}");
    }
}
