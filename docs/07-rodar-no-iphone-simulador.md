# Rodar no iPhone (Simulador iOS)

## Pelo terminal (recomendado)
```bash
./scripts/build-ios-sim.sh
# ou escolhendo o aparelho:
SIM_NAME="iPhone 16 Pro" ./scripts/build-ios-sim.sh
```
O script faz 4 passos:
1. **Unity** exporta o projeto Xcode com o **SDK do Simulador** (`Builds/iOS-Simulator`)
2. **xcodebuild** compila para `iphonesimulator` sem assinatura (`CODE_SIGNING_ALLOWED=NO`)
3. escolhe o simulador (o que você pediu, o que já estiver ligado ou o último iPhone da lista) e o liga
4. instala e abre o app `com.douglasqueiroz.ervasdobrasil`

## Pela Unity + Xcode
1. Menu **Ervas do Brasil ▸ Build ▸ iOS Simulator (projeto Xcode)**
2. Abra `Builds/iOS-Simulator/Unity-iPhone.xcodeproj` no Xcode
3. Escolha um simulador de iPhone e clique em ▶ Run

## Configurações aplicadas (`BuildTools.Configure`)
| Ajuste | Valor |
|---|---|
| SDK | Simulator SDK (arquitetura Universal/ARM64 para Apple Silicon) |
| iOS mínimo | 15.0 |
| Orientação | só paisagem (esquerda e direita) |
| Bundle id | `com.douglasqueiroz.ervasdobrasil` |

## Controles de toque
- **Lado esquerdo da tela:** arraste para andar (joystick dinâmico)
- **Botão grande à direita:** ação (mostra o texto, ex.: "Colher boldo")
- **Botões Bolsa e Livro**
- **Dentro das janelas:** arraste para rolar as listas e toque nos botões
- A interface respeita a **safe area** (notch e Dynamic Island)

## iPhone físico (depois)
Em **Ervas do Brasil ▸ Configurar Player Settings**, troque `iosSimulator` para `false` (ou chame `BuildTools.Configure(false)`), configure o Team de assinatura no Xcode e rode no aparelho.
