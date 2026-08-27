using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChanceRaridade
{
    public Carta.Raridade raridade;

    [Range(0f, 100f)]
    public float chance;
}

public enum PesoPacote
{
    Leve,
    Mediano,
    Pesado
}

[System.Serializable]
public class PacoteAdquirido
{
    [Header("Informações do pacote adquirido")]
    public string nomePacote;
    public Sprite imagemPacote;
    public PesoPacote peso;

    [Header("Cartas disponíveis neste pacote")]
    public List<Carta> cartasDisponiveis = new List<Carta>();

    [Header("Chances de raridade gravadas no momento da compra")]
    public List<ChanceRaridade> chancesRaridade = new List<ChanceRaridade>();

    [Header("Garantia do pacote pesado")]
    [Range(0f, 100f)] public float chanceGarantiaMitico = 50f;
    [Range(0f, 100f)] public float chanceGarantiaScarlet = 50f;

    public PacoteAdquirido(Pacote pacoteOrigem, PesoPacote pesoSorteado)
    {
        if (pacoteOrigem == null)
            return;

        nomePacote = pacoteOrigem.nomePacote;
        peso = pesoSorteado;

        imagemPacote = pacoteOrigem.imagemPacote;

        if (imagemPacote == null)
        {
            SpriteRenderer spriteRenderer = pacoteOrigem.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                imagemPacote = spriteRenderer.sprite;
        }

        cartasDisponiveis = new List<Carta>();
        if (pacoteOrigem.cartasDisponiveis != null)
        {
            for (int i = 0; i < pacoteOrigem.cartasDisponiveis.Count; i++)
            {
                if (pacoteOrigem.cartasDisponiveis[i] != null)
                    cartasDisponiveis.Add(pacoteOrigem.cartasDisponiveis[i]);
            }
        }

        chancesRaridade = CopiarListaChances(pacoteOrigem.ObterChancesDoPeso(pesoSorteado));

        chanceGarantiaMitico = pacoteOrigem.chanceGarantiaMiticoPesado;
        chanceGarantiaScarlet = pacoteOrigem.chanceGarantiaScarletPesado;
    }

    private List<ChanceRaridade> CopiarListaChances(List<ChanceRaridade> origem)
    {
        List<ChanceRaridade> copia = new List<ChanceRaridade>();

        if (origem == null)
            return copia;

        for (int i = 0; i < origem.Count; i++)
        {
            ChanceRaridade chanceOriginal = origem[i];
            if (chanceOriginal == null)
                continue;

            ChanceRaridade novaChance = new ChanceRaridade
            {
                raridade = chanceOriginal.raridade,
                chance = chanceOriginal.chance
            };

            copia.Add(novaChance);
        }

        return copia;
    }

    public string ObterTextoPeso()
    {
        switch (peso)
        {
            case PesoPacote.Leve:
                return "Leve: As chances de vir cartas muito raras são poucas.";

            case PesoPacote.Mediano:
                return "Mediano: Há algumas chances de poder ter alguma carta rara.";

            case PesoPacote.Pesado:
                return "Pesada: Possui uma carta rara garantida.";
        }

        return "Peso desconhecido.";
    }

    public List<Carta> AbrirPacote()
    {
        List<Carta> cartasObtidas = new List<Carta>();

        if (cartasDisponiveis == null || cartasDisponiveis.Count == 0)
        {
            Debug.LogWarning($"O pacote {nomePacote} não possui cartas disponíveis.");
            return cartasObtidas;
        }

        // Pacotes pesados possuem uma carta garantida Mítica ou Scarlet.
        if (peso == PesoPacote.Pesado)
        {
            Carta cartaGarantida = SortearCartaGarantidaPesada();

            if (cartaGarantida != null)
                cartasObtidas.Add(cartaGarantida);
        }

        // Todos os pacotes sempre entregam exatamente 10 cartas.
        while (cartasObtidas.Count < 10)
        {
            Carta cartaSorteada = SortearCartaPorChances(chancesRaridade);

            if (cartaSorteada == null)
                break;

            cartasObtidas.Add(cartaSorteada);
        }

        Embaralhar(cartasObtidas);
        return cartasObtidas;
    }

