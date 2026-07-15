using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Constroi um nivel inteiro a partir de um objeto Nivel (data-driven): chao e
    /// plataformas em 3D (inclusive moveis, quebraveis, que caem e elevatorias), cenario de
    /// fundo, CLIMA (chuva/neve/poeira/vento/som ambiente), coletaveis (com itens novos),
    /// inimigos (com robos, drones e torres inteligentes), perigos, OBSTACULOS interativos
    /// (caixas, portas, portoes, elevadores, escadas, coberturas), ponto de extracao,
    /// diretor de reforcos e, na fase final, o chefe.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public int TotalColetaveis { get; private set; }
        private Nivel _nivel;

        public void Construir(Nivel nivel)
        {
            _nivel = nivel;

            ConfigurarAmbiente(nivel);
            ConstruirFundo(nivel);
            ConstruirAmbienteGuerra(nivel);
            ConstruirClima(nivel);
            ConstruirPlataformas(nivel);
            ConstruirProps(nivel);
            ConstruirColetaveis(nivel);
            ConstruirHazards(nivel);
            ConstruirInimigos(nivel);
            ConstruirReforcos(nivel);
            ConstruirCheckpoints(nivel);
            ConstruirMeta(nivel);
        }

        private void ConstruirCheckpoints(Nivel n)
        {
            if (n.checkpoints == null || n.checkpoints.Count == 0) return;
            var holder = new GameObject("Checkpoints");
            holder.transform.SetParent(transform, false);
            foreach (float x in n.checkpoints)
            {
                var go = new GameObject("checkpoint");
                go.transform.SetParent(holder.transform, false);
                go.transform.position = new Vector3(x, 0f, 0f);
                go.AddComponent<Checkpoint>().Configurar();
            }
        }

        private void ConfigurarAmbiente(Nivel n)
        {
            var sol = GameObject.Find("Sol");
            if (sol != null)
            {
                var l = sol.GetComponent<Light>();
                if (l != null) l.color = n.luzCor;
            }
            if (n.ehChefe)
            {
                RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.36f);
                RenderSettings.fogColor = new Color(0.05f, 0.08f, 0.12f);
            }
            else
            {
                RenderSettings.ambientLight = new Color(0.45f, 0.42f, 0.4f);
            }
        }

        private void ConstruirFundo(Nivel n)
        {
            var bg = new GameObject("Fundo");
            bg.transform.SetParent(transform, false);
            bg.AddComponent<ParallaxBackground>().Configurar(n.background, n.comprimento);
        }

        private void ConstruirAmbienteGuerra(Nivel n)
        {
            float intensidade = n.ehChefe ? 0.5f : (n.temHelicoptero ? 1.5f : 1f);
            var go = new GameObject("AmbienteGuerra");
            go.transform.SetParent(transform, false);
            go.AddComponent<AmbienteGuerra>().Configurar(intensidade);
        }

        private void ConstruirClima(Nivel n)
        {
            var go = new GameObject("Clima");
            go.transform.SetParent(transform, false);
            go.AddComponent<SistemaClima>().Configurar(n.chuva, n.vento, n.neve);
        }

        private void ConstruirReforcos(Nivel n)
        {
            if (n.ehChefe) return; // chefe nao chama reforcos
            var go = new GameObject("DiretorReforcos");
            go.transform.SetParent(transform, false);
            go.AddComponent<ReinforcementDirector>().Configurar(n.temHelicoptero ? 8 : 5);
        }

        private void ConstruirPlataformas(Nivel n)
        {
            var holder = new GameObject("Plataformas");
            holder.transform.SetParent(transform, false);

            foreach (var p in n.plataformas)
            {
                if (p.tipo == TipoPlataforma.Solo)
                {
                    var chao = CriarCubo(holder.transform, p.pos, p.tamanho, CorPlataforma(p.tipo, n.ehChefe), "Solo");
                    chao.AddComponent<Platform>().ehSolo = true;
                    continue;
                }

                // bloco solido skinnable (voce pisa EM CIMA)
                var sp = PropDaPlataforma(p.tamanho.x);
                float topo = p.pos.y + p.tamanho.y * 0.5f;
                float alturaBloco = p.tamanho.y;
                if (sp != null)
                {
                    float esc = (p.tamanho.x * 0.98f) / Mathf.Max(0.01f, sp.bounds.size.x);
                    alturaBloco = Mathf.Clamp(sp.bounds.size.y * esc, 0.8f, 1.9f);
                }
                float centroY = topo - alturaBloco * 0.5f;
                Vector3 posBloco = new Vector3(p.pos.x, centroY, 0f);
                Vector3 tamBloco = new Vector3(p.tamanho.x, alturaBloco, 3f);

                string nome = NomeBloco(p.tipo);
                var go = CriarCubo(holder.transform, posBloco, tamBloco, CorPlataforma(p.tipo, n.ehChefe), nome);

                switch (p.tipo)
                {
                    case TipoPlataforma.Movel:
                        go.AddComponent<MovingPlatform>().Configurar(posBloco, p.movimento, p.velocidade);
                        break;
                    case TipoPlataforma.Elevatoria:
                        go.AddComponent<PlataformaElevatoria>().Configurar(posBloco, p.movimento.y, p.velocidade);
                        break;
                    case TipoPlataforma.Quebravel:
                        go.AddComponent<Platform>();
                        go.AddComponent<PlataformaQuebravel>().Configurar();
                        break;
                    case TipoPlataforma.Cai:
                        go.AddComponent<Platform>();
                        go.AddComponent<PlataformaQueCai>().Configurar();
                        break;
                    default:
                        go.AddComponent<Platform>();
                        break;
                }

                if (sp != null) SkinBloco(go.transform, sp);
            }
        }

        private string NomeBloco(TipoPlataforma t)
        {
            switch (t)
            {
                case TipoPlataforma.Movel:
                case TipoPlataforma.Elevatoria: return "PlataformaMovel";
                case TipoPlataforma.Quebravel:  return "PlataformaQuebravel";
                case TipoPlataforma.Cai:        return "PlataformaCai";
                default:                        return "Plataforma";
            }
        }

        private Sprite PropDaPlataforma(float largura)
        {
            string tipo = largura >= 4.2f ? "carro"
                        : largura >= 3.0f ? "sacos_areia"
                        : "caixa_municao";
            return SpriteLibrary.Get("Sprites/props/" + tipo, 180f);
        }

        private void SkinBloco(Transform bloco, Sprite sp)
        {
            var go = new GameObject("skin");
            go.transform.SetParent(bloco, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp; sr.sortingOrder = 8;

            Vector3 pe = bloco.localScale;
            float esc = (pe.x * 0.98f) / Mathf.Max(0.01f, sp.bounds.size.x);
            go.transform.localScale = new Vector3(esc / Mathf.Max(0.01f, pe.x),
                                                  esc / Mathf.Max(0.01f, pe.y), 1f);
            go.transform.localPosition = new Vector3(0, 0, -0.25f);
            go.AddComponent<Billboard>();
        }

        private GameObject CriarCubo(Transform pai, Vector3 pos, Vector3 escala, Color cor, string nome)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nome;
            go.transform.SetParent(pai, false);
            go.transform.position = pos;
            go.transform.localScale = escala;
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = MaterialUtil.Cor(cor, 0.05f, 0.15f);
            return go;
        }

        private Color CorPlataforma(TipoPlataforma t, bool chefe)
        {
            if (chefe)
                return (t == TipoPlataforma.Movel || t == TipoPlataforma.Elevatoria)
                       ? new Color(0.2f, 0.3f, 0.42f) : new Color(0.16f, 0.18f, 0.24f);
            switch (t)
            {
                case TipoPlataforma.Solo:       return new Color(0.30f, 0.26f, 0.20f);
                case TipoPlataforma.Flutuante:  return new Color(0.24f, 0.21f, 0.17f);
                case TipoPlataforma.Movel:
                case TipoPlataforma.Elevatoria: return GameConfig.AzulPremium;
                case TipoPlataforma.Quebravel:  return new Color(0.40f, 0.30f, 0.20f);
                case TipoPlataforma.Cai:        return new Color(0.35f, 0.28f, 0.22f);
                default: return Color.gray;
            }
        }

        private void ConstruirProps(Nivel n)
        {
            var holder = new GameObject("Props");
            holder.transform.SetParent(transform, false);
            foreach (var pr in n.props)
            {
                switch (pr.tipo)
                {
                    case "caixa":    CriarCaixa(holder.transform, pr); break;
                    case "porta":    CriarPorta(holder.transform, pr, false); break;
                    case "portao":   CriarPorta(holder.transform, pr, true); break;
                    case "elevador": CriarElevador(holder.transform, pr); break;
                    case "escada":   CriarEscada(holder.transform, pr); break;
                    default:
                        if (pr.tipo.StartsWith("cobertura_")) CriarCobertura(holder.transform, pr);
                        else CriarDecoracao(holder.transform, pr);
                        break;
                }
            }
        }

        private void CriarDecoracao(Transform pai, PropDef pr)
        {
            var sp = SpriteLibrary.Get("Sprites/props/" + pr.tipo, 180f);
            if (sp == null) return;
            var go = new GameObject("prop_" + pr.tipo);
            go.transform.SetParent(pai, false);
            go.transform.position = pr.pos;
            go.AddComponent<Decoration>().Configurar(sp, pr.escala, false);
        }

        private void CriarCaixa(Transform pai, PropDef pr)
        {
            var sp = SpriteLibrary.Get("Sprites/props/caixa_municao", 180f);
            var go = new GameObject("caixa");
            go.transform.SetParent(pai, false);
            go.transform.position = pr.pos;
            go.AddComponent<CaixaEmpurravel>().Configurar(sp, pr.escala);
        }

        private void CriarPorta(Transform pai, PropDef pr, bool portao)
        {
            var go = new GameObject(portao ? "portao" : "porta");
            go.transform.SetParent(pai, false);
            go.transform.position = pr.pos;
            go.AddComponent<Porta>().Configurar(1.4f, pr.escala, portao);
        }

        private void CriarElevador(Transform pai, PropDef pr)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "elevador";
            cube.transform.SetParent(pai, false);
            cube.transform.position = new Vector3(pr.pos.x, 0.5f, 0);
            cube.transform.localScale = new Vector3(2.6f, 0.5f, 3f);
            cube.GetComponent<MeshRenderer>().material = MaterialUtil.Cor(new Color(0.22f, 0.35f, 0.5f), 0.4f, 0.5f);
            cube.AddComponent<Elevador>().Configurar(2.6f, pr.escala, 2.2f);
        }

        private void CriarCobertura(Transform pai, PropDef pr)
        {
            string sub = pr.tipo.Substring("cobertura_".Length);
            if (sub == "concreto")
            {
                // bloco de concreto solido e visivel — BAIXO o bastante para pular por cima
                var bloco = CriarCubo(pai, new Vector3(pr.pos.x, 0.5f, 0), new Vector3(1.2f, 1.0f, 1.2f),
                                      new Color(0.35f, 0.35f, 0.38f), "Concreto");
                bloco.AddComponent<Platform>();
                return;
            }
            string arquivo = sub == "sacos" ? "sacos_areia" : (sub == "cerca" ? "cerca" : "sacos_areia");
            var sp = SpriteLibrary.Get("Sprites/props/" + arquivo, 180f);
            var go = new GameObject("cobertura_" + sub);
            go.transform.SetParent(pai, false);
            go.transform.position = pr.pos;
            go.AddComponent<CoberturaSolida>().Configurar(sp, 1.3f, 0.8f);
        }

        private void CriarEscada(Transform pai, PropDef pr)
        {
            float altura = pr.escala;
            var go = new GameObject("escada");
            go.transform.SetParent(pai, false);
            go.transform.position = pr.pos;

            // trilhos + degraus (cubos finos)
            Color cor = new Color(0.55f, 0.42f, 0.25f);
            for (int i = -1; i <= 1; i += 2)
            {
                var trilho = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trilho.transform.SetParent(go.transform, false);
                trilho.transform.localScale = new Vector3(0.1f, altura, 0.1f);
                trilho.transform.localPosition = new Vector3(i * 0.35f, altura * 0.5f, 0);
                var c = trilho.GetComponent<Collider>(); if (c != null) Destroy(c);
                trilho.GetComponent<MeshRenderer>().material = MaterialUtil.Cor(cor, 0.3f, 0.3f);
            }
            int degraus = Mathf.Max(3, Mathf.RoundToInt(altura / 0.5f));
            for (int d = 0; d < degraus; d++)
            {
                var deg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deg.transform.SetParent(go.transform, false);
                deg.transform.localScale = new Vector3(0.8f, 0.08f, 0.1f);
                deg.transform.localPosition = new Vector3(0, (d + 0.5f) * (altura / degraus), 0);
                var c = deg.GetComponent<Collider>(); if (c != null) Destroy(c);
                deg.GetComponent<MeshRenderer>().material = MaterialUtil.Cor(cor, 0.3f, 0.3f);
            }

            go.AddComponent<Climbable>().Configurar(0.9f, altura);
        }

        private void ConstruirColetaveis(Nivel n)
        {
            var holder = new GameObject("Coletaveis");
            holder.transform.SetParent(transform, false);
            int total = 0;
            foreach (var c in n.coletaveis)
            {
                var sp = ProceduralSprites.ItemIcon(c.tipo);
                if (sp == null) continue;
                var go = new GameObject("item_" + c.tipo);
                go.transform.SetParent(holder.transform, false);
                go.transform.position = c.pos;
                go.AddComponent<Collectible>().Configurar(c.tipo, sp);
                total++;
            }
            TotalColetaveis = total;
        }

        private void ConstruirHazards(Nivel n)
        {
            var holder = new GameObject("Perigos");
            holder.transform.SetParent(transform, false);
            foreach (var h in n.hazards)
            {
                if (h.tipo == "barril")
                {
                    var sp = SpriteLibrary.Get("Sprites/hazards/barril", 200f);
                    var go = new GameObject("barril");
                    go.transform.SetParent(holder.transform, false);
                    go.transform.position = h.pos;
                    go.AddComponent<ExplosiveBarrel>().Configurar(sp);
                }
                else
                {
                    var sp = SpriteLibrary.Get("Sprites/hazards/fogo", 200f);
                    var go = new GameObject("fogo");
                    go.transform.SetParent(holder.transform, false);
                    go.transform.position = h.pos;
                    go.AddComponent<Hazard>().Configurar(sp, 1.5f);
                }
            }
        }

        private void ConstruirInimigos(Nivel n)
        {
            var holder = new GameObject("Inimigos");
            holder.transform.SetParent(transform, false);
            foreach (var i in n.inimigos)
            {
                // inimigos inteligentes especiais
                if (i.tipo == "robo_sentinela")
                {
                    var sp = SpriteLibrary.Get("Sprites/enemies/robo_exterminador", 200f);
                    var go = new GameObject("robo_sentinela");
                    go.transform.SetParent(holder.transform, false);
                    go.transform.position = i.pos;
                    go.AddComponent<RoboSentinela>().Configurar(sp, i.pos, i.alcance);
                    continue;
                }
                if (i.tipo == "drone_inteligente")
                {
                    var sp = SpriteLibrary.Get("Sprites/enemies/drone_militar", 200f);
                    var go = new GameObject("drone_inteligente");
                    go.transform.SetParent(holder.transform, false);
                    go.transform.position = i.pos;
                    go.AddComponent<DroneInteligente>().Configurar(sp, i.pos, i.alcance);
                    continue;
                }
                if (i.tipo == "torre_vigia")
                {
                    var sp = SpriteLibrary.Get("Sprites/props/turret", 200f);
                    var go = new GameObject("torre_vigia");
                    go.transform.SetParent(holder.transform, false);
                    go.transform.position = i.pos;
                    go.AddComponent<TorreVigia>().Configurar(sp);
                    continue;
                }

                var spr = SpriteLibrary.Get("Sprites/enemies/" + i.tipo, 200f);
                if (spr == null) continue;
                var e = new GameObject("inimigo_" + i.tipo);
                e.transform.SetParent(holder.transform, false);
                e.transform.position = i.pos;
                e.AddComponent<EnemyController>().Configurar(i.tipo, spr, i.alcance, i.pos);
            }

            if (n.ehChefe)
            {
                var bossSprite = SpriteLibrary.Get("Sprites/enemies/mech", 200f);
                var go = new GameObject("CHEFE");
                go.transform.SetParent(holder.transform, false);
                go.transform.position = new Vector3(n.comprimento - 6f, 1.6f, 0);
                go.AddComponent<BossController>().Configurar(bossSprite);
            }

            if (n.temHelicoptero)
            {
                var heliSprite = SpriteLibrary.Get("Sprites/enemies/helicoptero", 200f);
                if (heliSprite != null)
                {
                    var go = new GameObject("Helicoptero");
                    go.transform.SetParent(holder.transform, false);
                    go.transform.position = new Vector3(n.comprimento * 0.5f, 8.5f, 0);
                    go.AddComponent<HelicopterEnemy>().Configurar(heliSprite);
                }
            }
        }

        private void ConstruirMeta(Nivel n)
        {
            if (n.ehChefe) return;
            var go = new GameObject("Extracao");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(n.comprimento, 0.5f, 0);
            go.AddComponent<LevelGoal>().Configurar();
        }
    }
}
