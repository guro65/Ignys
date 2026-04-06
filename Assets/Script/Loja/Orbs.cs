using UnityEngine;

public class Orbs : MonoBehaviour
{
    public static Orbs instancia;

    [Header("Quantidade de Orbs")]
    public int quantidade = 0;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AdicionarOrbs(int valor)
    {
        quantidade += valor;
        Debug.Log($"Orbs adicionados: {valor} | Total: {quantidade}");
    }

    public bool GastarOrbs(int valor)
    {
        if (quantidade < valor)
        {
            Debug.Log("Orbs insuficientes.");
            return false;
        }

        quantidade -= valor;
        Debug.Log($"Orbs gastos: {valor} | Restante: {quantidade}");
        return true;
    }
}