using System.Collections.Generic;
using UnityEngine;

public class ControladorInimigoNaCena : MonoBehaviour
{
    [Header("Inimigo carregado")]
    public InimigoBase inimigoAtual;

    [Header("Deck do inimigo nesta batalha")]
    public List<Carta> deckInimigo = new List<Carta>();

    [Header("Reserva do inimigo nesta batalha")]
    public List<Carta> reservaInimigo = new List<Carta>();

    public bool deckFoiCarregado = false;

    private void Awake()
    {
        CarregarInimigoSelecionado();
    }

    public void CarregarInimigoSelecionado()
    {
        deckFoiCarregado = false;

        if (GerenciadorInimigo.instancia == null)
        {
            Debug.LogError("GerenciadorInimigo não encontrado.");
            return;
        }

        if (GerenciadorInimigo.instancia.inimigoSelecionado == null)
        {
            Debug.LogError("Nenhum inimigo foi selecionado antes de entrar na cena.");
            return;
        }

        inimigoAtual = GerenciadorInimigo.instancia.inimigoSelecionado;
        if (inimigoAtual == null)
        {
            Debug.LogError("Falha ao carregar o inimigo atual.");
            return;
        }

        inimigoAtual.GerarDeck();

        deckInimigo.Clear();
        reservaInimigo.Clear();

        for (int i = 0; i < inimigoAtual.deckAtual.Count; i++)
        {
            if (inimigoAtual.deckAtual[i] != null)
                deckInimigo.Add(inimigoAtual.deckAtual[i]);
        }

        for (int i = 0; i < inimigoAtual.reservaAtual.Count; i++)
        {
            if (inimigoAtual.reservaAtual[i] != null)
                reservaInimigo.Add(inimigoAtual.reservaAtual[i]);
        }

        deckFoiCarregado = true;
        Debug.Log($"Inimigo na cena: {inimigoAtual.nomeInimigo} | Deck: {deckInimigo.Count} | Reserva: {reservaInimigo.Count}");
    }
}
