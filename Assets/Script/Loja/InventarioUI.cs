using UnityEngine;

public class InventarioUI : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private GameObject painelInventario;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject slotCartaPrefab;

    private void Start()
    {
        if (painelInventario != null)
            painelInventario.SetActive(false);
    }

    public void AbrirFecharInventario()
    {
        if (painelInventario == null)
            return;

        bool estaAtivo = painelInventario.activeSelf;
        painelInventario.SetActive(!estaAtivo);

        if (!estaAtivo)
        {
            AtualizarInventario();
        }
    }

    public void AtualizarInventario()
    {
        if (Inventario.instancia == null)
        {
            Debug.LogWarning("Inventário não encontrado.");
            return;
        }

        LimparInventarioUI();

        for (int i = 0; i < Inventario.instancia.cartasObtidas.Count; i++)
        {
            Carta carta = Inventario.instancia.cartasObtidas[i];

            if (carta == null)
                continue;

            GameObject novoSlot = Instantiate(slotCartaPrefab, content);

            CartaUI cartaUI = novoSlot.GetComponent<CartaUI>();
            if (cartaUI != null)
            {
                cartaUI.Configurar(carta);
            }
        }
    }

    private void LimparInventarioUI()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }
}