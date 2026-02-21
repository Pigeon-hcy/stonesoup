using UnityEngine;
using System.Collections.Generic;

public class Deck499 : Tile
{
    public GameObject[] bulletPrefabs; 

    public GameObject muzzleFlashObj;

	public float recoilForce = 100;
	public float shootForce = 1000f;

    public float cooldownTime = 0.1f;

	protected float _cooldownTimer;

    private List<int> _magazine = new List<int> { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3 };

    public int paperBullets = 5;
    public float paperSpreadAngle = 15f;

	private void ShuffleMagazine() {
		for (int i = _magazine.Count - 1; i > 0; i--) {
			int j = Random.Range(0, i + 1);
			int temp = _magazine[i];
			_magazine[i] = _magazine[j];
			_magazine[j] = temp;
		}
	}

	public override void init() {
		base.init();
		ShuffleMagazine();
	}

    public override void pickUp(Tile tilePickingUsUp) {
        base.pickUp(tilePickingUsUp);
        ShuffleMagazine();
    }

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

		int bulletType = _magazine[0];
		_magazine.RemoveAt(0);

		if (bulletType == 3) {
			tileUsingUs.takeDamage(tileUsingUs, 1);
		} else if (bulletType == 2) {
            Vector2 baseDir = tileUsingUs.aimDirection.normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x)*Mathf.Rad2Deg;
            for (int i = 0; i < paperBullets; i++) {
                float pelletAngle = (baseAngle + paperSpreadAngle * i) * Mathf.Deg2Rad;
                Vector2 pelletDir = new Vector2(Mathf.Cos(pelletAngle), Mathf.Sin(pelletAngle));

                Transform room = tileUsingUs.transform.parent;
                Vector2 roomLocal = room.InverseTransformPoint(muzzleFlashObj.transform.position);
                Vector2 grid = Tile.toGridCoord(roomLocal.x, roomLocal.y);
                int gx = Mathf.Clamp((int)grid.x, 0, LevelGenerator.ROOM_WIDTH - 1);
                int gy = Mathf.Clamp((int)grid.y, 0, LevelGenerator.ROOM_HEIGHT - 1);

                Tile bulletTile = Tile.spawnTile(bulletPrefabs[2], room, gx, gy);  
                bulletTile.addForce(pelletDir * shootForce);
            }
        } else {
		// Let's spawn the bullet. The bullet will probably need to be a child of the room. 
		//GameObject newBullet = Instantiate(bulletPrefabs[bulletType]);
            Transform room = tileUsingUs.transform.parent;
            Vector2 roomLocal = room.InverseTransformPoint(muzzleFlashObj.transform.position);
            Vector2 grid = Tile.toGridCoord(roomLocal.x, roomLocal.y);
            int gx = Mathf.Clamp((int)grid.x, 0, LevelGenerator.ROOM_WIDTH - 1);
            int gy = Mathf.Clamp((int)grid.y, 0, LevelGenerator.ROOM_HEIGHT - 1);

            Tile bulletTile = Tile.spawnTile(bulletPrefabs[bulletType], room, gx, gy);
            bulletTile.addForce(tileUsingUs.aimDirection.normalized * shootForce);

		}


		if (_magazine.Count == 0) {
			_magazine = new List<int> { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3 };
			ShuffleMagazine();
		}

		_cooldownTimer = cooldownTime;
	}

	public void deactivateFlash() {
		muzzleFlashObj.SetActive(false);
	}

}
