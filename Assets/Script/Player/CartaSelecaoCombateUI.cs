using UnityEngine;
using UnityEngine.UI;

public class CartaSelecaoCombateUI : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private Image imagemCarta;
    [SerializeField] private GameObject marcadorSelecao;
    [SerializeField] private Button botao;

    private Carta carta;
    private PreparacaoCombatePlayer preparacaoCombate;
    private bool selecionada = false;

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

        selecionada = false;
        AtualizarVisual();
    }

    public void Configurar(Carta cartaRecebida, PreparacaoCombatePlayer preparacao)
    {
        carta = cartaRecebida;
        preparacaoCombate = preparacao;

        selecionada = false;
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

    public void DefinirSelecionado(bool valor)
    {
        selecionada = valor;
        AtualizarVisual();
    }

    public bool EstaSelecionada()
    {
        return selecionada;
    }

    public Carta ObterCarta()
    {
        return carta;
    }

    private void AtualizarVisual()
    {
        if (marcadorSelecao != null)
            marcadorSelecao.SetActive(selecionada);
    }
}