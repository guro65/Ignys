using System.Collections.Generic;
using TMPro;
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
    [Min(0)] public int quantidadeCartasReserva = 3;
    public string tagSlotDeckPlayer = "SlotDeckPlayer";
    public string tagCartaPlayer = "CartaPlayer";

    [Header("Estado atual - Deck Principal")]
    public List<Carta> cartasSelecionadas = new List<Carta>();

    [Header("Estado atual - Reserva")]
    public List<Carta> cartasReservaSelecionadas = new List<Carta>();

    [Header("Lista dos slots do deck do player")]
    public List<Transform> slotsDeckPlayer = new List<Transform>();

    [Header("Cartas instanciadas no deck do player")]
    public List<GameObject> cartasInstanciadasNoDeck = new List<GameObject>();

    [Header("Controle")]
    public bool preparacaoConcluida = false;

    private readonly List<CartaSelecaoCombateUI> slotsUIInstanciados = new List<CartaSelecaoCombateUI>();
    private TMP_Text textoResumoSelecaoAuto;

    private void Start()
    {
        IniciarPreparacao();
    }

    public void IniciarPreparacao()
    {
        preparacaoConcluida = false;
        cartasSelecionadas.Clear();
        cartasReservaSelecionadas.Clear();
        Time.timeScale = 0f;

        if (painelInventario != null)
            painelInventario.SetActive(true);

        if (botaoConfirmar != null)
        {
            botaoConfirmar.onClick.RemoveAllListeners();
            botaoConfirmar.onClick.AddListener(ConfirmarSelecao);
        }

        CriarResumoAutomatico();
        AtualizarResumoAutomatico();
        AtualizarInventarioDeCombate();
    }

    public void AtualizarInventarioDeCombate()
    {
        if (Inventario.instancia == null)
        {
            Debug.LogError("Inventário não encontrado.");
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
        cartasReservaSelecionadas.Clear();
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
                slotUI.DefinirEstadoSelecao(CartaSelecaoCombateUI.EstadoSelecao.Nenhuma);
                slotsUIInstanciados.Add(slotUI);
            }
        }

        AtualizarResumoAutomatico();
    }

    public void AlternarSelecaoCarta(Carta carta, CartaSelecaoCombateUI slotUI)
    {
        if (carta == null || slotUI == null)
            return;

        CartaSelecaoCombateUI.EstadoSelecao estado = slotUI.ObterEstadoSelecao();

        if (estado == CartaSelecaoCombateUI.EstadoSelecao.Nenhuma)
        {
            if (cartasSelecionadas.Count < quantidadeCartasParaSelecionar)
            {
                cartasSelecionadas.Add(carta);
                slotUI.DefinirEstadoSelecao(CartaSelecaoCombateUI.EstadoSelecao.DeckPrincipal);
                Debug.Log($"{carta.nome} foi adicionada ao DECK principal.");
            }
            else if (cartasReservaSelecionadas.Count < quantidadeCartasReserva)
            {
                cartasReservaSelecionadas.Add(carta);
                slotUI.DefinirEstadoSelecao(CartaSelecaoCombateUI.EstadoSelecao.Reserva);
                Debug.Log($"{carta.nome} foi adicionada à RESERVA.");
            }
            else
            {
                Debug.Log("Deck principal e reserva já estão completos.");
            }
        }
        else if (estado == CartaSelecaoCombateUI.EstadoSelecao.DeckPrincipal)
        {
            RemoverUmaOcorrencia(cartasSelecionadas, carta);

            if (cartasReservaSelecionadas.Count < quantidadeCartasReserva)
            {
                cartasReservaSelecionadas.Add(carta);
                slotUI.DefinirEstadoSelecao(CartaSelecaoCombateUI.EstadoSelecao.Reserva);
                Debug.Log($"{carta.nome} foi movida do DECK para a RESERVA.");
            }
            else
            {
                slotUI.DefinirEstadoSelecao(CartaSelecaoCombateUI.EstadoSelecao.Nenhuma);
                Debug.Log($"{carta.nome} foi removida da seleção.");
            }
        }
        else
        {
            RemoverUmaOcorrencia(cartasReservaSelecionadas, carta);
            slotUI.DefinirEstadoSelecao(CartaSelecaoCombateUI.EstadoSelecao.Nenhuma);
            Debug.Log($"{carta.nome} foi removida da RESERVA.");
        }

        AtualizarResumoAutomatico();
    }

    public void ConfirmarSelecao()
    {
        if (cartasSelecionadas.Count != quantidadeCartasParaSelecionar)
        {
            Debug.Log($"Selecione exatamente {quantidadeCartasParaSelecionar} cartas para o deck principal.");
            AtualizarResumoAutomatico();
            return;
        }

        if (cartasReservaSelecionadas.Count != quantidadeCartasReserva)
        {
            Debug.Log($"Selecione exatamente {quantidadeCartasReserva} cartas para a reserva.");
            AtualizarResumoAutomatico();
            return;
        }

        BuscarSlotsDeckPlayer();

        if (slotsDeckPlayer.Count < quantidadeCartasParaSelecionar)
        {
            Debug.LogError($"Existem somente {slotsDeckPlayer.Count} slots de deck, mas são necessárias {quantidadeCartasParaSelecionar} cartas.");
            return;
        }

        // As cartas NÃO são instanciadas aqui. O CombateAmigavel espera a preparação terminar
        // e usa AnimadorEntradaDeckCombate para colocar os dois decks aos poucos.
        cartasInstanciadasNoDeck.Clear();

        if (painelInventario != null)
            painelInventario.SetActive(false);

        preparacaoConcluida = true;
        Time.timeScale = 1f;

        Debug.Log($"Preparação concluída: {cartasSelecionadas.Count} cartas no deck e {cartasReservaSelecionadas.Count} na reserva.");
    }

    public List<Carta> ObterDeckPrincipalSelecionadoCopia()
    {
        return new List<Carta>(cartasSelecionadas);
    }

    public List<Carta> ObterReservaSelecionadaCopia()
    {
        return new List<Carta>(cartasReservaSelecionadas);
    }

    public void LimparRegistroCartasInstanciadasNoDeck()
    {
        cartasInstanciadasNoDeck.Clear();
    }

    public void RegistrarCartaInstanciadaNoDeck(GameObject carta)
    {
        if (carta != null && !cartasInstanciadasNoDeck.Contains(carta))
            cartasInstanciadasNoDeck.Add(carta);
    }

    public void BuscarSlotsDeckPlayer()
    {
        slotsDeckPlayer.Clear();

        GameObject[] slotsEncontrados = GameObject.FindGameObjectsWithTag(tagSlotDeckPlayer);
        for (int i = 0; i < slotsEncontrados.Length; i++)
        {
            if (slotsEncontrados[i] != null)
                slotsDeckPlayer.Add(slotsEncontrados[i].transform);
        }

        slotsDeckPlayer.Sort((a, b) => a.name.CompareTo(b.name));
        Debug.Log($"Foram encontrados {slotsDeckPlayer.Count} slots com a tag {tagSlotDeckPlayer}.");
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

            cartaInstanciada.transform.SetParent(slotAtual);
            cartaInstanciada.transform.position = slotAtual.position;
            cartaInstanciada.transform.localScale = escalaOriginal;
            cartaInstanciada.tag = tagCartaPlayer;

            cartasInstanciadasNoDeck.Add(cartaInstanciada);
            Debug.Log($"Carta do player {cartaPrefab.nome} colocada no slot {slotAtual.name}.");
        }
    }

    private void CriarResumoAutomatico()
    {
        if (textoResumoSelecaoAuto != null || painelInventario == null)
            return;

        RectTransform parent = painelInventario.transform as RectTransform;
        if (parent == null)
            return;

        Transform existente = painelInventario.transform.Find("ResumoSelecaoDeck_Auto");
        if (existente != null)
        {
            textoResumoSelecaoAuto = existente.GetComponent<TMP_Text>();
            return;
        }

        GameObject obj = new GameObject("ResumoSelecaoDeck_Auto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(1000f, 86f);
        rt.anchoredPosition = new Vector2(0f, -10f);

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 23f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        textoResumoSelecaoAuto = tmp;
    }

    private void AtualizarResumoAutomatico()
    {
        if (textoResumoSelecaoAuto == null)
            CriarResumoAutomatico();

        if (textoResumoSelecaoAuto != null)
        {
            textoResumoSelecaoAuto.text =
                $"DECK PRINCIPAL: {cartasSelecionadas.Count}/{quantidadeCartasParaSelecionar}     |     " +
                $"RESERVA: {cartasReservaSelecionadas.Count}/{quantidadeCartasReserva}\n" +
                "Clique: DECK ? RESERVA ? remover";
        }
    }

    private void RemoverUmaOcorrencia(List<Carta> lista, Carta carta)
    {
        if (lista == null)
            return;

        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i] == carta)
            {
                lista.RemoveAt(i);
                return;
            }
        }
    }

    private void LimparInventarioUI()
    {
        slotsUIInstanciados.Clear();

        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }
}
