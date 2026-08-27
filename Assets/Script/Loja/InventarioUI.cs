using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventarioUI : MonoBehaviour
{
    [Header("Painel principal do inventário")]
    [SerializeField] private GameObject painelInventario;

    [Header("Botões das abas")]
    [SerializeField] private Button botaoCartas;
    [SerializeField] private Button botaoPacotes;

    [Header("Aba CARTAS")]
    [Tooltip("Este é o mesmo Content que o inventário antigo já utilizava.")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject slotCartaPrefab;
    [SerializeField] private GameObject painelAbaCartas;

    [Header("Aba PACOTES")]
    [SerializeField] private Transform contentPacotes;
    [SerializeField] private GameObject slotPacotePrefab;
    [SerializeField] private GameObject painelAbaPacotes;

    [Header("Painel de ações do pacote")]
    [Tooltip("Deixe este painel cobrindo a tela do inventário com uma Image e Raycast Target ligado para bloquear cliques atrás dele.")]
    [SerializeField] private GameObject painelAcoesPacote;
    [SerializeField] private TMP_Text textoNomePacoteSelecionado;
    [SerializeField] private TMP_Text textoResultadoPeso;
    [SerializeField] private Button botaoAbrirPacote;
    [SerializeField] private Button botaoPesarPacote;
    [SerializeField] private Button botaoFecharPacote;

    private PacoteAdquirido pacoteSelecionado;
    private bool mostrandoPacotes = false;

    private void Start()
    {
        ConfigurarBotoes();

        if (painelInventario != null)
            painelInventario.SetActive(false);

        if (painelAcoesPacote != null)
            painelAcoesPacote.SetActive(false);
    }

    private void ConfigurarBotoes()
    {
        if (botaoCartas != null)
        {
            botaoCartas.onClick.RemoveAllListeners();
            botaoCartas.onClick.AddListener(MostrarCartas);
        }

        if (botaoPacotes != null)
        {
            botaoPacotes.onClick.RemoveAllListeners();
            botaoPacotes.onClick.AddListener(MostrarPacotes);
        }

        if (botaoAbrirPacote != null)
        {
            botaoAbrirPacote.onClick.RemoveAllListeners();
            botaoAbrirPacote.onClick.AddListener(AbrirPacoteSelecionado);
        }

        if (botaoPesarPacote != null)
        {
            botaoPesarPacote.onClick.RemoveAllListeners();
            botaoPesarPacote.onClick.AddListener(PesarPacoteSelecionado);
        }

        if (botaoFecharPacote != null)
        {
            botaoFecharPacote.onClick.RemoveAllListeners();
            botaoFecharPacote.onClick.AddListener(FecharAcoesPacote);
        }
    }

    public void AbrirFecharInventario()
    {
        if (painelInventario == null)
            return;

        bool estaAtivo = painelInventario.activeSelf;
        painelInventario.SetActive(!estaAtivo);

        if (!estaAtivo)
        {
            MostrarCartas();
            AtualizarInventario();
        }
        else
        {
            FecharAcoesPacote();
        }
    }

    // Mantido para compatibilidade com o sistema antigo.
    // Agora atualiza as duas partes do inventário.
    public void AtualizarInventario()
    {
        AtualizarCartas();
        AtualizarPacotes();
    }

    public void MostrarCartas()
    {
        mostrandoPacotes = false;
        FecharAcoesPacote();

        if (painelAbaCartas != null)
            painelAbaCartas.SetActive(true);

        if (painelAbaPacotes != null)
            painelAbaPacotes.SetActive(false);

        AtualizarCartas();
    }

    public void MostrarPacotes()
    {
        mostrandoPacotes = true;
        FecharAcoesPacote();

        if (painelAbaCartas != null)
            painelAbaCartas.SetActive(false);

        if (painelAbaPacotes != null)
            painelAbaPacotes.SetActive(true);

        AtualizarPacotes();
    }

    public void AtualizarCartas()
    {
        if (Inventario.instancia == null)
        {
            Debug.LogWarning("Inventário não encontrado.");
            return;
        }

        if (content == null || slotCartaPrefab == null)
            return;

        LimparContent(content);

        for (int i = 0; i < Inventario.instancia.cartasObtidas.Count; i++)
        {
            Carta carta = Inventario.instancia.cartasObtidas[i];

            if (carta == null)
                continue;

            GameObject novoSlot = Instantiate(slotCartaPrefab, content);

            CartaUI cartaUI = novoSlot.GetComponent<CartaUI>();
            if (cartaUI != null)
                cartaUI.Configurar(carta);
        }
    }

    public void AtualizarPacotes()
    {
        if (Inventario.instancia == null)
        {
            Debug.LogWarning("Inventário não encontrado.");
            return;
        }

        if (contentPacotes == null || slotPacotePrefab == null)
            return;

        LimparContent(contentPacotes);

        for (int i = 0; i < Inventario.instancia.pacotesObtidos.Count; i++)
        {
            PacoteAdquirido pacote = Inventario.instancia.pacotesObtidos[i];

            if (pacote == null)
                continue;

            GameObject novoSlot = Instantiate(slotPacotePrefab, contentPacotes);
            PacoteInventarioUI pacoteUI = novoSlot.GetComponent<PacoteInventarioUI>();

            if (pacoteUI != null)
                pacoteUI.Configurar(pacote, this);
            else
                Debug.LogWarning("O prefab de slot de pacote não possui o script PacoteInventarioUI.");
        }
    }

    public void SelecionarPacote(PacoteAdquirido pacote)
    {
        if (!mostrandoPacotes || pacote == null)
            return;

        pacoteSelecionado = pacote;

        if (textoNomePacoteSelecionado != null)
            textoNomePacoteSelecionado.text = pacoteSelecionado.nomePacote;

        // O peso permanece escondido até o jogador apertar Pesar.
        if (textoResultadoPeso != null)
            textoResultadoPeso.text = "";

        if (painelAcoesPacote != null)
            painelAcoesPacote.SetActive(true);
    }

    public void PesarPacoteSelecionado()
    {
        if (pacoteSelecionado == null)
            return;

        if (textoResultadoPeso != null)
            textoResultadoPeso.text = pacoteSelecionado.ObterTextoPeso();

        Debug.Log($"Pacote pesado: {pacoteSelecionado.ObterTextoPeso()}");
    }

    public void AbrirPacoteSelecionado()
    {
        if (pacoteSelecionado == null)
            return;

        if (Inventario.instancia == null)
        {
            Debug.LogWarning("Inventário não encontrado.");
            return;
        }

        PacoteAdquirido pacoteQueSeraAberto = pacoteSelecionado;
        var cartasObtidas = Inventario.instancia.AbrirPacote(pacoteQueSeraAberto);

        if (cartasObtidas == null || cartasObtidas.Count == 0)
            return;

        for (int i = 0; i < cartasObtidas.Count; i++)
        {
            Carta carta = cartasObtidas[i];
            if (carta != null)
                Debug.Log($"Carta do pacote [{i + 1}/10]: {carta.nome} | {carta.raridade}");
        }

        FecharAcoesPacote();
        AtualizarPacotes();
        AtualizarCartas();
    }

    public void FecharAcoesPacote()
    {
        pacoteSelecionado = null;

        if (textoNomePacoteSelecionado != null)
            textoNomePacoteSelecionado.text = "";

        if (textoResultadoPeso != null)
            textoResultadoPeso.text = "";

        if (painelAcoesPacote != null)
            painelAcoesPacote.SetActive(false);
    }

    private void LimparContent(Transform alvo)
    {
        if (alvo == null)
            return;

        for (int i = alvo.childCount - 1; i >= 0; i--)
            Destroy(alvo.GetChild(i).gameObject);
    }
}
