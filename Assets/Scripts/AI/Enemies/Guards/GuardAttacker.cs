using UnityEngine;

public class GuardAttacker : MonoBehaviour
{
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] public float attackRange = 1.5f;
    [SerializeField] private LineRenderer weaponVisuals;
    [SerializeField] private float visualDuration = 0.2f;

    private Transform player;
    private PlayerStats playerStats;

    private void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }

    }

    public void Attack()
    {
        if (player == null || playerStats == null) return;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            var cast = Physics2D.Raycast(transform.position, (player.position - transform.position).normalized, attackRange);

            if(cast.collider != null && cast.collider.CompareTag("Player"))
            {
                playerStats.TakeDamage(attackDamage);
            }
            VisualEffects(cast);
        }
    }

    private async void VisualEffects(RaycastHit2D hit)
    {
        if (weaponVisuals != null)
        {
            weaponVisuals.SetPosition(0, transform.position);
            weaponVisuals.SetPosition(1, hit.point);
            weaponVisuals.enabled = true;
            await Awaitable.WaitForSecondsAsync(visualDuration);
            if (weaponVisuals != null) weaponVisuals.enabled = false;
        }
    }
}
