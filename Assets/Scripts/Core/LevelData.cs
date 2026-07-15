using System.Collections.Generic;
using UnityEngine;

namespace OperacaoResgate
{
    public enum TipoPlataforma { Solo, Flutuante, Movel, Quebravel, Cai, Elevatoria }

    [System.Serializable]
    public struct PlataformaDef
    {
        public Vector3 pos;
        public Vector3 tamanho;
        public TipoPlataforma tipo;
        public Vector3 movimento;
        public float velocidade;
        public PlataformaDef(float x, float y, float w, float h, TipoPlataforma t = TipoPlataforma.Solo)
        {
            pos = new Vector3(x, y, 0); tamanho = new Vector3(w, h, 3f);
            tipo = t; movimento = Vector3.zero; velocidade = 2f;
        }
    }

    [System.Serializable]
    public struct ColetavelDef
    {
        public Vector3 pos; public string tipo;
        public ColetavelDef(float x, float y, string t) { pos = new Vector3(x, y, 0); tipo = t; }
    }

    [System.Serializable]
    public struct InimigoDef
    {
        public Vector3 pos; public string tipo; public float alcance;
        public InimigoDef(float x, float y, string t, float r = 4f) { pos = new Vector3(x, y, 0); tipo = t; alcance = r; }
    }

    [System.Serializable]
    public struct HazardDef
    {
        public Vector3 pos; public string tipo;
        public HazardDef(float x, float y, string t) { pos = new Vector3(x, y, 0); tipo = t; }
    }

    [System.Serializable]
    public struct PropDef
    {
        public Vector3 pos; public string tipo; public float escala;
        public PropDef(float x, float y, string t, float s = 1f) { pos = new Vector3(x, y, 0); tipo = t; escala = s; }
    }

    public class Nivel
    {
        public string nome;
        public string objetivo = "Alcance o ponto de extracao.";
        public string background;
        public string musica;
        public Color luzCor;
        public float comprimento;
        public bool ehChefe;
        public bool temHelicoptero;
        public bool chuva;
        public bool neve;
        public float vento = 1f;
        public List<PlataformaDef> plataformas = new List<PlataformaDef>();
        public List<ColetavelDef> coletaveis = new List<ColetavelDef>();
        public List<InimigoDef> inimigos = new List<InimigoDef>();
        public List<HazardDef> hazards = new List<HazardDef>();
        public List<PropDef> props = new List<PropDef>();
        public List<float> checkpoints = new List<float>();
        public Vector3 spawnPlayer = new Vector3(-2, 2, 0);
    }

    /// <summary>
    /// Fabrica das 5 fases (agora maiores e mais movimentadas). Dificuldade crescente.
    /// </summary>
    public static class LevelData
    {
        public static int TotalNiveis => 5;

        public static Nivel Get(int index)
        {
            switch (index)
            {
                case 0: return Fase1();
                case 1: return Fase2();
                case 2: return Fase3();
                case 3: return Fase4();
                case 4: return Fase5Chefe();
                default: return Fase1();
            }
        }

        private static void ChaoContinuo(Nivel n, float xi, float xf, float y = 0f)
        {
            float passo = 8f;
            for (float x = xi; x < xf; x += passo)
                n.plataformas.Add(new PlataformaDef(x + passo / 2f, y - 1f, passo + 0.1f, 2f));
        }
        private static void P(Nivel n, float x, float y, float w, TipoPlataforma t = TipoPlataforma.Flutuante)
            => n.plataformas.Add(new PlataformaDef(x, y, w, 0.6f, t));
        private static void Mov(Nivel n, float x, float y, float w, Vector3 desl, float vel)
        {
            var m = new PlataformaDef(x, y, w, 0.6f, TipoPlataforma.Movel);
            m.movimento = desl; m.velocidade = vel; n.plataformas.Add(m);
        }
        private static void I(Nivel n, float x, string t, float r = 4f) => n.inimigos.Add(new InimigoDef(x, 0.6f, t, r));
        private static void IAr(Nivel n, float x, float y, string t, float r = 5f) => n.inimigos.Add(new InimigoDef(x, y, t, r));
        private static void C(Nivel n, float x, float y, string t) => n.coletaveis.Add(new ColetavelDef(x, y, t));
        private static void H(Nivel n, float x, string t) => n.hazards.Add(new HazardDef(x, 0.4f, t));

