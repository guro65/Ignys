using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecompensaEntregueCombate
{
    public TipoRecompensaInimigo tipo;
    public int quantidade;
    public string nome;
    public Carta carta;
    public Pacote pacote;
}

[System.Serializable]
public class RecompensaCombateRecebida
{
    public int orbsRecebidos;
    public List<RecompensaEntregueCombate> recompensasEntregues = new List<RecompensaEntregueCombate>();
    public List<PacoteAdquirido> pacotesRecebidos = new List<PacoteAdquirido>();
    public List<Carta> cartasRecebidas = new List<Carta>();

    // Compatibilidade com a versão anterior.
    public PacoteAdquirido pacoteRecebido;
    public Carta cartaRecebida;

    public bool PossuiAlgumaRecompensa()
    {
        return orbsRecebidos > 0 ||
               (recompensasEntregues != null && recompensasEntregues.Count > 0) ||
               (pacotesRecebidos != null && pacotesRecebidos.Count > 0) ||
               (cartasRecebidas != null && cartasRecebidas.Count > 0);
    }
}

public static class SistemaRecompensasCombate
{
    public static RecompensaCombateRecebida EntregarRecompensas(InimigoBase inimigo)
    {
        RecompensaCombateRecebida resultado = new RecompensaCombateRecebida();
        if (inimigo == null)
            return resultado;

        if (inimigo.recompensasAoDerrotar != null && inimigo.recompensasAoDerrotar.Count > 0)
        {
            EntregarRecompensasConfiguradas(inimigo, resultado);
            return resultado;
        }

        // Compatibilidade: se o inimigo ainda não foi configurado no novo formato,
        // utiliza o sistema antigo para não quebrar inimigos já existentes.
        EntregarRecompensasLegadas(inimigo, resultado);
        return resultado;
    }

    private static void EntregarRecompensasConfiguradas(InimigoBase inimigo, RecompensaCombateRecebida resultado)
    {
        for (int i = 0; i < inimigo.recompensasAoDerrotar.Count; i++)
        {
            RecompensaInimigoConfigurada configurada = inimigo.recompensasAoDerrotar[i];
            if (configurada == null)
                continue;

            float chance = Mathf.Clamp(configurada.chanceReceber, 0f, 100f);
            if (chance <= 0f)
                continue;

            if (chance < 100f && Random.Range(0f, 100f) >= chance)
                continue;

            int quantidade = Mathf.Max(1, configurada.quantidade);

            switch (configurada.tipo)
            {
                case TipoRecompensaInimigo.Orbs:
                    EntregarOrbs(quantidade, resultado);
                    break;

                case TipoRecompensaInimigo.Carta:
                    EntregarCarta(configurada.carta, quantidade, resultado);
                    break;

                case TipoRecompensaInimigo.Pacote:
                    EntregarPacote(configurada.pacote, quantidade, resultado);
                    break;
            }
        }
    }

    private static void EntregarOrbs(int quantidade, RecompensaCombateRecebida resultado)
    {
        quantidade = Mathf.Max(0, quantidade);
        if (quantidade <= 0)
            return;

        if (Orbs.instancia != null)
            Orbs.instancia.AdicionarOrbs(quantidade);
        else
            Debug.LogWarning("Orbs não encontrado. A recompensa de Orbs não pôde ser adicionada.");

        resultado.orbsRecebidos += quantidade;
        resultado.recompensasEntregues.Add(new RecompensaEntregueCombate
        {
            tipo = TipoRecompensaInimigo.Orbs,
            quantidade = quantidade,
            nome = "Orbs"
        });
    }

    private static void EntregarCarta(Carta carta, int quantidade, RecompensaCombateRecebida resultado)
    {
        if (carta == null)
        {
            Debug.LogWarning("Uma recompensa do tipo Carta não possui carta configurada.");
            return;
        }

        if (Inventario.instancia == null)
        {
            Debug.LogWarning("Inventário não encontrado. A recompensa de carta não pôde ser adicionada.");
            return;
        }

        quantidade = Mathf.Max(1, quantidade);
        for (int i = 0; i < quantidade; i++)
        {
            Inventario.instancia.AdicionarCarta(carta);
            resultado.cartasRecebidas.Add(carta);
        }

        if (resultado.cartaRecebida == null)
            resultado.cartaRecebida = carta;

        resultado.recompensasEntregues.Add(new RecompensaEntregueCombate
        {
            tipo = TipoRecompensaInimigo.Carta,
            quantidade = quantidade,
            nome = carta.nome,
            carta = carta
        });
    }

    private static void EntregarPacote(Pacote pacote, int quantidade, RecompensaCombateRecebida resultado)
    {
        if (pacote == null)
        {
            Debug.LogWarning("Uma recompensa do tipo Pacote não possui pacote configurado.");
            return;
        }

        if (Inventario.instancia == null)
        {
            Debug.LogWarning("Inventário não encontrado. A recompensa de pacote não pôde ser adicionada.");
            return;
        }

        quantidade = Mathf.Max(1, quantidade);
        for (int i = 0; i < quantidade; i++)
        {
            PacoteAdquirido adquirido = new PacoteAdquirido(pacote, pacote.SortearPesoPacote());
            Inventario.instancia.AdicionarPacote(adquirido);
            resultado.pacotesRecebidos.Add(adquirido);

            if (resultado.pacoteRecebido == null)
                resultado.pacoteRecebido = adquirido;
        }

        resultado.recompensasEntregues.Add(new RecompensaEntregueCombate
        {
            tipo = TipoRecompensaInimigo.Pacote,
            quantidade = quantidade,
            nome = pacote.nomePacote,
            pacote = pacote
        });
    }

    private static void EntregarRecompensasLegadas(InimigoBase inimigo, RecompensaCombateRecebida resultado)
    {
        int minimo = Mathf.Max(0, inimigo.orbsMinimosRecompensa);
        int maximo = Mathf.Max(minimo, inimigo.orbsMaximosRecompensa);
        int orbs = Random.Range(minimo, maximo + 1);
        if (orbs > 0)
            EntregarOrbs(orbs, resultado);

        if (Random.Range(0f, 100f) <= Mathf.Clamp(inimigo.chanceGanharPacote, 0f, 100f))
        {
            Pacote pacote = EscolherPacoteValido(inimigo.pacotesRecompensa);
            if (pacote != null)
                EntregarPacote(pacote, 1, resultado);
        }

        if (Random.Range(0f, 100f) <= Mathf.Clamp(inimigo.chanceGanharCarta, 0f, 100f))
        {
            Carta carta = EscolherCartaValida(inimigo.cartasRecompensa);
            if (carta != null)
                EntregarCarta(carta, 1, resultado);
        }
    }

    private static Pacote EscolherPacoteValido(List<Pacote> lista)
    {
        List<Pacote> validos = new List<Pacote>();
        if (lista != null)
        {
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i] != null)
                    validos.Add(lista[i]);
            }
        }

        if (validos.Count == 0)
            return null;

        return validos[Random.Range(0, validos.Count)];
    }

    private static Carta EscolherCartaValida(List<Carta> lista)
    {
        List<Carta> validas = new List<Carta>();
        if (lista != null)
        {
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i] != null)
                    validas.Add(lista[i]);
            }
        }

        if (validas.Count == 0)
            return null;

        return validas[Random.Range(0, validas.Count)];
    }
}
