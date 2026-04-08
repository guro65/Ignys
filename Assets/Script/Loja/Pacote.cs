using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChanceRaridade
{
    public Carta.Raridade raridade;

    [Range(0f, 100f)]
    public float chance;
}

public class Pacote : MonoBehaviour
{
    [Header("Informações do Pacote")]
    public string nomePacote;

    [Header("Cartas que podem sair neste pacote")]
    public List<Carta> cartasDisponiveis = new List<Carta>();

    [Header("Chance por raridade")]
    public List<ChanceRaridade> chancesPorRaridade = new List<ChanceRaridade>();

    [Header("Custos")]
    public int custoAbrir1 = 100;
    public int custoAbrir10 = 900;

    public void Abrir1()
    {
        if (Orbs.instancia == null)
        {
            Debug.LogError("Orbs não encontrado na cena.");
            return;
        }

        if (Inventario.instancia == null)
        {
            Debug.LogError("Inventario não encontrado na cena.");
            return;
        }

        if (!Orbs.instancia.GastarOrbs(custoAbrir1))
        {
            Debug.Log("Orbs insuficientes para abrir 1 pacote.");
            return;
        }

        Carta cartaObtida = SortearCarta();

        if (cartaObtida == null)
        {
            Debug.LogWarning("Nenhuma carta foi sorteada. Verifique a configuração do pacote.");
            return;
        }

        Inventario.instancia.AdicionarCarta(cartaObtida);

        Debug.Log($"Pacote {nomePacote} abriu 1 carta: {cartaObtida.nome} | Raridade: {cartaObtida.raridade}");
    }

    public void Abrir10()
    {
        if (Orbs.instancia == null)
        {
            Debug.LogError("Orbs não encontrado na cena.");
            return;
        }

        if (Inventario.instancia == null)
        {
            Debug.LogError("Inventario não encontrado na cena.");
            return;
        }

        if (!Orbs.instancia.GastarOrbs(custoAbrir10))
        {
            Debug.Log("Orbs insuficientes para abrir 10 pacotes.");
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            Carta cartaObtida = SortearCarta();

            if (cartaObtida != null)
            {
                Inventario.instancia.AdicionarCarta(cartaObtida);
                Debug.Log($"Carta {i + 1}: {cartaObtida.nome} | Raridade: {cartaObtida.raridade}");
            }
        }

        Debug.Log($"Pacote {nomePacote} abriu 10 cartas.");
    }

    public Carta SortearCartaSemCusto()
    {
        return SortearCarta();
    }

    public List<Carta> ObterCartasDisponiveis()
    {
        return cartasDisponiveis;
    }

    private Carta SortearCarta()
    {
        if (cartasDisponiveis == null || cartasDisponiveis.Count == 0)
        {
            Debug.LogWarning("A lista de cartas disponíveis está vazia.");
            return null;
        }

        Carta.Raridade raridadeSorteada = SortearRaridade();
        List<Carta> cartasDaRaridade = new List<Carta>();

        for (int i = 0; i < cartasDisponiveis.Count; i++)
        {
            if (cartasDisponiveis[i] != null && cartasDisponiveis[i].raridade == raridadeSorteada)
            {
                cartasDaRaridade.Add(cartasDisponiveis[i]);
            }
        }

        if (cartasDaRaridade.Count > 0)
        {
            int indice = Random.Range(0, cartasDaRaridade.Count);
            return cartasDaRaridade[indice];
        }

        Debug.LogWarning($"Não há cartas da raridade {raridadeSorteada} neste pacote. Será sorteada qualquer carta disponível.");

        List<Carta> cartasValidas = new List<Carta>();

        for (int i = 0; i < cartasDisponiveis.Count; i++)
        {
            if (cartasDisponiveis[i] != null)
            {
                cartasValidas.Add(cartasDisponiveis[i]);
            }
        }

        if (cartasValidas.Count == 0)
        {
            return null;
        }

        int fallbackIndice = Random.Range(0, cartasValidas.Count);
        return cartasValidas[fallbackIndice];
    }

    private Carta.Raridade SortearRaridade()
    {
        if (chancesPorRaridade == null || chancesPorRaridade.Count == 0)
        {
            Debug.LogWarning("Nenhuma chance por raridade foi configurada. Será usado Comum como padrão.");
            return Carta.Raridade.Comum;
        }

        float total = 0f;

        for (int i = 0; i < chancesPorRaridade.Count; i++)
        {
            total += chancesPorRaridade[i].chance;
        }

        if (total <= 0f)
        {
            Debug.LogWarning("A soma das chances está zerada. Será usado Comum como padrão.");
            return Carta.Raridade.Comum;
        }

        float valorSorteado = Random.Range(0f, total);
        float acumulado = 0f;

        for (int i = 0; i < chancesPorRaridade.Count; i++)
        {
            acumulado += chancesPorRaridade[i].chance;

            if (valorSorteado <= acumulado)
            {
                return chancesPorRaridade[i].raridade;
            }
        }

        return chancesPorRaridade[chancesPorRaridade.Count - 1].raridade;
    }
}