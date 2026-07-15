# 🎮 Como exportar o jogo executável (.exe)

O projeto já está **100% configurado para o build** — cena registrada, entrada
configurada, tela cheia 1920×1080 e nenhum código que quebre a exportação.
É só seguir os passos abaixo na Unity (2022.3 LTS).

## Passo a passo

1. Abra o projeto no **Unity Hub** e espere compilar.
2. Menu **File → Build Settings…**
3. Confira que **`Scenes/OperacaoResgate`** aparece marcada na lista
   *Scenes In Build* (já está registrada — não precisa arrastar nada).
4. Em *Platform*, selecione **Windows, Mac, Linux** com Target Platform
   **Windows** e Architecture **Intel 64-bit**.
5. Clique em **Build**.
6. Crie uma pasta nova (ex.: `Build/OperacaoResgate`) e confirme.
7. Ao terminar, a pasta terá:

```
OperacaoResgate/
├── Operacao Resgate.exe          ← é este que se clica para jogar
├── Operacao Resgate_Data/        ← dados do jogo (obrigatória, não apagar)
├── UnityPlayer.dll               ← motor (obrigatória)
├── UnityCrashHandler64.exe
└── MonoBleedingEdge/             ← runtime C# (obrigatória)
```

8. **Teste**: dê dois cliques em `Operacao Resgate.exe`. O jogo abre em tela
   cheia direto no menu.

## Para entregar (pasta compactada, como pede o enunciado)

- Compacte a **pasta inteira** do build em .zip (botão direito → Enviar para →
  Pasta compactada). O `.exe` **não funciona sozinho** — precisa das pastas
  `_Data` e `MonoBleedingEdge` e do `UnityPlayer.dll` juntos.

## Se algo der errado

| Sintoma | Causa e solução |
|---|---|
| Build cinza / botão desabilitado | Plataforma Windows não instalada: Unity Hub → Installs → ⚙ → Add Modules → **Windows Build Support (Mono)** |
| "Scene couldn't be loaded" | A cena saiu da lista: File → Build Settings → **Add Open Scenes** com a cena `OperacaoResgate` aberta |
| Tela preta ao abrir | Espere 2–3 s (primeiro carregamento compila shaders); se persistir, rode com `-screen-fullscreen 0` para janela |
| Antivírus bloqueia o .exe | Normal em builds sem assinatura digital — adicione exceção ou use "Executar assim mesmo" |
| Sem som | Verifique o dispositivo de áudio padrão do Windows antes de abrir o jogo |

## O que já foi deixado pronto neste pacote

- ✅ Cena `Assets/Scenes/OperacaoResgate.unity` registrada no Build Settings
  com o GUID correto (estava zerado — isso faria o build abrir uma cena vazia)
- ✅ Nome do produto: **Operacao Resgate** (sem acentos — evita problemas de
  caminho no Windows) e empresa: Jonata Silva Pinho
- ✅ Input Manager clássico ativo (o jogo usa `Input.GetKey`)
- ✅ Tela cheia 1920×1080 como padrão
- ✅ Nenhum `using UnityEditor` fora de `#if UNITY_EDITOR` (isso quebraria o build)
- ✅ Bootstrap automático: o jogo se monta sozinho ao carregar a cena
