using BeanBlaster.Combat;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Level
{
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
        private bool activated;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated) return;
            Health health = other.GetComponentInParent<Health>();
            if (health == null || health.Team != Team.Player) return;
            activated = true;
            levelManager ??= FindObjectOfType<LevelManager>();
            levelManager?.SetCheckpoint(transform.position);
        }
    }
}
