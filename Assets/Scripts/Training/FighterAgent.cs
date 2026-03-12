using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class FighterAgent : Agent
{
    [Header("References")]
    public NewFighter self;
    public NewFighter opponent;

    [Header("Tuning")]
    public float decisionIntervalSeconds = 0.05f; // ~20 decisions/sec (good start)

    float _timeSinceDecision;

    public FighterCommandBuffer commandBuffer;

    [Header("Rewards")]
    public float rewardPerDamageDealt = 0.01f;
    public float penaltyPerDamageTaken = 0.01f;
    public float stepPenalty = 0.0005f;

    // New (simple shaping)
    public float maxXDistanceForNormalization = 8f; // tune to your stage width
    public float distancePenalty = 0.0005f; // per step, scaled by normalized distance
    public float proximityReward = 0.0005f; // per step, scaled by (1 - normalized distance)

    // Reward for attempting attacks when close (helps learning early)
    public float attackAttemptReward = 0.0002f;
    public float attackAttemptMaxNormDist = 0.35f; // only reward attempts when close enough

    // Simple combo reward (based on consecutive damage events)
    public float comboWindowSeconds = 0.6f;
    public float comboRewardPerChainStep = 0.001f;
    public int comboRewardCap = 6; // cap combo multiplier to avoid runaway

    private float _lastDamageDealtTime = -999f;
    private int _comboCount = 0;

    [Header("Inputs")]
    const int observationSize = 17;
    public float maxHorizontalDistance = 20f;
    public float maxVerticalDistance = 3f;
    public float maxSpeedX = 12f;
    public float maxSpeedY = 12f;
    private int _prevSelfHp;
    private int _prevOppHp;
    public float wallSenseDistance = 3f; // how far to look for walls (world units)
    public override void Initialize()
    {
        if (self == null) self = GetComponent<NewFighter>();
        _timeSinceDecision = 0f;
        if (commandBuffer == null) commandBuffer = GetComponent<FighterCommandBuffer>();
    }

    public override void OnEpisodeBegin()
    {
        _timeSinceDecision = 0f;
        commandBuffer?.ClearAll();

        _prevSelfHp = self != null ? self.currentHealth : 0;
        _prevOppHp = opponent != null ? opponent.currentHealth : 0;
        
        _lastDamageDealtTime = -999f;
        _comboCount = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (self == null || opponent == null)
        {
            // Keep the observation size consistent even if not wired.
            for (int i = 0; i < observationSize; i++) sensor.AddObservation(0f);
            return;
        }

        // Health (normalized) 
        sensor.AddObservation((float)self.currentHealth / self.maxHealth);
        sensor.AddObservation((float)opponent.currentHealth / opponent.maxHealth);

        // Side info 
        sensor.AddObservation(self.IsOnLeftSide ? 1f : 0f);
        sensor.AddObservation(opponent.IsOnLeftSide ? 1f : 0f);

        // Grounded 
        sensor.AddObservation(self.onGround ? 1f : 0f);
        sensor.AddObservation(opponent.onGround ? 1f : 0f);

        // Self velocity (normalized)
        float vx = Mathf.Clamp(self.velocity.x / maxSpeedX, -1f, 1f);
        float vy = Mathf.Clamp(self.velocity.y / maxSpeedY, -1f, 1f);
        sensor.AddObservation(vx);
        sensor.AddObservation(vy);

        // Relative position (normalized)
        Vector3 delta = opponent.transform.position - self.transform.position;
        float dx = Mathf.Clamp(delta.x / maxHorizontalDistance, -1f, 1f);
        float dy = Mathf.Clamp(delta.y / maxVerticalDistance, -1f, 1f);
        sensor.AddObservation(dx);
        sensor.AddObservation(dy);

        // Distance magnitude
        float dist = delta.magnitude;
        float distNorm = Mathf.Clamp01(dist / maxHorizontalDistance);
        sensor.AddObservation(distNorm);

        // Cooldown / lockout
        sensor.AddObservation(self.currentState is Walking ? 1f : 0f);
        sensor.AddObservation(opponent.currentState is Walking ? 1f : 0f);

        // Recovery through current action (0 = free/no action, 1 = just started, -> 0 as it ends)
        sensor.AddObservation(GetActionLockout01(self));
        sensor.AddObservation(GetActionLockout01(opponent));

        // Wall distance sensing (left/right)
        float leftWall = SenseWallDistance(-1f);
        float rightWall = SenseWallDistance(+1f);

        // 0 = touching wall, 1 = no wall within sense range
        sensor.AddObservation(leftWall / wallSenseDistance);
        sensor.AddObservation(rightWall / wallSenseDistance);
    }

    private float GetActionLockout01(NewFighter f)
    {
        // If attacking remaining frames is cooldown
        if (f.currentState is Attacking && f.currentAction != null && f.currentAction.numberOfFrames > 0)
        {
            float remaining = Mathf.Clamp(f.currentAction.numberOfFrames - f.currentFrame, 0, f.currentAction.numberOfFrames);
            return remaining / f.currentAction.numberOfFrames; 
        }

        // If stunned/knockdown/throwing you’re locked out too.
        if (f.currentState is Stunned || f.currentState is Knockdown || f.currentState is Throwing)
            return 1f;

        return 0f;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Branch 0: Move (0=none, 1=left, 2=right)
        // Branch 1: Jump (0=no, 1=yes)
        // Branch 2: Attack (0=none, 1=light, 2=heavy, 3=throw)
        int horiz = actions.DiscreteActions[0];
        int vert = actions.DiscreteActions[1];
        int btn = actions.DiscreteActions[2];
        int jump = actions.DiscreteActions[3];

        commandBuffer?.ApplyDiscrete(horiz, vert, btn, jump);

        //Debug.Log($"Actions: move={move}, jump={jump}, attack={attack}");

        if (self != null && opponent != null)
        {
            int selfHp = self.currentHealth;
            int oppHp = opponent.currentHealth;

            int dealt = Mathf.Max(0, _prevOppHp - oppHp);
            int taken = Mathf.Max(0, _prevSelfHp - selfHp);

            float dx = Mathf.Abs(opponent.transform.position.x - self.transform.position.x);
            float normDist = Mathf.Clamp01(dx / Mathf.Max(0.0001f, maxXDistanceForNormalization));

            if (dealt > 0) AddReward(dealt * rewardPerDamageDealt);
            if (taken > 0) AddReward(-taken * penaltyPerDamageTaken);

            // penalize being far
            AddReward(-distancePenalty * normDist);

            // small reward for being close (encourages engagement)
            AddReward(proximityReward * (1f - normDist));

            // reward attack attempts only if close enough 
            if (btn != 0 && normDist <= attackAttemptMaxNormDist)
            {
                AddReward(attackAttemptReward);
            }

            // combo chain reward when you actually deal damage 
            if (dealt > 0)
            {
                if (Time.time - _lastDamageDealtTime <= comboWindowSeconds)
                    _comboCount++;
                else
                    _comboCount = 1;

                _lastDamageDealtTime = Time.time;

                int cappedCombo = Mathf.Min(_comboCount, comboRewardCap);
                AddReward(comboRewardPerChainStep * cappedCombo);
            }

            AddReward(-stepPenalty);
            
            _prevSelfHp = selfHp;
            _prevOppHp = oppHp;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;

        // Branch 0: horiz (0 neutral, 1 left, 2 right)
        // Branch 1: vert  (0 neutral, 1 down, 2 up)
        // Branch 2: btn   (0 none, 1 A, 2 B, 3 C)
        // Branch 3: jump  (0 no, 1 yes)

        d[0] = 0;
        d[1] = 0;
        d[2] = 0;
        d[3] = 0;

    #if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;

        // Horiz
        if (kb.aKey.isPressed) d[0] = 1;
        else if (kb.dKey.isPressed) d[0] = 2;

        // Vert (for crouch / combo inputs)
        if (kb.sKey.isPressed) d[1] = 1;
        else if (kb.wKey.isPressed) d[1] = 2;

        // Jump (tap)
        if (kb.wKey.isPressed) d[3] = 1;

        // Buttons (tap)
        if (kb.vKey.isPressed) d[2] = 1;
        else if (kb.bKey.isPressed) d[2] = 2;
        else if (kb.nKey.isPressed) d[2] = 3;
    #endif
    }

    private float SenseWallDistance(float dirSign)
    {
        Vector2 origin = transform.position;
        Vector2 dir = dirSign < 0 ? Vector2.left : Vector2.right;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, wallSenseDistance);

        if (hit.collider != null)
        {
            // Only treat as wall if surface normal is mostly horizontal
            if (Mathf.Abs(hit.normal.x) > 0.8f)
                return hit.distance;
        }

        return wallSenseDistance;
    }


    void Update()
    {
        // Optional: throttle decision rate (useful for fighting games)
        _timeSinceDecision += Time.deltaTime;
        if (_timeSinceDecision >= decisionIntervalSeconds)
        {
            _timeSinceDecision = 0f;
            RequestDecision();
        }
    }
}