        // ---- novos elementos (inimigos especiais, obstaculos, plataformas especiais) ----
        private static void Rb(Nivel n, float x, float r = 5f) => n.inimigos.Add(new InimigoDef(x, 0.6f, "robo_sentinela", r));
        private static void Dr(Nivel n, float x, float y, float r = 6f) => n.inimigos.Add(new InimigoDef(x, y, "drone_inteligente", r));
        private static void Tr(Nivel n, float x) => n.inimigos.Add(new InimigoDef(x, 0.4f, "torre_vigia", 4f));
        private static void Cx(Nivel n, float x) => n.props.Add(new PropDef(x, 0.4f, "caixa", 1.1f));
        // BARREIRAS MENORES: 3.2 era mais alto que o pulo (max ~3.25) — ninguem conseguia
        // pular. Com 2.2 um pulo simples passa por cima, e a porta continua abrindo com E.
        private static void Pt(Nivel n, float x) => n.props.Add(new PropDef(x, 0.4f, "porta", 2.2f));
        private static void Pg(Nivel n, float x) => n.props.Add(new PropDef(x, 0.4f, "portao", 2.2f));
        private static void El(Nivel n, float x, float subida) => n.props.Add(new PropDef(x, 0.4f, "elevador", subida));
        private static void Cb(Nivel n, float x, string tipo) => n.props.Add(new PropDef(x, 0.3f, "cobertura_" + tipo, 1.2f));
        private static void Es(Nivel n, float x, float altura) => n.props.Add(new PropDef(x, 0.4f, "escada", altura));
        private static void Qb(Nivel n, float x, float y, float w) => n.plataformas.Add(new PlataformaDef(x, y, w, 0.6f, TipoPlataforma.Quebravel));
        private static void Cai(Nivel n, float x, float y, float w) => n.plataformas.Add(new PlataformaDef(x, y, w, 0.6f, TipoPlataforma.Cai));
        private static void Ev(Nivel n, float x, float y, float w, float alt, float vel)
        {
            var p = new PlataformaDef(x, y, w, 0.6f, TipoPlataforma.Elevatoria);
            p.movimento = new Vector3(0, alt, 0); p.velocidade = vel; n.plataformas.Add(p);
        }

        // ---------------- FASE 1 — Favela Contaminada (64 -> 108) ----------------
        private static Nivel Fase1()
        {
            var n = new Nivel {
                nome = "Favela Contaminada", objetivo = "Atravesse a favela radioativa e elimine os mutantes ate a extracao.", background = "bg_favela",
                musica = "music_fase1", luzCor = new Color(0.75f, 0.95f, 0.6f),
                comprimento = 108f, vento = 1.2f
            };
            n.checkpoints.Add(24f); n.checkpoints.Add(46f); n.checkpoints.Add(72f); n.checkpoints.Add(94f);
            ChaoContinuo(n, -6, 114);
            P(n, 16, 2.2f, 3.5f); P(n, 22, 4.0f, 3.5f); P(n, 40, 2.6f, 4f);
            P(n, 58, 2.4f, 3.5f); P(n, 66, 3.6f, 3.5f); P(n, 80, 2.6f, 4f); P(n, 94, 2.4f, 3.5f); P(n, 100, 3.8f, 3.5f);

            n.props.Add(new PropDef(6, 0.2f, "sacos_areia", 1.2f));
            n.props.Add(new PropDef(34, 0.4f, "carro", 1.3f));
            n.props.Add(new PropDef(52, 0.3f, "caixa_municao", 1f));
            n.props.Add(new PropDef(70, 0.4f, "carro", 1.3f));
            n.props.Add(new PropDef(88, 0.3f, "sacos_areia", 1.1f));

            C(n,10,1.2f,"medalha"); C(n,16,3.2f,"energia"); C(n,22,5.0f,"medkit"); C(n,30,1.2f,"medalha");
            C(n,40,3.6f,"bateria"); C(n,48,1.2f,"medalha"); C(n,58,3.4f,"energia"); C(n,66,4.6f,"medkit");
            C(n,74,1.2f,"medalha"); C(n,80,3.6f,"bateria"); C(n,94,3.4f,"medalha"); C(n,100,4.8f,"cartao"); C(n,105,1.2f,"medalha");

            I(n,20,"zumbi_radioativo"); I(n,26,"policial_mutante"); I(n,34,"macaco_mutante"); I(n,46,"policial_mutante",5f);
            I(n,50,"cachorro_cyber"); I(n,56,"zumbi_radioativo"); I(n,62,"zumbi_radioativo"); I(n,70,"policial_mutante");
            I(n,78,"macaco_mutante"); I(n,86,"cobra_cyber"); I(n,94,"cachorro_cyber"); I(n,100,"zumbi_radioativo"); I(n,104,"policial_mutante",5f);

            H(n,36,"fogo"); H(n,54,"barril"); H(n,76,"fogo"); H(n,90,"barril");
            // extras leves na 1a fase (so itens novos + 1 caixa, sem poluir)
            Cx(n,45);
            C(n,14,1.4f,"municao"); C(n,30,1.4f,"escudo"); C(n,60,1.4f,"dinheiro"); C(n,86,1.4f,"xp"); C(n,103,1.4f,"vida");
            return n;
        }

