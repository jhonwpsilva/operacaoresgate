# 🪖 OPERAÇÃO RESGATE

**Jogo de plataforma de ação militar 2.5D** desenvolvido na **Unity (C#)** para a disciplina de **Game Development** do Centro Universitário **UniFECAF**.

> Um soldado heroico precisa atravessar uma zona de guerra devastada, coletar suprimentos, enfrentar inimigos e máquinas de combate, e desativar o núcleo de comando inimigo na sala de controle final para concluir o resgate.

---

## 🎮 Sobre o jogo

OPERAÇÃO RESGATE é um *side-scroller* de plataforma com visual **2.5D** (cenário e física tridimensionais com personagens em sprites), construído inteiramente em **C#**. O projeto cobre todos os pilares pedidos no trabalho:

- ✅ **Movimentação completa**: andar, correr, pular, **duplo salto**, agachar e escalar — com animações por troca de sprites.
- ✅ **5 fases** com dificuldade crescente, da praia de desembarque até a sala de comando do chefe.
- ✅ **HUD profissional**: barra de vida, barra de energia, pontuação, contador de itens, vidas, indicador de objetivo, **mini-mapa tático** e **barra de vida do chefe**.
- ✅ **Feedback visual e sonoro**: flashes de dano, explosões com luz, partículas de coleta, e trilha sonora + efeitos sonoros originais.
- ✅ **Inimigos variados** (soldado, drone voador, mech, veículo blindado) e um **chefe final** com ataques à distância.
- ✅ **Telas completas**: menu inicial, seleção de fase, como jogar, pausa, fase concluída, vitória e derrota.

---

## 🕹️ Controles

| Ação | Tecla |
|------|-------|
| Andar | `A` / `D` ou `←` / `→` |
| Correr | segurar `Shift` |
| Pular / Duplo salto | `Espaço` (aperte de novo no ar) |
| Agachar | `S` ou `↓` |
| Escalar (em escadas) | `W` / `↑` |
| Atirar | `J` ou **clique esquerdo** |
| Interagir | `E` |
| Pausar | `Esc` ou `P` |
| Confirmar nas telas | `Enter` |

---

## 🚀 Como abrir e jogar

### Pré-requisitos
- **Unity 2022.3.40f1** (LTS) — instale pelo **Unity Hub**.
  *Qualquer versão 2022.3.x abre o projeto normalmente; o Hub pode pedir para fazer um pequeno upgrade, basta aceitar.*

### Passos
1. Abra o **Unity Hub**.
2. Clique em **Add** → **Add project from disk**.
3. Selecione a pasta **`OperacaoResgate`** (esta pasta, que contém `Assets/`, `ProjectSettings/` e `Packages/`).
4. Clique no projeto para abri-lo. *(No primeiro carregamento o Unity importa os assets e gera os arquivos `.meta` — pode levar alguns minutos.)*
5. Na janela **Project**, abra a cena em **`Assets/Scenes/OperacaoResgate.unity`** (dê duplo clique).
6. Aperte o botão **▶ Play** no topo do editor.

> 💡 **O jogo se monta sozinho.** Toda a estrutura (câmera, luz, interface, fases) é criada por código em tempo de execução — não é preciso configurar nada no editor. Basta dar **Play**.

---

## 📦 Como gerar o executável (.exe)

1. Menu **File → Build Settings…**
2. Confirme que a cena **`Scenes/OperacaoResgate`** está na lista *Scenes In Build* (se não estiver, clique em **Add Open Scenes**).
3. Em **Platform**, selecione **Windows, Mac, Linux** e clique em **Switch Platform** se necessário.
4. Clique em **Build**, escolha uma pasta de saída (ex.: `Build/`) e aguarde.
5. O executável `OperacaoResgate.exe` (no Windows) será gerado na pasta escolhida, junto da pasta `OperacaoResgate_Data`.

> Para distribuição, compacte a pasta de build inteira em um `.zip`.

---

## 🗂️ Estrutura do projeto

```
OperacaoResgate/
├── Assets/
│   ├── Scenes/
│   │   └── OperacaoResgate.unity        # cena inicial (vazia — tudo é montado por código)
│   ├── Scripts/                         # 27 scripts C# (namespace OperacaoResgate)
│   │   ├── Core/                        # configuração, gerenciadores, dados de fase
│   │   │   ├── GameConfig.cs            # constantes e paleta de cores
│   │   │   ├── GameBootstrap.cs         # ponto de entrada automático (monta o jogo)
│   │   │   ├── GameManager.cs           # máquina de estados do jogo
│   │   │   ├── LevelData.cs             # definição data-driven das 5 fases
│   │   │   ├── SpriteLibrary.cs         # carregamento e cache de sprites
│   │   │   └── AudioManager.cs          # trilha e efeitos sonoros
│   │   ├── Player/                      # controle, vida e animação do soldado
│   │   ├── World/                       # construção de fase, plataformas, itens, perigos
│   │   ├── Enemies/                     # inimigos e chefe final
│   │   └── UI/                          # menus, HUD, câmera e transições
│   └── Resources/                       # assets carregados em runtime
│       ├── Sprites/  (player, enemies, props, items, hazards)
│       ├── Backgrounds/                 # cenários panorâmicos das fases e telas
│       └── Audio/                       # 12 efeitos + 3 trilhas (loop)
├── ProjectSettings/                     # versão da Unity e configurações
└── Packages/                            # dependências (uGUI, física, áudio…)
```

---

## 🧩 Arquitetura técnica

O projeto adota uma arquitetura **"à prova de erros"**: em vez de depender de objetos arrastados no editor (prefabs, referências no Inspector, Animator Controllers), **tudo é criado por código** em tempo de execução.

- **`GameBootstrap`** usa `[RuntimeInitializeOnLoadMethod]` para montar câmera, luz, *EventSystem* e gerenciadores assim que a cena carrega.
- **`GameManager`** é um *singleton* que controla a máquina de estados (Menu → Jogando → Pausado → Fase Completa → Vitória/Derrota) e os dados da partida (pontos, vidas, itens, fase atual).
- **`LevelData`** descreve as 5 fases de forma *data-driven* (posições de plataformas, inimigos, itens e perigos), e **`LevelBuilder`** as constrói no mundo 3D.
- **Animação** do personagem é feita por **troca de sprites** mapeados a partir do *sprite sheet*, sem Animator Controller.
- **Física real** com `Rigidbody` (gravidade manual para controle preciso de pulo), personagem preso ao plano XY com profundidade em Z (efeito 2.5D).

Essa abordagem garante que o jogo **funcione mesmo a partir de uma cena vazia**, eliminando a maioria dos erros de configuração.

---

## 🎨 Identidade visual

| Cor | Hex | Uso |
|-----|-----|-----|
| Azul Executivo | `#0F2D52` | fundos de interface |
| Azul Premium | `#1E5AA8` | botões e destaques |
| Dourado | `#D4A437` | título, pontuação, medalhas |
| Verde HUD | — | barra de vida cheia, sucesso |
| Vermelho Alerta | — | dano, chefe, derrota |

Os cenários, o soldado e os elementos foram processados a partir de *packs* de arte, com remoção de fundo e recorte de *sprites*.

---

## 🔊 Áudio

Todos os sons são **originais**, gerados proceduralmente (sem direitos autorais de terceiros):
- **12 efeitos**: pulo, duplo salto, moeda, item, tiro, dano, aterrissagem, explosão, clique, vitória, game over e checkpoint.
- **3 trilhas em loop**: menu, fase e batalha de chefe.

---

## 👤 Créditos

- **Desenvolvimento:** Jônata
- **Instituição:** Centro Universitário UniFECAF
- **Disciplina:** Game Development
- **Engine:** Unity 2022.3 LTS · Linguagem: C#

---

*Projeto acadêmico. Os efeitos sonoros são originais; a arte foi processada a partir de packs de imagens fornecidos para o trabalho.*