    private Carta SortearCartaGarantidaPesada()
    {
        List<Carta> cartasMiticas = ObterCartasDaRaridade(Carta.Raridade.Mitico);
        List<Carta> cartasScarlet = ObterCartasDaRaridade(Carta.Raridade.Scarlet);

        bool possuiMitico = cartasMiticas.Count > 0;
        bool possuiScarlet = cartasScarlet.Count > 0;

        if (!possuiMitico && !possuiScarlet)
        {
            Debug.LogWarning($"O pacote pesado {nomePacote} não possui cartas Míticas nem Scarlet. A garantia usará o sorteio normal.");
            return SortearCartaPorChances(chancesRaridade);
        }

        if (possuiMitico && !possuiScarlet)
            return cartasMiticas[Random.Range(0, cartasMiticas.Count)];

        if (!possuiMitico && possuiScarlet)
            return cartasScarlet[Random.Range(0, cartasScarlet.Count)];

        float chanceMitico = Mathf.Max(0f, chanceGarantiaMitico);
        float chanceScarlet = Mathf.Max(0f, chanceGarantiaScarlet);
        float total = chanceMitico + chanceScarlet;

        if (total <= 0f)
        {
            chanceMitico = 50f;
            chanceScarlet = 50f;
            total = 100f;
        }

        float sorteio = Random.Range(0f, total);

        if (sorteio <= chanceMitico)
            return cartasMiticas[Random.Range(0, cartasMiticas.Count)];

        return cartasScarlet[Random.Range(0, cartasScarlet.Count)];
    }

    private Carta SortearCartaPorChances(List<ChanceRaridade> chances)
    {
        if (cartasDisponiveis == null || cartasDisponiveis.Count == 0)
            return null;

        Carta.Raridade raridadeSorteada = SortearRaridade(chances);
        List<Carta> cartasDaRaridade = ObterCartasDaRaridade(raridadeSorteada);

        if (cartasDaRaridade.Count > 0)
            return cartasDaRaridade[Random.Range(0, cartasDaRaridade.Count)];

        // Se a raridade sorteada não existir neste pacote, evita perder uma das 10 cartas.
        List<Carta> cartasValidas = new List<Carta>();

        for (int i = 0; i < cartasDisponiveis.Count; i++)
        {
            if (cartasDisponiveis[i] != null)
                cartasValidas.Add(cartasDisponiveis[i]);
        }

        if (cartasValidas.Count == 0)
            return null;

        Debug.LogWarning($"Não há cartas da raridade {raridadeSorteada} no pacote {nomePacote}. Será usada outra carta disponível.");
        return cartasValidas[Random.Range(0, cartasValidas.Count)];
    }

    private Carta.Raridade SortearRaridade(List<ChanceRaridade> chances)
    {
        if (chances == null || chances.Count == 0)
            return Carta.Raridade.Comum;

        float total = 0f;

        for (int i = 0; i < chances.Count; i++)
        {
            if (chances[i] != null)
                total += Mathf.Max(0f, chances[i].chance);
        }

        if (total <= 0f)
            return Carta.Raridade.Comum;

        float valorSorteado = Random.Range(0f, total);
        float acumulado = 0f;

        for (int i = 0; i < chances.Count; i++)
        {
            if (chances[i] == null)
                continue;

            acumulado += Mathf.Max(0f, chances[i].chance);

            if (valorSorteado <= acumulado)
                return chances[i].raridade;
        }

        for (int i = chances.Count - 1; i >= 0; i--)
        {
            if (chances[i] != null)
                return chances[i].raridade;
        }

        return Carta.Raridade.Comum;
    }

