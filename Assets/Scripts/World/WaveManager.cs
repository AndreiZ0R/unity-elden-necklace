using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] public SpawnGate[] gates;
    [SerializeField] public GameObject gruntPrefab;
    [SerializeField] public GameObject guardianPrefab;
    [SerializeField] public GameObject knightPrefab;
    [SerializeField] public float spawnInterval = 0.8f;
    [SerializeField] public float restDuration = 5f;

    public int currentWave { get; private set; }
    private List<EnemyHealth> _aliveEnemies = new List<EnemyHealth>();

    public event System.Action<int> OnWaveChanged;
    public event System.Action OnWaveCleared;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    void Start() => StartCoroutine(WaveLoop());

    IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(2f);
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
            currentWave++;
            GameManager.Instance?.SetWave(currentWave);
            OnWaveChanged?.Invoke(currentWave);
            yield return StartCoroutine(StartWave(currentWave));
            yield return new WaitUntil(() => _aliveEnemies.Count == 0);
            OnWaveCleared?.Invoke();
            yield return new WaitForSeconds(restDuration);
        }
    }

    IEnumerator StartWave(int wave)
    {
        var spawnList = BuildSpawnList(wave);
        Shuffle(spawnList);

        foreach (var (prefab, gateIdx) in spawnList)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
            if (prefab == null) continue;

            SpawnGate gate = GetGate(gateIdx);
            if (gate == null) continue;

            Vector3 spawnPos = gate.spawnPoint != null ? gate.spawnPoint.position : gate.transform.position;
            if (!NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    spawnPos -= (spawnPos - Vector3.zero).normalized * 0.5f;
                    if (NavMesh.SamplePosition(spawnPos, out hit, 5f, NavMesh.AllAreas)) break;
                }
            }
            else
            {
                spawnPos = hit.position;
            }

            var go = Instantiate(prefab, spawnPos, gate.transform.rotation);
            var enemyHealth = go.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                _aliveEnemies.Add(enemyHealth);
                enemyHealth.OnDeath += OnEnemyDied;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    List<(GameObject prefab, int gateIndex)> BuildSpawnList(int wave)
    {
        var list = new List<(GameObject, int)>();
        int grunts = 2 + wave * 2;
        int guardians = wave >= 3 ? (wave - 2) : 0;
        int knights = wave >= 5 ? (wave - 4) : 0;

        int gate = 0;
        for (int i = 0; i < grunts; i++) { list.Add((gruntPrefab, gate % 4)); gate++; }
        for (int i = 0; i < guardians && guardianPrefab != null; i++) { list.Add((guardianPrefab, gate % 4)); gate++; }
        for (int i = 0; i < knights && knightPrefab != null; i++) { list.Add((knightPrefab, gate % 4)); gate++; }

        return list;
    }

    void OnEnemyDied(EnemyHealth enemy)
    {
        _aliveEnemies.Remove(enemy);
        GameManager.Instance?.AddScore(currentWave * 100);
    }

    SpawnGate GetGate(int index)
    {
        if (gates == null || gates.Length == 0) return null;
        foreach (var g in gates)
            if (g != null && g.gateIndex == index) return g;
        return gates[index % gates.Length];
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