        // ---------------- FASE 2 — Ruinas Urbanas (76 -> 128) ----------------
        private static Nivel Fase2()
        {
            var n = new Nivel {
                nome = "Ruinas Urbanas", objetivo = "Avance pelas ruinas, suba as plataformas e chegue a extracao.", background = "bg_ruinas",
                musica = "music_fase2", luzCor = new Color(1f, 0.85f, 0.7f),
                comprimento = 128f, chuva = true, vento = 1.1f
            };
            n.checkpoints.Add(28f); n.checkpoints.Add(52f); n.checkpoints.Add(80f); n.checkpoints.Add(108f);
            ChaoContinuo(n, -6, 136);
            P(n,18,1.5f,4f); P(n,25,2.8f,3.5f);
            Mov(n,32,3.2f,3.5f,new Vector3(0,2.6f,0),2.2f);
            P(n,40,3.6f,3.5f);
            P(n,56,2.4f,3.5f); P(n,64,3.4f,3.2f); P(n,72,4.4f,3.2f);
            P(n,96,2.6f,3.5f); Mov(n,104,3.0f,3.2f,new Vector3(6,0,0),2.6f); P(n,116,2.6f,3.5f);

            n.props.Add(new PropDef(8, 0.4f, "caixa_municao", 1f));
            n.props.Add(new PropDef(48, 0.4f, "carro", 1.3f));
            n.props.Add(new PropDef(62, 0.3f, "sacos_areia", 1.1f));
            n.props.Add(new PropDef(90, 0.4f, "carro", 1.2f));

            C(n,18,2.6f,"bateria"); C(n,25,4.3f,"medalha"); C(n,32,6.0f,"combustivel"); C(n,40,6.1f,"medkit");
            C(n,50,1.2f,"medalha"); C(n,58,3.5f,"chave"); C(n,64,4.4f,"energia"); C(n,72,5.6f,"medkit");
            C(n,90,1.2f,"medalha"); C(n,96,3.6f,"bateria"); C(n,110,1.2f,"medalha"); C(n,116,3.6f,"cartao"); C(n,124,1.2f,"medalha");

            I(n,10,"soldado_mutante"); I(n,28,"mutante_serra"); I(n,34,"cobra_cyber"); I(n,46,"soldado_mutante",5f);
            IAr(n,52,4.5f,"drone_militar",5f); I(n,58,"pantera_cyber"); I(n,64,"mutante_garras",5f);
            I(n,86,"soldado_granada"); I(n,92,"mutante_serra"); I(n,100,"cobra_cyber"); I(n,110,"brutamonte");
            I(n,118,"soldado_mutante",5f); I(n,124,"pantera_cyber");

            H(n,12,"fogo"); H(n,60,"barril"); H(n,88,"fogo"); H(n,114,"barril");
            // extras: robo, drone, porta, plataforma quebravel, cobertura e itens
            Rb(n,44,5f); Dr(n,78,6f); Tr(n,100); Cx(n,20); Cx(n,90); Pg(n,54);
            Cb(n,62,"concreto"); Es(n,25,3.6f); Qb(n,84,3.2f,3.2f);
            C(n,20,3.0f,"municao"); C(n,54,1.4f,"escudo"); C(n,96,4.2f,"xp"); C(n,116,4.2f,"dinheiro"); C(n,122,1.4f,"vida");
            return n;
        }

