using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle;

    public void Spawn(GemTypeSO gemType)
    { 
        _particle.textureSheetAnimation.RemoveSprite(0);
        _particle.textureSheetAnimation.AddSprite(gemType.Sprite);

        this.gameObject.SetActive(true);
        _particle.Play();
    }

    private void OnValidate()
    {
        if (_particle == null)
        {
            _particle = this.GetComponentInChildren<ParticleSystem>();
        }
    }
}
