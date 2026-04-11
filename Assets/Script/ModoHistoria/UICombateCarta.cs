using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class UICombateCarta : MonoBehaviour
{
    [Header("Referência do combate")]
    public CombateAmigavel combateAmigavel;

    [Header("Painel de ações")]
    public GameObject painelAcoesCarta;

    [Header("Painel de escolha de alvo")]
    public GameObject painelEscolhaAlvo;
    public TMP_Text textoAlvoSelecionado;

    [Header("Textos do hover")]
    public GameObject painelInfoHover;
    public TMP_Text textoNome;
    public TMP_Text textoDano;
    public TMP_Text textoVida;
    public TMP_Text textoDefesa;

    [Header("Carta atualmente selecionada")]
    public GameObject cartaSelecionada;

    private Camera cameraPrincipal;

    private void Start()
    {
        cameraPrincipal = Camera.main;

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);

        if (painelInfoHover != null)
            painelInfoHover.SetActive(false);
    }

    public void AbrirPainelCarta(GameObject carta)
    {
        if (carta == null)
            return;

        if (!carta.CompareTag("CartaPlayer"))
            return;

        cartaSelecionada = carta;

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(true);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);
    }

    public void FecharPainelCarta()
    {
        cartaSelecionada = null;

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);
    }

    public void EntrarModoEscolhaAlvo()
    {
        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(true);

        if (textoAlvoSelecionado != null)
            textoAlvoSelecionado.text = "Escolha uma carta inimiga no tabuleiro";
    }

    public void SairModoEscolhaAlvo()
    {
        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (textoAlvoSelecionado != null)
            textoAlvoSelecionado.text = "";
    }

    public void AtualizarTextoAlvoSelecionado(GameObject alvo)
    {
        if (textoAlvoSelecionado == null)
            return;

        Carta carta = alvo != null ? alvo.GetComponent<Carta>() : null;

        if (carta == null)
        {
            textoAlvoSelecionado.text = "Nenhum alvo";
            return;
        }

        textoAlvoSelecionado.text = $"Alvo: {carta.nome}";
    }

    public void BotaoFechar()
    {
        FecharPainelCarta();
    }

    public void BotaoVoltarDeck()
    {
        if (combateAmigavel == null || cartaSelecionada == null)
            return;

        combateAmigavel.VoltarCartaPlayerParaDeck(cartaSelecionada);
    }

    public void BotaoAtacar()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.BotaoAtacarCartaSelecionada();
    }

    public void BotaoHabilidade()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.BotaoHabilidadeCartaSelecionada();
    }

    public void BotaoPassarTurno()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.PassarTurno();
    }

    public void BotaoConfirmarAlvo()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.ConfirmarAlvoSelecionado();
    }

    public void BotaoCancelarAlvo()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.CancelarEscolhaAlvo();
    }

    public void BotaoResgatarCarta()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.BotaoResgatarCartaPlayer();
    }

    public void AtualizarHoverCartaTabuleiro()
    {
        if (cameraPrincipal == null || Mouse.current == null)
            return;

        Vector2 posicaoMouse = cameraPrincipal.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(posicaoMouse, Vector2.zero);

        if (hit.collider == null)
        {
            OcultarInfoHover();
            return;
        }

        GameObject objeto = hit.collider.gameObject;

        if (!objeto.CompareTag("CartaPlayer") && !objeto.CompareTag("CartaInimigo"))
        {
            OcultarInfoHover();
            return;
        }

        if (!EstaNoTabuleiro(objeto.transform))
        {
            OcultarInfoHover();
            return;
        }

        Carta carta = objeto.GetComponent<Carta>();
        if (carta == null)
        {
            OcultarInfoHover();
            return;
        }

        MostrarInfoHover(carta);
    }

    private void MostrarInfoHover(Carta carta)
    {
        if (painelInfoHover != null)
            painelInfoHover.SetActive(true);

        if (textoNome != null)
            textoNome.text = carta.nome;

        if (textoDano != null)
            textoDano.text = "Dano: " + carta.dano;

        if (textoVida != null)
            textoVida.text = "Vida: " + carta.vida;

        if (textoDefesa != null)
            textoDefesa.text = "Defesa: " + carta.defesa;
    }

    private void OcultarInfoHover()
    {
        if (painelInfoHover != null)
            painelInfoHover.SetActive(false);
    }

    private bool EstaNoTabuleiro(Transform cartaTransform)
    {
        if (cartaTransform == null || cartaTransform.parent == null)
            return false;

        return cartaTransform.parent.CompareTag("SlotTabuleiroPlayer") ||
               cartaTransform.parent.CompareTag("SlotTabuleiroInimigo");
    }
}