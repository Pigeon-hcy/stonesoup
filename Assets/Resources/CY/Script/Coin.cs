using UnityEngine;

public class Coin : Tile
{
	public float damageThreshold = 14;

	public float onGroundThreshold = 1f;

	public float searchEnemyRadius = 20f;
	public GameObject bulletPrefabOnDeath; 
	public float bulletForceOnDeath = 1000f;

	protected ContactPoint2D[] _contacts = null;
	protected string _dieCause = "unknown";
	protected bool _dieWasCalled = false;

	void Start() {
		_contacts = new ContactPoint2D[10];
		if (GetComponent<TrailRenderer>() != null) {
			GetComponent<TrailRenderer>().Clear();
		}
	}

	
	void Update() {
	}

	Tile FindNearestEnemy() {
		Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchEnemyRadius);
		Tile nearest = null;
		float nearestDist = float.MaxValue;
		foreach (Collider2D c in hits) {
			Tile t = c.GetComponentInParent<Tile>();
			if (t == null || t == this || !t.hasTag(TileTags.Enemy)) continue;
			float d = Vector2.Distance(transform.position, t.transform.position);
			if (d < nearestDist) {
				nearestDist = d;
				nearest = t;
			}
		}
		return nearest;
	}

	void TrySpawnBulletTowardEnemy() {
		if (bulletPrefabOnDeath == null) return;
		Tile enemy = FindNearestEnemy();
		if (enemy == null) return;
		Vector2 dir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
		Transform parent = _dieWasCalled ? transform.parent : (GameManager.instance != null ? GameManager.instance.transform : transform.parent);
		if (parent == null && GameManager.instance != null) parent = GameManager.instance.transform;
		GameObject go = Instantiate(bulletPrefabOnDeath);
		go.transform.parent = parent;
		go.transform.position = transform.position;
		go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
		Tile bullet = go.GetComponent<Tile>();
		if (bullet != null) {
			bullet.init();
			bullet.addForce(dir * bulletForceOnDeath);
		}
	}

	public override void takeDamage(Tile tileDamagingUs, int damageAmount, DamageType damageType) {
		bool willDie = health - damageAmount <= 0;
		if (willDie)
			_dieCause = (tileDamagingUs != null && tileDamagingUs.body != null
				&& tileDamagingUs.body.linearVelocity.magnitude >= 8f) ? "bullet" : "damage";
		base.takeDamage(tileDamagingUs, damageAmount, damageType);
	}

	protected override void die() {
		_dieWasCalled = true;
		TrySpawnBulletTowardEnemy();
		base.die();
	}

	void OnDestroy() {
		if (!_dieWasCalled)
			TrySpawnBulletTowardEnemy();
	}

	public virtual void OnCollisionEnter2D(Collision2D collision) {
		Tile otherTile = collision.gameObject.GetComponent<Tile>();
		if (otherTile == null) return;
		float impact = collisionImpactLevel(collision);
		if (impact >= damageThreshold)
			otherTile.takeDamage(this, 1);
	}
}
