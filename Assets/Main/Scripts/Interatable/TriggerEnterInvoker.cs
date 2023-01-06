using System;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEnterInvoker : MonoBehaviour
{
    public TriggerEnterCallback callback;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        LivingEntity entity = LivingEntity.FromCollider(collision);
        if (entity != null)
        {
            Player player = entity.GetComponent<Player>();
            if (player != null)
            {
                callback?.Invoke();
            }
        }
    }

    [Serializable]
    public class TriggerEnterCallback : UnityEvent { }
}
