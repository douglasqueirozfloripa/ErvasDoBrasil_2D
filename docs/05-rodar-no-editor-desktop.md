# Rodar no editor (desktop)

O jeito mais rápido de jogar e iterar.

1. Abra o projeto pelo Unity Hub.
2. Use o menu **Ervas do Brasil ▸ Criar ou abrir cena principal**. Ele cria `Assets/Scenes/Main.unity` e a coloca no Build Settings.
3. Aperte **Play** ▶.

> Na verdade dá para dar Play em qualquer cena: o `GameManager` se cria sozinho (`RuntimeInitializeOnLoadMethod`) e monta o mundo inteiro. O jogo abre no **menu inicial** (Continuar · Novo jogo · Jogos salvos · Bastidores do desenvolvimento · Sair do jogo). No editor o **Sair do jogo** não fecha nada (a Unity ignora o `Application.Quit()`): pare o Play. No app do Mac ele fecha o jogo.

## Controles
| Tecla | Ação |
|---|---|
| WASD / setas | andar |
| E / Espaço / Enter | interagir (o prompt aparece embaixo) |
| I / Tab | bolsa |
| B / L | livro |
| H | **Histórico**: os últimos avisos da partida (subir de nível e plantas liberadas em destaque); o mesmo que o botão ao lado da Bolsa |
| Esc | fora das janelas, abre o **Menu** (salvar, jogos salvos, bastidores, menu inicial); dentro, fecha a janela |
| M | liga e desliga o som do jogo (o mesmo que o botão do alto-falante no rodapé) |
| rodapé, à direita | botão de mudo e volume do jogo (arraste o pino); só o som do jogo muda, e a escolha fica gravada |
| clique no HUD | recolhe o painel de status para cima (clique de novo para abrir) |

## Testar a interface de toque no editor
Na aba **Game**, escolha uma resolução de iPhone em paisagem (por exemplo 2556×1179). Para ver o joystick e os botões no editor, mude temporariamente `InputCompat.IsTouchDevice` para `true`. O Device Simulator da Unity (Window ▸ General ▸ Device Simulator) também funciona.

## Dicas de iteração
- **Dados:** edite `Assets/Resources/Data/*.json` e dê Play. Avisos de dados inválidos aparecem no Console com o prefixo `[Dados]`.
- **Arte:** solte um PNG em `Assets/Art/...` com o nome certo e ele aparece no jogo (ver [12-arte.md](12-arte.md)).
- **Tempo:** o dia dura 240 s (`GameManager.DayLengthSeconds`). O Menu (Esc), os jogos salvos e os Bastidores pausam o tempo; Bolsa, Livro, Caldeirão, Varal, Pilão e Feira não. Trocar de janela no Mac abre o Menu sozinho (a música continua).
- **Saves:** ficam em `Application.persistentDataPath/saves`, um JSON legível por partida. Apague a pasta para começar do zero.
- **Ciclo de vida:** cada gancho importante (`Awake`, `Start`, primeiro `Update`/`LateUpdate`/`OnGUI`, foco, pausa, `OnDestroy`, `OnApplicationQuit`) aparece no Console com o prefixo `[Ciclo de vida]` e na aba **Ao vivo** da janela Bastidores.
- **Lint:** `./scripts/lint.sh` roda com o editor aberto (não usa a pasta `Library` em modo exclusivo, ao contrário dos testes e builds).
