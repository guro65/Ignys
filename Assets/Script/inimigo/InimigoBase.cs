using System.Collections.Generic;
using UnityEngine;

public enum TipoRecompensaInimigo
{
    Orbs,
    Carta,
    Pacote
}

[System.Serializable]
public class RecompensaInimigoConfigurada
{
    [Header("Tipo da recompensa")]
    public TipoRecompensaInimigo tipo = TipoRecompensaInimigo.Orbs;

    [Tooltip("Orbs: quantidade de Orbs. Carta/Pacote: quantidade de cópias.")]
    [Min(1)] public int quantidade = 1;

    [Tooltip("100 = sempre recebe. Use menos de 100 somente se quiser que esta recompensa seja aleatória.")]
    [Range(0f, 100f)] public float chanceReceber = 100f;

    [Header("Referência usada conforme o tipo")]
    public Carta carta;
    public Pacote pacote;
}

public class InimigoBase : MonoBehaviour
{
    public enum TipoDeckInimigo
    {
        Fixo,
        AleatorioPorPacote,
        Hibrido
    }

    [Header("Informações do Inimigo")]
    public string nomeInimigo;
    public TipoDeckInimigo tipoDeck = TipoDeckInimigo.Fixo;

    [Header("Vida do duelista inimigo")]
    [Min(1)] public int vidaMaximaDuelista = 30;

    [Header("Configuração Geral do Deck")]
    [Min(1)] public int quantidadeCartasNoDeck = 5;
    [Min(0)] public int quantidadeCartasReserva = 3;
    public bool permitirCartasRepetidas = true;

    [Header("Receber novas cartas durante o combate - compatibilidade")]
    [Tooltip("Mantido para outros modos/sistemas antigos. No CombateAmigavel, o Resgate agora usa a reserva pré-definida.")]
    public bool podeReceberNovasCartasDuranteCombate = false;

    [Header("Usado no modo FIXO e HÍBRIDO - Deck principal")]
    [Tooltip("No modo Fixo, as primeiras cartas preenchem o deck principal. Se Cartas Fixas Reserva estiver vazia, as cartas restantes daqui podem preencher a reserva.")]
    public List<Carta> cartasFixas = new List<Carta>();

    [Header("Usado no modo FIXO e HÍBRIDO - Reserva opcional")]
    [Tooltip("Se preenchida, esta lista é priorizada para as 3 cartas da reserva do inimigo.")]
    public List<Carta> cartasFixasReserva = new List<Carta>();

    [Header("Usado no modo ALEATÓRIO e HÍBRIDO")]
    public List<Pacote> pacotesPermitidos = new List<Pacote>();

    [Header("Recompensas configuráveis ao derrotar este inimigo")]
    [Tooltip("Adicione quantas recompensas quiser. Cada elemento pode ser Orbs, uma Carta ou um Pacote, com quantidade e chance próprias.")]
    public List<RecompensaInimigoConfigurada> recompensasAoDerrotar = new List<RecompensaInimigoConfigurada>();

    [Header("Recompensas antigas - compatibilidade")]
    [Tooltip("Só são usadas se a lista Recompensas Ao Derrotar acima estiver vazia.")]
    [Min(0)] public int orbsMinimosRecompensa = 100;
    [Min(0)] public int orbsMaximosRecompensa = 150;

    [Range(0f, 100f)] public float chanceGanharPacote = 0f;
    public List<Pacote> pacotesRecompensa = new List<Pacote>();

    [Range(0f, 100f)] public float chanceGanharCarta = 0f;
    public List<Carta> cartasRecompensa = new List<Carta>();

    [Header("Deck gerado em tempo de execução")]
    public List<Carta> deckAtual = new List<Carta>();

    [Header("Reserva gerada em tempo de execução")]
    public List<Carta> reservaAtual = new List<Carta>();

    public void GerarDeck()
    {
        deckAtual.Clear();
        reservaAtual.Clear();

        switch (tipoDeck)
        {
            case TipoDeckInimigo.Fixo:
                GerarDeckFixo();
                GerarReservaFixa();
                break;

            case TipoDeckInimigo.AleatorioPorPacote:
                PreencherListaComPacotes(deckAtual, quantidadeCartasNoDeck);
                PreencherListaComPacotes(reservaAtual, quantidadeCartasReserva);
                break;

            case TipoDeckInimigo.Hibrido:
                GerarDeckHibrido();
                GerarReservaHibrida();
                break;
        }

        Debug.Log($"Deck do inimigo {nomeInimigo} gerado com {deckAtual.Count} cartas e {reservaAtual.Count} cartas de reserva.");
    }

    public Carta SortearNovaCartaDuranteCombate()
    {
        if (!podeReceberNovasCartasDuranteCombate)
        {
            Debug.Log($"O inimigo {nomeInimigo} não pode receber cartas extras fora da reserva neste modo.");
            return null;
        }

        if (pacotesPermitidos == null || pacotesPermitidos.Count == 0)
        {
            Debug.LogWarning($"O inimigo {nomeInimigo} não possui pacotes permitidos para receber novas cartas.");
            return null;
        }

        int tentativas = 0;
        const int maxTentativas = 30;

        while (tentativas < maxTentativas)
        {
            tentativas++;
            Pacote pacoteEscolhido = EscolherPacoteAleatorio();
            if (pacoteEscolhido == null)
                continue;

            Carta novaCarta = pacoteEscolhido.SortearCartaSemCusto();
            if (novaCarta == null)
                continue;

            if (!permitirCartasRepetidas && JaExisteCartaNoDeckOuReserva(novaCarta))
                continue;

            return novaCarta;
        }

        Debug.LogWarning($"O inimigo {nomeInimigo} não conseguiu sortear uma nova carta durante o combate.");
        return null;
    }

