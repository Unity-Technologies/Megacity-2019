using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.MegaCity.Audio
{
    public class DeathSounds : MonoBehaviour
    {
        public static DeathSounds Instance; 
        public AudioClip[] deathSoundArray;
        public AudioClip[] killingSoundArray;

        private AudioSource audSource; 

        private void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }

            audSource = GetComponent<AudioSource>();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void PlayDeathSound()
        {
            
            audSource.PlayOneShot(deathSoundArray[Random.Range(0, deathSoundArray.Length)]);
        } 
    }

}