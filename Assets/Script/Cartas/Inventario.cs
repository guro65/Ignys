using System.Collections.Generic;
using UnityEngine;

public class Inventario : MonoBehaviour
{
    public static Inventario instancia;

    [Header("Cartas obtidas")]
    public List<Carta> cartasObtidas = new List<Carta>();

    [Header("Pacotes obtidos")]
    public List<PacoteAdquirido> pacotesObtidos = new List<PacoteAdquirido>();

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AdicionarCarta(Carta carta)
    {
        if (carta == null)
        {
            Debug.LogWarning("Tentou adicionar uma carta nula ao inventário.");
            return;
        }

        cartasObtidas.Add(carta);

        Debug.Log($"Carta adicionada ao inventário: {carta.nome} | Raridade: {carta.raridade}");
    }

    public bool PossuiCarta(string nomeCarta)
    {
        for (int i = 0; i < cartasObtidas.Count; i++)
        {
            if (cartasObtidas[i] != null && cartasObtidas[i].nome == nomeCarta)
                return true;
        }

        return false;
    }

    public int QuantidadeDeCopias(string nomeCarta)
    {
        int quantidade = 0;

        for (int i = 0; i < cartasObtidas.Count; i++)
        {
            if (cartasObtidas[i] != null && cartasObtidas[i].nome == nomeCarta)
                quantidade++;
        }

        return quantidade;
    }

    public void AdicionarPacote(PacoteAdquirido pacote)
    {
        if (pacote == null)
        {
            Debug.LogWarning("Tentou adicionar um pacote nulo ao inventário.");
            return;
        }

        pacotesObtidos.Add(pacote);
        Debug.Log($"Pacote adicionado ao inventário: {pacote.nomePacote}");
    }

    public void RemoverPacote(PacoteAdquirido pacote)
    {
        if (pacote == null)
            return;

        pacotesObtidos.Remove(pacote);
    }

    public bool PossuiPacote(string nomePacote)
    {
        for (int i = 0; i < pacotesObtidos.Count; i++)
        {
            if (pacotesObtidos[i] != null && pacotesObtidos[i].nomePacote == nomePacote)
                return true;
        }

        return false;
    }

    public int QuantidadeDePacotes(string nomePacote)
    {
        int quantidade = 0;

        for (int i = 0; i < pacotesObtidos.Count; i++)
        {
            if (pacotesObtidos[i] != null && pacotesObtidos[i].nomePacote == nomePacote)
                quantidade++;
        }

        return quantidade;
    }

    public List<Carta> AbrirPacote(PacoteAdquirido pacote)
    {
        List<Carta> cartasDoPacote = new List<Carta>();

        if (pacote == null)
        {
            Debug.LogWarning("Tentou abrir um pacote nulo.");
            return cartasDoPacote;
        }

        if (!pacotesObtidos.Contains(pacote))
        {
            Debug.LogWarning("O pacote que tentou abrir não pertence ao inventário.");
            return cartasDoPacote;
        }

        cartasDoPacote = pacote.AbrirPacote();

        if (cartasDoPacote == null || cartasDoPacote.Count == 0)
        {
            Debug.LogWarning($"O pacote {pacote.nomePacote} não conseguiu gerar cartas e não foi consumido.");
            return new List<Carta>();
        }

        for (int i = 0; i < cartasDoPacote.Count; i++)
        {
            if (cartasDoPacote[i] != null)
                AdicionarCarta(cartasDoPacote[i]);
        }

        RemoverPacote(pacote);

        Debug.Log($"Pacote {pacote.nomePacote} aberto. {cartasDoPacote.Count} cartas foram adicionadas ao inventário.");
        return cartasDoPacote;
    }
}
