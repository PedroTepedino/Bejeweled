using UnityEngine;

public class EffectsPoolling : APooling<ExplosionEffect>
{
    public void SpawnObj(GemTypeSO gemType, Vector3 position)
    {
        this.SpawnObj(position).Spawn(gemType);
    }
}