        // ---------------- FASE 3 — Campo Radioativo (84 -> 138) ----------------
        private static Nivel Fase3()
        {
            var n = new Nivel {
                nome = "Campo Radioativo", objetivo = "Cuidado com o helicoptero! Sobreviva ao campo e alcance a extracao.", background = "bg_industrial",
                musica = "music_fase3", luzCor = new Color(0.7f, 0.92f, 0.55f),
                comprimento = 138f, temHelicoptero = true, chuva = true, vento = 1.4f
            };
            n.checkpoints.Add(28f); n.checkpoints.Add(56f); n.checkpoints.Add(84f); n.checkpoints.Add(112f);
            ChaoContinuo(n, -6, 144);
            P(n,20,2.4f,3f); P(n,28,3.6f,3f); P(n,36,2.4f,3f);
            Mov(n,50,2.5f,3f,new Vector3(6,0,0),2.6f);
            P(n,72,2.6f,3f); P(n,80,3.8f,3f); Mov(n,98,2.6f,3f,new Vector3(0,3.2f,0),2.4f); P(n,116,2.6f,3f); P(n,124,3.8f,3f);

            n.props.Add(new PropDef(14, 0.4f, "caixa_municao", 1f));
            n.props.Add(new PropDef(44, 0.4f, "carro", 1.2f));
            n.props.Add(new PropDef(70, 0.3f, "sacos_areia", 1.1f));
            n.props.Add(new PropDef(108, 0.4f, "carro", 1.2f));

            H(n,12,"fogo"); H(n,24,"barril"); H(n,33,"fogo"); H(n,46,"barril"); H(n,58,"fogo"); H(n,66,"barril");
            H(n,92,"fogo"); H(n,104,"barril"); H(n,120,"fogo"); H(n,132,"barril");

            C(n,20,3.4f,"energia"); C(n,28,4.7f,"medkit"); C(n,36,3.4f,"medalha"); C(n,52,3.6f,"bateria");
            C(n,62,1.2f,"medalha"); C(n,72,3.6f,"cartao"); C(n,80,4.8f,"medkit"); C(n,98,4.2f,"combustivel");
            C(n,116,3.6f,"bateria"); C(n,124,4.8f,"medalha"); C(n,134,1.2f,"cartao");

            I(n,26,"tanque",7f); I(n,30,"lanca_chamas",5f); IAr(n,40,5f,"drone_militar",6f); I(n,56,"brutamonte"); I(n,68,"soldado_granada",6f);
            I(n,46,"cachorro_cyber"); I(n,88,"lanca_chamas",5f); IAr(n,96,5f,"drone_militar",6f); I(n,106,"mutante_serra");
            I(n,110,"tanque",7f); I(n,118,"brutamonte"); I(n,128,"cobra_cyber"); I(n,134,"soldado_granada",6f);
            // extras: robos, drones, torres, elevador, plataforma que cai e itens
            Rb(n,36,5f); Rb(n,100,5f); Dr(n,60,6.5f,6f); Dr(n,116,5.5f,6f); Tr(n,44); Tr(n,120);
            El(n,72,5f); Cai(n,52,3f,3f); Cx(n,24); Cb(n,86,"sacos");
            C(n,24,3.6f,"escudo"); C(n,58,1.4f,"municao"); C(n,72,6.4f,"xp"); C(n,104,1.4f,"dinheiro"); C(n,120,3.6f,"vida");
            return n;
        }

