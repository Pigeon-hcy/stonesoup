using UnityEngine;

public class Cheese : Tile
{
    public override void useAsItem(Tile tileUsingUs)
    {
        tileUsingUs.health += 1;
        takeDamage(tileUsingUs, health, DamageType.Normal);
        
    }

    public override void dropped(Tile tileDroppingUs)
    {
        base.dropped(tileDroppingUs);
        //look for nearby tile
        ratWallCheck();
    }

    public void ratWallCheck()
    {
        //look for nearby tile
        Vector2 gridPos = Tile.toGridCoord(transform.position);
        int gx = (int)gridPos.x;
        int gy = (int)gridPos.y;
        
        Vector2[] neighborWorldPositions = new Vector2[] {
            Tile.toWorldCoord(gx, gy + 1),
            Tile.toWorldCoord(gx, gy - 1),
            Tile.toWorldCoord(gx - 1, gy),
            Tile.toWorldCoord(gx + 1, gy)
        };

        foreach (Vector2 worldPos in neighborWorldPositions)
        {
            int numHit = Physics2D.OverlapPointNonAlloc(worldPos, _maybeColliderResults);
            for (int i = 0; i < numHit; i++)
            {
                Tile tile = _maybeColliderResults[i].GetComponent<Tile>();
                if (tile != null && tile is RatWall)
                {
                    tile.takeDamage(this, tile.health, DamageType.Explosive);
                    this.die();
                }
            }
        }        
    }
}
