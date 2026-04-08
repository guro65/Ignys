using System.Collections.Generic;
using UnityEngine;

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

    [Header("Configuração Geral do Deck")]
    [Min(1)] public int quantidadeCartasNoDeck = 5;
    public bool permitirCartasRepetidas = true;

    [Header("Usado no modo FIXO e HÍBRIDO")]
    public List<Carta> cartasFixas = new List<Carta>();

    [Header("Usado no modo ALEATÓRIO e HÍBRIDO")]
    public List<Pacote> pacotesPermitidos = new List<Pacote>();

    [Header("Deck gerado em tempo de execução")]
    public List<Carta> deckAtual = new List<Carta>();

    public void GerarDeck()
    {
        deckAtual.Clear();

        switch (tipoDeck)
        {
            case TipoDeckInimigo.Fixo:
                GerarDeckFixo();
                break;

            case TipoDeckInimigo.AleatorioPorPacote:
                GerarDeckAleatorioPorPacote();
                break;

            case TipoDeckInimigo.Hibrido:
                GerarDeckHibrido();
                break;
        }

        Debug.Log($"Deck do inimigo {nomeInimigo} gerado com {deckAtual.Count} cartas.");
    }

    private void GerarDeckFixo()
    {
        for (int i = 0; i < cartasFixas.Count; i++)
        {
            if (cartasFixas[i] != null)
            {
                if (permitirCartasRepetidas || !JaExisteCartaNoDeck(cartasFixas[i]))
                {
                    deckAtual.Add(cartasFixas[i]);
                }
            }

            if (deckAtual.Count >= quantidadeCartasNoDeck)
                break;
        }
    }

    private void GerarDeckAleatorioPorPacote()
    {
        if (pacotesPermitidos == null || pacotesPermitidos.Count == 0)
        {
            Debug.LogWarning($"O inimigo {nomeInimigo} não possui pacotes permitidos.");
            return;
        }

        int tentativas = 0;
        int maxTentativas = quantidadeCartasNoDeck * 30;

        while (deckAtual.Count < quantidadeCartasNoDeck && tentativas < maxTentativas)
        {
            tentativas++;

            Pacote pacoteEscolhido = EscolherPacoteAleatorio();
            if (pacoteEscolhido == null)
                continue;

            Carta cartaSorteada = pacoteEscolhido.SortearCartaSemCusto();
            if (cartaSorteada == null)
                continue;

            if (!permitirCartasRepetidas && JaExisteCartaNoDeck(cartaSorteada))
                continue;

            deckAtual.Add(cartaSorteada);
        }

        if (deckAtual.Count < quantidadeCartasNoDeck)
        {
            Debug.LogWarning($"O inimigo {nomeInimigo} não conseguiu completar o deck aleatório.");
        }
    }

    private void GerarDeckHibrido()
    {
        for (int i = 0; i < cartasFixas.Count; i++)
        {
            if (cartasFixas[i] != null)
            {
                if (permitirCartasRepetidas || !JaExisteCartaNoDeck(cartasFixas[i]))
                {
                    deckAtual.Add(cartasFixas[i]);
                }
            }

            if (deckAtual.Count >= quantidadeCartasNoDeck)
                return;
        }

        if (pacotesPermitidos == null || pacotesPermitidos.Count == 0)
        {
            Debug.LogWarning($"O inimigo {nomeInimigo} está em modo híbrido, mas não possui pacotes permitidos.");
            return;
        }

        int tentativas = 0;
        int maxTentativas = quantidadeCartasNoDeck * 30;

        while (deckAtual.Count < quantidadeCartasNoDeck && tentativas < maxTentativas)
        {
            tentativas++;

            Pacote pacoteEscolhido = EscolherPacoteAleatorio();
            if (pacoteEscolhido == null)
                continue;

            Carta cartaSorteada = pacoteEscolhido.SortearCartaSemCusto();
            if (cartaSorteada == null)
                continue;

            if (!permitirCartasRepetidas && JaExisteCartaNoDeck(cartaSorteada))
                continue;

            deckAtual.Add(cartaSorteada);
        }

        if (deckAtual.Count < quantidadeCartasNoDeck)
        {
            Debug.LogWarning($"O inimigo {nomeInimigo} não conseguiu completar o deck híbrido.");
        }
    }

    private Pacote EscolherPacoteAleatorio()
    {
        List<Pacote> pacotesValidos = new List<Pacote>();

        for (int i = 0; i < pacotesPermitidos.Count; i++)
        {
            if (pacotesPermitidos[i] != null)
            {
                pacotesValidos.Add(pacotesPermitidos[i]);
            }
        }

        if (pacotesValidos.Count == 0)
            return null;

        int indice = Random.Range(0, pacotesValidos.Count);
        return pacotesValidos[indice];
    }

    private bool JaExisteCartaNoDeck(Carta carta)
    {
        for (int i = 0; i < deckAtual.Count; i++)
        {
            if (deckAtual[i] != null && deckAtual[i].nome == carta.nome)
                return true;
        }

        return false;
    }
}