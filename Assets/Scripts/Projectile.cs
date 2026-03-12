using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NewCollisionBox))]
public class Projectile : MonoBehaviour
{
    [HideInInspector] public UnityEvent<Projectile> ProjectileDestroyed;
    public ActionData action;
    [SerializeField] private Vector3 velocity;
    private NewCollisionBox hitbox;
    private bool shouldBeDestroyed;
    public NewFighter owner { get; private set; }
    public NewFighterOld ownerOld { get; private set; }


    public void Init(NewFighter owner)
    {
        this.owner = owner;
    }
    public void InitOld(NewFighterOld owner)
    {
        this.ownerOld = owner;
        IgnoreOwnerCollisions(owner.transform);
    }

    private void Awake()
    {
        hitbox = GetComponent<NewCollisionBox>();
        hitbox.Collided.AddListener(OnHitboxCollides);
    }

    private void Update()
    {
        transform.Translate(velocity * 0.0167f);

        float leftBound = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0.5f, Mathf.Abs(Camera.main.transform.position.z))).x;
        float rightBound = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, Mathf.Abs(Camera.main.transform.position.z))).x;
        if (hitbox.boxCollider.bounds.max.x < leftBound || hitbox.boxCollider.bounds.min.x > rightBound)
        {
            shouldBeDestroyed = true;
        }
    }

    private void LateUpdate()
    {
        if (shouldBeDestroyed)
        {
            ProjectileDestroyed.Invoke(this);
            Destroy(gameObject);
        }
    }

    public void Pause()
    {
        velocity = Vector3.zero;
    }

    private void OnHitboxCollides(Collider2D col)
    {
        if (col == null) return;

        if (col.CompareTag("ProjectileBox"))
        {
            var otherProj = col.GetComponent<Projectile>();
            if (otherProj != null) otherProj.shouldBeDestroyed = true;
            shouldBeDestroyed = true;
            return;
        }

        if (col.CompareTag("FighterBox"))
        {
            // Find which fighter this box belongs to
            var hitNew = col.GetComponentInParent<NewFighter>();
            var hitOld = col.GetComponentInParent<NewFighterOld>();

            // If this projectile belongs to NEW fighter, ignore hitting that fighter
            if (owner != null && hitNew == owner)
                return;

            // If this projectile belongs to OLD fighter, ignore hitting that fighter
            if (ownerOld != null && hitOld == ownerOld)
                return;

            // Otherwise, valid hit
            shouldBeDestroyed = true;
        }
    }
    private void IgnoreOwnerCollisions(Transform ownerRoot)
    {
        if (ownerRoot == null) return;

        // All colliders on projectile (root + children)
        var projCols = GetComponentsInChildren<Collider2D>(true);

        // All colliders on the owner fighter (root + children)
        var ownerCols = ownerRoot.GetComponentsInChildren<Collider2D>(true);

        foreach (var pc in projCols)
        {
            if (pc == null) continue;
            foreach (var oc in ownerCols)
            {
                if (oc == null) continue;
                Physics2D.IgnoreCollision(pc, oc, true);
            }
        }
    }
}