    private void GerarDeckFixo()
    {
        if (cartasFixas == null)
            return;

        for (int i = 0; i < cartasFixas.Count && deckAtual.Count < quantidadeCartasNoDeck; i++)
        {
            Carta carta = cartasFixas[i];
            TentarAdicionarCarta(deckAtual, carta);
        }

        if (deckAtual.Count < quantidadeCartasNoDeck && pacotesPermitidos != null && pacotesPermitidos.Count > 0)
            PreencherListaComPacotes(deckAtual, quantidadeCartasNoDeck);
    }

    private void GerarReservaFixa()
    {
        if (quantidadeCartasReserva <= 0)
            return;

        if (cartasFixasReserva != null && cartasFixasReserva.Count > 0)
        {
            for (int i = 0; i < cartasFixasReserva.Count && reservaAtual.Count < quantidadeCartasReserva; i++)
                TentarAdicionarCarta(reservaAtual, cartasFixasReserva[i]);
        }
        else if (cartasFixas != null)
        {
            // Se não houver lista de reserva dedicada, usa o que sobrou depois das cartas do deck principal.
            int inicio = Mathf.Min(quantidadeCartasNoDeck, cartasFixas.Count);
            for (int i = inicio; i < cartasFixas.Count && reservaAtual.Count < quantidadeCartasReserva; i++)
                TentarAdicionarCarta(reservaAtual, cartasFixas[i]);
        }

        if (reservaAtual.Count < quantidadeCartasReserva && pacotesPermitidos != null && pacotesPermitidos.Count > 0)
            PreencherListaComPacotes(reservaAtual, quantidadeCartasReserva);

        if (reservaAtual.Count < quantidadeCartasReserva)
            Debug.LogWarning($"O inimigo {nomeInimigo} possui somente {reservaAtual.Count}/{quantidadeCartasReserva} cartas na reserva.");
    }

    private void GerarDeckHibrido()
    {
        if (cartasFixas != null)
        {
            for (int i = 0; i < cartasFixas.Count && deckAtual.Count < quantidadeCartasNoDeck; i++)
                TentarAdicionarCarta(deckAtual, cartasFixas[i]);
        }

        PreencherListaComPacotes(deckAtual, quantidadeCartasNoDeck);
    }

    private void GerarReservaHibrida()
    {
        if (quantidadeCartasReserva <= 0)
            return;

        if (cartasFixasReserva != null)
        {
            for (int i = 0; i < cartasFixasReserva.Count && reservaAtual.Count < quantidadeCartasReserva; i++)
                TentarAdicionarCarta(reservaAtual, cartasFixasReserva[i]);
        }

        PreencherListaComPacotes(reservaAtual, quantidadeCartasReserva);
    }

    private void PreencherListaComPacotes(List<Carta> destino, int quantidadeDesejada)
    {
        if (destino == null || destino.Count >= quantidadeDesejada)
            return;

        if (pacotesPermitidos == null || pacotesPermitidos.Count == 0)
            return;

        int tentativas = 0;
        int maxTentativas = Mathf.Max(30, quantidadeDesejada * 40);

        while (destino.Count < quantidadeDesejada && tentativas < maxTentativas)
        {
            tentativas++;
            Pacote pacoteEscolhido = EscolherPacoteAleatorio();
            if (pacoteEscolhido == null)
                continue;

            Carta carta = pacoteEscolhido.SortearCartaSemCusto();
            if (carta == null)
                continue;

            TentarAdicionarCarta(destino, carta);
        }
    }

    private bool TentarAdicionarCarta(List<Carta> destino, Carta carta)
    {
        if (destino == null || carta == null)
            return false;

        if (!permitirCartasRepetidas && JaExisteCartaNoDeckOuReserva(carta))
            return false;

        destino.Add(carta);
        return true;
    }

    private Pacote EscolherPacoteAleatorio()
    {
        List<Pacote> pacotesValidos = new List<Pacote>();

        if (pacotesPermitidos != null)
        {
            for (int i = 0; i < pacotesPermitidos.Count; i++)
            {
                if (pacotesPermitidos[i] != null)
                    pacotesValidos.Add(pacotesPermitidos[i]);
            }
        }

        if (pacotesValidos.Count == 0)
            return null;

        return pacotesValidos[Random.Range(0, pacotesValidos.Count)];
    }

    private bool JaExisteCartaNoDeckOuReserva(Carta carta)
    {
        if (carta == null)
            return false;

        for (int i = 0; i < deckAtual.Count; i++)
        {
            if (deckAtual[i] != null && deckAtual[i].nome == carta.nome)
                return true;
        }

        for (int i = 0; i < reservaAtual.Count; i++)
        {
            if (reservaAtual[i] != null && reservaAtual[i].nome == carta.nome)
                return true;
        }

        return false;
    }
}
