using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class CombateAmigavel : MonoBehaviour
{
    [Header("Referências")]
    public ControladorInimigoNaCena controladorInimigoNaCena;
    public UICombateCarta uiCombateCarta;
    public PreparacaoCombatePlayer preparacaoCombatePlayer;

    [Header("Vida dos duelistas - Combate Amigável")]
    [Min(1)] public int vidaMaximaPlayer = 30;
    public int vidaAtualPlayer = 30;
    public int vidaMaximaInimigo = 30;
    public int vidaAtualInimigo = 30;

    [Header("Reservas desta batalha")]
    [Tooltip("Preenchida automaticamente após a preparação do player.")]
    public List<Carta> reservaPlayer = new List<Carta>();

    [Tooltip("Preenchida automaticamente a partir do InimigoBase selecionado.")]
    public List<Carta> reservaInimigo = new List<Carta>();

    [Header("UI automática - opcional")]
    [Tooltip("Pode ficar vazio. O sistema cria sozinho em tempo de execução.")]
    public HUDDuelistasCombateUI hudDuelistasUI;

    [Tooltip("Pode ficar vazio. O sistema cria sozinho em tempo de execução.")]
    public ResultadoCombateUI resultadoCombateUI;

    [Tooltip("Pode ficar vazio. Cria e anima automaticamente a entrada dos dois decks após a preparação.")]
    public AnimadorEntradaDeckCombate animadorEntradaDeck;

    [Tooltip("Pode ficar vazio. Gera notificações compactas, banners de turno, apresentação VS e confirmações.")]
    public NotificacaoJogoUI notificacaoJogoUI;

    [Tooltip("Pode ficar vazio. Gera preview de dano, status, destaques, hover e feedback nas cartas.")]
    public FeedbackCartasCombateUI feedbackCartasUI;

    [Tooltip("Opcional. Usado apenas para registrar as cartas inimigas instanciadas pelo animador.")]
    public OrganizadorDeckInimigo organizadorDeckInimigo;

    [Tooltip("Somente usado se o combate não tiver sido iniciado pelo SelecionadorDeInimigo.")]
    public string cenaRetornoFallback = "";

    [Header("Estado do combate")]
    public bool combateIniciado = false;
    public bool combateFinalizado = false;

    [Header("Tags do Player")]
    public string tagCartaPlayer = "CartaPlayer";
    public string tagSlotDeckPlayer = "SlotDeckPlayer";
    public string tagSlotTabuleiroPlayer = "SlotTabuleiroPlayer";

    [Header("Tags do Inimigo")]
    public string tagCartaInimigo = "CartaInimigo";
    public string tagSlotDeckInimigo = "SlotDackInimigo";
    public string tagSlotTabuleiroInimigo = "SlotTabuleiroInimigo";

    [Header("Tag do Cemitério")]
    public string tagSlotCemiterio = "SlotCemiterio";

    [Header("Estado das cartas do Player")]
    public List<GameObject> cartasPlayerNoDeck = new List<GameObject>();
    public List<GameObject> cartasPlayerNoTabuleiro = new List<GameObject>();

    [Header("Estado das cartas do Inimigo")]
    public List<GameObject> cartasInimigoNoDeck = new List<GameObject>();
    public List<GameObject> cartasInimigoNoTabuleiro = new List<GameObject>();

    [Header("Controle de turno")]
    public bool turnoDoPlayer = true;
    public bool turnoDoInimigo = false;
    public bool inimigoExecutandoTurno = false;

    [Header("Energia do Player")]
    public int energiaMaximaPlayer = 5;
    public int energiaAtualPlayer = 5;

    [Range(0f, 100f)]
    public float chanceRecuperarEnergiaPlayer = 50f;

    [Header("Energia do Inimigo")]
    public int energiaMaximaInimigo = 5;
    public int energiaAtualInimigo = 5;

    [Range(0f, 100f)]
    public float chanceRecuperarEnergiaInimigo = 50f;

    [Header("Pontos de Resgate - compatibilidade")]
    [Tooltip("Mantidos para outros sistemas antigos. No CombateAmigavel atual, a reserva é acessada gastando uma ação, sem precisar de ponto de resgate.")]
    public int pontosResgatarPlayer = 0;
    public int pontosResgatarInimigo = 0;

    [Header("Sistema de ações por turno")]
    [Min(1)] public int maximoAcoesPorTurnoPlayer = 3;
    [Min(1)] public int maximoAcoesPorTurnoInimigo = 3;
    public int acoesRestantesPlayer = 3;
    public int acoesRestantesInimigo = 3;
    [Min(0)] public int maximoHabilidadesInimigoPorTurno = 1;

    [Tooltip("Ligado: uma carta colocada no tabuleiro neste turno só poderá atacar ou usar habilidade no próximo turno daquele duelista.")]
    public bool cartaRecemColocadaPrecisaEsperar = true;

    [Header("Contadores de turno para recuperar energia")]
    public int contadorTurnosPlayer = 0;
    public int contadorTurnosInimigo = 0;

    [Header("UI de Turno")]
    public TMP_Text textoTurno;

    [Header("UI opcional de recursos")]
    public TMP_Text textoEnergiaPlayer;
    public TMP_Text textoEnergiaInimigo;
    public TMP_Text textoResgatarPlayer;
    public TMP_Text textoResgatarInimigo;

    [Header("Ajustes da IA")]
    public float tempoEntreAcoesInimigo = 0.5f;

    [Header("Efeito visual de ataque padrao")]
    [Tooltip("Frames usados quando a carta atacante nao possui um efeito personalizado configurado no componente Carta.")]
    public Sprite[] framesEfeitoAtaquePadrao = new Sprite[0];

    [Tooltip("Tempo em segundos que cada frame do efeito padrao fica visivel.")]
    [Min(0.01f)] public float tempoEntreFramesEfeitoAtaquePadrao = 0.08f;

    [Tooltip("Deslocamento do efeito padrao em relacao ao centro da carta atingida.")]
    public Vector2 deslocamentoEfeitoAtaquePadrao = Vector2.zero;

    [Tooltip("Escala visual do efeito padrao.")]
    public Vector2 escalaEfeitoAtaquePadrao = Vector2.one;

    [Tooltip("Rotacao em graus do efeito padrao.")]
    public float rotacaoEfeitoAtaquePadrao = 0f;

    [Tooltip("Quantas ordens acima da carta atingida o efeito sera desenhado.")]
    public int ordemRenderizacaoAcimaDaCarta = 20;

    private GameObject cartaSendoArrastada;
    private Vector3 posicaoOriginalCarta;
    private Transform parentOriginalCarta;
    private Vector3 escalaOriginalCarta;
    private Camera cameraPrincipal;
    private bool estaArrastandoCarta = false;

    private readonly HashSet<GameObject> cartasPlayerQueAtacaramNesteTurno = new HashSet<GameObject>();
    private readonly HashSet<GameObject> cartasInimigoQueAtacaramNesteTurno = new HashSet<GameObject>();
    private readonly HashSet<string> cartasPlayerConhecidasPeloInimigo = new HashSet<string>();
    private readonly List<AtaquePendente> ataquesPendentesDoPlayer = new List<AtaquePendente>();

    private bool modoEscolhaAlvo = false;
    private GameObject cartaPlayerSelecionadaParaAtacar;
    private GameObject cartaInimigoAlvoSelecionada;

    private enum TipoAcaoComAlvo
    {
        Ataque,
        Habilidade
    }

    private TipoAcaoComAlvo acaoAtualComAlvo = TipoAcaoComAlvo.Ataque;
    private GameObject cartaPlayerSelecionadaParaHabilidade;
    private GameObject alvoSelecionadoParaHabilidade;
    private HabilidadeCarta habilidadePlayerSelecionada;
    private int indiceHabilidadePlayerSelecionada = -1;

    private readonly Dictionary<GameObject, Dictionary<int, int>> cooldownsHabilidades = new Dictionary<GameObject, Dictionary<int, int>>();
    private readonly Dictionary<GameObject, int> danoTotalCausadoPorCarta = new Dictionary<GameObject, int>();

    private readonly HashSet<GameObject> cartasPlayerQueUsaramHabilidadeNesteTurno = new HashSet<GameObject>();
    private readonly HashSet<GameObject> cartasInimigoQueUsaramHabilidadeNesteTurno = new HashSet<GameObject>();
    private bool ignorarConfirmacaoPassarTurno = false;
    private readonly HashSet<GameObject> cartasPlayerColocadasNesteTurno = new HashSet<GameObject>();
    private readonly HashSet<GameObject> cartasInimigoColocadasNesteTurno = new HashSet<GameObject>();
    private readonly List<BuffTemporario> buffsTemporariosAtivos = new List<BuffTemporario>();

    private class AtaquePendente
    {
        public GameObject atacante;
        public GameObject alvo;
        public bool ataqueDiretoAoDuelista;
    }

    private class BuffTemporario
    {
        public GameObject cartaObj;
        public Carta carta;
        public HabilidadeCarta.TipoBuff tipoBuff;
        public int valor;
        public int turnosRestantes;
        public bool pertenceAoPlayer;
        public bool ignorarPrimeiraChecagem;
    }

    private void Start()
    {
        cameraPrincipal = Camera.main;

        if (preparacaoCombatePlayer == null)
            preparacaoCombatePlayer = FindObjectOfType<PreparacaoCombatePlayer>();

        if (controladorInimigoNaCena == null)
            controladorInimigoNaCena = FindObjectOfType<ControladorInimigoNaCena>();

        if (organizadorDeckInimigo == null)
            organizadorDeckInimigo = FindObjectOfType<OrganizadorDeckInimigo>();

        StartCoroutine(AguardarPreparacaoEIniciarCombate());
    }

    private IEnumerator AguardarPreparacaoEIniciarCombate()
    {
        // A preparação usa Time.timeScale = 0. Por isso aguardamos por frames, não por segundos.
        if (preparacaoCombatePlayer != null)
        {
            while (!preparacaoCombatePlayer.preparacaoConcluida)
                yield return null;
        }
        else
        {
            // Mantém compatibilidade com cenas antigas que não possuem a tela de preparação.
            yield return null;
        }

        yield return InicializarCombate();
    }

    private IEnumerator InicializarCombate()
    {
        reservaPlayer.Clear();
        reservaInimigo.Clear();

        if (preparacaoCombatePlayer != null)
        {
            List<Carta> copiaReserva = preparacaoCombatePlayer.ObterReservaSelecionadaCopia();
            for (int i = 0; i < copiaReserva.Count; i++)
            {
                if (copiaReserva[i] != null)
                    reservaPlayer.Add(copiaReserva[i]);
            }
        }

        if (controladorInimigoNaCena != null)
        {
            for (int i = 0; i < controladorInimigoNaCena.reservaInimigo.Count; i++)
            {
                if (controladorInimigoNaCena.reservaInimigo[i] != null)
                    reservaInimigo.Add(controladorInimigoNaCena.reservaInimigo[i]);
            }
        }

        vidaAtualPlayer = Mathf.Max(1, vidaMaximaPlayer);

        if (controladorInimigoNaCena != null && controladorInimigoNaCena.inimigoAtual != null)
            vidaMaximaInimigo = Mathf.Max(1, controladorInimigoNaCena.inimigoAtual.vidaMaximaDuelista);
        else
            vidaMaximaInimigo = Mathf.Max(1, vidaMaximaInimigo);

        vidaAtualInimigo = vidaMaximaInimigo;

        hudDuelistasUI = hudDuelistasUI != null ? hudDuelistasUI : HUDDuelistasCombateUI.ObterOuCriar();
        resultadoCombateUI = resultadoCombateUI != null ? resultadoCombateUI : ResultadoCombateUI.ObterOuCriar();
        animadorEntradaDeck = animadorEntradaDeck != null ? animadorEntradaDeck : AnimadorEntradaDeckCombate.ObterOuCriar();
        notificacaoJogoUI = notificacaoJogoUI != null ? notificacaoJogoUI : NotificacaoJogoUI.ObterOuCriar();
        feedbackCartasUI = feedbackCartasUI != null ? feedbackCartasUI : FeedbackCartasCombateUI.ObterOuCriar();
        if (feedbackCartasUI != null)
            feedbackCartasUI.Configurar(this);

        combateIniciado = false;
        combateFinalizado = false;
        acoesRestantesPlayer = Mathf.Max(1, maximoAcoesPorTurnoPlayer);
        acoesRestantesInimigo = 0;

        AtualizarListasDeCartas();
        AtualizarHUDDuelistas();

        string nomeInimigoIntro = "OPONENTE";
        if (controladorInimigoNaCena != null && controladorInimigoNaCena.inimigoAtual != null &&
            !string.IsNullOrWhiteSpace(controladorInimigoNaCena.inimigoAtual.nomeInimigo))
            nomeInimigoIntro = controladorInimigoNaCena.inimigoAtual.nomeInimigo;

        if (notificacaoJogoUI != null)
            yield return notificacaoJogoUI.MostrarApresentacaoDuelo("PLAYER", nomeInimigoIntro);

        Notificar("Preparando os decks...", 0.9f);

        List<Carta> deckPlayerSelecionado = preparacaoCombatePlayer != null
            ? preparacaoCombatePlayer.ObterDeckPrincipalSelecionadoCopia()
            : new List<Carta>();

        List<Carta> deckInimigoSelecionado = controladorInimigoNaCena != null && controladorInimigoNaCena.deckInimigo != null
            ? new List<Carta>(controladorInimigoNaCena.deckInimigo)
            : new List<Carta>();

        if (animadorEntradaDeck != null)
        {
            yield return animadorEntradaDeck.ColocarDecksComAnimacao(
                deckPlayerSelecionado,
                deckInimigoSelecionado,
                tagSlotDeckPlayer,
                tagSlotDeckInimigo,
                tagCartaPlayer,
                tagCartaInimigo,
                preparacaoCombatePlayer,
                organizadorDeckInimigo);
        }
        else
        {
            // Fallback apenas se o sistema automático não puder ser criado.
            if (preparacaoCombatePlayer != null)
                preparacaoCombatePlayer.PosicionarCartasSelecionadasNoDeck();
            if (organizadorDeckInimigo != null)
                organizadorDeckInimigo.PosicionarCartasDoDeckInimigo();
        }

        AtualizarListasDeCartas();
        RegistrarCartasVisiveisDoPlayer();

        combateIniciado = true;
        IniciarTurnoDoPlayer();
        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();
        AtualizarHUDDuelistas();
        VerificarCondicoesDeFimDeCombate();
    }

    private void Update()
    {
        if (!combateIniciado || combateFinalizado)
            return;

        AtualizarListasDeCartas();
        RegistrarCartasVisiveisDoPlayer();
        AtualizarHUDDuelistas();
        AtualizarInput();
    }

    private void AtualizarInput()
    {
        if (cameraPrincipal == null || Mouse.current == null)
            return;

        if (uiCombateCarta != null)
            uiCombateCarta.AtualizarHoverCartaTabuleiro();

        if (modoEscolhaAlvo && turnoDoPlayer)
        {
            AtualizarEscolhaDeAlvoDoPlayer();
            return;
        }

        if (!turnoDoPlayer)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TentarComecarInteracaoPlayer();
        }

        if (Mouse.current.leftButton.isPressed && cartaSendoArrastada != null && estaArrastandoCarta)
        {
            ArrastarCartaPlayer();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && cartaSendoArrastada != null && estaArrastandoCarta)
        {
            SoltarCartaPlayer();
        }
    }

    public void AtualizarListasDeCartas()
    {
        cartasPlayerNoDeck.Clear();
        cartasPlayerNoTabuleiro.Clear();
        cartasInimigoNoDeck.Clear();
        cartasInimigoNoTabuleiro.Clear();

        GameObject[] cartasPlayer = GameObject.FindGameObjectsWithTag(tagCartaPlayer);
        for (int i = 0; i < cartasPlayer.Length; i++)
        {
            if (cartasPlayer[i] == null)
                continue;

            if (EstaEmSlotComTag(cartasPlayer[i].transform, tagSlotTabuleiroPlayer))
                cartasPlayerNoTabuleiro.Add(cartasPlayer[i]);
            else if (!EstaEmSlotComTag(cartasPlayer[i].transform, tagSlotCemiterio))
                cartasPlayerNoDeck.Add(cartasPlayer[i]);
        }

        GameObject[] cartasInimigo = GameObject.FindGameObjectsWithTag(tagCartaInimigo);
        for (int i = 0; i < cartasInimigo.Length; i++)
        {
            if (cartasInimigo[i] == null)
                continue;

            if (EstaEmSlotComTag(cartasInimigo[i].transform, tagSlotTabuleiroInimigo))
                cartasInimigoNoTabuleiro.Add(cartasInimigo[i]);
            else if (!EstaEmSlotComTag(cartasInimigo[i].transform, tagSlotCemiterio))
                cartasInimigoNoDeck.Add(cartasInimigo[i]);
        }
    }

    private void RegistrarCartasVisiveisDoPlayer()
    {
        for (int i = 0; i < cartasPlayerNoTabuleiro.Count; i++)
        {
            Carta carta = cartasPlayerNoTabuleiro[i] != null ? cartasPlayerNoTabuleiro[i].GetComponent<Carta>() : null;
            if (carta != null)
                cartasPlayerConhecidasPeloInimigo.Add(carta.nome);
        }
    }

    private void TentarComecarInteracaoPlayer()
    {
        Vector2 posicaoMouse = cameraPrincipal.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(posicaoMouse, Vector2.zero);

        if (hit.collider == null)
            return;

        GameObject objetoClicado = hit.collider.gameObject;

        if (!objetoClicado.CompareTag(tagCartaPlayer))
            return;

        if (CartaEstaParalisada(objetoClicado))
        {
            Debug.Log("Essa carta está com Sobrecarga e não pode ser usada agora.");
            Notificar("SOBRECARGA • carta bloqueada.");
            return;
        }

        if (EstaEmSlotComTag(objetoClicado.transform, tagSlotTabuleiroPlayer))
        {
            if (uiCombateCarta != null)
                uiCombateCarta.AbrirPainelCarta(objetoClicado);

            cartaSendoArrastada = null;
            estaArrastandoCarta = false;
            return;
        }

        cartaSendoArrastada = objetoClicado;
        posicaoOriginalCarta = cartaSendoArrastada.transform.position;
        parentOriginalCarta = cartaSendoArrastada.transform.parent;
        escalaOriginalCarta = cartaSendoArrastada.transform.localScale;
        estaArrastandoCarta = true;
    }

    private void ArrastarCartaPlayer()
    {
        Vector3 posicaoMouse = cameraPrincipal.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        posicaoMouse.z = 0f;
        cartaSendoArrastada.transform.position = posicaoMouse;
    }

    private void SoltarCartaPlayer()
    {
        Collider2D slotCemiterio = EncontrarSlotMaisProximo(cartaSendoArrastada.transform.position, tagSlotCemiterio);

        if (slotCemiterio != null)
        {
            // O cemitério é exclusivo para cartas derrotadas/sacrificadas pelo sistema.
            // Arrastar manualmente uma carta até ele nunca é permitido.
            VoltarCartaParaOrigem(cartaSendoArrastada, parentOriginalCarta, posicaoOriginalCarta, escalaOriginalCarta);

            Notificar("CEMITÉRIO • apenas cartas derrotadas podem entrar.", 1.1f);

            cartaSendoArrastada = null;
            estaArrastandoCarta = false;
            AtualizarListasDeCartas();
            return;
        }

        Collider2D slotEncontrado = EncontrarSlotMaisProximo(cartaSendoArrastada.transform.position, tagSlotTabuleiroPlayer);

        if (slotEncontrado != null)
        {
            Transform slotTransform = slotEncontrado.transform;

            if (SlotJaPossuiCarta(slotTransform))
            {
                VoltarCartaParaOrigem(cartaSendoArrastada, parentOriginalCarta, posicaoOriginalCarta, escalaOriginalCarta);
            }
            else if (!PlayerPossuiAcaoDisponivel())
            {
                VoltarCartaParaOrigem(cartaSendoArrastada, parentOriginalCarta, posicaoOriginalCarta, escalaOriginalCarta);
                MostrarSemAcoesPlayer();
            }
            else
            {
                cartaSendoArrastada.transform.SetParent(slotTransform);
                cartaSendoArrastada.transform.position = slotTransform.position;
                cartaSendoArrastada.transform.localScale = escalaOriginalCarta;

                ConsumirAcaoPlayer("colocar uma carta no tabuleiro");
                cartasPlayerColocadasNesteTurno.Add(cartaSendoArrastada);

                Carta carta = cartaSendoArrastada.GetComponent<Carta>();
                if (carta != null)
                    cartasPlayerConhecidasPeloInimigo.Add(carta.nome);
            }
        }
        else
        {
            VoltarCartaParaOrigem(cartaSendoArrastada, parentOriginalCarta, posicaoOriginalCarta, escalaOriginalCarta);
        }

        cartaSendoArrastada = null;
        estaArrastandoCarta = false;
        AtualizarListasDeCartas();
    }

    private void VoltarCartaParaOrigem(GameObject carta, Transform parentOriginal, Vector3 posicaoOriginal, Vector3 escalaOriginal)
    {
        if (carta == null)
            return;

        carta.transform.SetParent(parentOriginal);
        carta.transform.position = posicaoOriginal;
        carta.transform.localScale = escalaOriginal;
    }

    private void TentarDescartarCartaPlayer(GameObject carta)
    {
        // Mantido apenas para compatibilidade interna. No CombateAmigavel atual,
        // o jogador não pode enviar cartas manualmente ao cemitério.
        if (carta != null)
            VoltarCartaParaOrigem(carta, parentOriginalCarta, posicaoOriginalCarta, escalaOriginalCarta);

        Notificar("CEMITÉRIO • apenas cartas derrotadas podem entrar.");
    }

    private void TentarDescartarCartaInimigo(GameObject carta)
    {
        // O inimigo também não descarta manualmente para o cemitério neste modo.
        // Cemitério é reservado para derrota/sacrifício por regras de habilidade.
    }

    public void BotaoResgatarCartaPlayer()
    {
        ResgatarCartaPlayer();
    }

    public void ResgatarCartaPlayer()
    {
        if (combateFinalizado || !turnoDoPlayer)
        {
            Debug.Log("Só pode resgatar carta durante o turno do player.");
            Notificar("A reserva só pode ser usada no seu turno.");
            return;
        }

        if (!PlayerPossuiAcaoDisponivel())
        {
            MostrarSemAcoesPlayer();
            return;
        }

        if (reservaPlayer == null || reservaPlayer.Count == 0)
        {
            Debug.Log("A reserva do player está vazia.");
            Notificar("RESERVA VAZIA.");
            VerificarCondicoesDeFimDeCombate();
            return;
        }

        Transform slotLivre = EncontrarSlotLivre(tagSlotDeckPlayer);
        if (slotLivre == null)
        {
            Debug.Log("Player não possui slot livre no deck para resgatar carta.");
            Notificar("Sem espaço livre no Deck.");
            return;
        }

        if (uiCombateCarta != null)
            uiCombateCarta.FecharPainelCarta();

        SelecaoReservaCombateUI selecao = SelecaoReservaCombateUI.ObterOuCriar();
        if (selecao == null)
            return;

        selecao.Mostrar(reservaPlayer, CartaReservaPlayerEscolhida);
    }

    private void CartaReservaPlayerEscolhida(Carta cartaPrefab)
    {
        if (cartaPrefab == null || combateFinalizado || !turnoDoPlayer)
            return;

        if (!PlayerPossuiAcaoDisponivel())
            return;

        Transform slotLivre = EncontrarSlotLivre(tagSlotDeckPlayer);
        if (slotLivre == null)
            return;

        int indiceReserva = reservaPlayer.IndexOf(cartaPrefab);
        if (indiceReserva < 0)
            return;

        GameObject cartaInstanciada = Instantiate(cartaPrefab.gameObject);
        Vector3 escala = cartaInstanciada.transform.localScale;
        cartaInstanciada.transform.SetParent(slotLivre);
        cartaInstanciada.transform.position = slotLivre.position;
        cartaInstanciada.transform.localScale = escala;
        cartaInstanciada.tag = tagCartaPlayer;

        reservaPlayer.RemoveAt(indiceReserva);
        ConsumirAcaoPlayer("pegar uma carta da reserva");

        AtualizarListasDeCartas();
        AtualizarTextosDeRecursos();
        AtualizarHUDDuelistas();

        Notificar($"{cartaPrefab.nome} entrou da reserva.", 1.0f);

        Debug.Log($"Player resgatou da reserva a carta {cartaPrefab.nome}. Reserva restante: {reservaPlayer.Count}");
        VerificarCondicoesDeFimDeCombate();
    }

    private bool ResgatarCartaInimigo()
    {
        if (combateFinalizado || !turnoDoInimigo)
            return false;

        if (!InimigoPossuiAcaoDisponivel())
            return false;

        if (reservaInimigo == null || reservaInimigo.Count == 0)
            return false;

        Transform slotLivre = EncontrarSlotLivre(tagSlotDeckInimigo);
        if (slotLivre == null)
            return false;

        Carta cartaPrefab = EscolherMelhorCartaDaReservaInimiga();
        if (cartaPrefab == null)
            return false;

        GameObject cartaInstanciada = Instantiate(cartaPrefab.gameObject);
        Vector3 escala = cartaInstanciada.transform.localScale;
        cartaInstanciada.transform.SetParent(slotLivre);
        cartaInstanciada.transform.position = slotLivre.position;
        cartaInstanciada.transform.localScale = escala;
        cartaInstanciada.tag = tagCartaInimigo;

        reservaInimigo.Remove(cartaPrefab);
        ConsumirAcaoInimigo("pegar uma carta da reserva");

        AtualizarTextosDeRecursos();
        AtualizarListasDeCartas();
        AtualizarHUDDuelistas();

        Notificar($"{cartaPrefab.nome} entrou da reserva inimiga.", 1.0f);

        Debug.Log($"Inimigo resgatou da reserva a carta {cartaPrefab.nome}. Reserva restante: {reservaInimigo.Count}");
        return true;
    }

    private Carta EscolherMelhorCartaDaReservaInimiga()
    {
        Carta melhor = null;
        int melhorPontuacao = int.MinValue;

        for (int i = 0; i < reservaInimigo.Count; i++)
        {
            Carta carta = reservaInimigo[i];
            if (carta == null)
                continue;

            int pontuacao = carta.dano * 3 + carta.vida * 2 + carta.defesa * 2 + Random.Range(0, 6);
            if (pontuacao > melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhor = carta;
            }
        }

        return melhor;
    }

    public void VoltarCartaPlayerParaDeck(GameObject carta)
    {
        if (carta == null || !carta.CompareTag(tagCartaPlayer))
            return;

        if (!EstaEmSlotComTag(carta.transform, tagSlotTabuleiroPlayer))
            return;

        Transform slotLivreDeck = EncontrarSlotLivre(tagSlotDeckPlayer);
        if (slotLivreDeck == null)
            return;

        if (!PlayerPossuiAcaoDisponivel())
        {
            MostrarSemAcoesPlayer();
            return;
        }

        ConsumirAcaoPlayer("retornar uma carta ao deck");

        Vector3 escalaOriginal = carta.transform.localScale;
        carta.transform.SetParent(slotLivreDeck);
        carta.transform.position = slotLivreDeck.position;
        carta.transform.localScale = escalaOriginal;

        Carta cartaComp = carta.GetComponent<Carta>();
        if (cartaComp != null)
            cartasPlayerConhecidasPeloInimigo.Add(cartaComp.nome);

        AtualizarListasDeCartas();

        if (uiCombateCarta != null)
            uiCombateCarta.FecharPainelCarta();
    }

    public void BotaoAtacarCartaSelecionada()
    {
        if (combateFinalizado || !turnoDoPlayer)
            return;

        if (uiCombateCarta == null || uiCombateCarta.cartaSelecionada == null)
            return;

        GameObject atacante = uiCombateCarta.cartaSelecionada;

        if (!atacante.CompareTag(tagCartaPlayer))
            return;

        if (!EstaEmSlotComTag(atacante.transform, tagSlotTabuleiroPlayer))
            return;

        if (!PlayerPossuiAcaoDisponivel())
        {
            MostrarSemAcoesPlayer();
            return;
        }

        if (CartaPlayerAindaEstaEmPreparacao(atacante))
        {
            MostrarCartaRecemColocadaPlayer();
            return;
        }

        if (CartaEstaParalisada(atacante))
        {
            Debug.Log("Essa carta está com Sobrecarga e não pode atacar.");
            Notificar("SOBRECARGA: esta carta não pode atacar.");
            return;
        }

        if (cartasPlayerQueAtacaramNesteTurno.Contains(atacante) || JaExisteAtaquePendente(atacante))
        {
            Debug.Log("Essa carta do player já atacou ou já possui um ataque pendente neste turno.");
            Notificar("Esta carta já possui um ataque neste turno.");
            return;
        }

        AtualizarListasDeCartas();

        // Se o campo inimigo estiver vazio, o ataque já é preparado diretamente no duelista.
        if (cartasInimigoNoTabuleiro.Count == 0)
        {
            AtaquePendente ataqueDireto = new AtaquePendente
            {
                atacante = atacante,
                alvo = null,
                ataqueDiretoAoDuelista = true
            };

            if (!ConsumirAcaoPlayer("preparar um ataque direto"))
                return;

            ataquesPendentesDoPlayer.Add(ataqueDireto);
            cartasPlayerQueAtacaramNesteTurno.Add(atacante);

            if (uiCombateCarta != null)
                uiCombateCarta.FecharPainelCarta();

            if (feedbackCartasUI != null)
                feedbackCartasUI.LimparDestaques();
            Notificar("Ataque direto preparado • resolve ao passar o turno.", 1.25f);

            Debug.Log("Ataque direto contra o duelista inimigo foi preparado para o fim do turno.");
            return;
        }

        cartaPlayerSelecionadaParaAtacar = atacante;
        cartaInimigoAlvoSelecionada = null;
        cartaPlayerSelecionadaParaHabilidade = null;
        alvoSelecionadoParaHabilidade = null;
        habilidadePlayerSelecionada = null;
        indiceHabilidadePlayerSelecionada = -1;
        acaoAtualComAlvo = TipoAcaoComAlvo.Ataque;
        modoEscolhaAlvo = true;

        if (feedbackCartasUI != null)
            feedbackCartasUI.DestacarAlvos(cartasInimigoNoTabuleiro, false);

        if (uiCombateCarta != null)
            uiCombateCarta.EntrarModoEscolhaAlvo("Escolha uma carta inimiga para atacar");
    }

    public void ConfirmarAlvoSelecionado()
    {
        if (!modoEscolhaAlvo)
            return;

        if (acaoAtualComAlvo == TipoAcaoComAlvo.Ataque)
        {
            if (cartaPlayerSelecionadaParaAtacar == null || cartaInimigoAlvoSelecionada == null)
                return;

            if (JaExisteAtaquePendente(cartaPlayerSelecionadaParaAtacar))
            {
                Debug.Log("Essa carta do player já possui um ataque pendente.");
                Notificar("Esta carta já possui um ataque pendente.");
                return;
            }

            if (!PlayerPossuiAcaoDisponivel())
            {
                MostrarSemAcoesPlayer();
                return;
            }

            if (CartaPlayerAindaEstaEmPreparacao(cartaPlayerSelecionadaParaAtacar))
            {
                MostrarCartaRecemColocadaPlayer();
                return;
            }

            AtaquePendente novoAtaque = new AtaquePendente
            {
                atacante = cartaPlayerSelecionadaParaAtacar,
                alvo = cartaInimigoAlvoSelecionada
            };

            if (!ConsumirAcaoPlayer("atacar"))
                return;

            ataquesPendentesDoPlayer.Add(novoAtaque);
            cartasPlayerQueAtacaramNesteTurno.Add(cartaPlayerSelecionadaParaAtacar);

            RestaurarCorCarta(cartaInimigoAlvoSelecionada);

            modoEscolhaAlvo = false;
            cartaPlayerSelecionadaParaAtacar = null;
            cartaInimigoAlvoSelecionada = null;
            acaoAtualComAlvo = TipoAcaoComAlvo.Ataque;

            if (feedbackCartasUI != null)
                feedbackCartasUI.LimparDestaques();

            if (uiCombateCarta != null)
                uiCombateCarta.SairModoEscolhaAlvo();

            Notificar("Ataque confirmado • será resolvido ao passar o turno.", 1.0f);
            Debug.Log("Ataque do player confirmado para ser resolvido no fim do turno.");
            return;
        }

        if (acaoAtualComAlvo == TipoAcaoComAlvo.Habilidade)
        {
            if (cartaPlayerSelecionadaParaHabilidade == null || alvoSelecionadoParaHabilidade == null || habilidadePlayerSelecionada == null)
                return;

            if (!PlayerPossuiAcaoDisponivel())
            {
                MostrarSemAcoesPlayer();
                return;
            }

            if (CartaPlayerAindaEstaEmPreparacao(cartaPlayerSelecionadaParaHabilidade))
            {
                MostrarCartaRecemColocadaPlayer();
                return;
            }

            if (!PodeUsarHabilidade(cartaPlayerSelecionadaParaHabilidade, habilidadePlayerSelecionada, indiceHabilidadePlayerSelecionada, true))
                return;

            if (!PagarCustoEspecialSeNecessario(cartaPlayerSelecionadaParaHabilidade, habilidadePlayerSelecionada, true))
                return;

            if (!ConsumirAcaoPlayer("usar uma habilidade"))
                return;

            AplicarHabilidade(cartaPlayerSelecionadaParaHabilidade, alvoSelecionadoParaHabilidade, habilidadePlayerSelecionada);
            RegistrarUsoHabilidade(cartaPlayerSelecionadaParaHabilidade, habilidadePlayerSelecionada, indiceHabilidadePlayerSelecionada);
            cartasPlayerQueUsaramHabilidadeNesteTurno.Add(cartaPlayerSelecionadaParaHabilidade);

            RestaurarCorCarta(alvoSelecionadoParaHabilidade);

            modoEscolhaAlvo = false;
            cartaPlayerSelecionadaParaHabilidade = null;
            alvoSelecionadoParaHabilidade = null;
            habilidadePlayerSelecionada = null;
            indiceHabilidadePlayerSelecionada = -1;
            acaoAtualComAlvo = TipoAcaoComAlvo.Ataque;

            if (feedbackCartasUI != null)
                feedbackCartasUI.LimparDestaques();

            if (uiCombateCarta != null)
                uiCombateCarta.SairModoEscolhaAlvo();

            Debug.Log("Habilidade do player resolvida.");
        }
    }

    public void CancelarEscolhaAlvo()
    {
        if (cartaInimigoAlvoSelecionada != null)
            RestaurarCorCarta(cartaInimigoAlvoSelecionada);

        if (alvoSelecionadoParaHabilidade != null)
            RestaurarCorCarta(alvoSelecionadoParaHabilidade);

        modoEscolhaAlvo = false;
        cartaPlayerSelecionadaParaAtacar = null;
        cartaInimigoAlvoSelecionada = null;
        cartaPlayerSelecionadaParaHabilidade = null;
        alvoSelecionadoParaHabilidade = null;
        habilidadePlayerSelecionada = null;
        indiceHabilidadePlayerSelecionada = -1;
        acaoAtualComAlvo = TipoAcaoComAlvo.Ataque;

        if (feedbackCartasUI != null)
            feedbackCartasUI.LimparDestaques();

        if (uiCombateCarta != null)
            uiCombateCarta.SairModoEscolhaAlvo();
    }

    private void AtualizarEscolhaDeAlvoDoPlayer()
    {
        if (cameraPrincipal == null || Mouse.current == null)
            return;

        Vector2 posicaoMouse = cameraPrincipal.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(posicaoMouse, Vector2.zero);

        GameObject objetoSobMouse = hit.collider != null ? hit.collider.gameObject : null;
        bool alvoValidoSobMouse = objetoSobMouse != null && AlvoEhValidoParaAcaoAtual(objetoSobMouse);

        if (feedbackCartasUI != null)
        {
            if (alvoValidoSobMouse)
            {
                if (acaoAtualComAlvo == TipoAcaoComAlvo.Ataque)
                    feedbackCartasUI.MostrarPreviewAtaque(cartaPlayerSelecionadaParaAtacar, objetoSobMouse);
                else
                    feedbackCartasUI.MostrarPreviewHabilidade(cartaPlayerSelecionadaParaHabilidade, objetoSobMouse, habilidadePlayerSelecionada);
            }
            else
            {
                feedbackCartasUI.OcultarPreview();
            }
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (objetoSobMouse == null)
            return;

        if (!alvoValidoSobMouse)
        {
            Notificar("Alvo inválido para esta ação.", 0.85f);
            return;
        }

        GameObject objetoClicado = objetoSobMouse;

        if (acaoAtualComAlvo == TipoAcaoComAlvo.Ataque)
        {
            if (cartaInimigoAlvoSelecionada != null && cartaInimigoAlvoSelecionada != objetoClicado)
                RestaurarCorCarta(cartaInimigoAlvoSelecionada);

            cartaInimigoAlvoSelecionada = objetoClicado;
            PintarCartaDeVermelho(cartaInimigoAlvoSelecionada);
        }
        else
        {
            if (alvoSelecionadoParaHabilidade != null && alvoSelecionadoParaHabilidade != objetoClicado)
                RestaurarCorCarta(alvoSelecionadoParaHabilidade);

            alvoSelecionadoParaHabilidade = objetoClicado;
            PintarCartaDeVermelho(alvoSelecionadoParaHabilidade);
        }

        if (uiCombateCarta != null)
            uiCombateCarta.AtualizarTextoAlvoSelecionado(objetoClicado);
    }

    private bool AlvoEhValidoParaAcaoAtual(GameObject alvo)
    {
        if (alvo == null)
            return false;

        if (acaoAtualComAlvo == TipoAcaoComAlvo.Ataque)
            return alvo.CompareTag(tagCartaInimigo) && EstaEmSlotComTag(alvo.transform, tagSlotTabuleiroInimigo);

        if (cartaPlayerSelecionadaParaHabilidade == null || habilidadePlayerSelecionada == null)
            return false;

        if (habilidadePlayerSelecionada.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaInimiga)
            return alvo.CompareTag(tagCartaInimigo) && EstaEmSlotComTag(alvo.transform, tagSlotTabuleiroInimigo);

        if (habilidadePlayerSelecionada.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaAliada)
            return alvo.CompareTag(tagCartaPlayer) && EstaEmSlotComTag(alvo.transform, tagSlotTabuleiroPlayer);

        return false;
    }

    public void BotaoHabilidadeCartaSelecionada()
    {
        BotaoAbrirListaHabilidadesCartaSelecionada();
    }

    public void BotaoAbrirListaHabilidadesCartaSelecionada()
    {
        if (!turnoDoPlayer)
            return;

        if (uiCombateCarta == null || uiCombateCarta.cartaSelecionada == null)
            return;

        GameObject cartaObj = uiCombateCarta.cartaSelecionada;

        if (!cartaObj.CompareTag(tagCartaPlayer))
            return;

        if (!EstaEmSlotComTag(cartaObj.transform, tagSlotTabuleiroPlayer))
            return;

        if (!PlayerPossuiAcaoDisponivel())
        {
            MostrarSemAcoesPlayer();
            return;
        }

        if (CartaPlayerAindaEstaEmPreparacao(cartaObj))
        {
            MostrarCartaRecemColocadaPlayer();
            return;
        }

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null || !carta.TemHabilidadeValida())
        {
            Debug.Log("Essa carta não possui habilidade válida configurada.");
            Notificar("Esta carta não possui habilidade disponível.");
            return;
        }

        uiCombateCarta.AbrirPainelListaHabilidades(cartaObj);
    }

    public void BotaoSelecionarHabilidadeCartaSelecionada(int indiceHabilidade)
    {
        if (!turnoDoPlayer)
            return;

        if (uiCombateCarta == null || uiCombateCarta.cartaSelecionada == null)
            return;

        GameObject cartaObj = uiCombateCarta.cartaSelecionada;
        Carta carta = cartaObj.GetComponent<Carta>();

        if (carta == null)
            return;

        HabilidadeCarta habilidade = carta.ObterHabilidade(indiceHabilidade);
        if (habilidade == null)
        {
            Debug.Log("Habilidade inválida ou não configurada.");
            Notificar("Habilidade indisponível.");
            return;
        }

        habilidadePlayerSelecionada = habilidade;
        indiceHabilidadePlayerSelecionada = indiceHabilidade;
        cartaPlayerSelecionadaParaHabilidade = cartaObj;

        string textoEstado = ObterTextoEstadoHabilidade(cartaObj, habilidade, indiceHabilidade);
        uiCombateCarta.AbrirPainelConfirmacaoHabilidade(habilidade, textoEstado);
    }

    public void BotaoConfirmarUsoHabilidadeSelecionada()
    {
        if (!turnoDoPlayer)
            return;

        if (cartaPlayerSelecionadaParaHabilidade == null || habilidadePlayerSelecionada == null)
            return;

        GameObject cartaObj = cartaPlayerSelecionadaParaHabilidade;

        if (!PlayerPossuiAcaoDisponivel())
        {
            MostrarSemAcoesPlayer();
            return;
        }

        if (CartaPlayerAindaEstaEmPreparacao(cartaObj))
        {
            MostrarCartaRecemColocadaPlayer();
            return;
        }

        if (cartasPlayerQueUsaramHabilidadeNesteTurno.Contains(cartaObj))
        {
            Debug.Log("Essa carta do player já usou habilidade neste turno.");
            Notificar("Esta carta já usou habilidade neste turno.");
            return;
        }

        if (!PodeUsarHabilidade(cartaObj, habilidadePlayerSelecionada, indiceHabilidadePlayerSelecionada, true))
            return;

        if (habilidadePlayerSelecionada.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.PropriaCarta)
        {
            if (!PagarCustoEspecialSeNecessario(cartaObj, habilidadePlayerSelecionada, true))
                return;

            if (!ConsumirAcaoPlayer("usar uma habilidade"))
                return;

            AplicarHabilidade(cartaObj, cartaObj, habilidadePlayerSelecionada);
            RegistrarUsoHabilidade(cartaObj, habilidadePlayerSelecionada, indiceHabilidadePlayerSelecionada);
            cartasPlayerQueUsaramHabilidadeNesteTurno.Add(cartaObj);
            LimparSelecaoHabilidadePlayer();

            if (uiCombateCarta != null)
                uiCombateCarta.FecharTodosPaineisDeHabilidade();

            return;
        }

        cartaPlayerSelecionadaParaAtacar = null;
        cartaInimigoAlvoSelecionada = null;
        alvoSelecionadoParaHabilidade = null;
        acaoAtualComAlvo = TipoAcaoComAlvo.Habilidade;
        modoEscolhaAlvo = true;

        if (feedbackCartasUI != null)
        {
            if (habilidadePlayerSelecionada.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaInimiga)
                feedbackCartasUI.DestacarAlvos(cartasInimigoNoTabuleiro, false);
            else if (habilidadePlayerSelecionada.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaAliada)
                feedbackCartasUI.DestacarAlvos(cartasPlayerNoTabuleiro, true);
        }

        if (uiCombateCarta != null)
            uiCombateCarta.EntrarModoEscolhaAlvo(ObterMensagemEscolhaAlvoHabilidade(habilidadePlayerSelecionada));
    }

    public void BotaoCancelarConfirmacaoHabilidade()
    {
        LimparSelecaoHabilidadePlayer();

        if (uiCombateCarta != null)
            uiCombateCarta.FecharPainelConfirmacaoHabilidade();
    }

    private void LimparSelecaoHabilidadePlayer()
    {
        habilidadePlayerSelecionada = null;
        indiceHabilidadePlayerSelecionada = -1;
        cartaPlayerSelecionadaParaHabilidade = null;
        alvoSelecionadoParaHabilidade = null;
    }

    private string ObterMensagemEscolhaAlvoHabilidade(HabilidadeCarta habilidade)
    {
        if (habilidade == null)
            return "Escolha o alvo da habilidade";

        if (habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaInimiga)
            return "Escolha uma carta inimiga para receber a habilidade";

        if (habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaAliada)
            return "Escolha uma carta do seu lado para receber a habilidade";

        return "Escolha o alvo da habilidade";
    }

    public void PassarTurno()
    {
        if (combateFinalizado || !turnoDoPlayer || inimigoExecutandoTurno)
            return;

        if (!ignorarConfirmacaoPassarTurno && acoesRestantesPlayer > 0)
        {
            notificacaoJogoUI = notificacaoJogoUI != null ? notificacaoJogoUI : NotificacaoJogoUI.ObterOuCriar();
            if (notificacaoJogoUI != null)
            {
                notificacaoJogoUI.ConfirmarPassarTurno(acoesRestantesPlayer,
                    () =>
                    {
                        ignorarConfirmacaoPassarTurno = true;
                        PassarTurno();
                    });
                return;
            }
        }

        ignorarConfirmacaoPassarTurno = false;

        if (modoEscolhaAlvo)
            CancelarEscolhaAlvo();

        if (uiCombateCarta != null)
            uiCombateCarta.FecharPainelCarta();

        ResolverAtaquesPendentesDoPlayer();

        if (combateFinalizado)
            return;

        VerificarCondicoesDeFimDeCombate();
        if (combateFinalizado)
            return;

        turnoDoPlayer = false;
        turnoDoInimigo = true;
        acoesRestantesPlayer = 0;
        acoesRestantesInimigo = Mathf.Max(1, maximoAcoesPorTurnoInimigo);

        // Cartas colocadas pelo inimigo no turno inimigo anterior agora ficam prontas para agir.
        cartasInimigoColocadasNesteTurno.Clear();

        ProcessarBuffsTemporariosNoInicioDoTurno(false);
        ProcessarEfeitosNoInicioDoTurno(false);
        ProcessarCooldownsHabilidadesNoInicioDoTurno(false);

        if (combateFinalizado)
            return;

        VerificarCondicoesDeFimDeCombate();
        if (combateFinalizado)
            return;

        contadorTurnosInimigo++;
        TentarRecuperarEnergiaInimigo();

        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();
        AtualizarHUDDuelistas();

        if (notificacaoJogoUI != null)
            notificacaoJogoUI.MostrarBannerTurno(false);

        StartCoroutine(ExecutarTurnoDoInimigo());
    }

    private void ResolverAtaquesPendentesDoPlayer()
    {
        for (int i = 0; i < ataquesPendentesDoPlayer.Count; i++)
        {
            if (combateFinalizado)
                break;

            AtaquePendente ataque = ataquesPendentesDoPlayer[i];
            if (ataque == null || ataque.atacante == null)
                continue;

            if (!EstaEmSlotComTag(ataque.atacante.transform, tagSlotTabuleiroPlayer))
                continue;

            AtualizarListasDeCartas();

            if (ataque.ataqueDiretoAoDuelista)
            {
                // Se alguma carta inimiga surgiu antes da resolução, ela volta a proteger o duelista.
                if (cartasInimigoNoTabuleiro.Count > 0)
                {
                    Debug.Log("Ataque direto cancelado porque o inimigo voltou a possuir uma carta no tabuleiro.");
                    continue;
                }

                AplicarAtaqueDiretoNoInimigo(ataque.atacante);
                continue;
            }

            if (ataque.alvo == null)
                continue;

            if (!EstaEmSlotComTag(ataque.alvo.transform, tagSlotTabuleiroInimigo))
                continue;

            AplicarAtaque(ataque.atacante, ataque.alvo);
        }

        ataquesPendentesDoPlayer.Clear();
    }

    private void AtualizarTextoTurno()
    {
        if (textoTurno == null)
            return;

        // O banner automático já anuncia a troca de turno; este texto fica propositalmente compacto.
        textoTurno.enableAutoSizing = true;
        textoTurno.fontSizeMin = 10f;
        textoTurno.fontSizeMax = 18f;
        textoTurno.enableWordWrapping = false;

        if (turnoDoPlayer)
            textoTurno.text = "PLAYER";
        else if (turnoDoInimigo)
            textoTurno.text = "OPONENTE";
        else
            textoTurno.text = "";
    }

    private void AtualizarTextosDeRecursos()
    {
        if (textoEnergiaPlayer != null)
            textoEnergiaPlayer.text = $"Energia Player: {energiaAtualPlayer}/{energiaMaximaPlayer}";

        if (textoEnergiaInimigo != null)
            textoEnergiaInimigo.text = $"Energia Inimigo: {energiaAtualInimigo}/{energiaMaximaInimigo}";

        if (textoResgatarPlayer != null)
            textoResgatarPlayer.text = $"Reserva Player: {(reservaPlayer != null ? reservaPlayer.Count : 0)}";

        if (textoResgatarInimigo != null)
            textoResgatarInimigo.text = $"Reserva Inimigo: {(reservaInimigo != null ? reservaInimigo.Count : 0)}";
    }

    private bool PlayerPossuiAcaoDisponivel()
    {
        return turnoDoPlayer && !combateFinalizado && acoesRestantesPlayer > 0;
    }

    private bool InimigoPossuiAcaoDisponivel()
    {
        return turnoDoInimigo && !combateFinalizado && acoesRestantesInimigo > 0;
    }

    private bool ConsumirAcaoPlayer(string descricao)
    {
        if (!PlayerPossuiAcaoDisponivel())
            return false;

        acoesRestantesPlayer--;
        Debug.Log($"AÇÃO PLAYER -> {descricao}. Restantes: {acoesRestantesPlayer}/{maximoAcoesPorTurnoPlayer}");
        AtualizarHUDDuelistas();
        return true;
    }

    private bool ConsumirAcaoInimigo(string descricao)
    {
        if (!InimigoPossuiAcaoDisponivel())
            return false;

        acoesRestantesInimigo--;
        Debug.Log($"AÇÃO INIMIGO -> {descricao}. Restantes: {acoesRestantesInimigo}/{maximoAcoesPorTurnoInimigo}");
        AtualizarHUDDuelistas();
        return true;
    }

    private void MostrarSemAcoesPlayer()
    {
        Debug.Log("Player não possui mais ações neste turno.");
        Notificar("SEM AÇÕES • passe o turno.", 1.0f);
    }

    private void Notificar(string mensagem, float duracao = 1.2f)
    {
        notificacaoJogoUI = notificacaoJogoUI != null ? notificacaoJogoUI : NotificacaoJogoUI.ObterOuCriar();
        if (notificacaoJogoUI != null)
            notificacaoJogoUI.MostrarMensagem(mensagem, duracao);
        else if (hudDuelistasUI != null)
            hudDuelistasUI.MostrarMensagemTemporaria(mensagem, duracao);
    }

    private bool CartaPlayerAindaEstaEmPreparacao(GameObject cartaObj)
    {
        return cartaRecemColocadaPrecisaEsperar && cartaObj != null && cartasPlayerColocadasNesteTurno.Contains(cartaObj);
    }

    private bool CartaInimigoAindaEstaEmPreparacao(GameObject cartaObj)
    {
        return cartaRecemColocadaPrecisaEsperar && cartaObj != null && cartasInimigoColocadasNesteTurno.Contains(cartaObj);
    }

    private void MostrarCartaRecemColocadaPlayer()
    {
        Debug.Log("Carta recém-colocada ainda está em preparação e só poderá agir no próximo turno do player.");
        Notificar("AGUARDE • carta pronta no próximo turno.", 1.15f);
    }

    private IEnumerator ExecutarTurnoDoInimigo()
    {
        if (combateFinalizado)
            yield break;

        inimigoExecutandoTurno = true;
        AtualizarListasDeCartas();

        cartasInimigoQueAtacaramNesteTurno.Clear();
        cartasInimigoQueUsaramHabilidadeNesteTurno.Clear();

        yield return new WaitForSeconds(tempoEntreAcoesInimigo);
        if (combateFinalizado)
            yield break;

        int quantidadeParaColocar = CalcularQuantidadeIdealDeCartasParaColocar();

        for (int i = 0; i < quantidadeParaColocar; i++)
        {
            bool colocou = InimigoTentaColocarCartaNoTabuleiro();
            AtualizarListasDeCartas();
            AtualizarHUDDuelistas();

            if (!colocou)
                break;

            yield return new WaitForSeconds(tempoEntreAcoesInimigo);
            if (combateFinalizado)
                yield break;
        }

        // A reserva agora é uma ação normal: não precisa descartar uma carta nem gerar ponto de resgate.
        if (InimigoPossuiAcaoDisponivel() && DeveUsarReservaInimigo())
        {
            bool resgatou = ResgatarCartaInimigo();
            if (resgatou)
            {
                AtualizarListasDeCartas();
                AtualizarTextosDeRecursos();
                AtualizarHUDDuelistas();
                yield return new WaitForSeconds(tempoEntreAcoesInimigo);
            }
        }

        VerificarCondicoesDeFimDeCombate();
        if (combateFinalizado)
            yield break;

        // Usa no máximo algumas habilidades antes dos ataques para não gastar as 3 ações só em habilidades.
        int habilidadesUsadasNesteTurno = 0;
        for (int i = 0; i < cartasInimigoNoTabuleiro.Count && InimigoPossuiAcaoDisponivel(); i++)
        {
            if (habilidadesUsadasNesteTurno >= Mathf.Max(0, maximoHabilidadesInimigoPorTurno))
                break;

            GameObject cartaComHabilidade = cartasInimigoNoTabuleiro[i];

            if (TentarUsarHabilidadeInimigo(cartaComHabilidade))
            {
                habilidadesUsadasNesteTurno++;
                AtualizarListasDeCartas();
                AtualizarHUDDuelistas();
                VerificarCondicoesDeFimDeCombate();

                if (combateFinalizado)
                    yield break;

                yield return new WaitForSeconds(tempoEntreAcoesInimigo);
            }
        }

        AtualizarListasDeCartas();

        // Cada carta inimiga apta ataca uma vez.
        List<GameObject> atacantesDesteTurno = new List<GameObject>(cartasInimigoNoTabuleiro);
        for (int i = 0; i < atacantesDesteTurno.Count && InimigoPossuiAcaoDisponivel(); i++)
        {
            if (combateFinalizado)
                yield break;

            GameObject atacante = atacantesDesteTurno[i];
            if (atacante == null)
                continue;

            if (cartasInimigoQueAtacaramNesteTurno.Contains(atacante))
                continue;

            if (!EstaEmSlotComTag(atacante.transform, tagSlotTabuleiroInimigo))
                continue;

            if (CartaInimigoAindaEstaEmPreparacao(atacante))
                continue;

            AtualizarListasDeCartas();

            if (cartasPlayerNoTabuleiro.Count == 0)
            {
                if (!ConsumirAcaoInimigo("atacar diretamente o player"))
                    break;

                AplicarAtaqueDiretoNoPlayer(atacante);
                cartasInimigoQueAtacaramNesteTurno.Add(atacante);
                AtualizarHUDDuelistas();

                if (combateFinalizado)
                    yield break;

                yield return new WaitForSeconds(tempoEntreAcoesInimigo);
                continue;
            }

            GameObject alvoEscolhido = EscolherAlvoEstrategicoDoPlayer(atacante);
            if (alvoEscolhido != null)
            {
                if (!ConsumirAcaoInimigo("atacar uma carta"))
                    break;

                AplicarAtaque(atacante, alvoEscolhido);
                cartasInimigoQueAtacaramNesteTurno.Add(atacante);

                AtualizarListasDeCartas();
                AtualizarHUDDuelistas();
                VerificarCondicoesDeFimDeCombate();

                if (combateFinalizado)
                    yield break;

                yield return new WaitForSeconds(tempoEntreAcoesInimigo);
            }
        }

        VerificarCondicoesDeFimDeCombate();
        if (!combateFinalizado)
            EncerrarTurnoDoInimigo();
    }

    private int CalcularQuantidadeIdealDeCartasParaColocar()
    {
        AtualizarListasDeCartas();

        int slotsLivres = ContarSlotsLivres(tagSlotTabuleiroInimigo);
        int cartasNoDeck = cartasInimigoNoDeck.Count;
        int acoesDisponiveis = Mathf.Max(0, acoesRestantesInimigo);

        if (slotsLivres <= 0 || cartasNoDeck <= 0 || acoesDisponiveis <= 0)
            return 0;

        int cartasPlayerCampo = cartasPlayerNoTabuleiro.Count;
        int cartasInimigoCampo = cartasInimigoNoTabuleiro.Count;

        if (cartasPlayerCampo > cartasInimigoCampo)
            return Mathf.Min(slotsLivres, cartasNoDeck, acoesDisponiveis, 2);

        if (cartasInimigoCampo == 0)
            return Mathf.Min(1, Mathf.Min(slotsLivres, Mathf.Min(cartasNoDeck, acoesDisponiveis)));

        if (cartasInimigoCampo < 2 && cartasNoDeck > 0)
            return Mathf.Min(1, Mathf.Min(slotsLivres, Mathf.Min(cartasNoDeck, acoesDisponiveis)));

        if (Random.value < 0.45f)
            return Mathf.Min(1, Mathf.Min(slotsLivres, Mathf.Min(cartasNoDeck, acoesDisponiveis)));

        return 0;
    }

    private bool InimigoTentaColocarCartaNoTabuleiro()
    {
        if (!InimigoPossuiAcaoDisponivel())
            return false;

        AtualizarListasDeCartas();

        Transform slotLivre = EncontrarSlotLivre(tagSlotTabuleiroInimigo);
        if (slotLivre == null)
            return false;

        GameObject melhorCarta = EscolherCartaDoDeckDoInimigoParaJogar();
        if (melhorCarta == null)
            return false;

        Vector3 escala = melhorCarta.transform.localScale;
        melhorCarta.transform.SetParent(slotLivre);
        melhorCarta.transform.position = slotLivre.position;
        melhorCarta.transform.localScale = escala;

        ConsumirAcaoInimigo("colocar uma carta no tabuleiro");
        cartasInimigoColocadasNesteTurno.Add(melhorCarta);

        Debug.Log($"Inimigo colocou a carta {melhorCarta.name} no tabuleiro e ela ficará em preparação até o próximo turno do inimigo.");
        return true;
    }

    private bool DeveUsarReservaInimigo()
    {
        if (combateFinalizado || !InimigoPossuiAcaoDisponivel())
            return false;

        if (reservaInimigo == null || reservaInimigo.Count == 0)
            return false;

        if (EncontrarSlotLivre(tagSlotDeckInimigo) == null)
            return false;

        AtualizarListasDeCartas();

        // Prioriza a reserva quando o deck está acabando.
        if (cartasInimigoNoDeck.Count <= 1)
            return true;

        // Se o inimigo ainda tem bastante deck, às vezes prepara uma carta extra para turnos futuros.
        return cartasInimigoNoDeck.Count <= 3 && Random.value < 0.28f;
    }

    private void InimigoDescartaCartaDoDeckParaResgatar()
    {
        AtualizarListasDeCartas();

        if (cartasInimigoNoDeck.Count == 0)
            return;

        GameObject piorCarta = null;
        int piorPontuacao = int.MaxValue;

        for (int i = 0; i < cartasInimigoNoDeck.Count; i++)
        {
            GameObject cartaObj = cartasInimigoNoDeck[i];
            if (cartaObj == null)
                continue;

            Carta carta = cartaObj.GetComponent<Carta>();
            if (carta == null)
                continue;

            int pontuacao = carta.dano + carta.vida + carta.defesa + Random.Range(0, 4);

            if (pontuacao < piorPontuacao)
            {
                piorPontuacao = pontuacao;
                piorCarta = cartaObj;
            }
        }

        if (piorCarta == null)
            return;

        TentarDescartarCartaInimigo(piorCarta);
    }

    private GameObject EscolherCartaDoDeckDoInimigoParaJogar()
    {
        if (cartasInimigoNoDeck.Count == 0)
            return null;

        GameObject melhorCarta = null;
        int melhorPontuacao = int.MinValue;

        bool playerTemCartas = cartasPlayerNoTabuleiro.Count > 0;

        int maiorDanoPlayer = 0;
        int maiorVidaPlayer = 0;

        for (int i = 0; i < cartasPlayerNoTabuleiro.Count; i++)
        {
            if (cartasPlayerNoTabuleiro[i] == null)
                continue;

            Carta cartaPlayer = cartasPlayerNoTabuleiro[i].GetComponent<Carta>();
            if (cartaPlayer == null)
                continue;

            if (cartaPlayer.dano > maiorDanoPlayer)
                maiorDanoPlayer = cartaPlayer.dano;

            if (cartaPlayer.vida > maiorVidaPlayer)
                maiorVidaPlayer = cartaPlayer.vida;
        }

        for (int i = 0; i < cartasInimigoNoDeck.Count; i++)
        {
            GameObject cartaObj = cartasInimigoNoDeck[i];
            if (cartaObj == null)
                continue;

            Carta carta = cartaObj.GetComponent<Carta>();
            if (carta == null)
                continue;

            if (CartaEstaParalisada(cartaObj))
                continue;

            int pontuacao = 0;
            pontuacao += carta.dano * 4;
            pontuacao += carta.vida * 2;
            pontuacao += carta.defesa * 2;

            if (playerTemCartas)
            {
                if (carta.dano >= maiorVidaPlayer)
                    pontuacao += 20;

                if (carta.defesa >= maiorDanoPlayer / 2)
                    pontuacao += 10;
            }
            else
            {
                pontuacao += carta.dano * 2;
            }

            pontuacao += Random.Range(0, 4);

            if (pontuacao > melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhorCarta = cartaObj;
            }
        }

        return melhorCarta;
    }

    private GameObject EscolherAlvoEstrategicoDoPlayer(GameObject atacanteInimigo)
    {
        if (atacanteInimigo == null || cartasPlayerNoTabuleiro.Count == 0)
            return null;

        Carta atacante = atacanteInimigo.GetComponent<Carta>();
        if (atacante == null)
            return null;

        GameObject alvoParaFinalizar = null;
        int melhorPontuacaoFinalizacao = int.MinValue;

        GameObject alvoMaisPerigoso = null;
        int melhorPontuacaoPerigo = int.MinValue;

        for (int i = 0; i < cartasPlayerNoTabuleiro.Count; i++)
        {
            GameObject alvoObj = cartasPlayerNoTabuleiro[i];
            if (alvoObj == null)
                continue;

            Carta alvo = alvoObj.GetComponent<Carta>();
            if (alvo == null)
                continue;

            int danoReal = Mathf.Max(0, atacante.dano - alvo.defesa);

            if (danoReal >= alvo.vida)
            {
                int pontuacaoFinalizacao = 1000 + alvo.dano * 8 + alvo.vida + Random.Range(0, 6);

                if (pontuacaoFinalizacao > melhorPontuacaoFinalizacao)
                {
                    melhorPontuacaoFinalizacao = pontuacaoFinalizacao;
                    alvoParaFinalizar = alvoObj;
                }
            }

            int pontuacaoPerigo = alvo.dano * 10 + alvo.vida * 2 + Mathf.Max(0, danoReal) * 6 + Random.Range(0, 5);

            if (pontuacaoPerigo > melhorPontuacaoPerigo)
            {
                melhorPontuacaoPerigo = pontuacaoPerigo;
                alvoMaisPerigoso = alvoObj;
            }
        }

        if (alvoParaFinalizar != null)
            return alvoParaFinalizar;

        return alvoMaisPerigoso;
    }

    private bool TentarUsarHabilidadeInimigo(GameObject cartaObj)
    {
        if (!turnoDoInimigo)
            return false;

        if (!InimigoPossuiAcaoDisponivel())
            return false;

        if (cartaObj == null || !cartaObj.CompareTag(tagCartaInimigo))
            return false;

        if (CartaInimigoAindaEstaEmPreparacao(cartaObj))
            return false;

        if (!EstaEmSlotComTag(cartaObj.transform, tagSlotTabuleiroInimigo))
            return false;

        if (cartasInimigoQueUsaramHabilidadeNesteTurno.Contains(cartaObj))
            return false;

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null || !carta.TemHabilidadeValida())
            return false;

        for (int i = 0; i < Mathf.Min(carta.quantidadeHabilidades, carta.habilidades.Count, 4); i++)
        {
            HabilidadeCarta habilidade = carta.ObterHabilidade(i);
            if (habilidade == null)
                continue;

            if (!PodeUsarHabilidade(cartaObj, habilidade, i, false))
                continue;

            GameObject alvo = EscolherAlvoParaHabilidadeInimigo(cartaObj, habilidade);
            if (alvo == null)
                continue;

            if (!PagarCustoEspecialSeNecessario(cartaObj, habilidade, false))
                continue;

            if (!ConsumirAcaoInimigo("usar uma habilidade"))
                return false;

            AplicarHabilidade(cartaObj, alvo, habilidade);
            RegistrarUsoHabilidade(cartaObj, habilidade, i);
            cartasInimigoQueUsaramHabilidadeNesteTurno.Add(cartaObj);
            return true;
        }

        return false;
    }

    private GameObject EscolherAlvoParaHabilidadeInimigo(GameObject cartaObj, HabilidadeCarta habilidade)
    {
        if (cartaObj == null || habilidade == null)
            return null;

        if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Defesa || habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.PropriaCarta)
            return cartaObj;

        if (habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaInimiga)
            return EscolherAlvoEstrategicoDoPlayer(cartaObj);

        if (habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaAliada)
            return EscolherCartaAliadaParaBuffInimigo(habilidade);

        return null;
    }

    private GameObject EscolherCartaAliadaParaBuffInimigo(HabilidadeCarta habilidade)
    {
        if (habilidade == null || cartasInimigoNoTabuleiro.Count == 0)
            return null;

        GameObject melhorAlvo = null;
        int melhorPontuacao = int.MinValue;

        for (int i = 0; i < cartasInimigoNoTabuleiro.Count; i++)
        {
            GameObject alvoObj = cartasInimigoNoTabuleiro[i];
            if (alvoObj == null)
                continue;

            Carta alvo = alvoObj.GetComponent<Carta>();
            if (alvo == null)
                continue;

            int pontuacao = Random.Range(0, 5);

            if (habilidade.tipoBuff == HabilidadeCarta.TipoBuff.Dano)
                pontuacao += alvo.dano * 4;
            else if (habilidade.tipoBuff == HabilidadeCarta.TipoBuff.Vida)
                pontuacao += Mathf.Max(1, 20 - alvo.vida) * 3;
            else if (habilidade.tipoBuff == HabilidadeCarta.TipoBuff.Defesa)
                pontuacao += alvo.vida * 2 + alvo.defesa;
            else
                pontuacao += alvo.dano + alvo.vida + alvo.defesa;

            if (pontuacao > melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhorAlvo = alvoObj;
            }
        }

        return melhorAlvo;
    }

    private void AplicarHabilidade(GameObject usuarioObj, GameObject alvoObj, HabilidadeCarta habilidade)
    {
        if (usuarioObj == null || alvoObj == null || habilidade == null)
            return;

        Carta usuario = usuarioObj.GetComponent<Carta>();
        Carta alvo = alvoObj.GetComponent<Carta>();

        if (usuario == null || alvo == null)
            return;

        if (!habilidade.EstaConfigurada())
        {
            Debug.Log($"A habilidade {habilidade.nomeHabilidade} não está configurada corretamente.");
            return;
        }

        if (!ValidarAlvoDaHabilidade(usuarioObj, alvoObj, habilidade))
        {
            Debug.LogWarning($"Alvo inválido para a habilidade {habilidade.nomeHabilidade} de {usuario.nome}.");
            return;
        }

        int valor = Mathf.Max(0, habilidade.valorHabilidade);

        // Cada habilidade especial pode possuir seu proprio efeito visual.
        // O efeito aparece sobre o alvo definido pela propria habilidade.
        ReproduzirEfeitoVisualHabilidadeEspecial(usuarioObj, alvoObj, habilidade);

        switch (habilidade.tipoHabilidade)
        {
            case HabilidadeCarta.TipoHabilidade.Dano:
                AplicarDanoDeHabilidade(usuarioObj, usuario, alvoObj, alvo, valor);
                break;

            case HabilidadeCarta.TipoHabilidade.Defesa:
                AplicarBuffTemporario(usuarioObj, alvoObj, usuario, alvo, HabilidadeCarta.TipoBuff.Defesa, valor, habilidade.duracaoHabilidadeTurnos);
                break;

            case HabilidadeCarta.TipoHabilidade.Buff:
                AplicarBuffDeHabilidade(usuarioObj, alvoObj, usuario, alvo, habilidade, valor);
                break;

            case HabilidadeCarta.TipoHabilidade.Anulacao:
                AnularEfeitosNegativos(alvoObj, usuario, alvo);
                break;

            case HabilidadeCarta.TipoHabilidade.Disfarce:
                AplicarDisfarce(usuarioObj, alvoObj, usuario, alvo);
                break;
        }

        AplicarEfeitosDaHabilidade(usuarioObj, alvoObj, habilidade);

        AtualizarListasDeCartas();
        AtualizarTextosDeRecursos();
    }

    private bool ValidarAlvoDaHabilidade(GameObject usuarioObj, GameObject alvoObj, HabilidadeCarta habilidade)
    {
        if (usuarioObj == null || alvoObj == null || habilidade == null)
            return false;

        bool usuarioEhPlayer = usuarioObj.CompareTag(tagCartaPlayer);
        bool usuarioEhInimigo = usuarioObj.CompareTag(tagCartaInimigo);

        if (habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.PropriaCarta)
            return usuarioObj == alvoObj;

        if (habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaInimiga)
        {
            if (usuarioEhPlayer)
                return alvoObj.CompareTag(tagCartaInimigo) && EstaEmSlotComTag(alvoObj.transform, tagSlotTabuleiroInimigo);

            if (usuarioEhInimigo)
                return alvoObj.CompareTag(tagCartaPlayer) && EstaEmSlotComTag(alvoObj.transform, tagSlotTabuleiroPlayer);
        }

        if (habilidade.alvoHabilidade == HabilidadeCarta.AlvoHabilidade.CartaAliada)
        {
            if (usuarioEhPlayer)
                return alvoObj.CompareTag(tagCartaPlayer) && EstaEmSlotComTag(alvoObj.transform, tagSlotTabuleiroPlayer);

            if (usuarioEhInimigo)
                return alvoObj.CompareTag(tagCartaInimigo) && EstaEmSlotComTag(alvoObj.transform, tagSlotTabuleiroInimigo);
        }

        return false;
    }

    private void AplicarDanoDeHabilidade(GameObject usuarioObj, Carta usuario, GameObject alvoObj, Carta alvo, int valor)
    {
        if (DisfarceProtegeContraAtacante(alvoObj, usuarioObj))
        {
            Debug.Log($"HABILIDADE BLOQUEADA PELO DISFARCE -> {alvo.nome} não recebeu dano de {usuario.nome}.");
            return;
        }

        int vidaAntes = Mathf.Max(0, alvo.vida);
        int danoFinal = Mathf.Max(0, valor);
        alvo.vida = Mathf.Max(0, vidaAntes - danoFinal);

        if (feedbackCartasUI != null)
            feedbackCartasUI.MostrarTextoFlutuante(alvoObj, $"-{danoFinal} VIDA", new Color(1f, 0.35f, 0.35f, 1f));

        RegistrarDanoTotalCausado(usuarioObj, danoFinal);

        Debug.Log($"HABILIDADE DE DANO -> {usuario.nome} causou {danoFinal} de dano em {alvo.nome}. Vida antes: {vidaAntes} | Vida depois: {alvo.vida}");

        if (alvo.vida <= 0)
        {
            Debug.Log($"{alvo.nome} foi derrotada pela habilidade e enviada ao cemitério.");
            MoverCartaParaCemiterio(alvoObj);
        }
    }

    private void AplicarBuffDeHabilidade(GameObject usuarioObj, GameObject alvoObj, Carta usuario, Carta alvo, HabilidadeCarta habilidade, int valor)
    {
        if (habilidade.tipoBuff == HabilidadeCarta.TipoBuff.Nenhum)
        {
            Debug.LogWarning($"A habilidade de buff {habilidade.nomeHabilidade} de {usuario.nome} não possui tipo de buff válido.");
            return;
        }

        AplicarBuffTemporario(usuarioObj, alvoObj, usuario, alvo, habilidade.tipoBuff, valor, habilidade.duracaoHabilidadeTurnos);
    }

    private void AplicarBuffTemporario(GameObject usuarioObj, GameObject alvoObj, Carta usuario, Carta alvo, HabilidadeCarta.TipoBuff tipoBuff, int valor, int duracaoTurnos)
    {
        if (usuarioObj == null || alvoObj == null || usuario == null || alvo == null)
            return;

        int duracao = Mathf.Max(1, duracaoTurnos);
        int valorFinal = Mathf.Max(0, valor);

        if (valorFinal <= 0)
            return;

        if (tipoBuff == HabilidadeCarta.TipoBuff.Vida)
        {
            alvo.vida += valorFinal;
            if (feedbackCartasUI != null)
                feedbackCartasUI.MostrarTextoFlutuante(alvoObj, $"+{valorFinal} VIDA", new Color(0.3f, 1f, 0.48f, 1f));
            Debug.Log($"HABILIDADE TEMPORÁRIA -> {usuario.nome} aumentou a vida de {alvo.nome} em {valorFinal} por {duracao} turno(s). Vida atual: {alvo.vida}");
        }
        else if (tipoBuff == HabilidadeCarta.TipoBuff.Dano)
        {
            alvo.dano += valorFinal;
            if (feedbackCartasUI != null)
                feedbackCartasUI.MostrarTextoFlutuante(alvoObj, $"+{valorFinal} ATQ", new Color(1f, 0.72f, 0.25f, 1f));
            Debug.Log($"HABILIDADE TEMPORÁRIA -> {usuario.nome} aumentou o dano de {alvo.nome} em {valorFinal} por {duracao} turno(s). Dano atual: {alvo.dano}");
        }
        else if (tipoBuff == HabilidadeCarta.TipoBuff.Defesa)
        {
            alvo.defesa += valorFinal;
            if (feedbackCartasUI != null)
                feedbackCartasUI.MostrarTextoFlutuante(alvoObj, $"+{valorFinal} DEF", new Color(0.35f, 0.78f, 1f, 1f));
            Debug.Log($"HABILIDADE TEMPORÁRIA -> {usuario.nome} aumentou a defesa de {alvo.nome} em {valorFinal} por {duracao} turno(s). Defesa atual: {alvo.defesa}");
        }
        else
        {
            Debug.LogWarning($"Tipo de buff inválido na habilidade de {usuario.nome}.");
            return;
        }

        BuffTemporario buff = new BuffTemporario
        {
            cartaObj = alvoObj,
            carta = alvo,
            tipoBuff = tipoBuff,
            valor = valorFinal,
            turnosRestantes = duracao,
            pertenceAoPlayer = alvoObj.CompareTag(tagCartaPlayer),
            ignorarPrimeiraChecagem = true
        };

        buffsTemporariosAtivos.Add(buff);
    }



    public void BotaoAbrirHabilidadeConjuntoCartaSelecionada()
    {
        if (uiCombateCarta == null || uiCombateCarta.cartaSelecionada == null)
            return;

        GameObject cartaObj = uiCombateCarta.cartaSelecionada;

        if (!PlayerPossuiAcaoDisponivel())
        {
            MostrarSemAcoesPlayer();
            return;
        }

        if (CartaPlayerAindaEstaEmPreparacao(cartaObj))
        {
            MostrarCartaRecemColocadaPlayer();
            return;
        }

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null)
            return;

        for (int i = 0; i < carta.habilidades.Count; i++)
        {
            HabilidadeCarta habilidade = carta.habilidades[i];
            if (habilidade == null || !habilidade.EstaConfiguradaComoConjunto())
                continue;

            if (!PodeUsarHabilidade(cartaObj, habilidade, i, true))
                continue;

            habilidadePlayerSelecionada = habilidade;
            indiceHabilidadePlayerSelecionada = i;
            cartaPlayerSelecionadaParaHabilidade = cartaObj;

            string textoEstado = ObterTextoEstadoHabilidade(cartaObj, habilidade, i);
            uiCombateCarta.AbrirPainelConfirmacaoHabilidade(habilidade, textoEstado);
            return;
        }

        Debug.Log("Nenhuma habilidade em conjunto disponível para essa carta agora.");
    }

    public bool CartaPossuiHabilidadeConjuntoDisponivel(GameObject cartaObj)
    {
        if (cartaObj == null)
            return false;

        if (!PlayerPossuiAcaoDisponivel() || CartaPlayerAindaEstaEmPreparacao(cartaObj))
            return false;

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null)
            return false;

        for (int i = 0; i < carta.habilidades.Count; i++)
        {
            HabilidadeCarta habilidade = carta.habilidades[i];
            if (habilidade != null && habilidade.EstaConfiguradaComoConjunto() && PodeUsarHabilidade(cartaObj, habilidade, i, false))
                return true;
        }

        return false;
    }

    public string ObterNomePrimeiraHabilidadeConjuntoDisponivel(GameObject cartaObj)
    {
        if (cartaObj == null)
            return "Habilidade em Conjunto";

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null)
            return "Habilidade em Conjunto";

        for (int i = 0; i < carta.habilidades.Count; i++)
        {
            HabilidadeCarta habilidade = carta.habilidades[i];
            if (habilidade != null && habilidade.EstaConfiguradaComoConjunto() && PodeUsarHabilidade(cartaObj, habilidade, i, false))
            {
                if (!string.IsNullOrEmpty(habilidade.nomeBotaoConjunto))
                    return habilidade.nomeBotaoConjunto;

                if (!string.IsNullOrEmpty(habilidade.nomeHabilidade))
                    return habilidade.nomeHabilidade;

                return "Habilidade em Conjunto";
            }
        }

        return "Habilidade em Conjunto";
    }

    private bool CartaEstaParalisada(GameObject cartaObj)
    {
        Carta carta = cartaObj != null ? cartaObj.GetComponent<Carta>() : null;
        return carta != null && carta.efeitoSobrecargaAtivo && carta.turnosSobrecargaRestantes > 0;
    }

    private bool SaoOponentes(GameObject a, GameObject b)
    {
        if (a == null || b == null)
            return false;

        return (a.CompareTag(tagCartaPlayer) && b.CompareTag(tagCartaInimigo)) ||
               (a.CompareTag(tagCartaInimigo) && b.CompareTag(tagCartaPlayer));
    }

    private bool DisfarceProtegeContraAtacante(GameObject alvoObj, GameObject atacanteObj)
    {
        Carta alvo = alvoObj != null ? alvoObj.GetComponent<Carta>() : null;
        if (alvo == null || !alvo.disfarceAtivo)
            return false;

        return SaoOponentes(alvoObj, atacanteObj);
    }

    private void RemoverDisfarceSeAtacouOponente(GameObject atacanteObj, GameObject alvoObj)
    {
        Carta atacante = atacanteObj != null ? atacanteObj.GetComponent<Carta>() : null;
        if (atacante == null || !atacante.disfarceAtivo)
            return;

        if (!SaoOponentes(atacanteObj, alvoObj))
            return;

        SpriteRenderer spriteRenderer = atacanteObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && atacante.spriteOriginalAntesDoDisfarce != null)
            spriteRenderer.sprite = atacante.spriteOriginalAntesDoDisfarce;

        atacante.disfarceAtivo = false;
        atacante.spriteOriginalAntesDoDisfarce = null;

        Debug.Log($"DISFARCE ENCERRADO -> {atacante.nome} atacou e voltou para a aparência original.");
    }

    private void AplicarDisfarce(GameObject usuarioObj, GameObject alvoObj, Carta usuario, Carta alvo)
    {
        if (usuarioObj == null || alvoObj == null || usuario == null || alvo == null)
            return;

        SpriteRenderer spriteUsuario = usuarioObj.GetComponent<SpriteRenderer>();
        SpriteRenderer spriteAlvo = alvoObj.GetComponent<SpriteRenderer>();

        if (spriteUsuario == null || spriteAlvo == null || spriteAlvo.sprite == null)
        {
            Debug.LogWarning("Não foi possível aplicar Disfarce porque uma das cartas não possui SpriteRenderer ou sprite.");
            return;
        }

        if (!usuario.disfarceAtivo)
            usuario.spriteOriginalAntesDoDisfarce = spriteUsuario.sprite;

        spriteUsuario.sprite = spriteAlvo.sprite;
        usuario.disfarceAtivo = true;

        Debug.Log($"DISFARCE -> {usuario.nome} copiou a aparência de {alvo.nome}. Enquanto não atacar, não poderá receber dano de cartas oponentes.");
    }

    private void AnularEfeitosNegativos(GameObject alvoObj, Carta usuario, Carta alvo)
    {
        if (alvoObj == null || alvo == null)
            return;

        alvo.efeitoSobrecargaAtivo = false;
        alvo.turnosSobrecargaRestantes = 0;

        alvo.efeitoFogoAtivo = false;
        alvo.turnosFogoRestantes = 0;
        alvo.danoFogoPorTurno = 0;

        alvo.efeitoSangramentoAtivo = false;
        alvo.turnosSangramentoRestantes = 0;
        alvo.danoSangramentoPorTurno = 0;

        if (feedbackCartasUI != null)
            feedbackCartasUI.MostrarTextoFlutuante(alvoObj, "EFEITOS REMOVIDOS", new Color(0.4f, 0.9f, 1f, 1f));

        Debug.Log($"ANULAÇÃO -> {usuario.nome} removeu efeitos negativos de {alvo.nome}.");
    }

    private void AplicarEfeitosDaHabilidade(GameObject usuarioObj, GameObject alvoObj, HabilidadeCarta habilidade)
    {
        if (usuarioObj == null || alvoObj == null || habilidade == null || habilidade.efeitos == null)
            return;

        Carta usuario = usuarioObj.GetComponent<Carta>();
        Carta alvo = alvoObj.GetComponent<Carta>();

        if (usuario == null || alvo == null)
            return;

        for (int i = 0; i < habilidade.efeitos.Count; i++)
        {
            EfeitoHabilidade efeito = habilidade.efeitos[i];
            if (efeito == null)
                continue;

            float rolagem = Random.Range(0f, 100f);
            if (rolagem > efeito.chanceAplicar)
            {
                Debug.Log($"EFEITO FALHOU -> {efeito.tipoEfeito} não foi aplicado em {alvo.nome}. Chance: {efeito.chanceAplicar}% | Rolagem: {rolagem:F1}");
                continue;
            }

            if (efeito.tipoEfeito == EfeitoHabilidade.TipoEfeito.Sobrecarga)
            {
                alvo.efeitoSobrecargaAtivo = true;
                alvo.turnosSobrecargaRestantes = Mathf.Max(1, efeito.duracaoTurnos);
                if (feedbackCartasUI != null)
                    feedbackCartasUI.MostrarTextoFlutuante(alvoObj, "SOBRECARGA", new Color(1f, 0.9f, 0.35f, 1f));
                MoverCartaParaDeckPorEfeito(alvoObj);
                Debug.Log($"SOBRECARGA -> {alvo.nome} ficou paralisada por {alvo.turnosSobrecargaRestantes} turno(s) e voltou para o deck.");
            }
            else if (efeito.tipoEfeito == EfeitoHabilidade.TipoEfeito.Fogo)
            {
                alvo.efeitoFogoAtivo = true;
                alvo.turnosFogoRestantes = Mathf.Max(1, efeito.duracaoTurnos);
                alvo.danoFogoPorTurno = Mathf.Max(0, efeito.danoPorTurno);
                if (feedbackCartasUI != null)
                    feedbackCartasUI.MostrarTextoFlutuante(alvoObj, "FOGO", new Color(1f, 0.5f, 0.2f, 1f));
                Debug.Log($"FOGO -> {alvo.nome} está pegando fogo por {alvo.turnosFogoRestantes} turno(s). Dano por turno: {alvo.danoFogoPorTurno}.");
            }
            else if (efeito.tipoEfeito == EfeitoHabilidade.TipoEfeito.Sangramento)
            {
                alvo.efeitoSangramentoAtivo = true;
                alvo.turnosSangramentoRestantes = Mathf.Max(1, efeito.duracaoTurnos);
                alvo.danoSangramentoPorTurno = Mathf.Max(0, efeito.danoPorTurno);
                if (feedbackCartasUI != null)
                    feedbackCartasUI.MostrarTextoFlutuante(alvoObj, "SANG.", new Color(1f, 0.25f, 0.38f, 1f));
                Debug.Log($"SANGRAMENTO -> {alvo.nome} está sangrando por {alvo.turnosSangramentoRestantes} turno(s). Dano por turno: {alvo.danoSangramentoPorTurno}.");
            }
        }
    }

    private void MoverCartaParaDeckPorEfeito(GameObject cartaObj)
    {
        if (cartaObj == null)
            return;

        string tagDeck = cartaObj.CompareTag(tagCartaPlayer) ? tagSlotDeckPlayer : tagSlotDeckInimigo;
        Transform slotLivre = EncontrarSlotLivre(tagDeck);

        if (slotLivre == null)
        {
            Debug.LogWarning($"Não há slot livre no deck para mover {cartaObj.name} por efeito.");
            return;
        }

        Vector3 escala = cartaObj.transform.localScale;
        cartaObj.transform.SetParent(slotLivre);
        cartaObj.transform.position = slotLivre.position;
        cartaObj.transform.localScale = escala;

        AtualizarListasDeCartas();
    }

    private void ProcessarEfeitosNoInicioDoTurno(bool turnoDoPlayerIniciando)
    {
        List<GameObject> lista = turnoDoPlayerIniciando ? cartasPlayerNoTabuleiro : cartasInimigoNoTabuleiro;
        List<GameObject> listaDeck = turnoDoPlayerIniciando ? cartasPlayerNoDeck : cartasInimigoNoDeck;
        List<GameObject> todas = new List<GameObject>();
        todas.AddRange(lista);
        todas.AddRange(listaDeck);

        for (int i = todas.Count - 1; i >= 0; i--)
        {
            GameObject cartaObj = todas[i];
            Carta carta = cartaObj != null ? cartaObj.GetComponent<Carta>() : null;
            if (carta == null)
                continue;

            if (carta.efeitoFogoAtivo && carta.turnosFogoRestantes > 0)
            {
                AplicarDanoDeEfeitoContinuo(cartaObj, carta, carta.danoFogoPorTurno, "FOGO");
                carta.turnosFogoRestantes--;
                if (carta.turnosFogoRestantes <= 0)
                {
                    carta.efeitoFogoAtivo = false;
                    carta.danoFogoPorTurno = 0;
                }
            }

            if (carta.efeitoSangramentoAtivo && carta.turnosSangramentoRestantes > 0)
            {
                AplicarDanoDeEfeitoContinuo(cartaObj, carta, carta.danoSangramentoPorTurno, "SANGRAMENTO");
                carta.turnosSangramentoRestantes--;
                if (carta.turnosSangramentoRestantes <= 0)
                {
                    carta.efeitoSangramentoAtivo = false;
                    carta.danoSangramentoPorTurno = 0;
                }
            }

            if (carta.efeitoSobrecargaAtivo && carta.turnosSobrecargaRestantes > 0)
            {
                carta.turnosSobrecargaRestantes--;
                if (carta.turnosSobrecargaRestantes <= 0)
                {
                    carta.efeitoSobrecargaAtivo = false;
                    Debug.Log($"SOBRECARGA ENCERRADA -> {carta.nome} pode agir novamente.");
                }
            }
        }
    }

    private void AplicarDanoDeEfeitoContinuo(GameObject cartaObj, Carta carta, int dano, string nomeEfeito)
    {
        if (cartaObj == null || carta == null || dano <= 0)
            return;

        int vidaAntes = carta.vida;
        carta.vida = Mathf.Max(0, carta.vida - dano);

        if (feedbackCartasUI != null)
        {
            Color cor = nomeEfeito == "FOGO" ? new Color(1f, 0.48f, 0.2f, 1f) : new Color(1f, 0.25f, 0.38f, 1f);
            feedbackCartasUI.MostrarTextoFlutuante(cartaObj, $"-{dano} {nomeEfeito}", cor);
        }

        Debug.Log($"{nomeEfeito} -> {carta.nome} recebeu {dano} de dano. Vida antes: {vidaAntes} | Vida depois: {carta.vida}");

        if (carta.vida <= 0)
        {
            Debug.Log($"{carta.nome} foi derrotada por {nomeEfeito} e enviada ao cemitério.");
            MoverCartaParaCemiterio(cartaObj);
        }
    }

    private bool CartaNecessariaDeConjuntoEstaNoTabuleiro(GameObject cartaUsuario, HabilidadeCarta habilidade)
    {
        if (cartaUsuario == null || habilidade == null || !habilidade.ativacaoEmConjunto)
            return true;

        if (habilidade.cartaNecessariaNoTabuleiro == null)
            return false;

        string nomeNecessario = habilidade.cartaNecessariaNoTabuleiro.nome;
        List<GameObject> lista = cartaUsuario.CompareTag(tagCartaPlayer) ? cartasPlayerNoTabuleiro : cartasInimigoNoTabuleiro;

        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i] == null || lista[i] == cartaUsuario)
                continue;

            Carta carta = lista[i].GetComponent<Carta>();
            if (carta != null && carta.nome == nomeNecessario)
                return true;
        }

        return false;
    }

    private string ObterTextoEstadoHabilidade(GameObject cartaObj, HabilidadeCarta habilidade, int indiceHabilidade)
    {
        if (cartaObj == null || habilidade == null)
            return "Habilidade inválida.";

        string texto = "";

        if (habilidade.habilidadeEspecial)
        {
            texto += "Habilidade especial\n";

            if (habilidade.exigirSacrificioCartas)
                texto += $"Condição/Custo: sacrificar {habilidade.quantidadeCartasParaSacrificar} carta(s) aliada(s).\n";

            if (habilidade.exigirDanoTotalCausado)
                texto += $"Condição: causar {habilidade.danoTotalNecessario} de dano total. Atual: {ObterDanoTotalCausado(cartaObj)}.\n";

            if (habilidade.exigirVidaMenorOuIgual)
            {
                Carta carta = cartaObj.GetComponent<Carta>();
                int vidaAtual = carta != null ? carta.vida : 0;
                texto += $"Condição: vida menor ou igual a {habilidade.vidaNecessariaMenorOuIgual}. Atual: {vidaAtual}.\n";
            }

            if (habilidade.usarCooldownEspecial)
            {
                int cooldownAtual = ObterCooldownAtual(cartaObj, indiceHabilidade);
                texto += $"Cooldown especial: {habilidade.cooldownEspecialTurnos} turno(s). Atual: {cooldownAtual}.\n";
            }

            if (habilidade.ativacaoEmConjunto)
            {
                string nomeCarta = habilidade.cartaNecessariaNoTabuleiro != null ? habilidade.cartaNecessariaNoTabuleiro.nome : "não definida";
                texto += $"Conjunto: precisa da carta {nomeCarta} no tabuleiro aliado.\n";
            }
        }
        else if (habilidade.usarCooldown)
        {
            int cooldownAtual = ObterCooldownAtual(cartaObj, indiceHabilidade);
            texto += $"Cooldown: {habilidade.cooldownTurnos} turno(s). Atual: {cooldownAtual}.\n";
        }

        if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Dano)
            texto += $"Efeito base: causa {habilidade.valorHabilidade} de dano.\n";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Defesa)
            texto += $"Efeito base: aumenta {habilidade.valorHabilidade} de defesa por {habilidade.duracaoHabilidadeTurnos} turno(s).\n";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Buff)
            texto += $"Efeito base: aumenta {habilidade.valorHabilidade} de {habilidade.tipoBuff} por {habilidade.duracaoHabilidadeTurnos} turno(s).\n";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Anulacao)
            texto += "Efeito base: remove Sobrecarga, Fogo e Sangramento da carta aliada.\n";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Disfarce)
            texto += "Efeito base: copia o sprite da carta escolhida e evita dano de cartas oponentes até atacar.\n";

        if (habilidade.efeitos != null && habilidade.efeitos.Count > 0)
        {
            texto += "Efeitos adicionais:\n";
            for (int i = 0; i < habilidade.efeitos.Count; i++)
            {
                EfeitoHabilidade efeito = habilidade.efeitos[i];
                if (efeito == null)
                    continue;

                texto += $"- {efeito.tipoEfeito} | Chance: {efeito.chanceAplicar}% | Duração: {efeito.duracaoTurnos} turno(s) | Dano/turno: {efeito.danoPorTurno}\n";
            }
        }

        return texto;
    }

    private bool PodeUsarHabilidade(GameObject cartaObj, HabilidadeCarta habilidade, int indiceHabilidade, bool mostrarAviso)
    {
        if (cartaObj == null || habilidade == null)
            return false;

        if (!habilidade.EstaConfigurada() && !habilidade.EstaConfiguradaComoConjunto())
            return false;

        if (CartaEstaParalisada(cartaObj))
        {
            if (mostrarAviso)
            {
                Debug.Log("Essa carta está com Sobrecarga e não pode usar habilidade agora.");
                Notificar("SOBRECARGA • habilidade bloqueada.");
            }

            return false;
        }

        int cooldownAtual = ObterCooldownAtual(cartaObj, indiceHabilidade);

        if (!habilidade.habilidadeEspecial && habilidade.usarCooldown && cooldownAtual > 0)
        {
            if (mostrarAviso)
            {
                Debug.Log($"A habilidade {habilidade.nomeHabilidade} ainda está em cooldown por {cooldownAtual} turno(s).");
                Notificar($"COOLDOWN • {cooldownAtual} turno(s).");
            }

            return false;
        }

        if (habilidade.habilidadeEspecial && habilidade.usarCooldownEspecial && cooldownAtual > 0)
        {
            if (mostrarAviso)
            {
                Debug.Log($"A habilidade especial {habilidade.nomeHabilidade} ainda está em cooldown por {cooldownAtual} turno(s).");
                Notificar($"COOLDOWN ESPECIAL • {cooldownAtual} turno(s).");
            }

            return false;
        }

        if (!habilidade.habilidadeEspecial && !habilidade.ativacaoEmConjunto)
            return true;

        if (habilidade.ativacaoEmConjunto && !CartaNecessariaDeConjuntoEstaNoTabuleiro(cartaObj, habilidade))
        {
            if (mostrarAviso)
            {
                Debug.Log($"A habilidade em conjunto precisa da carta de conjunto no tabuleiro.");
                Notificar("CONJUNTO • carta necessária não está no campo.");
            }

            return false;
        }

        if (!habilidade.habilidadeEspecial)
            return true;

        if (habilidade.exigirSacrificioCartas)
        {
            int cartasDisponiveis = ContarCartasAliadasSacrificaveis(cartaObj);
            bool pode = cartasDisponiveis >= habilidade.quantidadeCartasParaSacrificar;

            if (!pode && mostrarAviso)
                Debug.Log($"Não há cartas aliadas suficientes para sacrificar. Necessário: {habilidade.quantidadeCartasParaSacrificar} | Disponível: {cartasDisponiveis}");

            if (!pode)
                return false;
        }

        if (habilidade.exigirDanoTotalCausado)
        {
            int danoAtual = ObterDanoTotalCausado(cartaObj);
            bool pode = danoAtual >= habilidade.danoTotalNecessario;

            if (!pode && mostrarAviso)
                Debug.Log($"Dano total insuficiente para usar {habilidade.nomeHabilidade}. Necessário: {habilidade.danoTotalNecessario} | Atual: {danoAtual}");

            if (!pode)
                return false;
        }

        if (habilidade.exigirVidaMenorOuIgual)
        {
            Carta carta = cartaObj.GetComponent<Carta>();
            bool pode = carta != null && carta.vida <= habilidade.vidaNecessariaMenorOuIgual;

            if (!pode && mostrarAviso)
                Debug.Log($"A vida da carta ainda não está baixa o suficiente para usar {habilidade.nomeHabilidade}.");

            if (!pode)
                return false;
        }

        return true;
    }

    private bool PagarCustoEspecialSeNecessario(GameObject cartaObj, HabilidadeCarta habilidade, bool usuarioEhPlayer)
    {
        if (cartaObj == null || habilidade == null)
            return false;

        if (!habilidade.habilidadeEspecial)
            return true;

        if (habilidade.exigirSacrificioCartas)
            return SacrificarCartasAliadas(cartaObj, habilidade.quantidadeCartasParaSacrificar);

        return true;
    }

    private void RegistrarUsoHabilidade(GameObject cartaObj, HabilidadeCarta habilidade, int indiceHabilidade)
    {
        if (cartaObj == null || habilidade == null)
            return;

        int cooldownParaAplicar = 0;

        if (habilidade.habilidadeEspecial)
        {
            if (habilidade.usarCooldownEspecial && habilidade.cooldownEspecialTurnos > 0)
                cooldownParaAplicar = habilidade.cooldownEspecialTurnos;
        }
        else
        {
            if (habilidade.usarCooldown && habilidade.cooldownTurnos > 0)
                cooldownParaAplicar = habilidade.cooldownTurnos;
        }

        if (cooldownParaAplicar <= 0)
            return;

        if (!cooldownsHabilidades.ContainsKey(cartaObj))
            cooldownsHabilidades.Add(cartaObj, new Dictionary<int, int>());

        cooldownsHabilidades[cartaObj][indiceHabilidade] = cooldownParaAplicar;
    }

    private int ObterCooldownAtual(GameObject cartaObj, int indiceHabilidade)
    {
        if (cartaObj == null)
            return 0;

        if (!cooldownsHabilidades.ContainsKey(cartaObj))
            return 0;

        if (!cooldownsHabilidades[cartaObj].ContainsKey(indiceHabilidade))
            return 0;

        return Mathf.Max(0, cooldownsHabilidades[cartaObj][indiceHabilidade]);
    }

    private void ProcessarCooldownsHabilidadesNoInicioDoTurno(bool turnoDoPlayerIniciando)
    {
        List<GameObject> cartasParaProcessar = turnoDoPlayerIniciando ? cartasPlayerNoTabuleiro : cartasInimigoNoTabuleiro;

        for (int i = 0; i < cartasParaProcessar.Count; i++)
        {
            GameObject cartaObj = cartasParaProcessar[i];
            if (cartaObj == null || !cooldownsHabilidades.ContainsKey(cartaObj))
                continue;

            List<int> indices = new List<int>(cooldownsHabilidades[cartaObj].Keys);

            for (int j = 0; j < indices.Count; j++)
            {
                int indice = indices[j];
                cooldownsHabilidades[cartaObj][indice]--;

                if (cooldownsHabilidades[cartaObj][indice] <= 0)
                    cooldownsHabilidades[cartaObj].Remove(indice);
            }
        }
    }

    private void RegistrarDanoTotalCausado(GameObject cartaObj, int dano)
    {
        if (cartaObj == null || dano <= 0)
            return;

        if (!danoTotalCausadoPorCarta.ContainsKey(cartaObj))
            danoTotalCausadoPorCarta.Add(cartaObj, 0);

        danoTotalCausadoPorCarta[cartaObj] += dano;
    }

    private int ObterDanoTotalCausado(GameObject cartaObj)
    {
        if (cartaObj == null || !danoTotalCausadoPorCarta.ContainsKey(cartaObj))
            return 0;

        return danoTotalCausadoPorCarta[cartaObj];
    }

    private int ContarCartasAliadasSacrificaveis(GameObject cartaUsuario)
    {
        List<GameObject> lista = cartaUsuario != null && cartaUsuario.CompareTag(tagCartaPlayer) ? cartasPlayerNoTabuleiro : cartasInimigoNoTabuleiro;
        int quantidade = 0;

        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i] != null && lista[i] != cartaUsuario)
                quantidade++;
        }

        return quantidade;
    }

    private bool SacrificarCartasAliadas(GameObject cartaUsuario, int quantidade)
    {
        if (quantidade <= 0)
            return true;

        List<GameObject> lista = cartaUsuario != null && cartaUsuario.CompareTag(tagCartaPlayer) ? cartasPlayerNoTabuleiro : cartasInimigoNoTabuleiro;
        List<GameObject> sacrificaveis = new List<GameObject>();

        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i] != null && lista[i] != cartaUsuario)
                sacrificaveis.Add(lista[i]);
        }

        if (sacrificaveis.Count < quantidade)
            return false;

        sacrificaveis.Sort((a, b) => PontuacaoCartaParaSacrificio(a).CompareTo(PontuacaoCartaParaSacrificio(b)));

        for (int i = 0; i < quantidade; i++)
        {
            MoverCartaParaCemiterio(sacrificaveis[i]);
        }

        AtualizarListasDeCartas();
        return true;
    }

    private int PontuacaoCartaParaSacrificio(GameObject cartaObj)
    {
        Carta carta = cartaObj != null ? cartaObj.GetComponent<Carta>() : null;
        if (carta == null)
            return int.MaxValue;

        return carta.dano + carta.vida + carta.defesa;
    }

    private void ProcessarBuffsTemporariosNoInicioDoTurno(bool turnoDoPlayerIniciando)
    {
        for (int i = buffsTemporariosAtivos.Count - 1; i >= 0; i--)
        {
            BuffTemporario buff = buffsTemporariosAtivos[i];

            if (buff == null || buff.cartaObj == null || buff.carta == null)
            {
                buffsTemporariosAtivos.RemoveAt(i);
                continue;
            }

            if (buff.pertenceAoPlayer != turnoDoPlayerIniciando)
                continue;

            if (buff.ignorarPrimeiraChecagem)
            {
                buff.ignorarPrimeiraChecagem = false;
                continue;
            }

            buff.turnosRestantes--;

            if (buff.turnosRestantes <= 0)
            {
                RemoverBuffTemporario(buff);
                buffsTemporariosAtivos.RemoveAt(i);
            }
        }
    }

    private void RemoverBuffTemporario(BuffTemporario buff)
    {
        if (buff == null || buff.carta == null)
            return;

        if (buff.tipoBuff == HabilidadeCarta.TipoBuff.Vida)
        {
            buff.carta.vida -= buff.valor;
            if (buff.carta.vida < 0)
                buff.carta.vida = 0;
        }
        else if (buff.tipoBuff == HabilidadeCarta.TipoBuff.Dano)
        {
            buff.carta.dano -= buff.valor;
            if (buff.carta.dano < 0)
                buff.carta.dano = 0;
        }
        else if (buff.tipoBuff == HabilidadeCarta.TipoBuff.Defesa)
        {
            buff.carta.defesa -= buff.valor;
            if (buff.carta.defesa < 0)
                buff.carta.defesa = 0;
        }

        if (feedbackCartasUI != null && buff.cartaObj != null)
        {
            string sigla = buff.tipoBuff == HabilidadeCarta.TipoBuff.Dano ? "ATQ" :
                (buff.tipoBuff == HabilidadeCarta.TipoBuff.Defesa ? "DEF" : "VIDA");
            feedbackCartasUI.MostrarTextoFlutuante(buff.cartaObj, $"-{buff.valor} {sigla}", new Color(0.78f, 0.8f, 0.86f, 1f));
        }

        Debug.Log($"BUFF ENCERRADO -> O bônus de {buff.valor} em {buff.tipoBuff} acabou para {buff.carta.nome}.");
    }

    private void RemoverTodosBuffsDaCarta(GameObject cartaObj)
    {
        if (cartaObj == null)
            return;

        for (int i = buffsTemporariosAtivos.Count - 1; i >= 0; i--)
        {
            BuffTemporario buff = buffsTemporariosAtivos[i];

            if (buff == null || buff.cartaObj == cartaObj)
            {
                buffsTemporariosAtivos.RemoveAt(i);
            }
        }
    }

    private void ReproduzirEfeitoVisualHabilidadeEspecial(GameObject usuarioObj, GameObject alvoObj, HabilidadeCarta habilidade)
    {
        if (usuarioObj == null || alvoObj == null || habilidade == null)
            return;

        // O sistema visual desta secao e exclusivo para habilidades marcadas como especiais.
        if (!habilidade.habilidadeEspecial)
            return;

        Sprite[] frames = habilidade.framesEfeitoHabilidadeEspecial;
        if (frames == null || frames.Length == 0)
            return;

        GameObject efeitoObj = new GameObject($"EfeitoHabilidadeEspecial_{habilidade.nomeHabilidade}");
        efeitoObj.layer = alvoObj.layer;

        Vector3 posicaoAlvo = alvoObj.transform.position;
        Vector2 deslocamento = habilidade.deslocamentoEfeitoHabilidadeEspecial;

        efeitoObj.transform.position = new Vector3(
            posicaoAlvo.x + deslocamento.x,
            posicaoAlvo.y + deslocamento.y,
            posicaoAlvo.z
        );

        efeitoObj.transform.rotation = Quaternion.Euler(0f, 0f, habilidade.rotacaoEfeitoHabilidadeEspecial);

        Vector2 escala = habilidade.escalaEfeitoHabilidadeEspecial;
        efeitoObj.transform.localScale = new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(escala.x)),
            Mathf.Max(0.0001f, Mathf.Abs(escala.y)),
            1f
        );

        SpriteRenderer rendererEfeito = efeitoObj.AddComponent<SpriteRenderer>();
        rendererEfeito.flipX = habilidade.inverterEfeitoHabilidadeEspecialHorizontal;
        rendererEfeito.flipY = habilidade.inverterEfeitoHabilidadeEspecialVertical;

        SpriteRenderer rendererAlvo = alvoObj.GetComponent<SpriteRenderer>();
        if (rendererAlvo != null)
        {
            rendererEfeito.sortingLayerID = rendererAlvo.sortingLayerID;
            rendererEfeito.sortingOrder = rendererAlvo.sortingOrder + ordemRenderizacaoAcimaDaCarta;
        }

        StartCoroutine(AnimarEfeitoVisualHabilidadeEspecial(
            efeitoObj,
            rendererEfeito,
            frames,
            Mathf.Max(0.01f, habilidade.tempoEntreFramesEfeitoHabilidadeEspecial)
        ));
    }

    private IEnumerator AnimarEfeitoVisualHabilidadeEspecial(GameObject efeitoObj, SpriteRenderer rendererEfeito, Sprite[] frames, float tempoEntreFrames)
    {
        if (efeitoObj == null || rendererEfeito == null || frames == null || frames.Length == 0)
        {
            if (efeitoObj != null)
                Destroy(efeitoObj);

            yield break;
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (efeitoObj == null || rendererEfeito == null)
                yield break;

            if (frames[i] != null)
                rendererEfeito.sprite = frames[i];

            yield return new WaitForSeconds(tempoEntreFrames);
        }

        if (efeitoObj != null)
            Destroy(efeitoObj);
    }

    private void ReproduzirEfeitoVisualAtaque(GameObject atacanteObj, GameObject alvoObj)
    {
        if (atacanteObj == null || alvoObj == null)
            return;

        Carta cartaAtacante = atacanteObj.GetComponent<Carta>();
        if (cartaAtacante == null)
            return;

        bool possuiEfeitoPersonalizado = cartaAtacante.framesEfeitoAtaque != null && cartaAtacante.framesEfeitoAtaque.Length > 0;

        Sprite[] frames = possuiEfeitoPersonalizado
            ? cartaAtacante.framesEfeitoAtaque
            : framesEfeitoAtaquePadrao;

        if (frames == null || frames.Length == 0)
            return;

        float tempoEntreFrames = possuiEfeitoPersonalizado
            ? cartaAtacante.tempoEntreFramesEfeitoAtaque
            : tempoEntreFramesEfeitoAtaquePadrao;

        Vector2 deslocamento = possuiEfeitoPersonalizado
            ? cartaAtacante.deslocamentoEfeitoAtaque
            : deslocamentoEfeitoAtaquePadrao;

        Vector2 escala = possuiEfeitoPersonalizado
            ? cartaAtacante.escalaEfeitoAtaque
            : escalaEfeitoAtaquePadrao;

        float rotacao = possuiEfeitoPersonalizado
            ? cartaAtacante.rotacaoEfeitoAtaque
            : rotacaoEfeitoAtaquePadrao;

        bool inverterHorizontal = possuiEfeitoPersonalizado && cartaAtacante.inverterEfeitoAtaqueHorizontal;
        bool inverterVertical = possuiEfeitoPersonalizado && cartaAtacante.inverterEfeitoAtaqueVertical;

        GameObject efeitoObj = new GameObject($"EfeitoAtaque_{cartaAtacante.nome}");
        efeitoObj.layer = alvoObj.layer;

        Vector3 posicaoAlvo = alvoObj.transform.position;
        efeitoObj.transform.position = new Vector3(
            posicaoAlvo.x + deslocamento.x,
            posicaoAlvo.y + deslocamento.y,
            posicaoAlvo.z
        );

        efeitoObj.transform.rotation = Quaternion.Euler(0f, 0f, rotacao);
        efeitoObj.transform.localScale = new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(escala.x)),
            Mathf.Max(0.0001f, Mathf.Abs(escala.y)),
            1f
        );

        SpriteRenderer rendererEfeito = efeitoObj.AddComponent<SpriteRenderer>();
        rendererEfeito.flipX = inverterHorizontal;
        rendererEfeito.flipY = inverterVertical;

        SpriteRenderer rendererAlvo = alvoObj.GetComponent<SpriteRenderer>();
        if (rendererAlvo != null)
        {
            rendererEfeito.sortingLayerID = rendererAlvo.sortingLayerID;
            rendererEfeito.sortingOrder = rendererAlvo.sortingOrder + ordemRenderizacaoAcimaDaCarta;
        }

        StartCoroutine(AnimarEfeitoVisualAtaque(
            efeitoObj,
            rendererEfeito,
            frames,
            Mathf.Max(0.01f, tempoEntreFrames)
        ));
    }

    private IEnumerator AnimarEfeitoVisualAtaque(GameObject efeitoObj, SpriteRenderer rendererEfeito, Sprite[] frames, float tempoEntreFrames)
    {
        if (efeitoObj == null || rendererEfeito == null || frames == null || frames.Length == 0)
        {
            if (efeitoObj != null)
                Destroy(efeitoObj);

            yield break;
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (efeitoObj == null || rendererEfeito == null)
                yield break;

            if (frames[i] != null)
                rendererEfeito.sprite = frames[i];

            yield return new WaitForSeconds(tempoEntreFrames);
        }

        if (efeitoObj != null)
            Destroy(efeitoObj);
    }

    private int CalcularDanoFinal(int danoAtacante, int defesaAlvo)
    {
        int danoBase = Mathf.Max(0, danoAtacante);
        int defesaBase = Mathf.Max(0, defesaAlvo);

        int danoFinal = danoBase - defesaBase;

        if (danoFinal < 0)
            danoFinal = 0;

        return danoFinal;
    }

    private void AplicarAtaque(GameObject atacanteObj, GameObject alvoObj)
    {
        if (combateFinalizado)
            return;

        if (atacanteObj == null || alvoObj == null)
        {
            Debug.LogWarning("Ataque cancelado: atacante ou alvo nulo.");
            return;
        }

        if (!CartaPodeReceberDano(atacanteObj))
        {
            Debug.LogWarning("Ataque cancelado: atacante inválido.");
            return;
        }

        if (CartaEstaParalisada(atacanteObj))
        {
            Debug.LogWarning("Ataque cancelado: a carta está com Sobrecarga e não pode atacar.");
            return;
        }

        if (!CartaPodeReceberDano(alvoObj))
        {
            Debug.LogWarning("Ataque cancelado: alvo inválido.");
            return;
        }

        Carta atacante = atacanteObj.GetComponent<Carta>();
        Carta alvo = alvoObj.GetComponent<Carta>();

        if (atacante == null || alvo == null)
        {
            Debug.LogWarning("Ataque cancelado: componente Carta não encontrado.");
            return;
        }

        if (DisfarceProtegeContraAtacante(alvoObj, atacanteObj))
        {
            Debug.Log($"ATAQUE BLOQUEADO PELO DISFARCE -> {alvo.nome} não recebeu dano de {atacante.nome}.");
            return;
        }

        ReproduzirEfeitoVisualAtaque(atacanteObj, alvoObj);

        RemoverDisfarceSeAtacouOponente(atacanteObj, alvoObj);

        int danoAtacante = Mathf.Max(0, atacante.dano);
        int defesaAlvo = Mathf.Max(0, alvo.defesa);
        int vidaAntes = Mathf.Max(0, alvo.vida);

        int danoFinal = CalcularDanoFinal(danoAtacante, defesaAlvo);

        alvo.vida -= danoFinal;
        if (feedbackCartasUI != null)
            feedbackCartasUI.MostrarTextoFlutuante(alvoObj, $"-{danoFinal} VIDA", new Color(1f, 0.35f, 0.35f, 1f));
        RegistrarDanoTotalCausado(atacanteObj, danoFinal);

        if (alvo.vida < 0)
            alvo.vida = 0;

        Debug.Log(
            $"ATAQUE -> {atacante.nome} atacou {alvo.nome} | " +
            $"Dano do atacante: {danoAtacante} | " +
            $"Defesa do alvo: {defesaAlvo} | " +
            $"Vida antes: {vidaAntes} | " +
            $"Dano final: {danoFinal} | " +
            $"Vida depois: {alvo.vida}"
        );

        if (alvo.vida <= 0)
        {
            Debug.Log($"{alvo.nome} foi derrotada e enviada ao cemitério.");

            if (cartaInimigoAlvoSelecionada == alvoObj)
                cartaInimigoAlvoSelecionada = null;

            MoverCartaParaCemiterio(alvoObj);
        }
    }

    private void AplicarAtaqueDiretoNoInimigo(GameObject atacanteObj)
    {
        if (combateFinalizado || atacanteObj == null)
            return;

        if (!atacanteObj.CompareTag(tagCartaPlayer) || !EstaEmSlotComTag(atacanteObj.transform, tagSlotTabuleiroPlayer))
            return;

        AtualizarListasDeCartas();
        if (cartasInimigoNoTabuleiro.Count > 0)
        {
            Debug.Log("Ataque direto bloqueado: o inimigo possui carta no tabuleiro.");
            return;
        }

        if (CartaEstaParalisada(atacanteObj))
            return;

        Carta atacante = atacanteObj.GetComponent<Carta>();
        if (atacante == null)
            return;

        int dano = Mathf.Max(0, atacante.dano);
        int vidaAntes = vidaAtualInimigo;

        if (feedbackCartasUI != null)
            feedbackCartasUI.AnimarAtaqueDireto(atacanteObj, true);

        vidaAtualInimigo = Mathf.Max(0, vidaAtualInimigo - dano);
        RegistrarDanoTotalCausado(atacanteObj, dano);
        RemoverDisfarceAoAtacarDireto(atacanteObj);

        if (hudDuelistasUI != null)
            hudDuelistasUI.AnimarDanoNoInimigo(dano);

        Notificar($"ATAQUE DIRETO • -{dano} VIDA", 0.95f);

        Debug.Log($"ATAQUE DIRETO -> {atacante.nome} atingiu o duelista inimigo | Vida: {vidaAntes} -> {vidaAtualInimigo}");
        AtualizarHUDDuelistas();
        VerificarCondicoesDeFimDeCombate();
    }

    private void AplicarAtaqueDiretoNoPlayer(GameObject atacanteObj)
    {
        if (combateFinalizado || atacanteObj == null)
            return;

        if (!atacanteObj.CompareTag(tagCartaInimigo) || !EstaEmSlotComTag(atacanteObj.transform, tagSlotTabuleiroInimigo))
            return;

        AtualizarListasDeCartas();
        if (cartasPlayerNoTabuleiro.Count > 0)
        {
            Debug.Log("Ataque direto bloqueado: o player possui carta no tabuleiro.");
            return;
        }

        if (CartaEstaParalisada(atacanteObj))
            return;

        Carta atacante = atacanteObj.GetComponent<Carta>();
        if (atacante == null)
            return;

        int dano = Mathf.Max(0, atacante.dano);
        int vidaAntes = vidaAtualPlayer;

        if (feedbackCartasUI != null)
            feedbackCartasUI.AnimarAtaqueDireto(atacanteObj, false);

        vidaAtualPlayer = Mathf.Max(0, vidaAtualPlayer - dano);
        RegistrarDanoTotalCausado(atacanteObj, dano);
        RemoverDisfarceAoAtacarDireto(atacanteObj);

        if (hudDuelistasUI != null)
            hudDuelistasUI.AnimarDanoNoPlayer(dano);

        Notificar($"VOCÊ RECEBEU ATAQUE DIRETO • -{dano} VIDA", 1.05f);

        Debug.Log($"ATAQUE DIRETO -> {atacante.nome} atingiu o player | Vida: {vidaAntes} -> {vidaAtualPlayer}");
        AtualizarHUDDuelistas();
        VerificarCondicoesDeFimDeCombate();
    }

    private void RemoverDisfarceAoAtacarDireto(GameObject atacanteObj)
    {
        Carta atacante = atacanteObj != null ? atacanteObj.GetComponent<Carta>() : null;
        if (atacante == null || !atacante.disfarceAtivo)
            return;

        SpriteRenderer spriteRenderer = atacanteObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && atacante.spriteOriginalAntesDoDisfarce != null)
            spriteRenderer.sprite = atacante.spriteOriginalAntesDoDisfarce;

        atacante.disfarceAtivo = false;
        atacante.spriteOriginalAntesDoDisfarce = null;
    }

    private void AtualizarHUDDuelistas()
    {
        if (hudDuelistasUI == null)
            return;

        string nomeInimigo = "INIMIGO";
        if (controladorInimigoNaCena != null && controladorInimigoNaCena.inimigoAtual != null &&
            !string.IsNullOrEmpty(controladorInimigoNaCena.inimigoAtual.nomeInimigo))
        {
            nomeInimigo = controladorInimigoNaCena.inimigoAtual.nomeInimigo;
        }

        hudDuelistasUI.AtualizarPlayer("PLAYER", vidaAtualPlayer, vidaMaximaPlayer, reservaPlayer != null ? reservaPlayer.Count : 0);
        hudDuelistasUI.AtualizarInimigo(nomeInimigo, vidaAtualInimigo, vidaMaximaInimigo, reservaInimigo != null ? reservaInimigo.Count : 0);
        hudDuelistasUI.AtualizarAcoes(acoesRestantesPlayer, maximoAcoesPorTurnoPlayer, acoesRestantesInimigo, maximoAcoesPorTurnoInimigo);
        hudDuelistasUI.AtualizarContadores(
            cartasPlayerNoDeck.Count,
            reservaPlayer != null ? reservaPlayer.Count : 0,
            ContarCartasNoCemiterio(true),
            cartasInimigoNoDeck.Count,
            reservaInimigo != null ? reservaInimigo.Count : 0,
            ContarCartasNoCemiterio(false));
        hudDuelistasUI.AtualizarEstadoCampo(cartasPlayerNoTabuleiro.Count > 0, cartasInimigoNoTabuleiro.Count > 0);
    }

    private int ContarCartasNoCemiterio(bool player)
    {
        int quantidade = 0;
        GameObject[] cemiterios = GameObject.FindGameObjectsWithTag(tagSlotCemiterio);
        for (int i = 0; i < cemiterios.Length; i++)
        {
            if (cemiterios[i] == null)
                continue;

            Carta[] cartas = cemiterios[i].GetComponentsInChildren<Carta>(true);
            for (int j = 0; j < cartas.Length; j++)
            {
                if (cartas[j] == null)
                    continue;
                GameObject obj = cartas[j].gameObject;
                if (player && obj.CompareTag(tagCartaPlayer))
                    quantidade++;
                else if (!player && obj.CompareTag(tagCartaInimigo))
                    quantidade++;
            }
        }
        return quantidade;
    }

    private void VerificarCondicoesDeFimDeCombate()
    {
        if (!combateIniciado || combateFinalizado)
            return;

        if (vidaAtualInimigo <= 0)
        {
            FinalizarCombate(true, "A vida do duelista inimigo chegou a 0.");
            return;
        }

        if (vidaAtualPlayer <= 0)
        {
            FinalizarCombate(false, "A vida do player chegou a 0.");
            return;
        }

        AtualizarListasDeCartas();

        if (!PlayerPossuiFormaDeContinuar())
        {
            FinalizarCombate(false, "O player ficou sem cartas utilizáveis e sem possibilidade de resgate.");
            return;
        }

        if (!InimigoPossuiFormaDeContinuar())
        {
            FinalizarCombate(true, "O inimigo ficou sem cartas utilizáveis e sem possibilidade de resgate.");
        }
    }

    private bool PlayerPossuiFormaDeContinuar()
    {
        if (cartasPlayerNoTabuleiro.Count > 0 || cartasPlayerNoDeck.Count > 0)
            return true;

        return reservaPlayer != null && reservaPlayer.Count > 0;
    }

    private bool InimigoPossuiFormaDeContinuar()
    {
        if (cartasInimigoNoTabuleiro.Count > 0 || cartasInimigoNoDeck.Count > 0)
            return true;

        return reservaInimigo != null && reservaInimigo.Count > 0;
    }

    private void FinalizarCombate(bool playerVenceu, string motivo)
    {
        if (combateFinalizado)
            return;

        combateFinalizado = true;
        turnoDoPlayer = false;
        turnoDoInimigo = false;
        inimigoExecutandoTurno = false;
        modoEscolhaAlvo = false;
        ataquesPendentesDoPlayer.Clear();

        if (uiCombateCarta != null)
        {
            uiCombateCarta.FecharPainelCarta();
            uiCombateCarta.FecharTodosPaineisDeHabilidade();
            uiCombateCarta.SairModoEscolhaAlvo();
        }

        if (feedbackCartasUI != null)
        {
            feedbackCartasUI.LimparDestaques();
            feedbackCartasUI.OcultarHover();
        }
        if (notificacaoJogoUI != null)
            notificacaoJogoUI.FecharConfirmacao();

        StopAllCoroutines();

        if (hudDuelistasUI != null)
            hudDuelistasUI.Ocultar();

        InimigoBase inimigo = controladorInimigoNaCena != null ? controladorInimigoNaCena.inimigoAtual : null;
        string nomeInimigo = inimigo != null ? inimigo.nomeInimigo : "Inimigo";

        resultadoCombateUI = resultadoCombateUI != null ? resultadoCombateUI : ResultadoCombateUI.ObterOuCriar();

        Debug.Log($"COMBATE FINALIZADO -> {(playerVenceu ? "VITÓRIA" : "DERROTA")} | Motivo: {motivo}");

        if (playerVenceu)
        {
            RecompensaCombateRecebida recompensa = SistemaRecompensasCombate.EntregarRecompensas(inimigo);
            if (resultadoCombateUI != null)
                resultadoCombateUI.MostrarVitoria(nomeInimigo, recompensa, SairDoCombate);
        }
        else
        {
            if (resultadoCombateUI != null)
                resultadoCombateUI.MostrarDerrota(nomeInimigo, TentarNovamente, SairDoCombate);
        }
    }

    private void TentarNovamente()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SairDoCombate()
    {
        Time.timeScale = 1f;
        string cenaRetorno = "";

        if (GerenciadorInimigo.instancia != null)
            cenaRetorno = GerenciadorInimigo.instancia.nomeCenaRetorno;

        if (string.IsNullOrEmpty(cenaRetorno))
            cenaRetorno = cenaRetornoFallback;

        if (GerenciadorInimigo.instancia != null)
            GerenciadorInimigo.instancia.LimparInimigoSelecionado();

        if (!string.IsNullOrEmpty(cenaRetorno))
        {
            SceneManager.LoadScene(cenaRetorno);
            return;
        }

        int buildAtual = SceneManager.GetActiveScene().buildIndex;
        if (SceneManager.sceneCountInBuildSettings > 1 && buildAtual != 0)
        {
            Debug.LogWarning("Cena de retorno não definida. Voltando para a cena de índice 0.");
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogWarning("Nenhuma cena de retorno foi encontrada. Defina cenaRetornoFallback no CombateAmigavel se necessário.");
    }

    private bool CartaPodeReceberDano(GameObject cartaObj)
    {
        if (cartaObj == null)
            return false;

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null)
            return false;

        // Não recebe dano se já estiver no cemitério
        if (EstaEmSlotComTag(cartaObj.transform, tagSlotCemiterio))
            return false;

        return true;
    }

    private void EncerrarTurnoDoInimigo()
    {
        if (combateFinalizado)
            return;

        AtualizarListasDeCartas();
        VerificarCondicoesDeFimDeCombate();
        if (combateFinalizado)
            return;

        cartasPlayerQueAtacaramNesteTurno.Clear();
        cartasInimigoQueAtacaramNesteTurno.Clear();
        cartasPlayerQueUsaramHabilidadeNesteTurno.Clear();
        cartasInimigoQueUsaramHabilidadeNesteTurno.Clear();

        turnoDoInimigo = false;
        turnoDoPlayer = true;
        inimigoExecutandoTurno = false;
        acoesRestantesInimigo = 0;
        acoesRestantesPlayer = Mathf.Max(1, maximoAcoesPorTurnoPlayer);

        // Cartas colocadas pelo player no turno anterior agora ficam prontas para agir.
        cartasPlayerColocadasNesteTurno.Clear();

        ProcessarBuffsTemporariosNoInicioDoTurno(true);
        ProcessarEfeitosNoInicioDoTurno(true);
        ProcessarCooldownsHabilidadesNoInicioDoTurno(true);

        VerificarCondicoesDeFimDeCombate();
        if (combateFinalizado)
            return;

        contadorTurnosPlayer++;
        TentarRecuperarEnergiaPlayer();

        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();
        AtualizarHUDDuelistas();

        if (notificacaoJogoUI != null)
            notificacaoJogoUI.MostrarBannerTurno(true);

        Debug.Log("Turno do inimigo encerrado. Agora é o turno do player.");
    }

    private void TentarRecuperarEnergiaPlayer()
    {
        if (contadorTurnosPlayer < 2)
            return;

        contadorTurnosPlayer = 0;

        if (energiaAtualPlayer >= energiaMaximaPlayer)
            return;

        float rolagem = Random.Range(0f, 100f);

        if (rolagem <= chanceRecuperarEnergiaPlayer)
        {
            energiaAtualPlayer++;
            Debug.Log("Player recuperou 1 token de energia.");
        }
        else
        {
            Debug.Log("Player não recuperou energia desta vez.");
        }
    }

    private void TentarRecuperarEnergiaInimigo()
    {
        if (contadorTurnosInimigo < 2)
            return;

        contadorTurnosInimigo = 0;

        if (energiaAtualInimigo >= energiaMaximaInimigo)
            return;

        float rolagem = Random.Range(0f, 100f);

        if (rolagem <= chanceRecuperarEnergiaInimigo)
        {
            energiaAtualInimigo++;
            Debug.Log("Inimigo recuperou 1 token de energia.");
        }
        else
        {
            Debug.Log("Inimigo não recuperou energia desta vez.");
        }
    }

    private bool JaExisteAtaquePendente(GameObject atacante)
    {
        for (int i = 0; i < ataquesPendentesDoPlayer.Count; i++)
        {
            if (ataquesPendentesDoPlayer[i] != null && ataquesPendentesDoPlayer[i].atacante == atacante)
                return true;
        }

        return false;
    }

    private int ContarSlotsLivres(string tagSlot)
    {
        GameObject[] slots = GameObject.FindGameObjectsWithTag(tagSlot);
        int livres = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            if (!SlotJaPossuiCarta(slots[i].transform))
                livres++;
        }

        return livres;
    }

    private Transform EncontrarSlotLivre(string tagSlot)
    {
        GameObject[] slots = GameObject.FindGameObjectsWithTag(tagSlot);
        List<Transform> lista = new List<Transform>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                lista.Add(slots[i].transform);
        }

        lista.Sort((a, b) => a.name.CompareTo(b.name));

        for (int i = 0; i < lista.Count; i++)
        {
            if (!SlotJaPossuiCarta(lista[i]))
                return lista[i];
        }

        return null;
    }

    private Collider2D EncontrarSlotMaisProximo(Vector3 posicaoCarta, string tagSlot)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(posicaoCarta);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].CompareTag(tagSlot))
                return colliders[i];
        }

        float raioBusca = 0.8f;
        Collider2D[] collidersProximos = Physics2D.OverlapCircleAll(posicaoCarta, raioBusca);

        Collider2D melhorSlot = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < collidersProximos.Length; i++)
        {
            if (collidersProximos[i] != null && collidersProximos[i].CompareTag(tagSlot))
            {
                float distancia = Vector2.Distance(posicaoCarta, collidersProximos[i].transform.position);

                if (distancia < menorDistancia)
                {
                    menorDistancia = distancia;
                    melhorSlot = collidersProximos[i];
                }
            }
        }

        return melhorSlot;
    }

    private bool SlotJaPossuiCarta(Transform slot)
    {
        if (slot == null)
            return false;

        for (int i = 0; i < slot.childCount; i++)
        {
            Transform filho = slot.GetChild(i);
            if (filho == null)
                continue;

            if (filho.CompareTag(tagCartaPlayer) || filho.CompareTag(tagCartaInimigo))
                return true;
        }

        return false;
    }

    private bool EstaEmSlotComTag(Transform cartaTransform, string tagSlot)
    {
        if (cartaTransform == null || cartaTransform.parent == null)
            return false;

        return cartaTransform.parent.CompareTag(tagSlot);
    }

    private void PintarCartaDeVermelho(GameObject carta)
    {
        if (carta == null)
            return;

        SpriteRenderer sr = carta.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(1f, 0.45f, 0.45f, 1f);
    }

    private void RestaurarCorCarta(GameObject carta)
    {
        if (carta == null)
            return;

        SpriteRenderer sr = carta.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;
    }

    private void MoverCartaParaCemiterio(GameObject carta)
    {
        if (carta == null)
            return;

        RemoverTodosBuffsDaCarta(carta);

        GameObject[] cemiterios = GameObject.FindGameObjectsWithTag(tagSlotCemiterio);
        if (cemiterios == null || cemiterios.Length == 0)
        {
            Debug.LogWarning("Nenhum SlotCemiterio foi encontrado.");
            Destroy(carta);
            return;
        }

        Transform cemiterio = cemiterios[0].transform;
        Vector3 posicaoInicial = carta.transform.position;
        Vector3 escalaOriginal = carta.transform.localScale;
        Quaternion rotacaoInicial = carta.transform.rotation;

        int indiceNoCemiterio = cemiterio.childCount;
        Vector3 offset = new Vector3(0.15f * indiceNoCemiterio, 0f, 0f);
        Vector3 destino = cemiterio.position + offset;

        // Reparenta imediatamente para as regras do combate já considerarem a carta derrotada,
        // mas visualmente ela ainda viaja até o cemitério.
        carta.transform.SetParent(cemiterio);
        carta.transform.position = posicaoInicial;
        carta.transform.localScale = escalaOriginal;

        Collider2D collider = carta.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        Carta cartaComp = carta.GetComponent<Carta>();
        if (cartaComp != null)
            cartaComp.vida = 0;

        if (feedbackCartasUI != null)
            feedbackCartasUI.MostrarTextoFlutuante(carta, "DERROTADA", new Color(0.85f, 0.86f, 0.9f, 1f));

        if (feedbackCartasUI != null)
            feedbackCartasUI.AnimarCartaParaCemiterio(carta, posicaoInicial, destino, escalaOriginal, rotacaoInicial);
        else
            StartCoroutine(AnimarCartaAteCemiterio(carta, posicaoInicial, destino, escalaOriginal, rotacaoInicial));

        if (uiCombateCarta != null && uiCombateCarta.cartaSelecionada == carta)
            uiCombateCarta.FecharPainelCarta();

        AtualizarListasDeCartas();
        AtualizarHUDDuelistas();
        VerificarCondicoesDeFimDeCombate();
    }

    private IEnumerator AnimarCartaAteCemiterio(GameObject carta, Vector3 inicio, Vector3 destino,
        Vector3 escalaOriginal, Quaternion rotacaoOriginal)
    {
        if (carta == null)
            yield break;

        SpriteRenderer sr = carta.GetComponent<SpriteRenderer>();
        Color corInicial = sr != null ? sr.color : Color.white;
        Color corFinal = new Color(0.45f, 0.45f, 0.45f, 1f);
        float sinal = Random.value < 0.5f ? -1f : 1f;
        Quaternion rotacaoFinal = rotacaoOriginal * Quaternion.Euler(0f, 0f, 12f * sinal);

        float t = 0f;
        const float duracao = 0.38f;
        while (t < duracao && carta != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            float suave = 1f - Mathf.Pow(1f - p, 3f);
            Vector3 arco = Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.28f;

            carta.transform.position = Vector3.Lerp(inicio, destino, suave) + arco;
            carta.transform.localScale = escalaOriginal * Mathf.Lerp(1f, 0.9f, p);
            carta.transform.rotation = Quaternion.Slerp(rotacaoOriginal, rotacaoFinal, p);

            if (sr != null)
                sr.color = Color.Lerp(corInicial, corFinal, p);

            yield return null;
        }

        if (carta != null)
        {
            carta.transform.position = destino;
            carta.transform.localScale = escalaOriginal * 0.9f;
            carta.transform.rotation = rotacaoFinal;
            if (sr != null)
                sr.color = corFinal;
        }
    }

    public bool PodeAtacarCartaParaUI(GameObject cartaObj)
    {
        if (cartaObj == null || combateFinalizado || !combateIniciado || !turnoDoPlayer)
            return false;
        if (!cartaObj.CompareTag(tagCartaPlayer) || !EstaEmSlotComTag(cartaObj.transform, tagSlotTabuleiroPlayer))
            return false;
        if (!PlayerPossuiAcaoDisponivel() || CartaPlayerAindaEstaEmPreparacao(cartaObj) || CartaEstaParalisada(cartaObj))
            return false;
        if (cartasPlayerQueAtacaramNesteTurno.Contains(cartaObj) || JaExisteAtaquePendente(cartaObj))
            return false;
        return true;
    }

    public bool PodeUsarHabilidadeCartaParaUI(GameObject cartaObj)
    {
        if (cartaObj == null || combateFinalizado || !combateIniciado || !turnoDoPlayer)
            return false;
        if (!cartaObj.CompareTag(tagCartaPlayer) || !EstaEmSlotComTag(cartaObj.transform, tagSlotTabuleiroPlayer))
            return false;
        if (!PlayerPossuiAcaoDisponivel() || CartaPlayerAindaEstaEmPreparacao(cartaObj) || CartaEstaParalisada(cartaObj))
            return false;
        if (cartasPlayerQueUsaramHabilidadeNesteTurno.Contains(cartaObj))
            return false;

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null)
            return false;

        int limite = Mathf.Min(Mathf.Clamp(carta.quantidadeHabilidades, 0, 4), carta.habilidades != null ? carta.habilidades.Count : 0);
        for (int i = 0; i < limite; i++)
        {
            HabilidadeCarta h = carta.ObterHabilidade(i);
            if (h != null && PodeUsarHabilidade(cartaObj, h, i, false))
                return true;
        }

        if (carta.habilidades != null)
        {
            for (int i = 0; i < carta.habilidades.Count; i++)
            {
                HabilidadeCarta h = carta.habilidades[i];
                if (h != null && h.EstaConfiguradaComoConjunto() && PodeUsarHabilidade(cartaObj, h, i, false))
                    return true;
            }
        }

        return false;
    }

    public bool PodeUsarHabilidadeIndiceParaUI(GameObject cartaObj, int indice)
    {
        if (!PodeUsarHabilidadeCartaParaUI(cartaObj))
            return false;

        Carta carta = cartaObj != null ? cartaObj.GetComponent<Carta>() : null;
        if (carta == null)
            return false;

        HabilidadeCarta habilidade = carta.ObterHabilidade(indice);
        return habilidade != null && PodeUsarHabilidade(cartaObj, habilidade, indice, false);
    }

    public bool PodeVoltarCartaParaDeckParaUI(GameObject cartaObj)
    {
        if (cartaObj == null || combateFinalizado || !combateIniciado || !turnoDoPlayer || !PlayerPossuiAcaoDisponivel())
            return false;
        if (!cartaObj.CompareTag(tagCartaPlayer) || !EstaEmSlotComTag(cartaObj.transform, tagSlotTabuleiroPlayer))
            return false;
        return EncontrarSlotLivre(tagSlotDeckPlayer) != null;
    }

    public bool PodeUsarReservaParaUI()
    {
        return combateIniciado && !combateFinalizado && turnoDoPlayer && PlayerPossuiAcaoDisponivel() &&
               reservaPlayer != null && reservaPlayer.Count > 0 && EncontrarSlotLivre(tagSlotDeckPlayer) != null;
    }

    public string ObterEstadoCurtoCartaParaUI(GameObject cartaObj)
    {
        if (cartaObj == null)
            return "";

        bool player = cartaObj.CompareTag(tagCartaPlayer);
        bool inimigo = cartaObj.CompareTag(tagCartaInimigo);
        if (!player && !inimigo)
            return "";

        if ((player && CartaPlayerAindaEstaEmPreparacao(cartaObj)) ||
            (inimigo && CartaInimigoAindaEstaEmPreparacao(cartaObj)))
            return "ESPERA";

        if (CartaEstaParalisada(cartaObj))
            return "BLOQ";

        bool atacou = player ? cartasPlayerQueAtacaramNesteTurno.Contains(cartaObj) :
            cartasInimigoQueAtacaramNesteTurno.Contains(cartaObj);
        bool usouHab = player ? cartasPlayerQueUsaramHabilidadeNesteTurno.Contains(cartaObj) :
            cartasInimigoQueUsaramHabilidadeNesteTurno.Contains(cartaObj);

        if (atacou && usouHab)
            return "USADA";
        if (atacou)
            return "ATQ";
        if (usouHab)
            return "HAB";

        bool turnoDono = player ? turnoDoPlayer : turnoDoInimigo;
        int acoes = player ? acoesRestantesPlayer : acoesRestantesInimigo;
        return turnoDono && acoes > 0 ? "PRONTA" : "";
    }

    public string ObterDescricaoAtaquePendenteParaUI(GameObject cartaObj)
    {
        if (cartaObj == null)
            return "";

        for (int i = 0; i < ataquesPendentesDoPlayer.Count; i++)
        {
            AtaquePendente ataque = ataquesPendentesDoPlayer[i];
            if (ataque == null || ataque.atacante != cartaObj)
                continue;

            if (ataque.ataqueDiretoAoDuelista)
                return "ATQ?VIDA";

            Carta alvo = ataque.alvo != null ? ataque.alvo.GetComponent<Carta>() : null;
            if (alvo == null)
                return "ATQ";
            string nome = alvo.nome ?? "";
            if (nome.Length > 9)
                nome = nome.Substring(0, 9);
            return "ATQ?" + nome;
        }

        return "";
    }

    public string ObterBonusTemporarioParaUI(GameObject cartaObj)
    {
        if (cartaObj == null)
            return "";

        int vida = 0;
        int dano = 0;
        int defesa = 0;

        for (int i = 0; i < buffsTemporariosAtivos.Count; i++)
        {
            BuffTemporario buff = buffsTemporariosAtivos[i];
            if (buff == null || buff.cartaObj != cartaObj)
                continue;

            if (buff.tipoBuff == HabilidadeCarta.TipoBuff.Vida)
                vida += buff.valor;
            else if (buff.tipoBuff == HabilidadeCarta.TipoBuff.Dano)
                dano += buff.valor;
            else if (buff.tipoBuff == HabilidadeCarta.TipoBuff.Defesa)
                defesa += buff.valor;
        }

        List<string> partes = new List<string>();
        if (dano > 0) partes.Add($"+{dano} ATQ");
        if (defesa > 0) partes.Add($"+{defesa} DEF");
        if (vida > 0) partes.Add($"+{vida} VIDA");
        return string.Join("  ", partes);
    }

    private void IniciarTurnoDoPlayer()
    {
        if (combateFinalizado)
            return;

        turnoDoPlayer = true;
        turnoDoInimigo = false;
        inimigoExecutandoTurno = false;
        acoesRestantesPlayer = Mathf.Max(1, maximoAcoesPorTurnoPlayer);
        acoesRestantesInimigo = 0;
        cartasPlayerColocadasNesteTurno.Clear();
        cartasPlayerQueAtacaramNesteTurno.Clear();
        cartasPlayerQueUsaramHabilidadeNesteTurno.Clear();
        cartasInimigoQueUsaramHabilidadeNesteTurno.Clear();
        ataquesPendentesDoPlayer.Clear();

        ProcessarBuffsTemporariosNoInicioDoTurno(true);
        ProcessarEfeitosNoInicioDoTurno(true);

        VerificarCondicoesDeFimDeCombate();
        if (combateFinalizado)
            return;

        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();
        AtualizarHUDDuelistas();

        if (notificacaoJogoUI != null)
            notificacaoJogoUI.MostrarBannerTurno(true);
    }

}