        // ---------------- FASE 4 — Linha de Frente (92 -> 152) ----------------
        private static Nivel Fase4()
        {
            var n = new Nivel {
                nome = "Linha de Frente", objetivo = "Rompa a linha inimiga com robos e helicoptero ate a extracao.", background = "bg_rua",
                musica = "music_fase3", luzCor = new Color(1f, 0.78f, 0.6f),
                comprimento = 152f, temHelicoptero = true, vento = 1.2f
            };
            n.checkpoints.Add(30f); n.checkpoints.Add(62f); n.checkpoints.Add(96f); n.checkpoints.Add(126f);
            ChaoContinuo(n, -6, 160);
            P(n,24,2.0f,3f); Mov(n,30,2.8f,3f,new Vector3(0,2.4f,0),2.4f); P(n,38,3.6f,3f); Mov(n,46,3.4f,3f,new Vector3(7,0,0),3f);
            P(n,70,2.4f,3f); P(n,78,3.4f,3f); Mov(n,88,3.0f,3f,new Vector3(0,2.8f,0),2.6f);
            P(n,116,2.4f,3f); P(n,124,3.4f,3f); Mov(n,134,3.0f,3f,new Vector3(6,0,0),2.8f); P(n,146,2.4f,3f);

            n.props.Add(new PropDef(10, 0.4f, "carro", 1.3f));
            n.props.Add(new PropDef(60, 0.4f, "caixa_municao", 1f));
            n.props.Add(new PropDef(80, 0.3f, "sacos_areia", 1.1f));
            n.props.Add(new PropDef(112, 0.4f, "carro", 1.3f));

            H(n,14,"barril"); H(n,58,"fogo"); H(n,64,"barril"); H(n,76,"fogo"); H(n,110,"barril"); H(n,122,"fogo"); H(n,140,"barril");

            C(n,24,3.0f,"medkit"); C(n,38,5.0f,"energia"); C(n,46,5.2f,"medalha"); C(n,62,1.2f,"combustivel");
            C(n,70,3.6f,"bateria"); C(n,78,4.8f,"medkit"); C(n,88,5.4f,"cartao"); C(n,116,3.6f,"medalha");
            C(n,124,4.8f,"bateria"); C(n,134,3.6f,"combustivel"); C(n,148,1.2f,"chave");

            I(n,8,"soldado_mutante"); I(n,18,"robo_exterminador",5f); IAr(n,34,5f,"drone_militar",6f); I(n,44,"soldado_granada",5f);
            I(n,52,"pantera_cyber"); I(n,62,"lanca_chamas",5f); I(n,74,"macaco_minigun"); I(n,84,"tanque",7f); I(n,90,"robo_exterminador",5f);
            I(n,104,"brutamonte"); IAr(n,112,5f,"drone_militar",6f); I(n,122,"macaco_minigun"); I(n,132,"lanca_chamas",5f);
            I(n,142,"tanque",6f); I(n,148,"soldado_granada",5f);
            // extras: robos, torres, drones, portao, elevatoria e itens
            Rb(n,52,5f); Rb(n,120,5f); Dr(n,74,6f,6f); Dr(n,132,6f,6f); Tr(n,34); Tr(n,90); Tr(n,140);
            Pg(n,64); El(n,104,6f); Ev(n,88,3f,3f,3.5f,2.4f); Cx(n,10); Cx(n,112); Cb(n,80,"concreto");
            C(n,24,3.0f,"escudo"); C(n,62,1.4f,"municao"); C(n,88,6.0f,"xp"); C(n,124,4.8f,"dinheiro"); C(n,148,3.0f,"vida");
            return n;
        }

        // ---------------- FASE 5 — Sala de Comando (CHEFE) (40 -> 58) ----------------
        private static Nivel Fase5Chefe()
        {
            var n = new Nivel {
                nome = "Sala de Comando", objetivo = "Destrua o Comando Inimigo e conclua o resgate.", background = "bg_boss",
                musica = "music_boss", luzCor = new Color(0.7f, 0.85f, 1f),
                comprimento = 58f, ehChefe = true
            };
            n.checkpoints.Add(20f); n.checkpoints.Add(40f);
            ChaoContinuo(n, -6, 64);
            P(n,8,2.5f,3f); P(n,20,3.2f,3f); P(n,30,2.5f,3f); P(n,44,3.0f,3f);

            n.props.Add(new PropDef(2, 0.4f, "sacos_areia", 1f));
            n.props.Add(new PropDef(52, 0.4f, "caixa_municao", 1f));

            C(n,8,3.5f,"medkit"); C(n,20,4.4f,"energia"); C(n,30,3.5f,"medkit"); C(n,44,4.2f,"bateria"); C(n,19,1.2f,"energia");

            // guardas de elite protegendo a sala antes do chefe
            I(n,12,"robo_exterminador",4f); I(n,26,"elefante_cyber",4f); I(n,36,"macaco_minigun",4f); I(n,48,"robo_exterminador",4f);
            // guardas roboticas e suprimentos antes do chefe
            Rb(n,20,4f); Tr(n,32); Cb(n,16,"sacos"); Cb(n,40,"concreto");
            C(n,8,3.5f,"escudo"); C(n,20,4.4f,"municao"); C(n,44,4.2f,"vida");
            return n;
        }
    }
}
