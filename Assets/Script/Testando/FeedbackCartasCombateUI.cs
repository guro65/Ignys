using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackCartasCombateUI : MonoBehaviour
{
    private class VisualCarta
    {
        public RectTransform raiz;
        public TMP_Text estado;
        public TMP_Text efeitos;
        public TMP_Text pendente;
        public Image[] bordas;
    }

    private static FeedbackCartasCombateUI instancia;

    [Header("Textos pequenos para não poluir a cena")]
    [SerializeField] private float fonteEstadoCarta = 11f;
    [SerializeField] private float fonteEfeitos = 10f;
    [SerializeField] private float fontePreview = 14f;
    [SerializeField] private float fonteHover = 13f;

    [Header("Cores")]
    [SerializeField] private Color corPronta = new Color(0.22f, 0.9f, 0.42f, 0.95f);
    [SerializeField] private Color corEspera = new Color(1f, 0.62f, 0.18f, 0.95f);
    [SerializeField] private Color corUsada = new Color(0.62f, 0.66f, 0.75f, 0.95f);
    [SerializeField] private Color corAlvoAtaque = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color corAlvoAliado = new Color(0.25f, 0.85f, 1f, 1f);

    private Canvas canvas;
    private RectTransform raizCanvas;
    private Camera cameraPrincipal;
    private CombateAmigavel combate;

    private readonly Dictionary<GameObject, VisualCarta> visuais = new Dictionary<GameObject, VisualCarta>();
    private readonly HashSet<GameObject> alvosDestacados = new HashSet<GameObject>();
    private Color corDestaqueAtual;
    private RectTransform painelPreview;
    private TMP_Text textoPreview;
    private RectTransform painelHover;
    private TMP_Text textoHover;
    private GameObject cartaHoverAtual;

    public static FeedbackCartasCombateUI ObterOuCriar()
    {
        if (instancia != null)
            return instancia;

        instancia = FindObjectOfType<FeedbackCartasCombateUI>();
        if (instancia != null)
            return instancia;

        GameObject obj = new GameObject("FeedbackCartasCombateUI");
        instancia = obj.AddComponent<FeedbackCartasCombateUI>();
        return instancia;
    }

    private void Awake()
    {
        if (instancia == null)
            instancia = this;
    }

    private void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }

    public void Configurar(CombateAmigavel combateRecebido)
    {
        combate = combateRecebido;
        cameraPrincipal = Camera.main;
        CriarCanvasSeNecessario();
    }

    private void Update()
    {
        if (combate == null || !combate.combateIniciado || combate.combateFinalizado)
        {
            OcultarHover();
            return;
        }

        if (cameraPrincipal == null)
            cameraPrincipal = Camera.main;

        AtualizarVisuaisDasCartas();
    }

    public void DestacarAlvos(IEnumerable<GameObject> alvos, bool aliado)
    {
        alvosDestacados.Clear();
        if (alvos != null)
        {
            foreach (GameObject alvo in alvos)
            {
                if (alvo != null)
                    alvosDestacados.Add(alvo);
            }
        }

        corDestaqueAtual = aliado ? corAlvoAliado : corAlvoAtaque;
    }

    public void LimparDestaques()
    {
        alvosDestacados.Clear();
        OcultarPreview();
    }

    public void MostrarPreviewAtaque(GameObject atacanteObj, GameObject alvoObj)
    {
        if (atacanteObj == null || alvoObj == null)
        {
            OcultarPreview();
            return;
        }

        Carta atacante = atacanteObj.GetComponent<Carta>();
        Carta alvo = alvoObj.GetComponent<Carta>();
        if (atacante == null || alvo == null)
        {
            OcultarPreview();
            return;
        }

        int dano = Mathf.Max(0, atacante.dano);
        int defesa = Mathf.Max(0, alvo.defesa);
        int final = Mathf.Max(0, dano - defesa);
        int vidaDepois = Mathf.Max(0, alvo.vida - final);
        string finaliza = vidaDepois <= 0 ? "  <color=#FF6363>DERROTA</color>" : "";

        MostrarPreview($"<b>{atacante.nome}</b> → <b>{alvo.nome}</b>\n{dano} ATQ - {defesa} DEF = <b>{final}</b> dano   VIDA {alvo.vida}→{vidaDepois}{finaliza}");
    }

    public void MostrarPreviewHabilidade(GameObject usuarioObj, GameObject alvoObj, HabilidadeCarta habilidade)
    {
        if (usuarioObj == null || alvoObj == null || habilidade == null)
        {
            OcultarPreview();
            return;
        }

        Carta alvo = alvoObj.GetComponent<Carta>();
        if (alvo == null)
        {
            OcultarPreview();
            return;
        }

        string efeito;
        int valor = Mathf.Max(0, habilidade.valorHabilidade);
        if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Dano)
            efeito = $"{valor} dano • VIDA {alvo.vida}→{Mathf.Max(0, alvo.vida - valor)}";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Defesa)
            efeito = $"+{valor} DEF • {Mathf.Max(1, habilidade.duracaoHabilidadeTurnos)}t";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Buff)
            efeito = $"+{valor} {habilidade.tipoBuff} • {Mathf.Max(1, habilidade.duracaoHabilidadeTurnos)}t";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Anulacao)
            efeito = "Remove efeitos negativos";
        else if (habilidade.tipoHabilidade == HabilidadeCarta.TipoHabilidade.Disfarce)
            efeito = "Copia a aparência do alvo";
        else
            efeito = "Habilidade";

        MostrarPreview($"<b>{habilidade.nomeHabilidade}</b> → {alvo.nome}\n{efeito}");
    }

    public void OcultarPreview()
    {
        if (painelPreview != null)
            painelPreview.gameObject.SetActive(false);
    }

    public void MostrarHover(GameObject cartaObj)
    {
        if (cartaObj == null || combate == null)
        {
            OcultarHover();
            return;
        }

        Carta carta = cartaObj.GetComponent<Carta>();
        if (carta == null)
        {
            OcultarHover();
            return;
        }

        CriarCanvasSeNecessario();
        CriarHoverSeNecessario();
        cartaHoverAtual = cartaObj;

        StringBuilder sb = new StringBuilder();
        int estrelas = Mathf.Clamp((int)carta.estrelas, 1, 3);
        sb.Append($"<b>{carta.nome}</b>  •  {carta.raridade}  •  {estrelas}★\n");
        sb.Append($"ATQ <b>{carta.dano}</b>   VIDA <b>{carta.vida}</b>   DEF <b>{carta.defesa}</b>");

        string bonus = combate.ObterBonusTemporarioParaUI(cartaObj);
        if (!string.IsNullOrEmpty(bonus))
            sb.Append($"\n<color=#75E7FF>{bonus}</color>");

        string status = MontarTextoStatus(carta);
        if (!string.IsNullOrEmpty(status))
            sb.Append($"\n{status}");

        List<string> nomes = new List<string>();
        int limite = Mathf.Min(Mathf.Clamp(carta.quantidadeHabilidades, 0, 4), carta.habilidades != null ? carta.habilidades.Count : 0);
        for (int i = 0; i < limite; i++)
        {
            HabilidadeCarta h = carta.ObterHabilidade(i);
            if (h != null && !string.IsNullOrWhiteSpace(h.nomeHabilidade))
                nomes.Add(h.nomeHabilidade);
        }
        if (nomes.Count > 0)
            sb.Append("\nHAB: " + string.Join(" • ", nomes));

        textoHover.text = sb.ToString();
        painelHover.gameObject.SetActive(true);
        PosicionarHoverPertoDaCarta(cartaObj);
    }

    public void OcultarHover()
    {
        cartaHoverAtual = null;
        if (painelHover != null)
            painelHover.gameObject.SetActive(false);
    }

    public void MostrarTextoFlutuante(GameObject cartaObj, string texto, Color cor)
    {
        if (cartaObj == null || string.IsNullOrWhiteSpace(texto))
            return;

        CriarCanvasSeNecessario();
        StartCoroutine(RotinaTextoFlutuante(cartaObj, texto, cor));
    }

    public void AnimarAtaqueDireto(GameObject atacanteObj, bool atacanteEhPlayer)
    {
        if (atacanteObj != null)
            StartCoroutine(RotinaAvancoCarta(atacanteObj, atacanteEhPlayer));
    }

    public void AnimarCartaParaCemiterio(GameObject cartaObj, Vector3 inicio, Vector3 destino, Vector3 escalaOriginal, Quaternion rotacaoOriginal)
    {
        if (cartaObj != null)
            StartCoroutine(RotinaCartaParaCemiterio(cartaObj, inicio, destino, escalaOriginal, rotacaoOriginal));
    }

    private void AtualizarVisuaisDasCartas()
    {
        HashSet<GameObject> ativas = new HashSet<GameObject>();
        AdicionarListaAtiva(combate.cartasPlayerNoTabuleiro, ativas);
        AdicionarListaAtiva(combate.cartasInimigoNoTabuleiro, ativas);

        List<GameObject> remover = new List<GameObject>();
        foreach (var par in visuais)
        {
            if (par.Key == null || !ativas.Contains(par.Key))
            {
                if (par.Value != null && par.Value.raiz != null)
                    Destroy(par.Value.raiz.gameObject);
                remover.Add(par.Key);
            }
        }
        for (int i = 0; i < remover.Count; i++)
            visuais.Remove(remover[i]);

        foreach (GameObject cartaObj in ativas)
        {
            if (cartaObj == null)
                continue;

            if (!visuais.TryGetValue(cartaObj, out VisualCarta visual) || visual == null || visual.raiz == null)
            {
                visual = CriarVisualCarta(cartaObj);
                visuais[cartaObj] = visual;
            }

            AtualizarVisualCarta(cartaObj, visual);
        }

        if (cartaHoverAtual != null && painelHover != null && painelHover.gameObject.activeSelf)
            PosicionarHoverPertoDaCarta(cartaHoverAtual);
    }

    private void AdicionarListaAtiva(List<GameObject> origem, HashSet<GameObject> destino)
    {
        if (origem == null)
            return;
        for (int i = 0; i < origem.Count; i++)
        {
            if (origem[i] != null)
                destino.Add(origem[i]);
        }
    }

    private VisualCarta CriarVisualCarta(GameObject cartaObj)
    {
        CriarCanvasSeNecessario();
        GameObject rootObj = new GameObject("Feedback_" + cartaObj.name, typeof(RectTransform));
        RectTransform root = rootObj.GetComponent<RectTransform>();
        root.SetParent(raizCanvas, false);

        VisualCarta visual = new VisualCarta();
        visual.raiz = root;

        visual.estado = CriarTexto("Estado", root, "", fonteEstadoCarta, Color.white,
            new Vector2(90f, 18f), new Vector2(0f, -42f), TextAlignmentOptions.Center);
        visual.estado.enableAutoSizing = true;
        visual.estado.fontSizeMin = 7f;
        visual.estado.fontSizeMax = fonteEstadoCarta;
        visual.estado.enableWordWrapping = false;

        visual.efeitos = CriarTexto("Efeitos", root, "", fonteEfeitos, Color.white,
            new Vector2(100f, 17f), new Vector2(0f, 42f), TextAlignmentOptions.Center);
        visual.efeitos.enableAutoSizing = true;
        visual.efeitos.fontSizeMin = 6f;
        visual.efeitos.fontSizeMax = fonteEfeitos;
        visual.efeitos.enableWordWrapping = false;

        visual.pendente = CriarTexto("Pendente", root, "", fonteEstadoCarta, new Color(1f, 0.82f, 0.25f, 1f),
            new Vector2(105f, 18f), new Vector2(0f, 29f), TextAlignmentOptions.Center);
        visual.pendente.enableAutoSizing = true;
        visual.pendente.fontSizeMin = 6f;
        visual.pendente.fontSizeMax = fonteEstadoCarta;
        visual.pendente.enableWordWrapping = false;

        visual.bordas = new Image[4];
        visual.bordas[0] = CriarImagem("BordaTopo", root, Color.clear).GetComponent<Image>();
        visual.bordas[1] = CriarImagem("BordaBaixo", root, Color.clear).GetComponent<Image>();
        visual.bordas[2] = CriarImagem("BordaEsq", root, Color.clear).GetComponent<Image>();
        visual.bordas[3] = CriarImagem("BordaDir", root, Color.clear).GetComponent<Image>();
        for (int i = 0; i < visual.bordas.Length; i++)
            visual.bordas[i].raycastTarget = false;

        return visual;
    }

    private void AtualizarVisualCarta(GameObject cartaObj, VisualCarta visual)
    {
        if (visual == null || visual.raiz == null || cameraPrincipal == null)
            return;

        SpriteRenderer sr = cartaObj.GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        Bounds b = sr.bounds;
        Vector2 minScreen = cameraPrincipal.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, b.center.z));
        Vector2 maxScreen = cameraPrincipal.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, b.center.z));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raizCanvas, minScreen, null, out Vector2 minLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raizCanvas, maxScreen, null, out Vector2 maxLocal);
        Vector2 tamanho = new Vector2(Mathf.Abs(maxLocal.x - minLocal.x), Mathf.Abs(maxLocal.y - minLocal.y));
        Vector2 centro = (minLocal + maxLocal) * 0.5f;
        visual.raiz.anchoredPosition = centro;
        visual.raiz.sizeDelta = tamanho;

        visual.estado.rectTransform.anchoredPosition = new Vector2(0f, -tamanho.y * 0.5f - 9f);
        visual.estado.rectTransform.sizeDelta = new Vector2(Mathf.Max(70f, tamanho.x + 20f), 18f);
        visual.efeitos.rectTransform.anchoredPosition = new Vector2(0f, tamanho.y * 0.5f + 9f);
        visual.efeitos.rectTransform.sizeDelta = new Vector2(Mathf.Max(70f, tamanho.x + 25f), 17f);
        visual.pendente.rectTransform.anchoredPosition = new Vector2(0f, tamanho.y * 0.5f - 5f);
        visual.pendente.rectTransform.sizeDelta = new Vector2(Mathf.Max(70f, tamanho.x + 25f), 17f);

        string estado = combate.ObterEstadoCurtoCartaParaUI(cartaObj);
        visual.estado.text = estado;
        visual.estado.color = estado == "ESPERA" ? corEspera : (estado == "PRONTA" ? corPronta : corUsada);

        Carta carta = cartaObj.GetComponent<Carta>();
        visual.efeitos.text = carta != null ? MontarTextoStatusCompacto(carta) : "";

        string pendente = combate.ObterDescricaoAtaquePendenteParaUI(cartaObj);
        visual.pendente.text = pendente;
        visual.pendente.gameObject.SetActive(!string.IsNullOrEmpty(pendente));

        bool destaque = alvosDestacados.Contains(cartaObj);
        AtualizarBordas(visual, tamanho, destaque);
    }

    private void AtualizarBordas(VisualCarta visual, Vector2 tamanho, bool ativo)
    {
        float espessura = 3f;
        float pulso = 0.65f + 0.35f * (Mathf.Sin(Time.unscaledTime * 8f) * 0.5f + 0.5f);
        Color c = ativo ? new Color(corDestaqueAtual.r, corDestaqueAtual.g, corDestaqueAtual.b, pulso) : Color.clear;

        RectTransform topo = visual.bordas[0].rectTransform;
        RectTransform baixo = visual.bordas[1].rectTransform;
        RectTransform esq = visual.bordas[2].rectTransform;
        RectTransform dir = visual.bordas[3].rectTransform;

        topo.sizeDelta = new Vector2(tamanho.x + 8f, espessura);
        baixo.sizeDelta = topo.sizeDelta;
        esq.sizeDelta = new Vector2(espessura, tamanho.y + 8f);
        dir.sizeDelta = esq.sizeDelta;
        topo.anchoredPosition = new Vector2(0f, tamanho.y * 0.5f + 3f);
        baixo.anchoredPosition = new Vector2(0f, -tamanho.y * 0.5f - 3f);
        esq.anchoredPosition = new Vector2(-tamanho.x * 0.5f - 3f, 0f);
        dir.anchoredPosition = new Vector2(tamanho.x * 0.5f + 3f, 0f);

        for (int i = 0; i < visual.bordas.Length; i++)
            visual.bordas[i].color = c;
    }

    private string MontarTextoStatusCompacto(Carta carta)
    {
        List<string> partes = new List<string>();
        if (carta.efeitoFogoAtivo)
            partes.Add($"<color=#FF8A42>F{Mathf.Max(0, carta.turnosFogoRestantes)}</color>");
        if (carta.efeitoSangramentoAtivo)
            partes.Add($"<color=#FF4D68>S{Mathf.Max(0, carta.turnosSangramentoRestantes)}</color>");
        if (carta.efeitoSobrecargaAtivo)
            partes.Add($"<color=#FFE66A>O{Mathf.Max(0, carta.turnosSobrecargaRestantes)}</color>");
        if (carta.disfarceAtivo)
            partes.Add("<color=#C58CFF>D</color>");
        return string.Join("  ", partes);
    }

    private string MontarTextoStatus(Carta carta)
    {
        List<string> partes = new List<string>();
        if (carta.efeitoFogoAtivo)
            partes.Add($"<color=#FF8A42>Fogo {carta.turnosFogoRestantes}t ({carta.danoFogoPorTurno}/t)</color>");
        if (carta.efeitoSangramentoAtivo)
            partes.Add($"<color=#FF4D68>Sang. {carta.turnosSangramentoRestantes}t ({carta.danoSangramentoPorTurno}/t)</color>");
        if (carta.efeitoSobrecargaAtivo)
            partes.Add($"<color=#FFE66A>Sobrecarga {carta.turnosSobrecargaRestantes}t</color>");
        if (carta.disfarceAtivo)
            partes.Add("<color=#C58CFF>Disfarce</color>");
        return string.Join(" • ", partes);
    }

    private void MostrarPreview(string texto)
    {
        CriarCanvasSeNecessario();
        if (painelPreview == null)
        {
            painelPreview = CriarImagem("PreviewCombate", raizCanvas, new Color(0.025f, 0.035f, 0.06f, 0.94f)).GetComponent<RectTransform>();
            painelPreview.sizeDelta = new Vector2(365f, 72f);
            painelPreview.anchorMin = new Vector2(0.5f, 1f);
            painelPreview.anchorMax = painelPreview.anchorMin;
            painelPreview.pivot = new Vector2(0.5f, 1f);
            painelPreview.anchoredPosition = new Vector2(0f, -112f);
            textoPreview = CriarTexto("Texto", painelPreview, "", fontePreview, Color.white,
                new Vector2(345f, 62f), Vector2.zero, TextAlignmentOptions.Center);
            textoPreview.enableAutoSizing = true;
            textoPreview.fontSizeMin = 10f;
            textoPreview.fontSizeMax = fontePreview;
        }
        painelPreview.gameObject.SetActive(true);
        textoPreview.text = texto;
    }

    private void CriarHoverSeNecessario()
    {
        if (painelHover != null)
            return;

        painelHover = CriarImagem("HoverCartaAuto", raizCanvas, new Color(0.025f, 0.035f, 0.06f, 0.96f)).GetComponent<RectTransform>();
        painelHover.sizeDelta = new Vector2(330f, 142f);
        textoHover = CriarTexto("Texto", painelHover, "", fonteHover, Color.white,
            new Vector2(310f, 126f), Vector2.zero, TextAlignmentOptions.TopLeft);
        textoHover.enableAutoSizing = true;
        textoHover.fontSizeMin = 9f;
        textoHover.fontSizeMax = fonteHover;
        textoHover.enableWordWrapping = true;
        painelHover.gameObject.SetActive(false);
    }

    private void PosicionarHoverPertoDaCarta(GameObject cartaObj)
    {
        if (cartaObj == null || painelHover == null || cameraPrincipal == null)
            return;

        SpriteRenderer sr = cartaObj.GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        Vector2 screen = cameraPrincipal.WorldToScreenPoint(sr.bounds.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raizCanvas, screen, null, out Vector2 local);
        float lado = local.x > 0f ? -1f : 1f;
        Vector2 pos = local + new Vector2(lado * 210f, 35f);
        pos.x = Mathf.Clamp(pos.x, -760f, 760f);
        pos.y = Mathf.Clamp(pos.y, -410f, 410f);
        painelHover.anchoredPosition = pos;
    }

    private IEnumerator RotinaTextoFlutuante(GameObject cartaObj, string texto, Color cor)
    {
        if (cartaObj == null || cameraPrincipal == null)
            yield break;

        TMP_Text tmp = CriarTexto("FeedbackFlutuante", raizCanvas, texto, 18f, cor,
            new Vector2(180f, 35f), Vector2.zero, TextAlignmentOptions.Center);
        tmp.fontStyle = FontStyles.Bold;
        RectTransform rt = tmp.rectTransform;
        Vector2 inicio = Vector2.zero;

        float t = 0f;
        const float duracao = 0.72f;
        while (t < duracao && cartaObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            SpriteRenderer sr = cartaObj.GetComponent<SpriteRenderer>();
            Vector2 screen = cameraPrincipal.WorldToScreenPoint(sr != null ? sr.bounds.center : cartaObj.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(raizCanvas, screen, null, out inicio);
            rt.anchoredPosition = inicio + new Vector2(0f, Mathf.Lerp(15f, 72f, p));
            Color c = cor;
            c.a = 1f - p;
            tmp.color = c;
            yield return null;
        }

        if (tmp != null)
            Destroy(tmp.gameObject);
    }

    private IEnumerator RotinaAvancoCarta(GameObject cartaObj, bool player)
    {
        if (cartaObj == null)
            yield break;

        Vector3 origem = cartaObj.transform.position;
        Vector3 destino = origem + new Vector3(0f, player ? 0.42f : -0.42f, 0f);
        float t = 0f;
        while (t < 0.1f && cartaObj != null)
        {
            t += Time.unscaledDeltaTime;
            cartaObj.transform.position = Vector3.Lerp(origem, destino, Mathf.Clamp01(t / 0.1f));
            yield return null;
        }
        t = 0f;
        while (t < 0.14f && cartaObj != null)
        {
            t += Time.unscaledDeltaTime;
            cartaObj.transform.position = Vector3.Lerp(destino, origem, Mathf.Clamp01(t / 0.14f));
            yield return null;
        }
        if (cartaObj != null)
            cartaObj.transform.position = origem;
    }

    private IEnumerator RotinaCartaParaCemiterio(GameObject cartaObj, Vector3 inicio, Vector3 destino,
        Vector3 escalaOriginal, Quaternion rotacaoOriginal)
    {
        if (cartaObj == null)
            yield break;

        SpriteRenderer sr = cartaObj.GetComponent<SpriteRenderer>();
        Color corInicial = sr != null ? sr.color : Color.white;
        Color corFinal = new Color(0.45f, 0.45f, 0.45f, 1f);
        float sinal = Random.value < 0.5f ? -1f : 1f;
        Quaternion rotacaoFinal = rotacaoOriginal * Quaternion.Euler(0f, 0f, 12f * sinal);

        float t = 0f;
        const float duracao = 0.38f;
        while (t < duracao && cartaObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);
            float suave = 1f - Mathf.Pow(1f - p, 3f);
            Vector3 arco = Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.28f;
            cartaObj.transform.position = Vector3.Lerp(inicio, destino, suave) + arco;
            cartaObj.transform.localScale = escalaOriginal * Mathf.Lerp(1f, 0.9f, p);
            cartaObj.transform.rotation = Quaternion.Slerp(rotacaoOriginal, rotacaoFinal, p);
            if (sr != null)
                sr.color = Color.Lerp(corInicial, corFinal, p);
            yield return null;
        }

        if (cartaObj != null)
        {
            cartaObj.transform.position = destino;
            cartaObj.transform.localScale = escalaOriginal * 0.9f;
            cartaObj.transform.rotation = rotacaoFinal;
            if (sr != null)
                sr.color = corFinal;
        }
    }

    private void CriarCanvasSeNecessario()
    {
        if (canvas != null)
            return;

        GameObject canvasObj = new GameObject("Canvas_FeedbackCartas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3300;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        raizCanvas = canvasObj.GetComponent<RectTransform>();
    }

    private RectTransform CriarImagem(string nome, Transform parent, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Image img = obj.GetComponent<Image>();
        img.color = cor;
        img.raycastTarget = false;
        return rt;
    }

    private TMP_Text CriarTexto(string nome, Transform parent, string texto, float tamanhoFonte, Color cor,
        Vector2 tamanho, Vector2 posicao, TextAlignmentOptions alinhamento)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = posicao;
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamanhoFonte;
        tmp.color = cor;
        tmp.alignment = alinhamento;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }
}
