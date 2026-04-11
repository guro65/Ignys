using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CombateAmigavel : MonoBehaviour
{
    [Header("Referências")]
    public ControladorInimigoNaCena controladorInimigoNaCena;
    public UICombateCarta uiCombateCarta;

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

    [Header("Pontos de Resgate")]
    public int pontosResgatarPlayer = 0;
    public int pontosResgatarInimigo = 0;

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

    private class AtaquePendente
    {
        public GameObject atacante;
        public GameObject alvo;
    }

    private void Start()
    {
        cameraPrincipal = Camera.main;
        AtualizarListasDeCartas();
        RegistrarCartasVisiveisDoPlayer();
        IniciarTurnoDoPlayer();
        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();
    }

    private void Update()
    {
        AtualizarListasDeCartas();
        RegistrarCartasVisiveisDoPlayer();
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
            TentarDescartarCartaPlayer(cartaSendoArrastada);
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
            else
            {
                cartaSendoArrastada.transform.SetParent(slotTransform);
                cartaSendoArrastada.transform.position = slotTransform.position;
                cartaSendoArrastada.transform.localScale = escalaOriginalCarta;

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
        if (!turnoDoPlayer)
        {
            VoltarCartaParaOrigem(carta, parentOriginalCarta, posicaoOriginalCarta, escalaOriginalCarta);
            return;
        }

        if (energiaAtualPlayer <= 0)
        {
            Debug.Log("Player sem energia para descartar carta.");
            VoltarCartaParaOrigem(carta, parentOriginalCarta, posicaoOriginalCarta, escalaOriginalCarta);
            return;
        }

        energiaAtualPlayer--;
        pontosResgatarPlayer++;
        AtualizarTextosDeRecursos();

        Debug.Log($"Player descartou {carta.name}. Energia restante: {energiaAtualPlayer}. Pontos de resgate: {pontosResgatarPlayer}");

        MoverCartaParaCemiterio(carta);
    }

    private void TentarDescartarCartaInimigo(GameObject carta)
    {
        if (!turnoDoInimigo)
            return;

        if (carta == null || !carta.CompareTag(tagCartaInimigo))
            return;

        if (energiaAtualInimigo <= 0)
            return;

        energiaAtualInimigo--;
        pontosResgatarInimigo++;
        AtualizarTextosDeRecursos();

        Debug.Log($"Inimigo descartou {carta.name}. Energia restante: {energiaAtualInimigo}. Pontos de resgate: {pontosResgatarInimigo}");

        MoverCartaParaCemiterio(carta);
    }

    public void BotaoResgatarCartaPlayer()
    {
        ResgatarCartaPlayer();
    }

    public void ResgatarCartaPlayer()
    {
        if (!turnoDoPlayer)
        {
            Debug.Log("Só pode resgatar carta no turno do player.");
            return;
        }

        if (pontosResgatarPlayer <= 0)
        {
            Debug.Log("Player sem pontos de resgate.");
            return;
        }

        Transform slotLivre = EncontrarSlotLivre(tagSlotDeckPlayer);
        if (slotLivre == null)
        {
            Debug.Log("Player não possui slot livre no deck para resgatar carta.");
            return;
        }

        if (Inventario.instancia == null || Inventario.instancia.cartasObtidas.Count == 0)
        {
            Debug.LogWarning("Inventário do player vazio.");
            return;
        }

        int indice = Random.Range(0, Inventario.instancia.cartasObtidas.Count);
        Carta cartaPrefab = Inventario.instancia.cartasObtidas[indice];

        if (cartaPrefab == null)
            return;

        GameObject cartaInstanciada = Instantiate(cartaPrefab.gameObject);
        Vector3 escala = cartaInstanciada.transform.localScale;

        cartaInstanciada.transform.SetParent(slotLivre);
        cartaInstanciada.transform.position = slotLivre.position;
        cartaInstanciada.transform.localScale = escala;
        cartaInstanciada.tag = tagCartaPlayer;

        pontosResgatarPlayer--;
        AtualizarTextosDeRecursos();
        AtualizarListasDeCartas();

        Debug.Log($"Player resgatou a carta {cartaPrefab.nome}.");
    }

    private bool ResgatarCartaInimigo()
    {
        if (!turnoDoInimigo)
            return false;

        if (pontosResgatarInimigo <= 0)
            return false;

        if (energiaAtualInimigo <= 0)
            return false;

        if (controladorInimigoNaCena == null || controladorInimigoNaCena.inimigoAtual == null)
            return false;

        if (!controladorInimigoNaCena.inimigoAtual.podeReceberNovasCartasDuranteCombate)
            return false;

        Transform slotLivre = EncontrarSlotLivre(tagSlotDeckInimigo);
        if (slotLivre == null)
            return false;

        Carta cartaPrefab = controladorInimigoNaCena.inimigoAtual.SortearNovaCartaDuranteCombate();
        if (cartaPrefab == null)
            return false;

        GameObject cartaInstanciada = Instantiate(cartaPrefab.gameObject);
        Vector3 escala = cartaInstanciada.transform.localScale;

        cartaInstanciada.transform.SetParent(slotLivre);
        cartaInstanciada.transform.position = slotLivre.position;
        cartaInstanciada.transform.localScale = escala;
        cartaInstanciada.tag = tagCartaInimigo;

        energiaAtualInimigo--;
        pontosResgatarInimigo--;
        AtualizarTextosDeRecursos();
        AtualizarListasDeCartas();

        Debug.Log($"Inimigo resgatou a carta {cartaPrefab.nome}.");
        return true;
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
        if (!turnoDoPlayer)
            return;

        if (uiCombateCarta == null || uiCombateCarta.cartaSelecionada == null)
            return;

        GameObject atacante = uiCombateCarta.cartaSelecionada;

        if (!atacante.CompareTag(tagCartaPlayer))
            return;

        if (!EstaEmSlotComTag(atacante.transform, tagSlotTabuleiroPlayer))
            return;

        if (cartasPlayerQueAtacaramNesteTurno.Contains(atacante))
        {
            Debug.Log("Essa carta do player já atacou neste turno.");
            return;
        }

        cartaPlayerSelecionadaParaAtacar = atacante;
        cartaInimigoAlvoSelecionada = null;
        modoEscolhaAlvo = true;

        if (uiCombateCarta != null)
            uiCombateCarta.EntrarModoEscolhaAlvo();
    }

    public void ConfirmarAlvoSelecionado()
    {
        if (!modoEscolhaAlvo || cartaPlayerSelecionadaParaAtacar == null || cartaInimigoAlvoSelecionada == null)
            return;

        if (JaExisteAtaquePendente(cartaPlayerSelecionadaParaAtacar))
        {
            Debug.Log("Essa carta do player já possui um ataque pendente.");
            return;
        }

        AtaquePendente novoAtaque = new AtaquePendente
        {
            atacante = cartaPlayerSelecionadaParaAtacar,
            alvo = cartaInimigoAlvoSelecionada
        };

        ataquesPendentesDoPlayer.Add(novoAtaque);
        cartasPlayerQueAtacaramNesteTurno.Add(cartaPlayerSelecionadaParaAtacar);

        RestaurarCorCarta(cartaInimigoAlvoSelecionada);

        modoEscolhaAlvo = false;
        cartaPlayerSelecionadaParaAtacar = null;
        cartaInimigoAlvoSelecionada = null;

        if (uiCombateCarta != null)
            uiCombateCarta.SairModoEscolhaAlvo();

        Debug.Log("Ataque do player confirmado para ser resolvido no fim do turno.");
    }

    public void CancelarEscolhaAlvo()
    {
        if (cartaInimigoAlvoSelecionada != null)
            RestaurarCorCarta(cartaInimigoAlvoSelecionada);

        modoEscolhaAlvo = false;
        cartaPlayerSelecionadaParaAtacar = null;
        cartaInimigoAlvoSelecionada = null;

        if (uiCombateCarta != null)
            uiCombateCarta.SairModoEscolhaAlvo();
    }

    private void AtualizarEscolhaDeAlvoDoPlayer()
    {
        if (cameraPrincipal == null || Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 posicaoMouse = cameraPrincipal.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(posicaoMouse, Vector2.zero);

        if (hit.collider == null)
            return;

        GameObject objetoClicado = hit.collider.gameObject;

        if (!objetoClicado.CompareTag(tagCartaInimigo))
            return;

        if (!EstaEmSlotComTag(objetoClicado.transform, tagSlotTabuleiroInimigo))
            return;

        if (cartaInimigoAlvoSelecionada != null && cartaInimigoAlvoSelecionada != objetoClicado)
            RestaurarCorCarta(cartaInimigoAlvoSelecionada);

        cartaInimigoAlvoSelecionada = objetoClicado;
        PintarCartaDeVermelho(cartaInimigoAlvoSelecionada);

        if (uiCombateCarta != null)
            uiCombateCarta.AtualizarTextoAlvoSelecionado(objetoClicado);
    }

    public void BotaoHabilidadeCartaSelecionada()
    {
        if (uiCombateCarta == null || uiCombateCarta.cartaSelecionada == null)
            return;

        if (!uiCombateCarta.cartaSelecionada.CompareTag(tagCartaPlayer))
            return;

        Debug.Log("Habilidade ainda não implementada.");
    }

    public void PassarTurno()
    {
        if (!turnoDoPlayer || inimigoExecutandoTurno)
            return;

        if (modoEscolhaAlvo)
            CancelarEscolhaAlvo();

        if (uiCombateCarta != null)
            uiCombateCarta.FecharPainelCarta();

        ResolverAtaquesPendentesDoPlayer();

        turnoDoPlayer = false;
        turnoDoInimigo = true;

        contadorTurnosInimigo++;
        TentarRecuperarEnergiaInimigo();

        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();

        StartCoroutine(ExecutarTurnoDoInimigo());
    }

    private void ResolverAtaquesPendentesDoPlayer()
    {
        for (int i = 0; i < ataquesPendentesDoPlayer.Count; i++)
        {
            AtaquePendente ataque = ataquesPendentesDoPlayer[i];

            if (ataque == null || ataque.atacante == null || ataque.alvo == null)
                continue;

            if (!EstaEmSlotComTag(ataque.atacante.transform, tagSlotTabuleiroPlayer))
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

        if (turnoDoPlayer)
            textoTurno.text = "Turno do Player";
        else if (turnoDoInimigo)
            textoTurno.text = "Turno do Inimigo";
        else
            textoTurno.text = "Turno indefinido";
    }

    private void AtualizarTextosDeRecursos()
    {
        if (textoEnergiaPlayer != null)
            textoEnergiaPlayer.text = $"Energia Player: {energiaAtualPlayer}/{energiaMaximaPlayer}";

        if (textoEnergiaInimigo != null)
            textoEnergiaInimigo.text = $"Energia Inimigo: {energiaAtualInimigo}/{energiaMaximaInimigo}";

        if (textoResgatarPlayer != null)
            textoResgatarPlayer.text = $"Resgatar Player: {pontosResgatarPlayer}";

        if (textoResgatarInimigo != null)
            textoResgatarInimigo.text = $"Resgatar Inimigo: {pontosResgatarInimigo}";
    }

    private IEnumerator ExecutarTurnoDoInimigo()
    {
        inimigoExecutandoTurno = true;
        AtualizarListasDeCartas();

        cartasInimigoQueAtacaramNesteTurno.Clear();

        yield return new WaitForSeconds(tempoEntreAcoesInimigo);

        int quantidadeParaColocar = CalcularQuantidadeIdealDeCartasParaColocar();

        for (int i = 0; i < quantidadeParaColocar; i++)
        {
            bool colocou = InimigoTentaColocarCartaNoTabuleiro();
            AtualizarListasDeCartas();

            if (!colocou)
                break;

            yield return new WaitForSeconds(tempoEntreAcoesInimigo);
        }

        if (DeveDescartarParaResgatar())
        {
            InimigoDescartaCartaDoDeckParaResgatar();
            AtualizarListasDeCartas();
            AtualizarTextosDeRecursos();
            yield return new WaitForSeconds(tempoEntreAcoesInimigo);
        }

        if (pontosResgatarInimigo > 0)
        {
            bool resgatou = ResgatarCartaInimigo();
            if (resgatou)
            {
                AtualizarListasDeCartas();
                AtualizarTextosDeRecursos();
                yield return new WaitForSeconds(tempoEntreAcoesInimigo);
            }
        }

        for (int i = 0; i < cartasInimigoNoTabuleiro.Count; i++)
        {
            GameObject atacante = cartasInimigoNoTabuleiro[i];

            if (atacante == null)
                continue;

            if (cartasInimigoQueAtacaramNesteTurno.Contains(atacante))
                continue;

            if (!EstaEmSlotComTag(atacante.transform, tagSlotTabuleiroInimigo))
                continue;

            GameObject alvoEscolhido = EscolherAlvoEstrategicoDoPlayer(atacante);

            if (alvoEscolhido != null)
            {
                AplicarAtaque(atacante, alvoEscolhido);
                cartasInimigoQueAtacaramNesteTurno.Add(atacante);

                AtualizarListasDeCartas();
                yield return new WaitForSeconds(tempoEntreAcoesInimigo);
            }
        }

        EncerrarTurnoDoInimigo();
    }

    private int CalcularQuantidadeIdealDeCartasParaColocar()
    {
        AtualizarListasDeCartas();

        int slotsLivres = ContarSlotsLivres(tagSlotTabuleiroInimigo);
        int cartasNoDeck = cartasInimigoNoDeck.Count;

        if (slotsLivres <= 0 || cartasNoDeck <= 0)
            return 0;

        int cartasPlayerCampo = cartasPlayerNoTabuleiro.Count;
        int cartasInimigoCampo = cartasInimigoNoTabuleiro.Count;

        if (cartasPlayerCampo > cartasInimigoCampo)
            return Mathf.Min(slotsLivres, cartasNoDeck, 2);

        if (cartasInimigoCampo == 0)
            return Mathf.Min(1, Mathf.Min(slotsLivres, cartasNoDeck));

        if (cartasInimigoCampo < 2 && cartasNoDeck > 0)
            return Mathf.Min(1, Mathf.Min(slotsLivres, cartasNoDeck));

        if (Random.value < 0.45f)
            return Mathf.Min(1, Mathf.Min(slotsLivres, cartasNoDeck));

        return 0;
    }

    private bool InimigoTentaColocarCartaNoTabuleiro()
    {
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

        Debug.Log($"Inimigo colocou a carta {melhorCarta.name} no tabuleiro.");
        return true;
    }

    private bool DeveDescartarParaResgatar()
    {
        if (controladorInimigoNaCena == null || controladorInimigoNaCena.inimigoAtual == null)
            return false;

        if (!controladorInimigoNaCena.inimigoAtual.podeReceberNovasCartasDuranteCombate)
            return false;

        if (energiaAtualInimigo <= 0)
            return false;

        if (pontosResgatarInimigo > 0)
            return false;

        Transform slotLivreNoDeck = EncontrarSlotLivre(tagSlotDeckInimigo);
        if (slotLivreNoDeck != null)
            return false;

        if (cartasInimigoNoDeck.Count == 0)
            return false;

        return true;
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

        int danoAtacante = Mathf.Max(0, atacante.dano);
        int defesaAlvo = Mathf.Max(0, alvo.defesa);
        int vidaAntes = Mathf.Max(0, alvo.vida);

        int danoFinal = CalcularDanoFinal(danoAtacante, defesaAlvo);

        alvo.vida -= danoFinal;

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
        AtualizarListasDeCartas();

        cartasPlayerQueAtacaramNesteTurno.Clear();
        cartasInimigoQueAtacaramNesteTurno.Clear();

        turnoDoInimigo = false;
        turnoDoPlayer = true;
        inimigoExecutandoTurno = false;

        contadorTurnosPlayer++;
        TentarRecuperarEnergiaPlayer();

        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();

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

        GameObject[] cemiterios = GameObject.FindGameObjectsWithTag(tagSlotCemiterio);
        if (cemiterios == null || cemiterios.Length == 0)
        {
            Debug.LogWarning("Nenhum SlotCemiterio foi encontrado.");
            Destroy(carta);
            return;
        }

        Transform cemiterio = cemiterios[0].transform;
        Vector3 escalaOriginal = carta.transform.localScale;

        int indiceNoCemiterio = cemiterio.childCount;
        Vector3 offset = new Vector3(0.15f * indiceNoCemiterio, 0f, 0f);

        carta.transform.SetParent(cemiterio);
        carta.transform.position = cemiterio.position + offset;
        carta.transform.localScale = escalaOriginal;

        Collider2D collider = carta.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        SpriteRenderer sr = carta.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(0.45f, 0.45f, 0.45f, 1f);

        Carta cartaComp = carta.GetComponent<Carta>();
        if (cartaComp != null)
        {
            cartaComp.vida = 0;
        }

        if (uiCombateCarta != null && uiCombateCarta.cartaSelecionada == carta)
        {
            uiCombateCarta.FecharPainelCarta();
        }
    }

    private void IniciarTurnoDoPlayer()
    {
        turnoDoPlayer = true;
        turnoDoInimigo = false;
        inimigoExecutandoTurno = false;
        cartasPlayerQueAtacaramNesteTurno.Clear();
        ataquesPendentesDoPlayer.Clear();

        AtualizarTextoTurno();
        AtualizarTextosDeRecursos();
    }
}