using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombateAmigavel : MonoBehaviour
{
    [Header("Tags do Player")]
    public string tagCartaPlayer = "CartaPlayer";
    public string tagSlotDeckPlayer = "SlotDeckPlayer";
    public string tagSlotTabuleiroPlayer = "SlotTabuleiroPlayer";

    [Header("Tags do Inimigo")]
    public string tagCartaInimigo = "CartaInimigo";
    public string tagSlotDeckInimigo = "SlotDackInimigo";
    public string tagSlotTabuleiroInimigo = "SlotTabuleiroInimigo";

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

    [Header("UI da carta")]
    public UICombateCarta uiCombateCarta;

    [Header("Ajustes da IA")]
    [Range(0f, 1f)] public float chanceBaseDeColocarCarta = 0.75f;
    [Range(0f, 1f)] public float chanceDeRecuarCarta = 0.20f;
    [Range(0f, 1f)] public float chanceExtraDeAtacar = 0.85f;
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
            else
                cartasPlayerNoDeck.Add(cartasPlayer[i]);
        }

        GameObject[] cartasInimigo = GameObject.FindGameObjectsWithTag(tagCartaInimigo);
        for (int i = 0; i < cartasInimigo.Length; i++)
        {
            if (cartasInimigo[i] == null)
                continue;

            if (EstaEmSlotComTag(cartasInimigo[i].transform, tagSlotTabuleiroInimigo))
                cartasInimigoNoTabuleiro.Add(cartasInimigo[i]);
            else
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

    private IEnumerator ExecutarTurnoDoInimigo()
    {
        inimigoExecutandoTurno = true;
        AtualizarListasDeCartas();

        cartasInimigoQueAtacaramNesteTurno.Clear();

        yield return new WaitForSeconds(tempoEntreAcoesInimigo);

        int slotsLivresInimigo = ContarSlotsLivres(tagSlotTabuleiroInimigo);
        bool playerTemCartasNoTabuleiro = cartasPlayerNoTabuleiro.Count > 0;

        int quantidadeDeColocacoes = DefinirQuantidadeDeJogadasDoInimigo(slotsLivresInimigo, playerTemCartasNoTabuleiro);

        for (int i = 0; i < quantidadeDeColocacoes; i++)
        {
            bool colocou = InimigoTentaColocarCartaNoTabuleiro();
            AtualizarListasDeCartas();

            if (!colocou)
                break;

            yield return new WaitForSeconds(tempoEntreAcoesInimigo);
        }

        if (cartasInimigoNoTabuleiro.Count > 0 && Random.value < chanceDeRecuarCarta)
        {
            InimigoTentaRecuarCarta();
            AtualizarListasDeCartas();
            yield return new WaitForSeconds(tempoEntreAcoesInimigo);
        }

        if (cartasPlayerNoTabuleiro.Count > 0)
        {
            for (int i = 0; i < cartasInimigoNoTabuleiro.Count; i++)
            {
                GameObject atacante = cartasInimigoNoTabuleiro[i];

                if (atacante == null)
                    continue;

                if (cartasInimigoQueAtacaramNesteTurno.Contains(atacante))
                    continue;

                if (!EstaEmSlotComTag(atacante.transform, tagSlotTabuleiroInimigo))
                    continue;

                if (Random.value > chanceExtraDeAtacar)
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
        }

        EncerrarTurnoDoInimigo();
    }

    private int DefinirQuantidadeDeJogadasDoInimigo(int slotsLivresInimigo, bool playerTemCartasNoTabuleiro)
    {
        if (slotsLivresInimigo <= 0 || cartasInimigoNoDeck.Count <= 0)
            return 0;

        int quantidade = 0;

        if (playerTemCartasNoTabuleiro)
        {
            quantidade = Random.value < chanceBaseDeColocarCarta ? Random.Range(1, Mathf.Min(3, slotsLivresInimigo + 1)) : 0;
        }
        else
        {
            quantidade = Random.value < 0.50f ? 1 : 0;
        }

        quantidade = Mathf.Min(quantidade, cartasInimigoNoDeck.Count);
        return quantidade;
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

    private GameObject EscolherCartaDoDeckDoInimigoParaJogar()
    {
        if (cartasInimigoNoDeck.Count == 0)
            return null;

        GameObject melhorCarta = null;
        int melhorPontuacao = int.MinValue;

        bool playerTemCartas = cartasPlayerNoTabuleiro.Count > 0;

        for (int i = 0; i < cartasInimigoNoDeck.Count; i++)
        {
            GameObject cartaObj = cartasInimigoNoDeck[i];
            if (cartaObj == null)
                continue;

            Carta carta = cartaObj.GetComponent<Carta>();
            if (carta == null)
                continue;

            int pontuacao = carta.dano + carta.vida + carta.defesa;

            if (playerTemCartas)
                pontuacao += carta.dano * 2;
            else
                pontuacao += carta.vida;

            pontuacao += Random.Range(0, 6);

            if (pontuacao > melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhorCarta = cartaObj;
            }
        }

        return melhorCarta;
    }

    private void InimigoTentaRecuarCarta()
    {
        AtualizarListasDeCartas();

        if (cartasInimigoNoTabuleiro.Count == 0)
            return;

        Transform slotLivreDeck = EncontrarSlotLivre(tagSlotDeckInimigo);
        if (slotLivreDeck == null)
            return;

        GameObject piorCarta = null;
        int piorPontuacao = int.MaxValue;

        for (int i = 0; i < cartasInimigoNoTabuleiro.Count; i++)
        {
            GameObject cartaObj = cartasInimigoNoTabuleiro[i];
            if (cartaObj == null)
                continue;

            Carta carta = cartaObj.GetComponent<Carta>();
            if (carta == null)
                continue;

            int pontuacao = carta.vida + carta.defesa + Random.Range(0, 4);

            if (pontuacao < piorPontuacao)
            {
                piorPontuacao = pontuacao;
                piorCarta = cartaObj;
            }
        }

        if (piorCarta == null)
            return;

        Vector3 escala = piorCarta.transform.localScale;
        piorCarta.transform.SetParent(slotLivreDeck);
        piorCarta.transform.position = slotLivreDeck.position;
        piorCarta.transform.localScale = escala;

        Debug.Log($"Inimigo recuou a carta {piorCarta.name} para o deck.");
    }

    private GameObject EscolherAlvoEstrategicoDoPlayer(GameObject atacanteInimigo)
    {
        if (atacanteInimigo == null || cartasPlayerNoTabuleiro.Count == 0)
            return null;

        Carta atacante = atacanteInimigo.GetComponent<Carta>();
        if (atacante == null)
            return null;

        GameObject melhorAlvo = null;
        int melhorPontuacao = int.MinValue;

        for (int i = 0; i < cartasPlayerNoTabuleiro.Count; i++)
        {
            GameObject alvoObj = cartasPlayerNoTabuleiro[i];
            if (alvoObj == null)
                continue;

            Carta alvo = alvoObj.GetComponent<Carta>();
            if (alvo == null)
                continue;

            int danoReal = Mathf.Max(0, atacante.dano - alvo.defesa);
            int pontuacao = 0;

            if (danoReal >= alvo.vida)
                pontuacao += 1000;

            pontuacao += danoReal * 10;
            pontuacao += alvo.dano * 4;
            pontuacao += alvo.vida;
            pontuacao += Random.Range(0, 10);

            if (pontuacao > melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhorAlvo = alvoObj;
            }
        }

        return melhorAlvo;
    }

    private void AplicarAtaque(GameObject atacanteObj, GameObject alvoObj)
    {
        if (atacanteObj == null || alvoObj == null)
            return;

        Carta atacante = atacanteObj.GetComponent<Carta>();
        Carta alvo = alvoObj.GetComponent<Carta>();

        if (atacante == null || alvo == null)
            return;

        int danoRecebido = Mathf.Max(0, atacante.dano - alvo.defesa);
        alvo.vida -= danoRecebido;

        Debug.Log($"{atacante.nome} atacou {alvo.nome}. Dano base: {atacante.dano} | Defesa do alvo: {alvo.defesa} | Dano final: {danoRecebido} | Vida restante: {alvo.vida}");

        if (alvo.vida <= 0)
        {
            Debug.Log($"{alvo.nome} foi destruída nesta cena.");
            if (cartaInimigoAlvoSelecionada == alvoObj)
                cartaInimigoAlvoSelecionada = null;

            Destroy(alvoObj);
        }
    }

    private void EncerrarTurnoDoInimigo()
    {
        AtualizarListasDeCartas();

        cartasPlayerQueAtacaramNesteTurno.Clear();
        cartasInimigoQueAtacaramNesteTurno.Clear();

        turnoDoInimigo = false;
        turnoDoPlayer = true;
        inimigoExecutandoTurno = false;

        Debug.Log("Turno do inimigo encerrado. Agora é o turno do player.");
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

    private void IniciarTurnoDoPlayer()
    {
        turnoDoPlayer = true;
        turnoDoInimigo = false;
        inimigoExecutandoTurno = false;
        cartasPlayerQueAtacaramNesteTurno.Clear();
        ataquesPendentesDoPlayer.Clear();
    }
}