    private List<Carta> ObterCartasDaRaridade(Carta.Raridade raridade)
    {
        List<Carta> resultado = new List<Carta>();

        if (cartasDisponiveis == null)
            return resultado;

        for (int i = 0; i < cartasDisponiveis.Count; i++)
        {
            Carta carta = cartasDisponiveis[i];

            if (carta != null && carta.raridade == raridade)
                resultado.Add(carta);
        }

        return resultado;
    }

    private void Embaralhar(List<Carta> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int indiceAleatorio = Random.Range(0, i + 1);
            Carta temporaria = lista[i];
            lista[i] = lista[indiceAleatorio];
            lista[indiceAleatorio] = temporaria;
        }
    }
}

public class Pacote : MonoBehaviour
{
    [Header("Informações do Pacote")]
    public string nomePacote;
    public Sprite imagemPacote;

    [Header("Cartas que podem sair neste pacote")]
    public List<Carta> cartasDisponiveis = new List<Carta>();

    [Header("Preço único do pacote")]
    [Min(0)] public int precoPacote = 100;

    [Header("Chance de peso ao COMPRAR o pacote")]
    [Range(0f, 100f)] public float chancePacoteLeve = 60f;
    [Range(0f, 100f)] public float chancePacoteMediano = 30f;
    [Range(0f, 100f)] public float chancePacotePesado = 10f;

    [Header("Chances de raridade - Pacote LEVE")]
    public List<ChanceRaridade> chancesRaridadeLeve = new List<ChanceRaridade>();

    [Header("Chances de raridade - Pacote MEDIANO")]
    public List<ChanceRaridade> chancesRaridadeMediano = new List<ChanceRaridade>();

    [Header("Chances de raridade - Pacote PESADO")]
    public List<ChanceRaridade> chancesRaridadePesado = new List<ChanceRaridade>();

    [Header("Garantia do pacote PESADO")]
    [Tooltip("Peso relativo para a carta garantida ser Mítica.")]
    [Range(0f, 100f)] public float chanceGarantiaMiticoPesado = 50f;

    [Tooltip("Peso relativo para a carta garantida ser Scarlet.")]
    [Range(0f, 100f)] public float chanceGarantiaScarletPesado = 50f;

    [Header("Compatibilidade - usada por inimigos e outros sistemas")]
    [Tooltip("Mantida porque InimigoBase usa SortearCartaSemCusto(). Se vazia, será usada a tabela Mediano.")]
    public List<ChanceRaridade> chancesPorRaridade = new List<ChanceRaridade>();

    public void ComprarPacote()
    {
        if (Orbs.instancia == null)
        {
            Debug.LogError("Orbs não encontrado na cena.");
            return;
        }

        if (Inventario.instancia == null)
        {
            Debug.LogError("Inventário não encontrado na cena.");
            return;
        }

        if (!Orbs.instancia.GastarOrbs(precoPacote))
        {
            Debug.Log($"Orbs insuficientes para comprar o pacote {nomePacote}.");
            return;
        }

        PesoPacote pesoSorteado = SortearPesoPacote();
        PacoteAdquirido pacoteAdquirido = new PacoteAdquirido(this, pesoSorteado);

        Inventario.instancia.AdicionarPacote(pacoteAdquirido);

        // O peso não é mostrado ao jogador aqui de propósito.
        // Ele só descobre usando o botão Pesar no inventário.
        Debug.Log($"Pacote {nomePacote} comprado e enviado ao inventário.");
    }

    public PesoPacote SortearPesoPacote()
    {
        float leve = Mathf.Max(0f, chancePacoteLeve);
        float mediano = Mathf.Max(0f, chancePacoteMediano);
        float pesado = Mathf.Max(0f, chancePacotePesado);
        float total = leve + mediano + pesado;

        if (total <= 0f)
        {
            Debug.LogWarning($"As chances de peso do pacote {nomePacote} estão zeradas. Será usado Leve.");
            return PesoPacote.Leve;
        }

        float sorteio = Random.Range(0f, total);

        if (sorteio <= leve)
            return PesoPacote.Leve;

        sorteio -= leve;

        if (sorteio <= mediano)
            return PesoPacote.Mediano;

        return PesoPacote.Pesado;
    }

