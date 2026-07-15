# OPERAÇÃO RESGATE — CORREÇÕES (progressão, tiro, aviões, tanques)

**Esta rodada conserta o que estava travando o jogo:**

1. **"Nada avança" / caía nos vãos** → agora o **chão é contínuo** em todas as fases: você nunca cai
   num buraco mortal. As plataformas são opcionais (pra pegar brinde no alto).
2. **Pulos não alcançavam** → **pulo mais alto** (13) e duplo pulo (11). Plataformas rebaixadas.
3. **Plataformas confusas** → cada plataforma virou um **BLOCO SÓLIDO** (pedra/carro/caixa) e você
   pisa **em cima** dele (o visual bate com onde você pisa).
4. **Tiro saía torto / não pela arma** → agora o tiro sai **reto do cano** (traçador horizontal),
   com clarão e faísca. Acerto confiável em qualquer altura na linha.
5. **Não atirava pra cima nos aviões** → mira automática em **aeronaves** (helicóptero + aviões):
   olhe na direção e atire que acerta.
6. **Aviões não usavam míssil / não dava pra abater** → adicionei o **AVIÃO INIMIGO**: cruza baixo,
   **solta míssil** em você e **pode ser derrubado** (cai girando e explode).
7. **Tanques não andavam/atiravam** → **tanque mais rápido** e agora tem tanque no **meio** das fases
   (não só no fim). **Inimigos pesados (tanque, mech, exterminador, elefante) disparam MÍSSIL** com ogiva e rastro.
8. **Cão "só desliza"** → adicionei **galope** (quique + passada) quando ele corre. ⚠️ **Aviso honesto:**
   o cão é **uma imagem só** — animação de perna de verdade exige uma folha com quadros de caminhada
   (o pack não trouxe isso pro cão). O galope disfarça, mas não são pernas mexendo.

> **Não testo na Unity aqui** — validei por análise (12 scripts ok). Se a posição do cano, o galope
> ou a frequência dos aviões ainda ficarem estranhos, me diz que eu calibro os números na hora.

---

# OPERAÇÃO RESGATE — Atualização GRANDE (fases maiores, arte do pack, cão de combate, ambiente de guerra)

Esta entrega junta tudo: inimigos que **lutam**, **cão** com laser/fogo, **player maior que o cão**,
**música por fase**, **cenários nítidos**, **tiro com efeitos**, **fases maiores**, **21 inimigos com a
arte do seu pack** e **aviões + explosões** no cenário.

---

## 1. FASES MAIORES
Comprimentos aumentados (mais chão, plataformas, inimigos, brindes, perigos e checkpoints):

| Fase | Antes | Agora |
|------|-------|-------|
| 1 Favela | 64 | **108** |
| 2 Ruínas | 76 | **128** |
| 3 Campo Radioativo | 84 | **138** |
| 4 Linha de Frente | 92 | **152** |
| 5 Sala de Comando (chefe) | 40 | **58** |

## 2. INIMIGOS com a arte do pack (21 novos sprites)
Recortei e limpei os vilões das folhas do pack e liguei a cada tipo do jogo:
- **Humanos:** soldados, policiais mutantes, **lança-chamas**, **motosserra**, **garras**, brutamonte, **minigun**, granadeiro, zumbi radioativo.
- **Robôs/veículos:** exterminador (Terminator), drone, drone militar, mech, **helicóptero**, **tanque**.
- **Animais cyber:** **cobra**, **pantera**, **cachorro** (rottweiler), **elefante com torre**.
- **Props novos:** **igreja** gótica, **bomba** com timer, **avião** (caça).

> Esses arquivos ficam em `Assets/Resources/Sprites/enemies/` e `.../props/` e **substituem** os
> sprites antigos dos inimigos (mesmos nomes) — é só copiar por cima.

## 3. AMBIENTE DE GUERRA (novo) — `AmbienteGuerra.cs`
Ao fundo, sem atrapalhar o jogo: **caças cruzando o céu** (às vezes largando bomba distante) e
**explosões** com clarão e fumaça subindo. Fases com helicóptero têm mais movimento; a sala do chefe, menos.

## 4. Inimigos que lutam — `EnemyController.cs`
Ficam no chão (não flutuam), perseguem de longe, **atiradores atiram** e o corpo-a-corpo dá o bote/investida com dano. Mantêm **espaçamento** (não colam mais em você).

## 5. Cão K9 de combate — `CompanionDog.cs`
**Menor que o soldado**, **cospe fogo** de perto, **dispara laser** de longe, dá o bote, late/rosna, cura e evolui.

## 6. Áudio por fase — coloque cada faixa no lugar
Seus 4 áudios em `.ogg` em `Assets/Resources/Audio/`:

| Onde | Arquivo | Sua faixa |
|------|---------|-----------|
| Menu | `music_menu.ogg` | Capa_de_Menu |
| Fase 1 | `music_fase1.ogg` | Missão |
| Fase 2 | `music_fase2.ogg` | Missão_2 |
| Fases 3 e 4 | `music_fase3.ogg` | Missão_3 |
| Fase 5 (chefe) | `music_boss` | trilha de chefe atual |

> **Apague o `music_menu.wav` antigo** do projeto (senão a Unity fica com dois arquivos de menu).

## 7. Cenários nítidos — `ParallaxBackground.cs`
Uma pintura só, nítida, com pan suave (sem embaçar/espelhar), usando suas artes em alta:
favela tóxica, ruínas, campo radioativo, linha de frente, sala de comando + vitória/derrota.

## 8. Tiro com efeitos + pulo — `PlayerController.cs` / `GameConfig.cs` / `LevelBuilder.cs`
Clarão do cano, traçador e faísca; acerto confiável. Pulo mais alto (alcança os brindes) e plataformas com “pele” de prop (caixa/sacos/carro).

---

## Como aplicar
1. Copie os **scripts** por cima dos seus (mantendo as pastas `Assets/Scripts/...`).
2. Copie **`Assets/Resources/`** inteiro para o projeto:
   - `Audio/` (4 `.ogg`) — **e apague o `music_menu.wav` antigo**.
   - `Backgrounds/`, `Sprites/enemies/`, `Sprites/props/`, `Sprites/hazards/`, `Sprites/companion/`.
3. A Unity reimporta e recompila ao focar o editor.

## Controles
- Andar **A/D** · Correr **Shift** · Pular/Duplo **Espaço/W/↑** · Agachar **S/↓** · Atirar **J/clique/Ctrl** · Interagir **E**

## Observação
Sem Unity aqui pra rodar ao vivo — tudo validado por análise (balanço de chaves ok em 11 scripts). Se
algum número ficar estranho (tamanho de sprite, altura de pulo, densidade de inimigos, enquadramento
do fundo, frequência dos aviões/explosões), me diz que eu calibro.
