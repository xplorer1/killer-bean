using UnityEngine;

namespace BeanBlaster.Core
{
    /// <summary>Ensures directly opened gameplay scenes still receive persistent services.</summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (GameManager.Instance == null)
            {
                new GameObject("GameManager", typeof(GameManager));
            }
        }
    }
}
