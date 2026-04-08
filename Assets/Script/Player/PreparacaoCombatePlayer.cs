using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreparacaoCombatePlayer : MonoBehaviour
{
    [Header("UI da seleção")]
    public GameObject painelInventario;
    public Transform content;
    public GameObject slotCartaSelecaoPrefab;
    public Button botaoConfirmar;

    [Header("Configuração do deck do player")]
    [Min(1)] public int quantidadeCartasParaSelecionar = 5;
    public string tagSlotDeckPlayer = "SlotDeckPlayer";
    public string tagCartaPlayer = "CartaPlayer";

    [Header("Estado atual")]
    public List<Carta> cartasSelecionadas = new List<Carta>();

    [Header("Lista dos slots do deck do player")]
    public List<Transform> slotsDeckPlayer = new List<Transform>();

    [Header("Cartas instanciadas no deck do player")]
    public List<GameObject> cartasInstanciadasNoDeck = new List<GameObject>();

    [Header("Controle")]
    public bool preparacaoConcluida = false;

    private readonly List<CartaSelecaoCombateUI> slotsUIInstanciados = new List<CartaSelecaoCombateUI>();

    private void Start()
    {
        IniciarPreparacao();
    }

    public void IniciarPreparacao()
    {
        preparacaoConcluida = false;
        cartasSelecionadas.Clear();
        Time.timeScale = 0f;

        if (painelInventario != null)
            painelInventario.SetActive(true);

        if (botaoConfirmar != null)
        {
            botaoConfirmar.onClick.RemoveAllListeners();
            botaoConfirmar.onClick.AddListener(ConfirmarSelecao);
        }

        AtualizarInventarioDeCombate();
    }

    public void AtualizarInventarioDeCombate()
    {
        if (Inventario.instancia == null)
        {
            Debug.LogError("Inventario não encontrado.");
            return;
        }

        if (content == null)
        {
            Debug.LogError("O Content da seleção de combate não foi definido.");
            return;
        }

        if (slotCartaSelecaoPrefab == null)
        {
            Debug.LogError("O prefab do slot de seleção não foi definido.");
            return;
        }

        cartasSelecionadas.Clear();
        LimparInventarioUI();

        for (int i = 0; i < Inventario.instancia.cartasObtidas.Count; i++)
        {
            Carta carta = Inventario.instancia.cartasObtidas[i];

            if (carta == null)
                continue;

            GameObject novoSlot = Instantiate(slotCartaSelecaoPrefab, content);
            CartaSelecaoCombateUI slotUI = novoSlot.GetComponent<CartaSelecaoCombateUI>();

            if (slotUI != null)
            {
                slotUI.Configurar(carta, this);
                slotUI.DefinirSelecionado(false);
                slotsUIInstanciados.Add(slotUI);
            }
        }
    }

    public void AlternarSelecaoCarta(Carta carta, CartaSelecaoCombateUI slotUI)
    {
        if (carta == null || slotUI == null)
            return;

        if (slotUI.EstaSelecionada())
        {
            RemoverCartaDaSelecao(carta);
            slotUI.DefinirSelecionado(false);
            Debug.Log($"Carta removida da seleção: {carta.nome}");
            Debug.Log($"Total selecionado: {cartasSelecionadas.Count}/{quantidadeCartasParaSelecionar}");
            return;
        }

        if (cartasSelecionadas.Count >= quantidadeCartasParaSelecionar)
        {
            Debug.Log($"Você só pode selecionar {quantidadeCartasParaSelecionar} cartas.");
            return;
        }

        cartasSelecionadas.Add(carta);
        slotUI.DefinirSelecionado(true);

        Debug.Log($"Carta adicionada à seleção: {carta.nome}");
        Debug.Log($"Total selecionado: {cartasSelecionadas.Count}/{quantidadeCartasParaSelecionar}");
    }

    private void RemoverCartaDaSelecao(Carta carta)
    {
        for (int i = cartasSelecionadas.Count - 1; i >= 0; i--)
        {
            if (cartasSelecionadas[i] == carta)
            {
                cartasSelecionadas.RemoveAt(i);
                return;
            }
        }
    }

    public void ConfirmarSelecao()
    {
        if (cartasSelecionadas.Count != quantidadeCartasParaSelecionar)
        {
            Debug.Log($"Selecione exatamente {quantidadeCartasParaSelecionar} cartas para iniciar o combate.");
            return;
        }

        BuscarSlotsDeckPlayer();
        PosicionarCartasSelecionadasNoDeck();

        if (painelInventario != null)
            painelInventario.SetActive(false);

        preparacaoConcluida = true;
        Time.timeScale = 1f;

        Debug.Log("Preparação do combate concluída. O combate pode começar.");
    }

    public void BuscarSlotsDeckPlayer()
    {
        slotsDeckPlayer.Clear();

        GameObject[] slotsEncontrados = GameObject.FindGameObjectsWithTag(tagSlotDeckPlayer);

        for (int i = 0; i < slotsEncontrados.Length; i++)
        {
            if (slotsEncontrados[i] != null)
            {
                slotsDeckPlayer.Add(slotsEncontrados[i].transform);
            }
        }

        slotsDeckPlayer.Sort((a, b) => a.name.CompareTo(b.name));

        Debug.Log($"Foram encontrados {slotsDeckPlayer.Count} slots com a tag {tagSlotDeckPlayer}.");

        for (int i = 0; i < slotsDeckPlayer.Count; i++)
        {
            Debug.Log($"Slot Player [{i}] encontrado: {slotsDeckPlayer[i].name}");
        }
    }

    public void PosicionarCartasSelecionadasNoDeck()
    {
        if (slotsDeckPlayer.Count == 0)
        {
            Debug.LogWarning("Nenhum slot do deck do player foi encontrado.");
            return;
        }

        cartasInstanciadasNoDeck.Clear();

        int quantidadeParaPosicionar = Mathf.Min(cartasSelecionadas.Count, slotsDeckPlayer.Count);

        for (int i = 0; i < quantidadeParaPosicionar; i++)
        {
            Carta cartaPrefab = cartasSelecionadas[i];
            Transform slotAtual = slotsDeckPlayer[i];

            if (cartaPrefab == null || slotAtual == null)
                continue;

            GameObject cartaInstanciada = Instantiate(cartaPrefab.gameObject);

            Vector3 escalaOriginal = cartaInstanciada.transform.localScale;

            cartaInstanciada.transform.position = slotAtual.position;
            cartaInstanciada.transform.SetParent(slotAtual);
            cartaInstanciada.transform.position = slotAtual.position;
            cartaInstanciada.transform.localScale = escalaOriginal;
            cartaInstanciada.tag = tagCartaPlayer;

            cartasInstanciadasNoDeck.Add(cartaInstanciada);

            Debug.Log($"Carta do player {cartaPrefab.nome} colocada no slot {slotAtual.name} com a tag {tagCartaPlayer}.");
        }

        Debug.Log($"{quantidadeParaPosicionar} cartas do player foram posicionadas nos slots do deck.");
    }

    private void LimparInventarioUI()
    {
        slotsUIInstanciados.Clear();

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }
}