# Correções — Cão + Tiro + Áudio (e o que ainda falta)

Testei junto com você e vários problemas eram **das minhas entregas** (o cão principalmente).
Corrigi os que são bem delimitados e verificáveis. Segue o que mudou e, no fim, a lista honesta
do que ainda falta — porque tentar consertar tudo de uma vez, sem eu rodar seu Unity, é
justamente o que gera os "erros" que você não quer.

## Corrigido neste pacote

**1. Cão com manchas / halo cinza**
As bordas escuras do recorte (cor do fundo do `Cão.png`) apareciam como halo cinza sobre o
cenário claro — eu tinha testado só em fundo escuro. Refiz os 12 quadros com: remoção de fundo
por flood-fill + manutenção só do maior pedaço (some as manchas soltas) + erosão de 2px na
borda (mata a franja escura). Testei sobre branco e sobre céu: limpo.

**2. Cão "andando de costas"**
Os quadros do seu sprite olham para a DIREITA (a antena/cabeça está do lado direito) — meu
código assumia esquerda e espelhava errado. Invertido. Agora ele anda pra frente.

**3. Cão grande demais**
Estava 1.65 de altura (e largo). Baixei para 1.3 — menor que o soldado (~2.1). Fica na variável
`alvoAltura` do `CompanionDog.cs` se quiser ajustar.

**4. Tiro saindo do pé, não da arma**
O cano (`PosCano`) estava fixo em `up * 1.05` (altura do quadril) e não seguia a mira. O player
tem pivô no pé e ~2.1 de altura, então o tiro saía lá embaixo. Agora:
`transform.position + up * 1.25 + _aimDir * 0.85` — sai na altura da arma e acompanha a mira
(atirando pra cima, a boca do cano sobe junto). Também baixei o limiar de mira vertical (0.4 →
0.33) pra atirar pra cima/baixo ficar mais fácil.
> Mira = MOUSE (cima/frente/baixo). Atirar = botão esquerdo ou J. Trocar arma = 1/2/3/4, Q ou
> scroll. Recarregar = R. Granada = G.

**5. Arma sem som / música alta demais**
Era balanço: baixei a música (`MusicVolume` 0.55 → 0.38) e subi os efeitos (`SfxVolume`
0.8 → 1.0) no `AudioManager`. O tiro se ouve e a música não abafa. (Se você mexeu no volume em
Opções dentro do jogo, ajuste lá também.)

**6. Música do menu** — seu `Capa_de_Menu.mp3` tocando no menu (do pacote anterior, mantido).

## Como instalar
1. Feche o Unity. 2. Copie a pasta `Assets/` por cima da sua (mescla). 3. Espere compilar antes
do Play. 4. Play.

Arquivos: `CompanionDog.cs`, `PlayerController.cs`, `AudioManager.cs`, `GameManager.cs`, os 13
PNGs do cão em `Resources/Sprites/companion/`, e `Capa_de_Menu.mp3` em `Resources/Audio/`.

> Sobre o `PlayerController.cs`: esta versão inclui também as melhorias de movimento do lote
> anterior (aceleração/desaceleração + deslize). Se você não tinha instalado, ganha de brinde.

---

## O que AINDA falta (vamos por lotes, senão vira bagunça)

É uma lista grande de coisas DIFERENTES. Dá pra fazer, mas não tudo de uma vez às cegas.
Agrupei por tipo. Me diz por onde começar que eu ataco um bloco por vez, testável.

**A) Menu (layout)** — os botões tampam o título "OPERAÇÃO"; e os painéis RECORDE/MEDALHAS e
NÍVEL/COMANDANTE ficam na capa (você quer só dentro do jogo). É reposicionar/remover no
`MainMenuController`. Rápido.

**B) Sprites com fundo / manchas (vilões, igreja, aviões, tratores, barril)** — mesmo problema
do cão: as artes têm fundo que vira mancha. Precisam do MESMO tratamento (recorte + limpeza).
Mando um lote de sprites limpos por vez.

**C) Objetos flutuando + igreja torta** — obstáculos/barril/igreja no ar e igreja inclinada. É
posicionamento no chão (Y) e rotação (billboard), no código de spawn/cenário. Um bloco só disso.

**D) Vilões que não andam** — a IA que reconstruí move os inimigos do `EnemyController`. Mas
aviões, tratores, drones e robôs são scripts SEPARADOS (`AviaoInimigo`, `RoboSentinela`, etc.)
que ainda não toquei. Preciso ver cada um.

**E) Não consigo pular/passar alguns obstáculos** — colisão/altura de objetos. Código.

**F) Telas de "SETOR LIBERADO" e HUD muito básicas** — repaginar a UI. Bloco de design.

**G) Inserir os vários vilões que você mandou** — depende do (B) + fichas + spawn. Grande, por
lotes.

**Ordem sugerida:** (1) A (menu, você vê na hora) → (2) C (parar de flutuar / endireitar igreja)
→ (3) B+G (limpar e inserir vilões, em lotes) → (4) D (mover aviões/tratores/robôs) → (5) E/F
(colisões e UI). Confirma a ordem ou fala o que mais te incomoda que eu começo por aí.
