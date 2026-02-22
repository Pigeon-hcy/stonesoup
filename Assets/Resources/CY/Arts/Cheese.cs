using UnityEngine;

public class Cheese : Tile
{
    public override void useAsItem(Tile tileUsingUs)
    {
        tileUsingUs.health += 1;
        takeDamage(tileUsingUs, health, DamageType.Normal);
        
    }
}
