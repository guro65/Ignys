using System.Collections.Generic;
using UnityEngine;

public class Inventario : MonoBehaviour
{
    public static Inventario instancia;

    [Header("Cartas obtidas")]
    public List<Carta> cartasObtidas = new List<Carta>();

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
            {
                return true;
            }
        }

        return false;
    }

    public int QuantidadeDeCopias(string nomeCarta)
    {
        int quantidade = 0;

        for (int i = 0; i < cartasObtidas.Count; i++)
        {
            if (cartasObtidas[i] != null && cartasObtidas[i].nome == nomeCarta)
            {
                quantidade++;
            }
        }

        return quantidade;
    }
}