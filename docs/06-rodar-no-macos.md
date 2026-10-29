# Rodar como app do macOS

## Pelo terminal
```bash
./scripts/build-macos.sh
```
O script gera `Builds/macOS/ErvasDoBrasil.app` (Mono, **release**, janela 1280×720 redimensionável) e abre o app. O log fica em `Builds/unity-macos.log`.

Para usar o Profiler ou ver erros na tela, gere um build de desenvolvimento com `./scripts/build-macos.sh --dev`. Esse build abre a porta 55000 e anuncia o IP e o nome do Mac na rede local: use só numa rede de confiança e não distribua. Todo build passa antes pelo portão de segurança, e o release é conferido depois (`boot.config` sem `player-connection`). Para conferir o app aberto: `./scripts/check-security.sh`. Ver [Segurança](15-seguranca.md).

## Pela Unity
Menu **Ervas do Brasil ▸ Build ▸ macOS (.app)** (release).

## Detalhes
- Bundle id: `com.douglasqueiroz.ervasdobrasil`
- Ícone: a Vó benzedeira, gerada por `python3 tools/arte/icone.py`. O Mac usa `Assets/Icon/app_icon_macos.png`, que já vem no formato dos ícones do Dock (squircle de 824 px em 1024, cantos contínuos e sombra), porque o macOS não recorta o ícone; o iOS usa o quadrado `app_icon.png`. O `BuildTools.Configure` aplica os dois em todo build. Se o Dock mostrar o ícone antigo, é cache do macOS: feche o app e abra de novo (ou rode `killall Dock`).
- Backend: **Mono**, para builds rápidos. Para distribuir, troque para IL2CPP em Player Settings.
- Na primeira execução, o macOS pode pedir confirmação (app não assinado). Clique com o botão direito ▸ **Abrir**.
- Controles: os mesmos do editor (teclado e mouse). Esc abre o Menu da partida.
- Jogos salvos: `~/Library/Application Support/Douglas Queiroz/Ervas do Brasil/saves/` (um `.json` por partida).
- Para ter um atalho na mesa: Finder ▸ clique com o botão direito no `.app` ▸ **Criar Alias** e arraste para a mesa. O alias continua valendo depois de um novo build.
