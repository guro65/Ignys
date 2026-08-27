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

    [Header("Botões de ação - opcionais / encontrados automaticamente")]
    public Button botaoAtacarAcao;
    public Button botaoHabilidadeAcao;
    public Button botaoVoltarDeckAcao;
    public Button botaoReservaAcao;

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

    [Header("Textos do hover antigo - mantidos como fallback")]
    public GameObject painelInfoHover;
    public TMP_Text textoNome;
    public TMP_Text textoDano;
    public TMP_Text textoVida;
    public TMP_Text textoDefesa;

    [Header("Carta atualmente selecionada")]
    public GameObject cartaSelecionada;

    private Camera cameraPrincipal;
    private FeedbackCartasCombateUI feedbackCartasUI;

    private void Start()
    {
        cameraPrincipal = Camera.main;
        if (combateAmigavel == null)
            combateAmigavel = FindObjectOfType<CombateAmigavel>();

        feedbackCartasUI = FeedbackCartasCombateUI.ObterOuCriar();
        if (feedbackCartasUI != null && combateAmigavel != null)
            feedbackCartasUI.Configurar(combateAmigavel);

        ConfigurarBotoesHabilidades();
        ConfigurarBotoesConfirmacaoHabilidade();
        ConfigurarBotaoHabilidadeConjunto();
        DescobrirBotoesDeAcaoAutomaticamente();

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

    private void LateUpdate()
    {
        AtualizarInteratividadeBotoes();
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

    private void DescobrirBotoesDeAcaoAutomaticamente()
    {
        Button[] botoes = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < botoes.Length; i++)
        {
            Button btn = botoes[i];
            if (btn == null)
                continue;

            string texto = btn.name.ToLowerInvariant();
            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                texto += " " + tmp.text.ToLowerInvariant();

            bool dentroPainelAcao = painelAcoesCarta != null && btn.transform.IsChildOf(painelAcoesCarta.transform);

            if (botaoAtacarAcao == null && dentroPainelAcao && texto.Contains("atacar"))
                botaoAtacarAcao = btn;
            else if (botaoHabilidadeAcao == null && dentroPainelAcao && texto.Contains("habil"))
                botaoHabilidadeAcao = btn;
            else if (botaoVoltarDeckAcao == null && dentroPainelAcao && (texto.Contains("voltar") || texto.Contains("deck")))
                botaoVoltarDeckAcao = btn;
            else if (botaoReservaAcao == null && (texto.Contains("reserva") || texto.Contains("resgatar")))
                botaoReservaAcao = btn;
        }
    }

    private void AtualizarInteratividadeBotoes()
    {
        if (combateAmigavel == null)
            return;

        if (botaoAtacarAcao != null)
            botaoAtacarAcao.interactable = cartaSelecionada != null && combateAmigavel.PodeAtacarCartaParaUI(cartaSelecionada);
        if (botaoHabilidadeAcao != null)
            botaoHabilidadeAcao.interactable = cartaSelecionada != null && combateAmigavel.PodeUsarHabilidadeCartaParaUI(cartaSelecionada);
        if (botaoVoltarDeckAcao != null)
            botaoVoltarDeckAcao.interactable = cartaSelecionada != null && combateAmigavel.PodeVoltarCartaParaDeckParaUI(cartaSelecionada);
        if (botaoReservaAcao != null)
            botaoReservaAcao.interactable = combateAmigavel.PodeUsarReservaParaUI();

        if (painelListaHabilidades != null && painelListaHabilidades.activeSelf && cartaSelecionada != null && botoesHabilidades != null)
        {
            for (int i = 0; i < botoesHabilidades.Length; i++)
            {
                if (botoesHabilidades[i] != null && botoesHabilidades[i].gameObject.activeSelf)
                    botoesHabilidades[i].interactable = combateAmigavel.PodeUsarHabilidadeIndiceParaUI(cartaSelecionada, i);
            }
        }
    }

    private void AtualizarBotaoHabilidadeConjunto()
    {
        if (botaoHabilidadeConjunto == null)
            return;

        bool podeMostrar = combateAmigavel != null && cartaSelecionada != null && combateAmigavel.CartaPossuiHabilidadeConjuntoDisponivel(cartaSelecionada);
        botaoHabilidadeConjunto.gameObject.SetActive(podeMostrar);
        botaoHabilidadeConjunto.interactable = podeMostrar && combateAmigavel.PodeUsarHabilidadeCartaParaUI(cartaSelecionada);

        if (podeMostrar && textoBotaoHabilidadeConjunto != null)
            textoBotaoHabilidadeConjunto.text = combateAmigavel.ObterNomePrimeiraHabilidadeConjuntoDisponivel(cartaSelecionada);
    }

    public void AbrirPainelCarta(GameObject carta)
    {
        if (carta == null || !carta.CompareTag("CartaPlayer"))
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
        AtualizarInteratividadeBotoes();
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
            {
                botoesHabilidades[i].gameObject.SetActive(mostrarBotao);
                botoesHabilidades[i].interactable = mostrarBotao && combateAmigavel != null && combateAmigavel.PodeUsarHabilidadeIndiceParaUI(cartaSelecionada, i);
            }

            if (textosBotoesHabilidades != null && i < textosBotoesHabilidades.Length && textosBotoesHabilidades[i] != null)
                textosBotoesHabilidades[i].text = mostrarBotao ? habilidade.nomeHabilidade : "";
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
        {
            textoAlvoSelecionado.text = mensagem;
            textoAlvoSelecionado.enableAutoSizing = true;
            textoAlvoSelecionado.fontSizeMin = 10f;
        }
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
        textoAlvoSelecionado.text = carta == null ? "Nenhum alvo" : $"Alvo: {carta.nome}";
    }

    public void BotaoHabilidadeConjunto()
    {
        if (combateAmigavel != null)
            combateAmigavel.BotaoAbrirHabilidadeConjuntoCartaSelecionada();
    }

    public void BotaoFechar() => FecharPainelCarta();

    public void BotaoVoltarDeck()
    {
        if (combateAmigavel != null && cartaSelecionada != null)
            combateAmigavel.VoltarCartaPlayerParaDeck(cartaSelecionada);
    }

    public void BotaoAtacar()
    {
        if (combateAmigavel != null)
            combateAmigavel.BotaoAtacarCartaSelecionada();
    }

    public void BotaoHabilidade()
    {
        if (combateAmigavel != null)
            combateAmigavel.BotaoAbrirListaHabilidadesCartaSelecionada();
    }

    public void BotaoSelecionarHabilidade(int indice)
    {
        if (combateAmigavel != null)
            combateAmigavel.BotaoSelecionarHabilidadeCartaSelecionada(indice);
    }

    public void BotaoSelecionarHabilidade1() => BotaoSelecionarHabilidade(0);
    public void BotaoSelecionarHabilidade2() => BotaoSelecionarHabilidade(1);
    public void BotaoSelecionarHabilidade3() => BotaoSelecionarHabilidade(2);
    public void BotaoSelecionarHabilidade4() => BotaoSelecionarHabilidade(3);

    public void BotaoConfirmarUsoHabilidade()
    {
        if (combateAmigavel != null)
            combateAmigavel.BotaoConfirmarUsoHabilidadeSelecionada();
    }

    public void BotaoCancelarUsoHabilidade()
    {
        if (combateAmigavel != null)
            combateAmigavel.BotaoCancelarConfirmacaoHabilidade();
    }

    public void BotaoHabilidadeAntigo() => BotaoHabilidade();

    public void BotaoPassarTurno()
    {
        if (combateAmigavel != null)
            combateAmigavel.PassarTurno();
    }

    public void BotaoConfirmarAlvo()
    {
        if (combateAmigavel != null)
            combateAmigavel.ConfirmarAlvoSelecionado();
    }

    public void BotaoCancelarAlvo()
    {
        if (combateAmigavel != null)
            combateAmigavel.CancelarEscolhaAlvo();
    }

    public void BotaoResgatarCarta()
    {
        if (combateAmigavel != null)
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
        if ((!objeto.CompareTag("CartaPlayer") && !objeto.CompareTag("CartaInimigo")) || !EstaNoTabuleiro(objeto.transform))
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

        if (feedbackCartasUI == null)
            feedbackCartasUI = FeedbackCartasCombateUI.ObterOuCriar();

        if (feedbackCartasUI != null)
        {
            if (painelInfoHover != null)
                painelInfoHover.SetActive(false);
            feedbackCartasUI.MostrarHover(objeto);
            return;
        }

        MostrarInfoHover(carta);
    }

    private void MostrarInfoHover(Carta carta)
    {
        if (painelInfoHover != null)
            painelInfoHover.SetActive(true);
        if (textoNome != null)
            textoNome.text = $"{carta.nome} • {carta.raridade}";
        if (textoDano != null)
            textoDano.text = "ATQ " + carta.dano;
        if (textoVida != null)
            textoVida.text = "VIDA " + carta.vida;
        if (textoDefesa != null)
            textoDefesa.text = "DEF " + carta.defesa;
    }

    private void OcultarInfoHover()
    {
        if (painelInfoHover != null)
            painelInfoHover.SetActive(false);
        if (feedbackCartasUI != null)
            feedbackCartasUI.OcultarHover();
    }

    private bool EstaNoTabuleiro(Transform cartaTransform)
    {
        if (cartaTransform == null || cartaTransform.parent == null)
            return false;
        return cartaTransform.parent.CompareTag("SlotTabuleiroPlayer") || cartaTransform.parent.CompareTag("SlotTabuleiroInimigo");
    }
}
