using UnityEngine;

public class Cheese : Tile
{
    public override void useAsItem(Tile tileUsingUs)
    {
        tileUsingUs.health += 1;
        tileUsingUs.addForce(tileUsingUs.aimDirection.normalized * 10000f);
        takeDamage(tileUsingUs, health, DamageType.Normal);
        
    }
}
