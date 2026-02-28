using UnityEngine;

public class UKGun : Tile
{
	public GameObject bulletPrefab;

    public GameObject coinPrefab;

	public GameObject muzzleFlashObj;

	public float recoilForce = 100;
	public float shootForce = 1000f;

    public float coinForce = 300f;

    protected bool _nextShotIsCoin = true;



	public float cooldownTime = 0.1f;

	protected float _cooldownTimer;

	protected void aim() {
		_sprite.transform.localPosition = new Vector3(1f, 0, 0);
		float aimAngle = Mathf.Atan2(_tileHoldingUs.aimDirection.y, _tileHoldingUs.aimDirection.x)*Mathf.Rad2Deg;
		transform.localRotation = Quaternion.Euler(0, 0, aimAngle);
		if (_tileHoldingUs.aimDirection.x < 0) {
			_sprite.flipY = true;
			muzzleFlashObj.transform.localPosition = new Vector3(muzzleFlashObj.transform.localPosition.x, -Mathf.Abs(muzzleFlashObj.transform.localPosition.y), muzzleFlashObj.transform.localPosition.z);
		}
		else {
			_sprite.flipY = false;
			muzzleFlashObj.transform.localPosition = new Vector3(muzzleFlashObj.transform.localPosition.x, Mathf.Abs(muzzleFlashObj.transform.localPosition.y), muzzleFlashObj.transform.localPosition.z);
		}
	}

	protected virtual void Update() {
		if (_cooldownTimer > 0) {
			_cooldownTimer -= Time.deltaTime;
		}

		if (_tileHoldingUs != null) {
			// If we're held, rotate and aim the gun.
			aim();
		}
		else {
			// Otherwise, move the gun back to the normal position. 
			_sprite.transform.localPosition = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}
		updateSpriteSorting();
	}

	public override void useAsItem(Tile tileUsingUs) {
		if (_cooldownTimer > 0) {
			return;
		}

		// First, make sure we're aimed properly (to avoid shooting ourselves by accident)
		aim();


		// Check to see if the muzzle is overlapping anything. 
		int numBlockers = Physics2D.OverlapPointNonAlloc(muzzleFlashObj.transform.position, _maybeColliderResults);
		for (int i = 0; i < numBlockers && i < _maybeColliderResults.Length; i++) {
			if (!_maybeColliderResults[i].isTrigger && _maybeColliderResults[i] != mainCollider) {
				ObjShake maybeSpriteShake = _sprite.GetComponent<ObjShake>();
				if (maybeSpriteShake != null) {
					maybeSpriteShake.shake();
				}

				return;
			}
		}

		muzzleFlashObj.SetActive(true);
		Invoke("deactivateFlash", 0.1f);
		tileUsingUs.addForce(-recoilForce*tileUsingUs.aimDirection.normalized);

		if (_nextShotIsCoin) {
			GameObject newCoin = Instantiate(coinPrefab);
			newCoin.transform.parent = tileUsingUs.transform.parent;
            newCoin.transform.position = muzzleFlashObj.transform.position;
			newCoin.transform.rotation = transform.rotation;


			newCoin.GetComponent<Tile>().init();
			newCoin.GetComponent<Tile>().addForce(tileUsingUs.aimDirection.normalized*coinForce);
            _nextShotIsCoin = false;
		}
		else {
		GameObject newBullet = Instantiate(bulletPrefab);
		newBullet.transform.parent = tileUsingUs.transform.parent;
		newBullet.transform.position = muzzleFlashObj.transform.position;
		newBullet.transform.rotation = transform.rotation;

		newBullet.GetComponent<Tile>().init();
		newBullet.GetComponent<Tile>().addForce(tileUsingUs.aimDirection.normalized*shootForce);    
		_nextShotIsCoin = true;
		}

		_cooldownTimer = cooldownTime;
	}

	public void deactivateFlash() {
		muzzleFlashObj.SetActive(false);
	}

    public override void dropped(Tile tileDroppingUs) {
        base.dropped(tileDroppingUs);
        _nextShotIsCoin = true;
    }
}
