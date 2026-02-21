using UnityEngine;

public class RBullet : Tile
{
	public float damageThreshold = 14;

	public float onGroundThreshold = 1f;

	protected float _destroyTimer = 0.5f;

	protected ContactPoint2D[] _contacts = null;

    public float explosiveRadius = 2f;

    public float explosiveForce = 1000f;
    public int explosiveDamage = 3;

	void Start() {
		_contacts = new ContactPoint2D[10];
		if (GetComponent<TrailRenderer>() != null) {
			GetComponent<TrailRenderer>().Clear();
		}
	}

	void Update() {
		// If we're moving kinda slow now we can just delete ourselves.
		if (_body.linearVelocity.magnitude <= onGroundThreshold) {
			_destroyTimer -= Time.deltaTime;
			if (_destroyTimer <= 0) {
				die();
			}
		}
	}

	public virtual void OnCollisionEnter2D(Collision2D collision) {
        Tile otherTile = collision.gameObject.GetComponent<Tile>();

        if (otherTile.tags == TileTags.Wall || otherTile.tags == TileTags.Enemy) {
            explode();
            die();
            return;
        }

        float impact = collisionImpactLevel(collision);
        if (impact >= damageThreshold) {
            otherTile.takeDamage(this, 1);
            die();
        }
	}

    public void explode() {
        Collider2D[] maybeColliders = Physics2D.OverlapCircleAll(transform.position, explosiveRadius);
        foreach (Collider2D maybeCollider in maybeColliders) {
            Tile tile = maybeCollider.GetComponent<Tile>();
            if(tile != null && tile != this)
            {
                tile.takeDamage(this, explosiveDamage, DamageType.Explosive);
                tile.addForce((tile.transform.position-transform.position).normalized*explosiveForce);
            }
        }
    }
}
