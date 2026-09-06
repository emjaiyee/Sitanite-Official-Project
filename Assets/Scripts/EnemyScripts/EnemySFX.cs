using UnityEngine;

public class EnemySFX : MonoBehaviour
{
    private AudioSource audioSource;


    [Header ("Place Animation SFX Here")]

    [SerializeField] private AudioClip[] walkSFX;
    [SerializeField] private AudioClip[] attackSFX;
    [SerializeField] private AudioClip deathSFX;

    private int walkSFXIndex = 0;
    private int attackSFXIndex = 0;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    

    public void PlayWalkSFX()
    {
        audioSource.PlayOneShot(walkSFX[walkSFXIndex]);
        walkSFXIndex++;
    }


    public void PlayAttackSFX()             //Attack, Shoot or Casting SFX
    {
        audioSource.PlayOneShot(attackSFX[attackSFXIndex]);
        attackSFXIndex++;
    }



    public void PlayDeathSFX()
    {
        audioSource.PlayOneShot(deathSFX);    
    }

}