    public List<ChanceRaridade> ObterChancesDoPeso(PesoPacote peso)
    {
        List<ChanceRaridade> lista = null;

        switch (peso)
        {
            case PesoPacote.Leve:
                lista = chancesRaridadeLeve;
                break;

            case PesoPacote.Mediano:
                lista = chancesRaridadeMediano;
                break;

            case PesoPacote.Pesado:
                lista = chancesRaridadePesado;
                break;
        }

        if (lista != null && lista.Count > 0)
            return lista;

        // Fallback para não quebrar pacotes antigos enquanto você configura as novas tabelas.
        if (chancesPorRaridade != null && chancesPorRaridade.Count > 0)
            return chancesPorRaridade;

        return lista ?? new List<ChanceRaridade>();
    }

    // Mantido porque InimigoBase já utiliza este método para gerar decks.
    // Ele NÃO compra nem consome um pacote do inventário.
    public Carta SortearCartaSemCusto()
    {
        List<ChanceRaridade> chancesPadrao = chancesPorRaridade;

        if (chancesPadrao == null || chancesPadrao.Count == 0)
            chancesPadrao = chancesRaridadeMediano;

        return SortearCartaComTabela(chancesPadrao);
    }

    public List<Carta> ObterCartasDisponiveis()
    {
        return cartasDisponiveis;
    }

    private Carta SortearCartaComTabela(List<ChanceRaridade> chances)
    {
        if (cartasDisponiveis == null || cartasDisponiveis.Count == 0)
        {
            Debug.LogWarning("A lista de cartas disponíveis está vazia.");
            return null;
        }

        Carta.Raridade raridadeSorteada = SortearRaridadeComTabela(chances);
        List<Carta> cartasDaRaridade = new List<Carta>();

        for (int i = 0; i < cartasDisponiveis.Count; i++)
        {
            if (cartasDisponiveis[i] != null && cartasDisponiveis[i].raridade == raridadeSorteada)
                cartasDaRaridade.Add(cartasDisponiveis[i]);
        }

        if (cartasDaRaridade.Count > 0)
            return cartasDaRaridade[Random.Range(0, cartasDaRaridade.Count)];

        List<Carta> cartasValidas = new List<Carta>();

        for (int i = 0; i < cartasDisponiveis.Count; i++)
        {
            if (cartasDisponiveis[i] != null)
                cartasValidas.Add(cartasDisponiveis[i]);
        }

        if (cartasValidas.Count == 0)
            return null;

        Debug.LogWarning($"Não há cartas da raridade {raridadeSorteada} neste pacote. Será sorteada qualquer carta disponível.");
        return cartasValidas[Random.Range(0, cartasValidas.Count)];
    }

    private Carta.Raridade SortearRaridadeComTabela(List<ChanceRaridade> chances)
    {
        if (chances == null || chances.Count == 0)
        {
            Debug.LogWarning("Nenhuma chance por raridade foi configurada. Será usado Comum como padrão.");
            return Carta.Raridade.Comum;
        }

        float total = 0f;

        for (int i = 0; i < chances.Count; i++)
        {
            if (chances[i] != null)
                total += Mathf.Max(0f, chances[i].chance);
        }

        if (total <= 0f)
        {
            Debug.LogWarning("A soma das chances está zerada. Será usado Comum como padrão.");
            return Carta.Raridade.Comum;
        }

        float valorSorteado = Random.Range(0f, total);
        float acumulado = 0f;

        for (int i = 0; i < chances.Count; i++)
        {
            if (chances[i] == null)
                continue;

            acumulado += Mathf.Max(0f, chances[i].chance);

            if (valorSorteado <= acumulado)
                return chances[i].raridade;
        }

        for (int i = chances.Count - 1; i >= 0; i--)
        {
            if (chances[i] != null)
                return chances[i].raridade;
        }

        return Carta.Raridade.Comum;
    }
}
