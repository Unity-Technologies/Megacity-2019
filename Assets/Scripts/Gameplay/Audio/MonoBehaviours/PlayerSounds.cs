using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Megacity.Audio
{
    public class PlayerSounds : MonoBehaviour
    {
        public static PlayerSounds Instance; 
        public AudioClip[] deathSoundsArray;
        public AudioClip[] killingSoundsArray;

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
        
        public void PlayDeathSound()
        {
            var audClip = deathSoundsArray[Random.Range(0, deathSoundsArray.Length)]; 
            audSource.pitch = Random.Range(0.8f, 1.2f);
            audSource.volume = Random.Range(0.9f , 1f);
            audSource.PlayOneShot(audClip);
        } 
        public void PlayKillSound()
        {
            var audClip = killingSoundsArray[Random.Range(0, killingSoundsArray.Length)]; 
            audSource.pitch = Random.Range(0.8f, 1.2f);
            audSource.volume = Random.Range(0.9f , 1f);
            audSource.PlayOneShot(audClip);
        } 
    }

}