using BeanBlaster.Combat;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Level
{
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
        private void OnTriggerEnter2D(Collider2D other)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health == null || health.Team != Team.Player) return;
            levelManager ??= FindObjectOfType<LevelManager>();
            levelManager?.CompleteLevel();
        }
    }
}
