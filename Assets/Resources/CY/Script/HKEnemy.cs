using UnityEngine;
using System.Collections;

public class HKEnemy : BasicAICreature
{
	// How much force we inflict if something collides with us.
	public float damageForce = 1000;
	public int damageAmount = 1;

	// We use counters to determine when to next try to move.
	protected float _nextMoveCounter;
	public float timeBetweenMovesMin = 1.5f;
	public float timeBetweenMovesMax = 3f;



/// //////////////////////////////////////////////////////////////

    protected Tile _targetTile = null;
    public float preferredDistanceFromTarget = 3f;
    public float distanceTolerance = 1.5f;

    protected int _remainAttackRemaining = 0;
    public int maxAttacksPerShoot = 3; 

    protected bool _isCoolingDown = false;   
    protected float _attackCooldown = 0f;     
    public float cooldownAfterBurst = 5f;      
    protected bool _isShootingBurst = false;    
    protected float _timeBetweenAttacks = 0.1f;

    protected float _timeSinceLastStep = 0f; //time since last step taken
    

	// Occasionally we'll start with a weapon pre-spawned on top of us. 
	public GameObject[] maybeWeaponsToStartWith;

	public override void init() {
		base.init();

		// Here's where we spawn a random weapon for us. 
		if (maybeWeaponsToStartWith != null && maybeWeaponsToStartWith.Length > 0) {
			GameObject maybeWeaponPrefab = GlobalFuncs.randElem(maybeWeaponsToStartWith);
			if (maybeWeaponPrefab != null) {
				Vector2 ourGridPos = toGridCoord(localX, localY);
                Tile gunTile = Tile.spawnTile(maybeWeaponPrefab, transform.parent, (int)ourGridPos.x, (int)ourGridPos.y);
                gunTile.pickUp(this);
			}
		}
	}



	public override void Start() {
		_targetGridPos = Tile.toGridCoord(globalX, globalY);
		_nextMoveCounter = Random.Range(timeBetweenMovesMin, timeBetweenMovesMax);
	}

	void Update() {
		// Update our counters.
		if (_nextMoveCounter > 0) {
			_nextMoveCounter -= Time.deltaTime;
		}

		// When it's time to try a new move.
		if (_nextMoveCounter <= 0) {
			takeStep();
		}

		updateSpriteSorting();

		if (_isCoolingDown)
		{
			_attackCooldown -= Time.deltaTime;
			if (_attackCooldown <= 0f)
			{
				_isCoolingDown = false;
				_attackCooldown = 0f;
			}
		}
	}	

	protected override void takeStep() {
		// Try to move to one of our neighboring positions if it is empty.
		_neighborPositions.Clear();

		// We test neighbor locations by casting in specific directions. 

		Vector2 upGridNeighbor = new Vector2(_targetGridPos.x, _targetGridPos.y+1);
		if (pathIsClear(toWorldCoord(upGridNeighbor))) {
			_neighborPositions.Add(upGridNeighbor);
		}
		Vector2 rightGridNeighbor = new Vector2(_targetGridPos.x+1, _targetGridPos.y);
		if (pathIsClear(toWorldCoord(rightGridNeighbor))) {
			_neighborPositions.Add(rightGridNeighbor);
		}
		Vector2 downGridNeighbor = new Vector2(_targetGridPos.x, _targetGridPos.y-1);
		if (pathIsClear(toWorldCoord(downGridNeighbor))) {
			_neighborPositions.Add(downGridNeighbor);
		}
		Vector2 leftGridNeighbor = new Vector2(_targetGridPos.x-1, _targetGridPos.y);
		if (pathIsClear(toWorldCoord(leftGridNeighbor))) {
			_neighborPositions.Add(leftGridNeighbor);
		}

		// If there's an empty neighbor, choose one randomly.
		if (_neighborPositions.Count > 0) {
			_targetGridPos = GlobalFuncs.randElem(_neighborPositions);
			_nextMoveCounter = Random.Range(timeBetweenMovesMin, timeBetweenMovesMax);
		}
	}


	public override void tileDetected(Tile otherTile) {
		if (otherTile == this) {
			return;
		}
		if (tileWereHolding != null && otherTile.hasTag(tagsWeChase) && !_isCoolingDown && !_isShootingBurst) {
			_targetTile = otherTile;
			StartCoroutine(tripleShoot(tileWereHolding));
		}
		// If we're not holding a weapon and we detect a nearby weapon, PICK IT UP!
		if (tileWereHolding == null && otherTile.hasTag(TileTags.Weapon) && otherTile.hasTag(TileTags.CanBeHeld)) {
			otherTile.pickUp(this);
		}
	}


	// Colliding with somethign we want to attack should hurt it.
	void OnCollisionEnter2D(Collision2D collision) {
		Tile otherTile = collision.gameObject.GetComponent<Tile>();
		if (otherTile != null && otherTile.hasTag(tagsWeChase)) {
			otherTile.takeDamage(this, damageAmount);
			Vector2 toOtherTile = (Vector2)otherTile.transform.position - (Vector2)transform.position;
			toOtherTile.Normalize();
			otherTile.addForce(damageForce*toOtherTile);
		}
	}

	// Check for potential weapons the moment we overlap them (we also poll for them). 
	void OnTriggerEnter2D(Collider2D other) {
		Tile otherTile = other.GetComponent<Tile>();
		if (otherTile != null && tileWereHolding == null && otherTile.hasTag(TileTags.CanBeHeld) && otherTile.hasTag(TileTags.Weapon)) {
			otherTile.pickUp(this);
		}
	}

    IEnumerator tripleShoot(Tile weaponTile)
    {
        _isShootingBurst = true;
        for (int i = 0; i < maxAttacksPerShoot; i++)
        {
            yield return new WaitForSeconds(_timeBetweenAttacks);
            if (_targetTile != null)
                aimDirection = ((Vector2)_targetTile.transform.position - (Vector2)transform.position).normalized;
            weaponTile.useAsItem(this);
        }
        _isShootingBurst = false;
        _isCoolingDown = true;
        _attackCooldown = cooldownAfterBurst;
    }

}
