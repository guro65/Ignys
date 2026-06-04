using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class UICombateCarta : MonoBehaviour
{
    [Header("Referência do combate")]
    public CombateAmigavel combateAmigavel;

    [Header("Painel de ações")]
    public GameObject painelAcoesCarta;

    [Header("Painel de lista de habilidades")]
    public GameObject painelListaHabilidades;
    public Button[] botoesHabilidades = new Button[4];
    public TMP_Text[] textosBotoesHabilidades = new TMP_Text[4];

    [Header("Painel de confirmação de habilidade")]
    public GameObject painelConfirmacaoHabilidade;
    public TMP_Text textoNomeHabilidade;
    public TMP_Text textoDescricaoHabilidade;
    public TMP_Text textoCustoHabilidade;
    public Button botaoConfirmarHabilidade;
    public Button botaoCancelarHabilidade;

    [Header("Botão opcional de habilidade em conjunto")]
    public Button botaoHabilidadeConjunto;
    public TMP_Text textoBotaoHabilidadeConjunto;

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

        ConfigurarBotoesHabilidades();
        ConfigurarBotoesConfirmacaoHabilidade();
        ConfigurarBotaoHabilidadeConjunto();

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);

        if (painelInfoHover != null)
            painelInfoHover.SetActive(false);
    }

    private void ConfigurarBotoesHabilidades()
    {
        if (botoesHabilidades == null)
            return;

        for (int i = 0; i < botoesHabilidades.Length; i++)
        {
            int indice = i;

            if (botoesHabilidades[i] == null)
                continue;

            botoesHabilidades[i].onClick.RemoveAllListeners();
            botoesHabilidades[i].onClick.AddListener(() => BotaoSelecionarHabilidade(indice));
        }
    }

    private void ConfigurarBotoesConfirmacaoHabilidade()
    {
        if (botaoConfirmarHabilidade != null)
        {
            botaoConfirmarHabilidade.onClick.RemoveAllListeners();
            botaoConfirmarHabilidade.onClick.AddListener(BotaoConfirmarUsoHabilidade);
        }

        if (botaoCancelarHabilidade != null)
        {
            botaoCancelarHabilidade.onClick.RemoveAllListeners();
            botaoCancelarHabilidade.onClick.AddListener(BotaoCancelarUsoHabilidade);
        }
    }

    private void ConfigurarBotaoHabilidadeConjunto()
    {
        if (botaoHabilidadeConjunto != null)
        {
            botaoHabilidadeConjunto.onClick.RemoveAllListeners();
            botaoHabilidadeConjunto.onClick.AddListener(BotaoHabilidadeConjunto);
            botaoHabilidadeConjunto.gameObject.SetActive(false);
        }
    }

    private void AtualizarBotaoHabilidadeConjunto()
    {
        if (botaoHabilidadeConjunto == null)
            return;

        bool podeMostrar = combateAmigavel != null && cartaSelecionada != null && combateAmigavel.CartaPossuiHabilidadeConjuntoDisponivel(cartaSelecionada);
        botaoHabilidadeConjunto.gameObject.SetActive(podeMostrar);

        if (podeMostrar && textoBotaoHabilidadeConjunto != null)
            textoBotaoHabilidadeConjunto.text = combateAmigavel.ObterNomePrimeiraHabilidadeConjuntoDisponivel(cartaSelecionada);
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

        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);

        AtualizarBotaoHabilidadeConjunto();
    }

    public void FecharPainelCarta()
    {
        cartaSelecionada = null;

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);

        if (botaoHabilidadeConjunto != null)
            botaoHabilidadeConjunto.gameObject.SetActive(false);
    }

    public void AbrirPainelListaHabilidades(GameObject cartaObj)
    {
        if (cartaObj == null)
            return;

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null)
            return;

        cartaSelecionada = cartaObj;

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);

        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(true);

        AtualizarBotoesListaHabilidades(carta);
        AtualizarBotaoHabilidadeConjunto();
    }

    private void AtualizarBotoesListaHabilidades(Carta carta)
    {
        int limite = carta != null ? Mathf.Clamp(carta.quantidadeHabilidades, 0, 4) : 0;

        for (int i = 0; i < 4; i++)
        {
            HabilidadeCarta habilidade = carta != null ? carta.ObterHabilidade(i) : null;
            bool mostrarBotao = i < limite && habilidade != null;

            if (botoesHabilidades != null && i < botoesHabilidades.Length && botoesHabilidades[i] != null)
                botoesHabilidades[i].gameObject.SetActive(mostrarBotao);

            if (textosBotoesHabilidades != null && i < textosBotoesHabilidades.Length && textosBotoesHabilidades[i] != null)
            {
                if (mostrarBotao)
                    textosBotoesHabilidades[i].text = habilidade.nomeHabilidade;
                else
                    textosBotoesHabilidades[i].text = "";
            }
        }
    }

    public void AbrirPainelConfirmacaoHabilidade(HabilidadeCarta habilidade, string textoEstado)
    {
        if (habilidade == null)
            return;

        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(true);

        if (textoNomeHabilidade != null)
            textoNomeHabilidade.text = !string.IsNullOrEmpty(habilidade.nomeHabilidade) ? habilidade.nomeHabilidade : habilidade.nomeBotaoConjunto;

        if (textoDescricaoHabilidade != null)
            textoDescricaoHabilidade.text = habilidade.descricaoHabilidade;

        if (textoCustoHabilidade != null)
            textoCustoHabilidade.text = textoEstado;
    }

    public void FecharPainelConfirmacaoHabilidade()
    {
        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);
    }

    public void FecharTodosPaineisDeHabilidade()
    {
        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (botaoHabilidadeConjunto != null)
            botaoHabilidadeConjunto.gameObject.SetActive(false);
    }

    public void EntrarModoEscolhaAlvo(string mensagem = "Escolha uma carta inimiga no tabuleiro")
    {
        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);

        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(true);

        if (textoAlvoSelecionado != null)
            textoAlvoSelecionado.text = mensagem;
    }

    public void SairModoEscolhaAlvo()
    {
        if (painelEscolhaAlvo != null)
            painelEscolhaAlvo.SetActive(false);

        if (painelAcoesCarta != null)
            painelAcoesCarta.SetActive(false);

        if (painelListaHabilidades != null)
            painelListaHabilidades.SetActive(false);

        if (painelConfirmacaoHabilidade != null)
            painelConfirmacaoHabilidade.SetActive(false);

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

    public void BotaoHabilidadeConjunto()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.BotaoAbrirHabilidadeConjuntoCartaSelecionada();
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

        combateAmigavel.BotaoAbrirListaHabilidadesCartaSelecionada();
    }

    public void BotaoSelecionarHabilidade(int indice)
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.BotaoSelecionarHabilidadeCartaSelecionada(indice);
    }

    public void BotaoSelecionarHabilidade1()
    {
        BotaoSelecionarHabilidade(0);
    }

    public void BotaoSelecionarHabilidade2()
    {
        BotaoSelecionarHabilidade(1);
    }

    public void BotaoSelecionarHabilidade3()
    {
        BotaoSelecionarHabilidade(2);
    }

    public void BotaoSelecionarHabilidade4()
    {
        BotaoSelecionarHabilidade(3);
    }

    public void BotaoConfirmarUsoHabilidade()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.BotaoConfirmarUsoHabilidadeSelecionada();
    }

    public void BotaoCancelarUsoHabilidade()
    {
        if (combateAmigavel == null)
            return;

        combateAmigavel.BotaoCancelarConfirmacaoHabilidade();
    }

    public void BotaoHabilidadeAntigo()
    {
        BotaoHabilidade();
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
