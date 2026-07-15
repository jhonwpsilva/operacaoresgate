# 🎬 Guia rápido de montagem + Roteiro do vídeo

Este arquivo te ajuda a (1) abrir o projeto rapidinho e (2) gravar o vídeo pitch de até 4 minutos.

---

## ⚡ Montagem em 6 passos

1. Instale o **Unity Hub** e a versão **Unity 2022.3.40f1** (ou qualquer 2022.3.x).
2. No Hub: **Add → Add project from disk → selecione a pasta `OperacaoResgate`**.
3. Abra o projeto (espere a importação inicial terminar).
4. Abra a cena **`Assets/Scenes/OperacaoResgate.unity`**.
5. Aperte **▶ Play**. O menu inicial aparece — clique em **INICIAR MISSÃO**.
6. Para o `.exe`: **File → Build Settings → Build**.

> Se aparecer algum erro de compilação na primeira vez, geralmente é só o Unity ainda importando. Aguarde a barra de progresso sumir e os scripts compilarem (canto inferior direito do editor).

---

## 🎥 Roteiro do vídeo pitch (até 4 minutos)

> Grave a tela com OBS, ShareX ou a própria captura do Windows (`Win + G`). Fale por cima narrando.

### ⏱️ 0:00 – 0:30 — Abertura
- Mostre o **menu inicial** com o título OPERAÇÃO RESGATE.
- Fale: *"Esse é o OPERAÇÃO RESGATE, um jogo de plataforma de ação militar em 2.5D que eu desenvolvi na Unity com C# para a disciplina de Game Development."*
- Cite a proposta: *"O jogador controla um soldado numa missão de resgate por uma zona de guerra, com 5 fases de dificuldade crescente."*

### ⏱️ 0:30 – 1:30 — Mecânicas de movimento
- Entre na **Fase 1** e demonstre ao vivo:
  - Andar e **correr** (Shift).
  - **Pular** e **duplo salto**.
  - **Agachar** e **escalar**.
- Fale: *"O personagem tem física real com Rigidbody — andar, correr, pular, duplo salto, agachar e escalar — e cada ação tem sua própria animação por troca de sprites."*

### ⏱️ 1:30 – 2:30 — Combate, itens e HUD
- Mostre **atirar** em inimigos, **coletar itens** (medkit, energia, medalha).
- Aponte a **HUD**: barra de vida, energia, pontuação, itens, mini-mapa.
- Detone um **barril explosivo** perto de um inimigo.
- Fale: *"O HUD mostra vida, energia, pontuação e um mini-mapa tático. Tem inimigos variados — soldados, drones voadores, mechs — itens de cura e energia, e perigos como fogo e barris explosivos."*

### ⏱️ 2:30 – 3:20 — Progressão e chefe final
- Mostre a transição entre fases (tela **SETOR LIBERADO**).
- Vá até a **Fase 5 – Sala de Comando** e mostre o **chefe** (use a *Seleção de Fase* no menu para ir direto, se quiser).
- Mostre a **barra de vida do chefe** e o combate.
- Fale: *"Cada fase aumenta a dificuldade, terminando na sala de comando, onde o jogador enfrenta o chefe final para desativar o núcleo inimigo e concluir o resgate."*

### ⏱️ 3:20 – 4:00 — Encerramento
- Mostre rapidamente a tela de **vitória** (MISSÃO CONCLUÍDA) e a de **derrota**.
- Fale sobre a parte técnica: *"O projeto foi todo construído por código, numa arquitetura à prova de erros: a cena começa vazia e o jogo se monta sozinho em tempo de execução. São 27 scripts em C#, com áudio original gerado proceduralmente."*
- Finalize: *"Esse foi o OPERAÇÃO RESGATE. Obrigado!"*

---

## 🗣️ Dicas de gravação
- Deixe o jogo já aberto e testado **antes** de gravar.
- Fale com calma; 4 minutos dá tempo de sobra se você for direto ao ponto.
- Se errar uma jogada, siga em frente — o importante é mostrar as funcionalidades.
- Grave o áudio num ambiente silencioso.
