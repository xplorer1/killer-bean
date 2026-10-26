using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Level;
using UnityEngine;

namespace BeanBlaster.Economy
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CoinPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int value = 10;
        [SerializeField] private bool permanentPlacedPickup;
        [SerializeField, Min(0f)] private float dropLifetime = 12f;
        [SerializeField, Min(0f)] private float magnetRange = 4f;
        [SerializeField, Min(0f)] private float magnetSpeed = 8f;
        [SerializeField] private SpriteRenderer visual;

        private Transform player;
        private LevelManager levelManager;
        private float elapsed;
        private bool collected;

        public int Value => value;

        public void Configure(int coinValue, bool isPermanent, Transform playerTarget = null, LevelManager manager = null)
        {
            value = Mathf.Max(1, coinValue);
            permanentPlacedPickup = isPermanent;
            player = playerTarget;
            levelManager = manager;
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject == null ? null : playerObject.transform;
            }
            if (player != null && Vector2.Distance(transform.position, player.position) <= magnetRange)
            {
                transform.position = Vector2.MoveTowards(transform.position, player.position, magnetSpeed * Time.deltaTime);
            }
            if (!permanentPlacedPickup)
            {
                elapsed += Time.deltaTime;
                if (visual != null && elapsed >= dropLifetime - 2f)
                {
                    visual.enabled = Mathf.FloorToInt(elapsed * 10f) % 2 == 0;
                }
                if (elapsed >= dropLifetime)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        public bool TryCollect(Collider2D other)
        {
            Health health = other == null ? null : other.GetComponentInParent<Health>();
            if (collected || health == null || health.Team != Team.Player)
            {
                return false;
            }
            collected = true;
            EconomyService.AddCoins(value, CoinSource.Pickup);
            levelManager?.OnCoinsCollected(value);
            gameObject.SetActive(false);
            return true;
        }
    }
}
