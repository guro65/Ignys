using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CartaSelecaoCombateUI : MonoBehaviour
{
    public enum EstadoSelecao
    {
        Nenhuma,
        DeckPrincipal,
        Reserva
    }

    [Header("Referências UI")]
    [SerializeField] private Image imagemCarta;
    [SerializeField] private GameObject marcadorSelecao;
    [SerializeField] private Button botao;

    [Header("Indicador automático DECK / RESERVA")]
    [Min(3f)][SerializeField] private float tamanhoFonteIndicador = 7f;
    [Min(8f)][SerializeField] private float alturaIndicador = 16f;

    private Carta carta;
    private PreparacaoCombatePlayer preparacaoCombate;
    private EstadoSelecao estadoAtual = EstadoSelecao.Nenhuma;

    private GameObject indicadorEstadoAuto;
    private Image fundoIndicadorAuto;
    private TMP_Text textoIndicadorAuto;

    private void Awake()
    {
        if (imagemCarta == null)
            imagemCarta = GetComponent<Image>();

        if (botao == null)
            botao = GetComponent<Button>();

        if (botao != null)
        {
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(AoClicar);
        }

        CriarIndicadorAutomaticoSeNecessario();
        estadoAtual = EstadoSelecao.Nenhuma;
        AtualizarVisual();
    }

    public void Configurar(Carta cartaRecebida, PreparacaoCombatePlayer preparacao)
    {
        carta = cartaRecebida;
        preparacaoCombate = preparacao;

        estadoAtual = EstadoSelecao.Nenhuma;
        CriarIndicadorAutomaticoSeNecessario();
        AtualizarVisual();

        if (carta == null)
        {
            Debug.LogWarning("CartaSelecaoCombateUI recebeu uma carta nula.");
            return;
        }

        SpriteRenderer spriteRenderer = carta.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"A carta {carta.nome} não possui SpriteRenderer.");
            return;
        }

        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"A carta {carta.nome} não possui sprite definido.");
            return;
        }

        if (imagemCarta != null)
        {
            imagemCarta.sprite = spriteRenderer.sprite;
            imagemCarta.preserveAspect = true;
        }
    }

    private void AoClicar()
    {
        if (preparacaoCombate == null || carta == null)
            return;

        preparacaoCombate.AlternarSelecaoCarta(carta, this);
    }

    public void DefinirEstadoSelecao(EstadoSelecao novoEstado)
    {
        estadoAtual = novoEstado;
        AtualizarVisual();
    }

    public EstadoSelecao ObterEstadoSelecao()
    {
        return estadoAtual;
    }

    // Compatibilidade com o sistema antigo.
    public void DefinirSelecionado(bool valor)
    {
        DefinirEstadoSelecao(valor ? EstadoSelecao.DeckPrincipal : EstadoSelecao.Nenhuma);
    }

    public bool EstaSelecionada()
    {
        return estadoAtual != EstadoSelecao.Nenhuma;
    }

    public bool EstaNoDeckPrincipal()
    {
        return estadoAtual == EstadoSelecao.DeckPrincipal;
    }

    public bool EstaNaReserva()
    {
        return estadoAtual == EstadoSelecao.Reserva;
    }

    public Carta ObterCarta()
    {
        return carta;
    }

    private void CriarIndicadorAutomaticoSeNecessario()
    {
        if (indicadorEstadoAuto != null)
            return;

        RectTransform raiz = transform as RectTransform;
        if (raiz == null)
            return;

        Transform existente = transform.Find("IndicadorEstado_Auto");
        if (existente != null)
        {
            indicadorEstadoAuto = existente.gameObject;
            fundoIndicadorAuto = indicadorEstadoAuto.GetComponent<Image>();
            textoIndicadorAuto = indicadorEstadoAuto.GetComponentInChildren<TMP_Text>();
            return;
        }

        indicadorEstadoAuto = new GameObject("IndicadorEstado_Auto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform indicadorRT = indicadorEstadoAuto.GetComponent<RectTransform>();
        indicadorRT.SetParent(transform, false);
        indicadorRT.anchorMin = new Vector2(0f, 0f);
        indicadorRT.anchorMax = new Vector2(1f, 0f);
        indicadorRT.pivot = new Vector2(0.5f, 0f);
        indicadorRT.sizeDelta = new Vector2(0f, alturaIndicador);
        indicadorRT.anchoredPosition = Vector2.zero;

        fundoIndicadorAuto = indicadorEstadoAuto.GetComponent<Image>();
        fundoIndicadorAuto.color = new Color(0f, 0f, 0f, 0.78f);
        fundoIndicadorAuto.raycastTarget = false;

        GameObject textoObj = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textoRT = textoObj.GetComponent<RectTransform>();
        textoRT.SetParent(indicadorRT, false);
        textoRT.anchorMin = Vector2.zero;
        textoRT.anchorMax = Vector2.one;
        textoRT.offsetMin = Vector2.zero;
        textoRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textoObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = tamanhoFonteIndicador;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 3f;
        tmp.fontSizeMax = tamanhoFonteIndicador;
        tmp.enableWordWrapping = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        textoIndicadorAuto = tmp;
    }

    private void AtualizarVisual()
    {
        bool selecionada = estadoAtual != EstadoSelecao.Nenhuma;

        if (marcadorSelecao != null)
        {
            marcadorSelecao.SetActive(selecionada);
            Image marcadorImage = marcadorSelecao.GetComponent<Image>();
            if (marcadorImage != null)
            {
                marcadorImage.color = estadoAtual == EstadoSelecao.Reserva
                    ? new Color(1f, 0.55f, 0.12f, 0.82f)
                    : new Color(0.18f, 0.68f, 1f, 0.82f);
            }
        }

        if (indicadorEstadoAuto != null)
            indicadorEstadoAuto.SetActive(selecionada);

        if (textoIndicadorAuto != null)
        {
            if (estadoAtual == EstadoSelecao.DeckPrincipal)
                textoIndicadorAuto.text = "DECK";
            else if (estadoAtual == EstadoSelecao.Reserva)
                textoIndicadorAuto.text = "RESERVA";
            else
                textoIndicadorAuto.text = "";
        }

        if (fundoIndicadorAuto != null)
        {
            fundoIndicadorAuto.color = estadoAtual == EstadoSelecao.Reserva
                ? new Color(0.75f, 0.3f, 0.04f, 0.92f)
                : new Color(0.03f, 0.4f, 0.72f, 0.92f);
        }
    }
}
